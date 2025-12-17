using System.Threading.Tasks;
using System.IO;

namespace stockDataImporter.Logic.Messaging
{
	public interface IEmailService
	{
		Task SendErrorMailAsync(string subject, string body);
	}
}
