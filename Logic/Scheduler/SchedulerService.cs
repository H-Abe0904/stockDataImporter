using System;
using System.Threading;
using System.Threading.Tasks;
using stockDataImporter.Logic;
using stockDataImporter.Logic.Messaging;
using stockDataImporter.Logic.ImportStockData;
using Microsoft.VisualBasic;
using Org.BouncyCastle.Security;

namespace stockDataImporter.Logic.Scheduler
{
	/// <summary>
	/// タイマー用スケジューラサービス
	/// 在庫データ取込みを15分単位で起動させるための登録クラス
	/// </summary>
	public class SchedulerService(
		IOhkenApiService ohkenApiService,
		IEmailService emailService,
		IStockCsvDownloaderService stockCsvDownloaderService,
		IMySqlDataLoaderService mySqlDataLoaderService,
		IMasterImportService masterImportService,
		IFtpsClientService ftpsClientService) : ISchedulerService
	{
		private readonly SemaphoreSlim _semaphore = new(1, 1);
		private readonly IOhkenApiService _ohkenApiService = ohkenApiService;
		private readonly IEmailService _emailService = emailService;
		private readonly IFtpsClientService _ftpsClientService = ftpsClientService;
		private readonly IStockCsvDownloaderService _stockCsvDownloaderService = stockCsvDownloaderService;
		private readonly IMySqlDataLoaderService _mySqlDataLoaderService = mySqlDataLoaderService;
		private readonly IMasterImportService _masterImportService = masterImportService;
		private readonly PeriodicTimer _timer = new(TimeSpan.FromMinutes(1));

		/// <summary>
		/// 商品マスタ更新処理メイン(毎日2:30に自動作成)
		/// </summary>
		/// <returns></returns>
		public async Task UPD_ProductMST()
		{
			var now = DateTime.Now;
			var currentTime = now.TimeOfDay;
			// 毎日 2:00 に商品マスタ取込処理を実行
			// 毎日2時半の定期処理
			if (now.Hour == 2 && now.Minute == 30)
			{
				// 日次処理(毎日0時)
				Console.WriteLine("-> 商品マスタ更新中...");
				await _masterImportService.ImportMasterDataAsync();

				//	コンソールの情報をクリア
				Console.WriteLine("\n3秒後に画面をクリアして待機状態に戻ります...");
				await Task.Delay(3000);
				Console.Clear();
				Console.WriteLine($"{DateTime.Now:HH:mm:ss} 現在待機中です...");
			}
		}

		/// <summary>
		/// 在庫データ出力処理メイン(15分単位での自動作成)
		/// </summary>
		/// <returns></returns>
		public async Task ECB_StockIF()
		{
			var now = DateTime.Now;
			var currentTime = now.TimeOfDay;

			// 15分単位での在庫データ出力処理(7:08～23:38分まで)
			if (currentTime >= new TimeSpan(7, 8, 0) && currentTime <= new TimeSpan(23, 38, 0))
			{
				// デバッグ時はここを書換えて1~5分毎に動作させる
				if (now.Minute % 15 == 8)   // 毎時 8, 23, 38, 53分に判定
				{
					// 受注残CSV書き出し
					Console.WriteLine("-> 受注残データを処理中...");
					await _ohkenApiService.FetchBackOrdersAsync();

					// 受注残・在庫CSVをDBに取り込み
					Console.WriteLine("-> データ取込中...");
					await _mySqlDataLoaderService.ExecuteQueryAsync("backOrders");
					await _mySqlDataLoaderService.ExecuteQueryAsync("stock");

					// DBから在庫CSVダウンロード・ECB FTPSアップロード処理
					Console.WriteLine("-> 有効在庫データを出力中...");
					await _stockCsvDownloaderService.ExecBySettings("EC");

					Console.WriteLine("-> FTPサーバーへアップロード中...");
					//await _ftpsClientService.ExecUploadFileAsync(); //	12/23 検証のためコメントアウト

					// FTPS接続テスト(デバッグ用)
					await _ftpsClientService.TestConnectionAsync();

					//	成功報告メールを送信
					await _emailService.SendErrorMailAsync("在庫データ連携成功", "在庫データ連携が正常に完了しました。", "Debug");

					//	コンソールの情報をクリア
					Console.WriteLine("\n3秒後に画面をクリアして待機状態に戻ります...");
					await Task.Delay(3000);
					Console.Clear();
					Console.WriteLine($"{DateTime.Now:HH:mm:ss} 現在待機中です...");
				}
			}
		}
		public async Task DL_Stock_ForCustomer()
		{
			var now = DateTime.Now;
			var currentTime = now.TimeOfDay;
			if (currentTime >= new TimeSpan(7, 10, 0) || currentTime <= new TimeSpan(19, 10, 0))
			{

					await _stockCsvDownloaderService.ExecBySettings("Customer");
					await _emailService.SendErrorMailAsync("A在庫データ連携成功", "在庫データ連携が正常に完了しました。", "Debug");

					//	コンソールの情報をクリア
					Console.WriteLine("\n3秒後に画面をクリアして待機状態に戻ります...");
					await Task.Delay(3000);
					Console.Clear();
					Console.WriteLine($"{DateTime.Now:HH:mm:ss} 現在待機中です...");
				
			}

		}

		/// <summary>
		/// 在庫データ出力処理メイン(15分単位での自動作成)
		/// </summary>
		/// <returns></returns>
		public async Task StartAsync()
		{

			Console.Clear();
			Console.WriteLine("========================================================");
			Console.WriteLine($" バッチ処理開始時刻: {DateTime.Now:yyyy/MM/dd HH:mm:ss}");
			Console.WriteLine("========================================================");
			while (await _timer.WaitForNextTickAsync())
			{
				await _semaphore.WaitAsync();   // 排他制御
				try
				{
					await UPD_ProductMST(); // 商品マスタ更新処理
					await ECB_StockIF();    // 在庫データ出力処理
					await DL_Stock_ForCustomer();

                }
				catch (Exception ex)
				{
					Console.WriteLine($"在庫データ連携中にエラーが発生しました: {ex.Message}");
					await _emailService.SendErrorMailAsync("在庫データ連携エラー", $"{ex.InnerException}\n {ex.InnerException?.StackTrace}", "Rocs");
				}
				finally
				{

					_semaphore.Release();   // 排他制御解除
				}
			}
		}
	}
}