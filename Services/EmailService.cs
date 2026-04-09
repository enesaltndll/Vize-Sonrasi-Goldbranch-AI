using Microsoft.Extensions.Configuration;
using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace GoldBranchAI.Services
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlContent)
        {
            var emailSettings = _configuration.GetSection("EmailSettings");

            string GetRequired(string key)
            {
                var v = emailSettings[key];
                return string.IsNullOrWhiteSpace(v) ? throw new InvalidOperationException($"EmailSettings:{key} is missing.") : v;
            }

            var smtpServer = GetRequired("SmtpServer");
            var smtpPortStr = emailSettings["SmtpPort"];
            if (!int.TryParse(smtpPortStr, out var smtpPort))
                throw new InvalidOperationException("EmailSettings:SmtpPort is invalid.");

            var senderName = GetRequired("SenderName");
            var senderEmail = GetRequired("SenderEmail");
            var username = GetRequired("Username");
            var password = emailSettings["Password"]; // optional: empty => test mode

            // Şifre boş bırakılmışsa test modundayız demektir, sahte yanıt döndürelim.
            if (string.IsNullOrEmpty(password) || password == "your_app_password" || password == "sifrenizi_girin")
            {
                Console.WriteLine($"[TEST MAIL - Gönderilmedi] Kime: {toEmail} | Konu: {subject}");
                return;
            }

            try
            {
                using (var client = new SmtpClient(smtpServer, smtpPort))
                {
                    client.Credentials = new NetworkCredential(username, password);
                    client.EnableSsl = true;

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(senderEmail, senderName),
                        Subject = subject,
                        Body = htmlContent,
                        IsBodyHtml = true
                    };
                    mailMessage.To.Add(toEmail);

                    await client.SendMailAsync(mailMessage);
                    Console.WriteLine($"[MAIL GÖNDERİLDİ] Kime: {toEmail} | Konu: {subject}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MAIL HATASI] {ex.Message}");
            }
        }
    }
}
