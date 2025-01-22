using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace Bulky.Utility
{
    public class SmtpEmailSender:IEmailSender
    {

        private readonly string _smtpServer;
        private readonly int _smtpPort;
        private readonly string _smtpUser;
        private readonly string _smtpPass;

        public SmtpEmailSender(IConfiguration configuration)
        {
            _smtpServer = configuration.GetValue<string>("EmailSettings:SmtpServer");
            _smtpPort = configuration.GetValue<int>("EmailSettings:SmtpPort");
            _smtpUser = configuration.GetValue<string>("EmailSettings:Username");
            _smtpPass = configuration.GetValue<string>("EmailSettings:Password");
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            using (var client = new SmtpClient(_smtpServer, _smtpPort))
            {
                client.Credentials = new NetworkCredential(_smtpUser, _smtpPass);
                client.EnableSsl = true;

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_smtpUser, "Bulky Book"), 
                    Subject = subject,
                    Body = htmlMessage,
                    IsBodyHtml = true 
                };

                mailMessage.To.Add(email); 

                await client.SendMailAsync(mailMessage); 
            }
        }


    }
}
