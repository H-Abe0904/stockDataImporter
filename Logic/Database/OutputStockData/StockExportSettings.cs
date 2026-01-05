namespace stockDataImporter.Logic.ImportStockData
{
	/**
	 * 在庫データ出力設定(appsettings.json)を受け取るためのクラス
	 */

	/// <summary>
	/// 在庫データ出力設定を受け取るためのクラス
	/// </summary>
	public class ExportConfig
	{
		public string ViewName { get; set; } = string.Empty;
		public string ExportFileNameFormat { get; set; } = string.Empty;
		public string CopyTargetDir { get; set; } = string.Empty;
	}
	public class StockExportSettings
	{
		// JSONの "StockEcDataExportSettings" と完全に一致させる
		public ExportConfig StockEcDataExportSettings { get; set; } = new();

		public ExportConfig CustomerStockDataExportSettings { get; set; } = new();
	}
	/// <summary>
	/// 在庫データコピー設定を受け取るためのクラス
	/// </summary>
	public class CopyStockDataSettings
	{
		public string SourceFilePath { get; set; } = string.Empty;
		public string AscensusPath { get; set; } = string.Empty;
		public string CustomerPath { get; set; } = string.Empty;
		public string MultiPurposePath { get; set; } = string.Empty;
	}
}
