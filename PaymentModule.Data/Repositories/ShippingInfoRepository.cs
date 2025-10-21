using Microsoft.EntityFrameworkCore;
using PaymentModule.Data.Abstractions;
using PaymentModule.Data.Entities;

namespace PaymentModule.Data.Repositories
{
    public class ShippingInfoRepository : IShippingInfoRepository
    {
        private readonly CloneEbayDbContext _context;

        public ShippingInfoRepository(CloneEbayDbContext context)
        {
            _context = context;
        }

        public async Task<ShippingInfo> CreateAsync(ShippingInfo shippingInfo)
        {
            await _context.ShippingInfos.AddAsync(shippingInfo);
            await _context.SaveChangesAsync();
            return shippingInfo;
        }

        public async Task<ShippingInfo> GetByOrderIdAsync(int orderId)
        {
            return await _context.ShippingInfos
                .FirstOrDefaultAsync(s => s.OrderId == orderId);
        }

        public async Task<ShippingInfo> GetByTrackingNumberAsync(string trackingNumber)
        {
            return await _context.ShippingInfos
                .FirstOrDefaultAsync(s => s.TrackingNumber == trackingNumber);
        }

        public async Task UpdateStatusAsync(int shippingInfoId, string newStatus)
        {
            var shippingInfo = await _context.ShippingInfos.FindAsync(shippingInfoId);
            if (shippingInfo != null)
            {
                shippingInfo.Status = newStatus;
                await _context.SaveChangesAsync();
            }
        }
    }
}