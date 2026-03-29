using Microsoft.AspNetCore.Identity.UI.Services;
using System.Net;
using System.Net.Mail;

namespace SyndiceoWeb.Services
{
    public class EmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;

        public EmailSender(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var host = _configuration["MailtrapSettings:Host"];
            var port = int.Parse(_configuration["MailtrapSettings:Port"]);
            var userName = _configuration["MailtrapSettings:UserName"];
            var password = _configuration["MailtrapSettings:Password"];

            using (var client = new SmtpClient(host, port))
            {
                client.Credentials = new NetworkCredential(userName, password);
                client.EnableSsl = true; 

                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(userName, "Syndiceo Support"),
                    Subject = subject,
                    Body = htmlMessage,
                    IsBodyHtml = true
                };

                mailMessage.To.Add(email);

                try
                {
                    await client.SendMailAsync(mailMessage);
                }
                catch (SmtpException ex)
                {
                    throw new Exception($"Грешка при изпращане: {ex.Message}");
                }
            }
        }
    }
}