namespace stockDataImporter.Logic
{
	public interface IMySqlDataLoaderService
	{
		Task ExecuteQueryAsync(string fileNameKey);
	}
}