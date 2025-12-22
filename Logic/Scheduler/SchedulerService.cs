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
	public class SchedulerService : ISchedulerService
	{
		private readonly IOhkenApiService _ohkenApiService;
		private readonly IEmailService _emailService;
		private readonly IFtpsClientService _ftpsClientService;
		private readonly IStockCsvDownloaderService _stockCsvDownloaderService;
		private readonly IMySqlDataLoaderService _mySqlDataLoaderService;
		private readonly PeriodicTimer _timer = new PeriodicTimer(TimeSpan.FromMinutes(15));

		public SchedulerService(IOhkenApiService ohkenApiService,
			IEmailService emailService,
			IStockCsvDownloaderService stockCsvDownloaderService,
			IMySqlDataLoaderService mySqlDataLoaderService,
			IFtpsClientService ftpsClientService)
		{
			_ohkenApiService = ohkenApiService;
			_emailService = emailService;
			_ftpsClientService = ftpsClientService;
			_stockCsvDownloaderService = stockCsvDownloaderService;
			_mySqlDataLoaderService = mySqlDataLoaderService;
		}

		/// <summary>
		/// 販売管理システムの自動実行exeファイルのパス
		/// </summary>
		public static string exePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Programs\Ohken\HanbaiENTCloud\bin\Ohken.Hanbai.HBRelationKicker.UI.exe";


		public async Task StartAsync()
		{
			Console.WriteLine($"{DateTime.Now:HH:mm:ss}スケジューラ開始");
			while (await _timer.WaitForNextTickAsync())
			{
				var now = DateTime.Now;

				// 15分単位での在庫データ出力処理
				try
				{
					if (now.Hour >= 7 && now.Hour < 23)
					{
						if (now.Minute % 15 == 0)
						{
							await _ohkenApiService.RunOhkenKickerAsync(exePath);
							
							await _mySqlDataLoaderService.ExecuteQueryAsync("backOrders");
							await _mySqlDataLoaderService.ExecuteQueryAsync("stock");

							await _stockCsvDownloaderService.
		
						}
					}
				}
				catch (Exception ex)
				{
					Console.WriteLine($"在庫データ取込み処理でエラー発生: {ex.Message}");
					await _emailService.SendErrorMailAsync("在庫データ取込み処理エラー", ex.Message, "System");
				}
			}
		}
	}
}