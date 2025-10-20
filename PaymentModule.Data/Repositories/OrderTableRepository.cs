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
    public class OrderTableRepository : IOrderTableRepository
    {
        private readonly CloneEbayDbContext _dbContext;
        public OrderTableRepository(CloneEbayDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        public async Task<OrderTable?> GetOrderTableByBuyerId(int id)
        {
            return await _dbContext.OrderTables.FirstOrDefaultAsync(ot => ot.BuyerId == id);
        }
        public async Task<OrderTable?> GetUnpaidOrderTableByBuyerId(int id)
        {
            return await _dbContext.OrderTables.FirstOrDefaultAsync(ot => ot.BuyerId == id && ot.Status == "Unpaid");
        }
        public async Task CreateOrderTableAsync(OrderTable order)
        {
            await _dbContext.OrderTables.AddAsync(order);
            await _dbContext.SaveChangesAsync();
        }

        public async Task UpdateOrderTableAsync(OrderTable order)
        {
            _dbContext.OrderTables.Update(order);
            await _dbContext.SaveChangesAsync();
        }

    }
}
