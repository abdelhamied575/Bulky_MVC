using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace Bulky.Utility
{
    public class EmailService
    {

        private readonly IEmailSender _emailSender;

        public EmailService(IEmailSender emailSender)
        {
            _emailSender = emailSender;
        }

        public async Task SendWelcomeEmail(string userEmail)
        {
            var subject = "Welcome to Bulky Book!";
            var htmlMessage = "<h1>Thank you for joining us!</h1><p>We are excited to have you on board.</p>";

            await _emailSender.SendEmailAsync(userEmail, subject, htmlMessage);
        }


    }
}
