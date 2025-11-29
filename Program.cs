using Microsoft.Extensions.Configuration;
using stockDataImporter.Logic;

namespace stockDataImporter
{
	internal class Program
	{
		static async Task Main(string[] args)
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

			var dataLoader = new MySqlDataLoader(connectionString);

			foreach (var order in ImportConfigMap.GetImportOrders())
			{
				var config = ImportConfigMap.Map[order];
				await dataLoader.ExecuteSPAsync("ImportDataFromCsv", config.FileName, config.TableName);
			}
		}
	}
}
