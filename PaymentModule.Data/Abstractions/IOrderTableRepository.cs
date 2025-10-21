using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PaymentModule.Data.Entities;

namespace PaymentModule.Data.Abstractions
{
    public interface IOrderTableRepository
    {
        Task<OrderTable?> GetOrderTableByBuyerId(int id);
        Task<OrderTable?> GetUnpaidOrderTableByBuyerId(int id);
        Task CreateOrderTableAsync(OrderTable order);
        Task UpdateOrderTableAsync(OrderTable order);
        Task<OrderTable> GetOrderTableByIdAsync(int orderId);
        Task<List<OrderTable>> GetOrderHistoryByBuyerIdAsync(int buyerId);
        Task<OrderTable> GetOrderDetailsAsync(int orderId);
    }
}
