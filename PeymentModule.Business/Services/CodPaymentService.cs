using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PaymentModule.Business.Abstractions;
using PaymentModule.Data;
using PaymentModule.Data.Entities;

namespace PaymentModule.Business.Services
{
    public class CodPaymentService : ICodPaymentService
    {
        private readonly CloneEbayDbContext _context;

        public CodPaymentService(CloneEbayDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// ✅ Lấy danh sách đơn COD thuộc về Seller hiện tại
        /// </summary>
        public async Task<IEnumerable<OrderTable>> GetCodOrdersBySellerAsync(int sellerId)
        {
            return await _context.OrderTables
                .Include(o => o.Buyer)
                .Include(o => o.Payments)
                .Include(o => o.OrderItems)
                    .ThenInclude(i => i.Product)
                .Where(o =>
                    o.OrderItems.Any(i => i.Product.SellerId == sellerId) &&
                    o.Payments.Any(p => p.Method == "COD"))
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }

        /// <summary>
        /// ✅ Lấy chi tiết 1 đơn COD cụ thể (kiểm tra quyền Seller)
        /// </summary>
        public async Task<OrderTable?> GetCodOrderDetailAsync(int orderId, int sellerId)
        {
            var order = await _context.OrderTables
                .Include(o => o.Buyer)
                .Include(o => o.Address)
                .Include(o => o.Payments)
                .Include(o => o.OrderItems)
                    .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(o =>
                    o.Id == orderId &&
                    o.OrderItems.Any(i => i.Product.SellerId == sellerId));

            return order;
        }

        /// <summary>
        /// ✅ Seller xác nhận đơn hàng (bắt đầu giao hàng)
        /// </summary>
        public async Task<bool> ConfirmOrderAsync(int orderId, int sellerId)
        {
            var order = await GetCodOrderDetailAsync(orderId, sellerId);
            if (order == null) return false;

            order.Status = "Confirmed";
            await _context.SaveChangesAsync();

            // Tạo Shipping Info giả định
            var shipping = new ShippingInfo
            {
                OrderId = order.Id,
                Carrier = "Standard Delivery",
                TrackingNumber = $"SHIP-{Guid.NewGuid():N}".Substring(0, 10),
                Status = "Shipping",
                EstimatedArrival = DateTime.Now.AddDays(3)
            };

            _context.ShippingInfos.Add(shipping);
            await _context.SaveChangesAsync();

            return true;
        }

        /// <summary>
        /// ✅ Seller đánh dấu đơn đã giao hàng
        /// </summary>
        public async Task<bool> MarkAsDeliveredAsync(int orderId, int sellerId)
        {
            var order = await GetCodOrderDetailAsync(orderId, sellerId);
            if (order == null) return false;

            var shipping = await _context.ShippingInfos.FirstOrDefaultAsync(s => s.OrderId == orderId);
            if (shipping == null) return false;

            shipping.Status = "Delivered";
            order.Status = "Delivered";

            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// ✅ Seller xác nhận người mua đã trả tiền COD
        /// </summary>
        public async Task<bool> VerifyCodPaymentAsync(int orderId, int sellerId)
        {
            var order = await GetCodOrderDetailAsync(orderId, sellerId);
            if (order == null) return false;

            var payment = await _context.Payments.FirstOrDefaultAsync(p => p.OrderId == orderId);
            if (payment == null) return false;

            if (order.Status != "Delivered")
                throw new InvalidOperationException("Không thể xác nhận COD vì đơn hàng chưa được giao.");

            payment.Status = "Paid";
            payment.PaidAt = DateTime.Now;
            order.Status = "Completed";

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
