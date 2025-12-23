namespace stockDataImporter.Logic
{
    /// <summary>
    /// JSON(appsettings.json)の "ImportPathSettings" セクションを受け取るためのクラス
    /// </summary>
    public class ImportPathSettings
    {
        // JSONのキー名と一致させる必要があります
        public string StockDataPath { get; set; } = string.Empty;
        public string BackOrderPath { get; set; } = string.Empty;
    }
}