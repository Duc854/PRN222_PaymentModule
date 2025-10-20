using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using PaymentModule.Business.Abstractions;

namespace PaymentModule.Business.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            var settings = _config.GetSection("EmailSettings");
            string fromEmail = settings["SenderEmail"];
            string fromName = settings["SenderName"];
            string password = settings["Password"];
            string host = settings["SmtpServer"];
            int port = int.Parse(settings["Port"]);

            var mail = new MailMessage()
            {
                From = new MailAddress(fromEmail, fromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            mail.To.Add(to);

            using var smtp = new SmtpClient(host, port)
            {
                Credentials = new NetworkCredential(fromEmail, password),
                EnableSsl = true
            };

            await smtp.SendMailAsync(mail);
        }

        // Email khi thanh toán thành công
        public async Task SendOrderConfirmationAsync(string to, string orderId, decimal total)
        {
            string subject = $"✅ Đơn hàng {orderId} đã thanh toán thành công";
            string body = $@"
                <h3>Cảm ơn bạn đã mua hàng!</h3>
                <p>Mã đơn hàng: <b>{orderId}</b></p>
                <p>Tổng tiền: <b>{total:n0} VND</b></p>
                <p>Chúng tôi sẽ sớm gửi hàng cho bạn.</p>";

            await SendEmailAsync(to, subject, body);
        }

        // Email khi trạng thái đơn hàng thay đổi
        public async Task SendOrderStatusAsync(string to, string orderId, string newStatus)
        {
            string subject = $"📦 Cập nhật trạng thái đơn hàng {orderId}";
            string body = $@"
                <p>Đơn hàng <b>{orderId}</b> hiện đang ở trạng thái: 
                <b>{newStatus}</b>.</p>";

            await SendEmailAsync(to, subject, body);
        }
    }
}
