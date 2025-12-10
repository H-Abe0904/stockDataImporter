// using System.Threading.Tasks;
// using System.IO;
// using System;
// using stockDataImporter.Logic.ImportStockData;
// using FluentFTP;

// namespace stockDataImporter.Logic.ImportStockData
// {
// 	/// <summary>
// 	/// FTPSクライアントサービス
// 	/// </summary>
// 	public class FtpsClientService : IFtpsClientService
// 	{
// 		/// <summary>
// 		/// FTPS接続情報
// 		/// </summary>
// 		private readonly FtpsConnectionInfo _connectionInfo;

// 		/// <summary>
// 		/// コンストラクタ
// 		/// </summary>
// 		/// <param name="connectionInfo">FTPS接続情報</param>
// 		/// <exception cref="ArgumentNullException">nullの場合のエラー処理</exception>
// 		public FtpsClientService(FtpsConnectionInfo connectionInfo)
// 		{
// 			_connectionInfo = connectionInfo;
// 		}

// 		/// <summary>
// 		/// ファイルアップロード処理
// 		/// </summary>
// 		/// <param name="localFilePath">ローカルファイルパス</param
// 		/// <param name="connectionInfo">FTPS接続情報</param>
// 		public async Task UploadFileAsync(string localFilePath, FtpsConnectionInfo _connectionInfo)
// 		{
// 			// アップロード処理の実装
// 			using var client = new AsyncFtpClient(_connectionInfo.Host, _connectionInfo.Username, _connectionInfo.Password, _connectionInfo.Port);

// 			client.Config.EncryptionMode = FtpEncryptionMode.Explicit;
// 			client.Config.DataConnectionType = FtpDataConnectionType.AutoPassive;

// 			await client.ConnectAsync()

// 			await Task.Delay(1000); // 仮の非同期処理
// 			Console.WriteLine($"Uploaded {localFilePath} to {connectionInfo.Host}");
// 		}

// 		/// <summary>
// 		/// 接続テスト処理
// 		/// </summary>
// 		/// <param name="connectionInfo">FTPS接続情報</param>
// 		/// <returns>成功結果</returns>
// 		public Task<bool> TestConnectionAsync(FtpsConnectionInfo connectionInfo)
// 		{
// 			// 接続テスト処理の実装
// 			Console.WriteLine($"Testing connection to {connectionInfo.Host}, {connectionInfo.RemoteDirectory}");
// 			return Task.FromResult(true); // 仮の成功結果
// 		}
// 	}
// }