using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using stockDataImporter.Logic.ImportStockData;
using stockDataImporter.Logic.Messaging;
using stockDataImporter.Logic.Scheduler;
using stockDataImporter.Logic;
using System.Configuration;
using System.Reflection;
using Org.BouncyCastle.Crypto.Prng;

namespace stockDataImporter
{
	internal class Program
	{
		private const string MutexName = @"Global\stockDataImporter";
		private const int MaxRetryCount = 3;
		private const int RetryIntervalMs = 5000;
		private static IStockCsvDownloaderService? _stockCsvDownloader;
		private static IFtpsClientService? _ftpsClientService;
		private static IEmailService? _emailService;
		private static ISchedulerService? _schedulerService;
		static async Task Init()
		{
			// 文字コード (Shift_JISなど) を扱うための準備
			Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

			// 各種設定の読込(appsettings.json)
			IConfigurationRoot config = new ConfigurationBuilder()
				.SetBasePath(AppContext.BaseDirectory)
				.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
				.Build();

			// 自動実行ツールパスの取得(固定の相対パス)
			string relativePath = config["ExternalApps:OhkenHBRelationKickerExePath"]!;

			// 自動実行ツールの絶対パスの取得
			string exePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), relativePath);

			// OhkenAPIサービスの初期化
			var djnApi = new OhkenApiService(exePath);

			// DB接続情報の取得
			var connString = config.GetConnectionString("DefaultConnection")
			?? throw new InvalidOperationException("DB接続情報が取得できません。");

			// CSVファイル取込順序設定の取得
			var pathSettings = config.GetSection("ImportPathSettings").Get<ImportPathSettings>();

			//	マスタ取込パス設定の取得
			var mstPathSettings = config.GetSection("ImportMSTPathSettings").Get<ImportMSTPathSettings>();

			// メール設定情報の取得
			var mailConfig = config.GetSection("EdiMailConfig").Get<EmailConfig>();

			// 在庫データ出力設定の取得
			var StockExportSettings = new StockExportSettings
			{
				StockEcDataExportSettings = config.GetSection("StockEcDataExportSettings").Get<ExportConfig>() ?? new ExportConfig(),
				CustomerStockDataExportSettings = config.GetSection("CustomerStockDataExportSettings").Get<ExportConfig>() ?? new ExportConfig()
			};

			//	CSVファイルコピー先情報の設定
			var copyStockDataSettings = config.GetSection("CopyStockDataSettings").Get<CopyStockDataSettings>()
		?? new CopyStockDataSettings();

			// FTPS接続情報の取得
			var FtpsConnectionInfo = config.GetSection("FtpsConnectionInfo").Get<FtpsConnectionInfo>();

			// サービス初期化
			var ohkenApiService = new OhkenApiService(exePath);
			_emailService = new EmailService(mailConfig!);
			var dataLoader = new MySqlDataLoaderService(connString!, pathSettings!, _emailService!);
			var masterImportService = new MasterImportService(connString!, mstPathSettings!, _emailService!);
			_stockCsvDownloader = new StockCsvDownload(connString!, StockExportSettings!, _emailService, copyStockDataSettings!);
			_ftpsClientService = new FtpsClientService(FtpsConnectionInfo!, _emailService!);
			// スケジューラサービスの初期化・開始
			_schedulerService = new SchedulerService(
				ohkenApiService,
				_emailService,
				_stockCsvDownloader,
				dataLoader,
				masterImportService,
				_ftpsClientService);
		}
		/// <summary>
		/// リトライ監視用ルーチン
		/// </summary>
		/// <param name="action">顧客向け在庫CSV作成ルーチン</param>
		/// <returns></returns>
		static async Task RetryExec(Func<Task> action)
		{
			int retryAttempt = 0;
			while (true)
			{
				try
				{
					await action();
					break;
				}
				catch (Exception ex)
				{
					retryAttempt++;
					Console.WriteLine($"エラーが発生しました (試行 {retryAttempt}/{MaxRetryCount}): {ex.Message}");
					if (retryAttempt >= MaxRetryCount)
					{
						await _emailService!.SendErrorMailAsync("顧客向けCSV出力エラー", $"{ex}", "Debug");
						throw;
					}
					Console.WriteLine($"{RetryIntervalMs / 1000}秒後にリトライします...");
					await Task.Delay(RetryIntervalMs);
				}
			}
		}

		/// <summary>
		/// 顧客向け在庫CSV出力処理(DSからの呼び出し用)
		/// </summary>
		/// <param name="args">DS呼び出し用の引数</param>
		/// <returns></returns>
		static async Task PublishCSV(string[] args)
		{
			if (args.Length > 0 && args[0].ToLower() == "--customer")
			{
				using var fileLock = new Mutex(false, MutexName);
				bool hasHandle = false;
				try
				{
					hasHandle = fileLock.WaitOne();

					Console.WriteLine("【DS連携モード】処理を開始します...");
					await RetryExec(async () =>
					{
						await _schedulerService!.ProcessBackOrders();
						await _stockCsvDownloader!.ExecBySettings("Customer");
					});

					Console.WriteLine("【DS連携モード】全ての処理が完了しました。");
				}
				finally
				{
					if (hasHandle)
					{
						fileLock.ReleaseMutex();
					}
				}
			}
			else
			{
				Console.WriteLine("常駐モードでスケジューラ開始します");
				await _schedulerService!.StartAsync();
			}
		}

		/// <summary>
		/// メインエントリポイント
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		static async Task Main(string[] args)
		{
			await Init();
			await RetryExec(() => PublishCSV(args));

		}
	}
}