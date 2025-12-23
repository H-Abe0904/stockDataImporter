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

namespace stockDataImporter
{
	internal class Program
	{
		/// <summary>
		/// メインエントリポイント
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		static async Task Main()
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
			var mailConfig = config.GetSection("MailConfig").Get<MailConfig>();

			// 在庫データ出力設定の取得
			var StockExportSettings = config.GetSection("StockExportSettings").Get<StockExportSettings>();

			// FTPS接続情報の取得
			var FtpsConnectionInfo = config.GetSection("FtpsConnectionInfo").Get<FtpsConnectionInfo>();

			// サービス初期化
			var ohkenApiService = new OhkenApiService(exePath);
			var emailService = new EmailService(mailConfig!);
			var dataLoader = new MySqlDataLoaderService(connString!, pathSettings!);
			var masterImportService = new MasterImportService(connString!, mstPathSettings!);
			var stockCsvDownloader = new StockCsvDownload(connString!, StockExportSettings!);
			var ftpsClientService = new FtpsClientService(FtpsConnectionInfo!);

			// スケジューラサービスの初期化・開始
			var schedulerService = new SchedulerService(
				ohkenApiService,
				emailService,
				stockCsvDownloader,
				dataLoader,
				masterImportService,			
				ftpsClientService);
			await schedulerService.StartAsync();
		}
	}
}