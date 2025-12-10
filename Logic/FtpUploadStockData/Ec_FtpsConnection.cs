namespace stockDataImporter.Logic
{
	public class Ec_FtpsConnectionInfo
	{
		public string Host { get; set; };
		public int Port { get; set; };
		public string FtpUser { get; }
		public string FtpPassword { get; }
		public bool EnableSsl { get; set; } = true;
		public string RemoteDirectory { get; set; }



		/// <summary>
		/// コンストラクタ(仮)
		/// </summary>
		/// <param name="ftpServer">FTPサーバーアドレス</param>
		/// <param name="ftpUser">FTPユーザー名</param>
		/// <param name="ftpPassword">FTPパスワード</param>
		/// <exception cref="ArgumentNullException">nullの場合のエラー処理</exception>
		// public Ec_FtpsConnection(string ftpServer, string ftpUser, string ftpPassword)
		// {
		// 	FtpServer = ftpServer ?? throw new ArgumentNullException(nameof(ftpServer));
		// 	FtpUser = ftpUser ?? throw new ArgumentNullException(nameof(ftpUser));
		// 	FtpPassword = ftpPassword ?? throw new ArgumentNullException(nameof(ftpPassword));
		// }
	}
}