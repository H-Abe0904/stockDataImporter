using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using stockDataImporter.Logic.ImportStockData;
using stockDataImporter.Logic.Messaging;
using stockDataImporter.Logic;
using System.Configuration;
using System.Reflection;

namespace stockDataImporter
{
	internal class Program
	{
		/// <summary>
		/// 販売管理システムの自動実行exeファイルのパス
		/// </summary>
		public static string exePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Programs\Ohken\HanbaiENTCloud\bin\Ohken.Hanbai.HBRelationKicker.UI.exe";

		/// <summary>
		/// エラーメール配信
		/// </summary>
		/// <param name="subject">件名</param>
		/// <param name="body">本文</param>
		/// <param name="mappingKey">宛先メールアドレスのラベル</param>
		/// <returns></returns>
		static async Task SendErrMailAsync(string title, string message, string mappingKey)
		{
			var configuration = new ConfigurationBuilder()
				.SetBasePath(Directory.GetCurrentDirectory())
				.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
				.Build();
			try
			{
				var mailConfig = configuration.GetSection("EdiMailConfig");
				var mailService = new EmailService(mailConfig);
				Console.WriteLine("メール送信開始");
				await mailService.SendErrorMailAsync(title, message, mappingKey);

			}
			catch (Exception ex)
			{
				Console.WriteLine($"メール送信失敗{ex.Message}");
			}
		}

		/// <summary>
		/// 販売管理システムから売上残データを取得
		/// </summary>
		static async Task Get_BackOrders(string exePath)
		{
			try
			{
				var app = new ProcessStartInfo();

				app.FileName = exePath;
				app.Arguments = "/co:1 /data:1 /code:4109 /winlogin:False"; // 受注伝票データ取得用引数

				using var process = Process.Start(app);                     // 受注伝票データ取得プロセス起動(完了フラグの伝票を除く)

				if (process != null)
				{
					process.WaitForExit();
				}
			}
			catch (Exception ex)
			{
				await SendErrMailAsync("受注伝票データ取得エラー", $"受注伝票データ取得中にエラーが発生しました: {ex.Message}", "Debug");
				Console.WriteLine($"エラーが発生しました: {ex.Message}");
			}
		}

		/// <summary>
		/// データ挿入処理(MySQL)
		/// </summary>
		/// <returns>終了</returns>
		static async Task InsertData()
		{
			try
			{
				// appsettings.jsonから接続文字列を取得
				var configuration = new ConfigurationBuilder()
					.SetBasePath(Directory.GetCurrentDirectory())
					.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
					.Build();
				var connectionString = configuration.GetConnectionString("DefaultConnection");

				if (string.IsNullOrEmpty(connectionString))
				{
					Console.WriteLine("接続文字列が見つかりません。appsettings.jsonを確認してください。");
					return;
				}

				// MySQLデータローダーの初期化
				var dataLoader = new MySqlDataLoaderService(connectionString);

				// インポート順序に従ってデータを挿入
				foreach (var order in ImportConfigMap.GetImportOrders())
				{
					var config = ImportConfigMap.Map[order];
					await dataLoader.ExecuteQueryAsync(config.FileName);
				}

				await getStockData(connectionString); // 有効在庫データ取得処理
			}
			catch (Exception ex)
			{
				await SendErrMailAsync("データ挿入エラー", $"データ挿入中にエラーが発生しました: {ex.InnerException}", "Debug");
				Console.WriteLine($"エラーが発生しました: {ex.Message}");
			}

		}

		/// <summary>
		/// / 有効在庫データ取得処理
		/// </summary>
		/// <param name="connectionString"> </param>
		static async Task getStockData(string connectionString)
		{
			string outFileName = "stock_forEC.csv";
			string outputFilePath = @"\\chuo3\edi\data\sys\" + outFileName;

			try
			{
				var stockCsvDownloader = new StockCsvDownload(connectionString);
				await stockCsvDownloader.DownloadAsync("vrwrk_cglink_workstock", outputFilePath);
			}
			catch (Exception ex)
			{
				await SendErrMailAsync("在庫データ取得エラー", $"在庫データ取得中にエラーが発生しました: {ex.Message}", "Debug");
				Console.WriteLine($"エラーが発生しました: {ex.Message}");
			}
		}
		/// <summary>
		/// 在庫データのアップロード
		/// </summary>
		/// <returns></returns>
		static async Task Put_StockData()
		{
			var configuration = new ConfigurationBuilder()
				.SetBasePath(Directory.GetCurrentDirectory())
				.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
				.Build();
			try
			{
				var ftpsInfo = configuration.GetSection("FtpsConnection").Get<FtpsConnectionInfo>();

				var ftpsService = new FtpsClientService(ftpsInfo!);

				await ftpsService.UploadFileAsync("stock_forEC.csv", ftpsInfo!);
			}
			catch (Exception ex)
			{
				await SendErrMailAsync("FTPSアップロードエラー", $"FTPSエラーが発生しました: {ex.Message}", "Debug");
				Console.WriteLine($"エラーが発生しました: {ex.Message}");

			}
		}

		/// <summary>
		/// FTPSサーバへの接続テスト
		/// </summary>
		/// <returns></returns>
		static async Task TestConnection()
		{
			var configuration = new ConfigurationBuilder()
				.SetBasePath(Directory.GetCurrentDirectory())
				.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
				.Build();
			try
			{
				var ftpsInfo = configuration.GetSection("FtpsConnectionInfo").Get<FtpsConnectionInfo>();
				if (ftpsInfo == null)
				{
					Console.WriteLine("エラー: 設定ファイルに 'FtpsConnection' セクションが見つかりません。");
					return;
				}

				Console.WriteLine($"接続先確認: Host={ftpsInfo!.Host}, User={ftpsInfo!.Username}");

				IFtpsClientService ftpsClientService = new FtpsClientService(ftpsInfo!);
				var result = await ftpsClientService.TestConnectionAsync(ftpsInfo!);

				if (result)
				{
					Console.WriteLine("接続テストに成功しました。");

					await SendErrMailAsync("接続テストに成功しました", "デバッグ用", "Debug");
				}
				else
				{
					Console.WriteLine("接続テストに失敗しました。");
				}
			}
			catch (Exception ex)
			{

				await SendErrMailAsync("FTPS接続テストエラー", $"FTPS接続テスト中にエラーが発生しました: {ex.Message}", "Debug");
				Console.WriteLine($"接続テスト中にエラーが発生しました: {ex.Message}");
			}
		}
		/// <summary>
		/// 在庫データ出力処理メイン(15分単位での自動作成)
		/// </summary>
		/// <returns></returns>
		static async Task StockDataImport()
		{
			Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
			await Get_BackOrders(exePath);
			await InsertData();
			// await Put_StockData();
			await TestConnection(); //	FTPサーバ接続確認用
		}
		// await Main();

		/// <summary>
		/// メインエントリポイント
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		static async Task Main()
		{



		}
	}
}