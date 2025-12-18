using System.Threading.Tasks;
using System.IO;

namespace stockDataImporter.Logic.Messaging
{
	public interface IEmailService
	{
		/// <summary>
		/// エラー時メール配信処理
		/// </summary>
		/// <param name="toAddress">宛先メールアドレス</param>
		/// <param name="subject">件名</param>
		/// <param name="body">本文</param>
		/// <returns></returns>
		Task SendErrorMailAsync(string subject, string body, string mappingKey = "ToAddress");
	}
}
