namespace stockDataImporter.Logic.Scheduler
{
	/// <summary>
	/// 在庫データ出力処理メイン(15分単位での自動作成)
	/// </summary>
	/// <returns></returns>
	public interface ISchedulerService
	{
		Task StartAsync();
	}
}