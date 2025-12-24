namespace stockDataImporter.Logic
{
	public interface IOhkenApiService
	{
		Task FetchBackOrdersAsync();
	}
}