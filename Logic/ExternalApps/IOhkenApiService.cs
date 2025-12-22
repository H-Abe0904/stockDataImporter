namespace stockDataImporter.Logic
{
	public interface IOhkenApiService
	{
		Task RunOhkenKickerAsync(string exePath);
	}
}