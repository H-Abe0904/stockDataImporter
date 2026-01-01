namespace stockDataImporter.Logic.ImportStockData
{
	public interface IStockCsvDownloaderService
	{
		// クラス側の実装に合わせて名前を統一
        Task DL_AllStockCsv();
		Task<string> ExecBySettings(string label);
	}
}