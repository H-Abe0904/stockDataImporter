namespace stockDataImporter.Logic
{
	public interface IMySqlDataLoaderService
	{
		Task ExecuteQueryAsync(string fileName);
	
		Task LoadDataAsync(string tableName, string filePath);
	}
}