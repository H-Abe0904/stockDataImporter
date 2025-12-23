namespace stockDataImporter.Logic.ImportStockData
{
	public interface IStockCsvDownloaderService
	{
		Task DownloadStockCsvAsync();
	}
}