using System;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace stockDataImporter.Logic.Messaging
{
	public class EmailService : IEmailService
	{
		private readonly IConfiguration _config;

		public EmailService(IConfiguration config)
		{
			_config = config;
		}

		public async Task SendErrorMailAsync(string subject, string body)
		{
			
		}
	}
}