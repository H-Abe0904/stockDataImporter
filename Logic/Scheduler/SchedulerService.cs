using System;
using System.Threading;
using System.Threading.Tasks;
using stockDataImporter.Logic;
using stockDataImporter.Logic.Messaging;
using stockDataImporter.Logic.ImportStockData;

namespace stockDataImporter.Logic.Scheduler
{
	/// <summary>
	/// タイマー用スケジューラサービス
	/// 在庫データ取込みを15分単位で起動させるための登録クラス
	/// </summary>
	public class SchedulerService(IOhkenApiService ohkenApiService,
		IEmailService emailService,
		IStockCsvDownloaderService stockCsvDownloaderService,
		IMySqlDataLoaderService mySqlDataLoaderService,
		IMasterImportService masterImportService,
		IFtpsClientService ftpsClientService) : ISchedulerService
	{
		private readonly IOhkenApiService _ohkenApiService = ohkenApiService;
		private readonly IEmailService _emailService = emailService;
		private readonly IFtpsClientService _ftpsClientService = ftpsClientService;
		private readonly IStockCsvDownloaderService _stockCsvDownloaderService = stockCsvDownloaderService;
		private readonly IMySqlDataLoaderService _mySqlDataLoaderService = mySqlDataLoaderService;

		private readonly IMasterImportService _masterImportService = masterImportService;
		private readonly PeriodicTimer _timer = new(TimeSpan.FromMinutes(15));

		/// <summary>
		/// 在庫データ出力処理メイン(15分単位での自動作成)
		/// </summary>
		/// <returns></returns>
		public async Task StartAsync()
		{
			Console.WriteLine($"{DateTime.Now:HH:mm:ss}スケジューラ開始");
			while (await _timer.WaitForNextTickAsync())
			{
				var now = DateTime.Now;

				try
				{
					// 15分単位での在庫データ出力処理(7時～23時45分まで)
					if (now.Hour >= 7 && now.Hour <= 23)
					{
						if (now.Minute % 15 == 0)
						{
							// 受注残CSV書き出し
							await _ohkenApiService.RunOhkenKickerAsync();

							// 受注残・在庫CSVをDBに取り込み
							await _mySqlDataLoaderService.ExecuteQueryAsync("backOrders");
							await _mySqlDataLoaderService.ExecuteQueryAsync("stock");

							// DBから在庫CSVダウンロード・ECB FTPSアップロード処理
							await _stockCsvDownloaderService.DownloadStockCsvAsync();
							// await _ftpsClientService.UploadFileAsync(); 12/23 検証のためコメントアウト
							
							// FTPS接続テスト(デバッグ用)
							await _ftpsClientService.TestConnectionAsync();
						}
					}
					// 毎日2時の定期処理
					if (now.Hour == 2 && now.Minute == 30)
					{
						// 日次処理(毎日0時)
						await _masterImportService.ImportMasterDataAsync();

					}
				}
				catch (Exception ex)
				{
					Console.WriteLine($"在庫データ取込み処理でエラー発生: {ex.Message}");
					await _emailService.SendErrorMailAsync("在庫データ取込み処理エラー", ex.Message, "Debug");
				}
			}
		}
	}
}