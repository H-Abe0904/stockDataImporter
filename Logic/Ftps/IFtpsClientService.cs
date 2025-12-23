using System.Threading.Tasks;
using System.IO;

namespace stockDataImporter.Logic.ImportStockData
{
	/// <summary>
	/// FTPSクライアントサービスインターフェース
	/// </summary>
	public interface IFtpsClientService
	{
		/// <summary>
		/// ファイルアップロード処理
		/// </summary>
		/// <param name="localFilePath">ローカルファイルパス</param>
		/// <param name="connectionInfo">FTPS接続情報</param>
		Task UploadFileAsync();

		/// <summary>
		/// 接続テスト処理
		/// </summary>
		/// <param name="connectionInfo">FTPS接続情報</param>
		Task<bool> TestConnectionAsync();
	}
}