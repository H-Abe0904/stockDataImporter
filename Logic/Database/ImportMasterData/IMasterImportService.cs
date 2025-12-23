namespace stockDataImporter.Logic
{
	/// <summary>
	/// 在庫データ出力処理メイン(15分単位での自動作成)
	/// </summary>
	/// <returns></returns>
	public interface IMasterImportService
	{
		Task ImportMasterDataAsync();
	}
}