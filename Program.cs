using System.Diagnostics;
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
		static void Get_BackOrders(string exePath)
		{
			var app = new ProcessStartInfo();

			app.FileName = exePath;
			app.Arguments = "/co:1 /data:1 /code:4109 /winlogin:False"; // 受注伝票データ取得用引数

			Process.Start(app); // 受注伝票データ取得プロセス起動(完了フラグの伝票を除く)
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
				await dataLoader.ExecuteSPAsync(config.FileName, config.TableName);
			}
		}
		/// <summary>
		/// メインエントリポイント
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		static async Task Main(string[] args)
		{
			Get_BackOrders(exePath);
			await InsertData();


		}
	}
}
