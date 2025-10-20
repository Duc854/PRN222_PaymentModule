using Microsoft.EntityFrameworkCore;
using PaymentModule.Data.Abstractions;
using PaymentModule.Data.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PaymentModule.Data.Repositories
{
    public class OrderItemRepository : IOrderItemRepository
    {
        private readonly CloneEbayDbContext _dbContext;

        public OrderItemRepository(CloneEbayDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IEnumerable<OrderItem>> GetOrderItemsByOrderId(int orderId)
        {
            return await _dbContext.OrderItems
                .Include(i => i.Product)
                .Where(i => i.OrderId == orderId)
                .ToListAsync();
        }

        public async Task CreateOrderItemAsync(OrderItem item)
        {
            await _dbContext.OrderItems.AddAsync(item);
            await _dbContext.SaveChangesAsync();
        }

        public async Task UpdateOrderItemAsync(OrderItem item)
        {
            _dbContext.OrderItems.Update(item);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<Product?> GetProductByIdAsync(int productId)
        {
            return await _dbContext.Products.FirstOrDefaultAsync(p => p.Id == productId);
        }
        public async Task<OrderItem?> GetOrderItemByIdAsync(int id)
        {
            return await _dbContext.OrderItems
                .Include(o => o.Order)
                .FirstOrDefaultAsync(o => o.Id == id);
        }

        public async Task DeleteOrderItemAsync(OrderItem item)
        {
            _dbContext.OrderItems.Remove(item);
            await _dbContext.SaveChangesAsync();
        }

    }
}
