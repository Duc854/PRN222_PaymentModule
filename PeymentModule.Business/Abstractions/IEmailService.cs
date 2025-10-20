using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PaymentModule.Business.Abstractions
{
    public interface IEmailService
    {
        //hàm thực hiện gửi mail
        Task SendEmailAsync(string to, string subject, string body);

        //Hàm gửi mail khi đơn hàng thanh toán thành công
        Task SendOrderConfirmationAsync(string to, string orderId, decimal total);

        // Email khi trạng thái đơn hàng thay đổi
        Task SendOrderStatusAsync(string to, string orderId, string newStatus);
    }
}
