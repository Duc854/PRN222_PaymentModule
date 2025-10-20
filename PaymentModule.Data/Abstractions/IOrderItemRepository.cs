using PaymentModule.Data.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PaymentModule.Data.Abstractions
{
    public interface IOrderItemRepository
    {
        Task<IEnumerable<OrderItem>> GetOrderItemsByOrderId(int orderId);
        Task CreateOrderItemAsync(OrderItem item);
        Task UpdateOrderItemAsync(OrderItem item);
        Task<Product?> GetProductByIdAsync(int productId);
        Task<OrderItem?> GetOrderItemByIdAsync(int id);
        Task DeleteOrderItemAsync(OrderItem item);

    }
}
