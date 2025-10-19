using Microsoft.EntityFrameworkCore;
using PaymentModule.Data.Abstractions;
using PaymentModule.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            return await _dbContext.OrderItems.Where(oi => oi.OrderId == orderId).Include(oi => oi.Product).ToListAsync();
        }
    }
}
