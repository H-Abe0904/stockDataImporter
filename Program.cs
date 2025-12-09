using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using stockDataImporter.Logic;

namespace stockDataImporter
{
	internal class Program
	{
		/// <summary>
		/// 販売管理システムの自動実行exeファイルのパス
		/// </summary>
		public static string exePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Programs\Ohken\HanbaiENTCloud\bin\Ohken.Hanbai.HBRelationKicker.UI.exe";

		/// <summary>
		/// 販売管理システムから売上残データを取得
		/// </summary>
		static async Task Get_BackOrders(string exePath)
		{
			var app = new ProcessStartInfo();

			app.FileName = exePath;
			app.Arguments = "/co:1 /data:1 /code:4109 /winlogin:False"; // 受注伝票データ取得用引数

			using var process = Process.Start(app); 					// 受注伝票データ取得プロセス起動(完了フラグの伝票を除く)

			if (process != null)
			{
				process.WaitForExit();
			}
		}

		/// <summary>
		/// データ挿入処理(MySQL)
		/// </summary>
		/// <returns>終了</returns>
		static async Task InsertData()
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
			var dataLoader = new MySqlDataLoader(connectionString);

			// インポート順序に従ってデータを挿入
			foreach (var order in ImportConfigMap.GetImportOrders())
			{
				var config = ImportConfigMap.Map[order];
				await dataLoader.ExecuteQueryAsync(config.FileName);
			}

			await getStockData(connectionString); // 有効在庫データ取得処理
		}

		/// <summary>
		/// / 有効在庫データ取得処理
		/// </summary>
		/// <param name="connectionString"> </param>
		static async Task getStockData(string connectionString)
		{
			string fileName = "stock_forEC.csv";
			string outputFilePath = @"\\chuo3\edi\data\sys\" + fileName;

			var stockCsvDownloader = new StockCsvDownload(connectionString);
			await stockCsvDownloader.DownloadAsync("vrwrk_cglink_workstock", outputFilePath);
		}

		/// <summary>
		/// メインエントリポイント
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		static async Task Main(string[] args)
		{
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            await Get_BackOrders(exePath);
			await InsertData();


		}
	}
}
