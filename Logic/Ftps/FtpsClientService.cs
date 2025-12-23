using System.Threading.Tasks;
using System.IO;
using System;
using stockDataImporter.Logic.ImportStockData;
using FluentFTP;

namespace stockDataImporter.Logic.ImportStockData
{
	/// <summary>
	/// FTPSクライアントサービス
	/// </summary>
	public class FtpsClientService : IFtpsClientService
	{
		/// <summary>
		/// FTPS接続情報
		/// </summary>
		private readonly FtpsConnectionInfo _connectionInfo;


		/// <summary>
		/// コンストラクタ
		/// </summary>
		/// <param name="connectionInfo">FTPS接続情報</param>
		/// <exception cref="ArgumentNullException">nullの場合のエラー処理</exception>
		public FtpsClientService(FtpsConnectionInfo connectionInfo)
		{
			_connectionInfo = connectionInfo ?? throw new ArgumentNullException(nameof(connectionInfo));
		}

		/// <summary>
		/// ファイルアップロード処理
		/// </summary>
		/// <param name="localFilePath">ローカルファイルパス</param
		/// <param name="connectionInfo">FTPS接続情報</param>
		public async Task UploadFileAsync(string localFilePath)
		{
			// アップロード処理の実装
			using var client = new AsyncFtpClient(
			_connectionInfo.Host,
			_connectionInfo.Username,
			_connectionInfo.Password,
			_connectionInfo.Port
			);

			client.Config.EncryptionMode = FtpEncryptionMode.Explicit;              //	Explicitモードで通信
			client.Config.DataConnectionType = FtpDataConnectionType.AutoPassive;   //	Passiveモードで通信

			//	証明書を使用しないため強制的にTrue
			client.Config.ValidateAnyCertificate = true;

			try
			{
				await client.Connect();
				string remoteFilePath = Path.Combine(_connectionInfo.RemoteDirectory, Path.GetFileName(localFilePath));

				var result = await client.UploadFile(localFilePath, remoteFilePath);

				if (result == FtpStatus.Success)
				{
					Console.WriteLine($"FTPS: {localFilePath} を {remoteFilePath} に正常にアップロードしました。");
				}
				else
				{
					Console.WriteLine($"FTPS: {localFilePath} のアップロードに失敗しました。");
				}

			}
			catch (Exception ex)
			{
				Console.WriteLine($"FTPS接続/アップロードエラー: {ex.Message}");
				throw; // エラーを呼び出し元に伝える
			}
			finally
			{
				// 6. 接続の切断 (Disposeで自動切断されますが、明示的に行う場合)
				if (client.IsConnected)
				{
					await client.Disconnect();
				}
			}
			;
		}

		/// <summary>
		/// 接続テスト処理
		/// </summary>
		/// <returns>成功結果</returns>
		public async Task<bool> TestConnectionAsync()
		{
			// 接続テスト処理の実装
			using var client = new AsyncFtpClient(_connectionInfo.Host, _connectionInfo.Username, _connectionInfo.Password, _connectionInfo.Port);

			client.Config.EncryptionMode = FtpEncryptionMode.Explicit;
			client.Config.DataConnectionType = FtpDataConnectionType.AutoPassive;

			//	証明書を使用しないため強制的にTrue
			client.Config.ValidateAnyCertificate = true;


			Console.WriteLine($"Testing connection to {_connectionInfo.Host}, {_connectionInfo.RemoteDirectory}");

			try
			{

				await client.Connect();
				return client.IsConnected;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"FTPS接続エラー: {ex.Message}");
				return false;
			}
			finally
			{
				if (client.IsConnected)
				{
					await client.Disconnect();
				}
			}
		}

		public Task UploadFileAsync()
		{
			throw new NotImplementedException();
		}
	}
}