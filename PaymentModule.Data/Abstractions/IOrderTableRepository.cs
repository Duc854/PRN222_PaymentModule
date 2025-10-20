using PaymentModule.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PaymentModule.Data.Abstractions
{
    public interface IOrderTableRepository
    {
        Task<OrderTable?> GetOrderTableByBuyerId(int id);
        Task<OrderTable?> GetUnpaidOrderTableByBuyerId(int id);
        Task CreateOrderTableAsync(OrderTable order);
        Task UpdateOrderTableAsync(OrderTable order);
    }
}
