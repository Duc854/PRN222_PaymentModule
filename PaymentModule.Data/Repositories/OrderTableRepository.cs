using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PaymentModule.Data.Abstractions;
using PaymentModule.Data.Entities;

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

        public async Task<OrderTable> GetOrderTableByIdAsync(int orderId)
        {
            return await _dbContext.OrderTables
                                 .Include(o => o.Buyer)
                                 .Include(o => o.Address)
                                 .FirstOrDefaultAsync(o => o.Id == orderId);
        }

        public async Task<List<OrderTable>> GetOrderHistoryByBuyerIdAsync(int buyerId)
        {
            return await _dbContext.OrderTables
                .Include(o => o.ShippingInfos) // Tự động lấy ShippingInfo kèm theo
                .Where(o => o.BuyerId == buyerId && o.Status != "Unpaid") // Chỉ lấy đơn đã thanh toán
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }

        public async Task<OrderTable> GetOrderDetailsAsync(int orderId)
        {
            return await _dbContext.OrderTables
                .Include(o => o.ShippingInfos) // Tự động lấy ShippingInfo kèm theo
                .FirstOrDefaultAsync(o => o.Id == orderId);
        }
    }
}
