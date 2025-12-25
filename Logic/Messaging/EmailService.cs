using System;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using stockDataImporter.Logic.Messaging;
using Microsoft.Extensions.Configuration;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using MailKit;

namespace stockDataImporter.Logic.Messaging
{
	/// <summary>
	/// 
	/// </summary>
	/// <param name="mailConfig"></param>
	public class EmailService : IEmailService
		
	{
		public EmailService(EmailConfig config)
		{
			_config = config ?? throw new ArgumentNullException(nameof(config));
		}

        private readonly EmailConfig _config;
        /// <summary>
        /// エラー時メール配信処理
        /// </summary>
        /// <param name="subject">件名</param>
        /// <param name="body">本文</param>
        /// <param name="targetAddr">宛先メールアドレス</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task SendErrorMailAsync(string subject, string body, string? targetAddr = null)
		{
			string targetEmail;

			//	第3引数の宛先メールアドレスの判定
			if (!string.IsNullOrEmpty(targetAddr) &&
			_config.AddrListMap != null &&
			_config.AddrListMap.TryGetValue(targetAddr, out var MappedAddr))
			{
				targetEmail = MappedAddr;
			}
			else
			{
				targetEmail = _config.ToAddress;
			}

			//	メール送信内容の定義
			var message = new MimeMessage();
			message.From.Add(new MailboxAddress(_config.FromAddress, _config.FromAddress));
			message.To.Add(new MailboxAddress(_config.FromAddress, targetEmail));
			message.Subject = subject;

			message.Body = new TextPart("plain")
			{
				Text = body
			};

			//	メールサーバ接続定義
			using var client = new MailKit.Net.Smtp.SmtpClient();
			try
			{
				//	SSL証明書エラーの回避用
				client.ServerCertificateValidationCallback = (s, c, h, e) => true;

				//	メールサーバ接続
				await client.ConnectAsync(_config.Host, _config.Port, SecureSocketOptions.StartTls);

				//	メールサーバ認証
				await client.AuthenticateAsync(_config.Username, _config.Password);

				//	メール送信
				await client.SendAsync(message);

				//	メールサーバ切断
				await client.DisconnectAsync(true);

				Console.WriteLine($"メール送信完了: {targetEmail}");
			}
			catch (Exception ex)
			{
				Console.WriteLine($"エラーが発生しました。{ex.Message}");
			}
		}
	}
}