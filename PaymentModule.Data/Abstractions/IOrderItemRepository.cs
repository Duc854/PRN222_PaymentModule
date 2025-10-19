using PaymentModule.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PaymentModule.Data.Abstractions
{
    public interface IOrderItemRepository
    {
        Task<IEnumerable<OrderItem>> GetOrderItemsByOrderId(int orderId);
    }
}
