using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PaymentModule.Data.Entities;

namespace PaymentModule.Business.Abstractions
{
    public interface ICodPaymentService
    {
        Task<IEnumerable<OrderTable>> GetCodOrdersBySellerAsync(int sellerId);
        Task<OrderTable?> GetCodOrderDetailAsync(int orderId, int sellerId);
        Task<bool> ConfirmOrderAsync(int orderId, int sellerId);
        Task<bool> MarkAsDeliveredAsync(int orderId, int sellerId);
        Task<bool> VerifyCodPaymentAsync(int orderId, int sellerId);

    }
}
