namespace stockDataImporter.Logic.ImportStockData
{
	/// <summary>
	/// FTP接続情報
	/// </summary>
	public class FtpsConnectionInfo
	{
		public required string Host { get; set; }
		public int Port { get; set; } = 21;
		public required string Username { get; set; }
		public required string Password { get; set; }
		public bool EnableSsl { get; set; } = true;
		public StockDataConfig StockData { get; set; } = new();
	}
	public class StockDataConfig
	{
		public string LocalFilePath { get; set; } = string.Empty;
		public string RemoteDirectory { get; set; } = string.Empty;
	}
}