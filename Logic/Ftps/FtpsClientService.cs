using System.Threading.Tasks;
using System.IO;
using System;
using stockDataImporter.Logic.ImportStockData;
using stockDataImporter.Logic.Messaging;
using stockDataImporter.Logic;
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
		private readonly IEmailService _emailService;

		/// <summary>
		/// コンストラクタ
		/// </summary>
		/// <param name="connectionInfo">FTPS接続情報</param>
		/// <exception cref="ArgumentNullException">nullの場合のエラー処理</exception>
		public FtpsClientService(FtpsConnectionInfo connectionInfo, IEmailService emailService)
		{
			_connectionInfo = connectionInfo ?? throw new ArgumentNullException(nameof(connectionInfo));
			_emailService = emailService;
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
				string fileName = Path.GetFileName(localFilePath);  //	StockCsvDownloadServiceで生成したファイル名を使用
				string remoteFilePath = Path.Combine(_connectionInfo.StockData.RemoteDirectory, fileName).Replace("\\", "/");

				await client.Connect();
				// string remoteFilePath = Path.Combine(_connectionInfo.StockData.RemoteDirectory, fileName).Replace("\\", "/");
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
				await _emailService.SendErrorMailAsync("FTPS接続エラー", $"FTPS接続/アップロードエラーが発生しました。\nエラー内容: {ex.Message} \n{ex.InnerException?.StackTrace}", "Debug");
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
		}

		/// <summary>
		/// 接続テスト処理
		/// </summary>
		/// <returns>成功結果</returns>
		public async Task<bool> TestConnectionAsync(string? localFilePath = null)
		{
			//	ファイルアップロード処理デバッグ用(引数がnullならJSON読み込み)
			string pathForTest = localFilePath ?? _connectionInfo.StockData.LocalFilePath;

			// 接続テスト処理の実装
			using var client = new AsyncFtpClient(_connectionInfo.Host, _connectionInfo.Username, _connectionInfo.Password, _connectionInfo.Port);

			//	指定された接続形式を設定
			client.Config.EncryptionMode = FtpEncryptionMode.Explicit;
			client.Config.DataConnectionType = FtpDataConnectionType.AutoPassive;

			//	証明書を使用しないため強制的にTrue
			client.Config.ValidateAnyCertificate = true;

			Console.WriteLine($"Testing connection to {_connectionInfo.Host}, {_connectionInfo.StockData.RemoteDirectory}");

			string fileName = Path.GetFileName(pathForTest);
			string remoteFilePath = Path.Combine(_connectionInfo.StockData.RemoteDirectory, fileName).Replace("\\", "/");
			try
			{
				await client.Connect();
				Console.WriteLine($"Debug: {fileName}");
				await _emailService.SendErrorMailAsync($"デバッグ: {fileName}", $"デバッグ: {fileName}", "Debug");

				return client.IsConnected;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"FTPS接続エラー: {ex.Message}");
				await _emailService.SendErrorMailAsync("FTPS接続テストエラー", $"FTPS接続エラーが発生しました。\nエラー内容: {ex.Message} \n{ex.InnerException?.StackTrace}", "Debug");
				return false;
			}
			finally
			{
				if (client.IsConnected)	await client.Disconnect();
				
			}
		}

		/// <summary>
		/// FTPサーバへのアップロード処理
		/// </summary>
		/// <param name="localPath">CSVファイルのローカルファイルパス</param>
		/// <returns></returns>
		/// <exception cref="InvalidOperationException">パス設定エラー時の処理</exception>
		/// <exception cref="FileNotFoundException">ファイルが見つからなかった際の処理</exception>
		public async Task ExecUploadFileAsync(string? localPath = null)
		{
			string path = !string.IsNullOrEmpty(localPath)
			? localPath
			: _connectionInfo.StockData.LocalFilePath;
			string remoteDir = _connectionInfo.StockData.RemoteDirectory;

			if (string.IsNullOrEmpty(path))
			{
				throw new InvalidOperationException("ローカルパスが設定されていません。");
			}
			if (!File.Exists(path))
			{
				throw new FileNotFoundException($"アップロード対象のファイルがありません: {path}");
			}
			await UploadFileAsync(path);
		}
	}
}