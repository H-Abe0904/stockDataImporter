namespace stockDataImporter.Logic.ImportStockData
{
	public class StockExportSettings
	{
		// JSONのキー名と一致させる必要があります
		public string ViewName { get; set; } = string.Empty;
		public string ExportFileNameFormat { get; set; } = string.Empty;
		public string CopyTargetDir { get; set; } = string.Empty;
	}
}