namespace stockDataImporter.Logic.Messaging
{
	/// <summary>
	/// メール設定
	/// </summary>
	public class MailConfig
	{
		public required string Host { get; set; }
		public required string FromAddress { get; set; }
		public required string ToAddress { get; set; }
		public required string Username { get; set; }
		public required string Password { get; set;}
		public int Port { get; set; }
		public bool EnableSsl { get; set; }

		public Dictionary<string, string>? AddrListMap { get; set; }
	}
}