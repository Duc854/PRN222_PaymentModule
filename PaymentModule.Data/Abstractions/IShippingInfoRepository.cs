using PaymentModule.Data.Entities;

namespace PaymentModule.Data.Abstractions
{
    public interface IShippingInfoRepository
    {
        Task<ShippingInfo> CreateAsync(ShippingInfo shippingInfo);
        Task<ShippingInfo> GetByOrderIdAsync(int orderId);
        Task<ShippingInfo> GetByTrackingNumberAsync(string trackingNumber);
        Task UpdateStatusAsync(int shippingInfoId, string newStatus);
    }
}