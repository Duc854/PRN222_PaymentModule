using System;
using System.Threading.Tasks;

namespace PaymentModule.Business.Abstraction
{
    // DTO này giữ nguyên
    public class CreateShipmentResponseDto
    {
        public bool Success { get; set; }
        public string TrackingNumber { get; set; }
        public string Carrier { get; set; }
        public string InitialStatus { get; set; }
        public DateTime? EstimatedArrival { get; set; }
        public string Message { get; set; }
    }

    // === DTO MỚI (Cho Reroute) ===
    public class RerouteResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string NewStatus { get; set; }
    }

    // === DTO CŨ (Bị xóa) ===
    // public class ShipmentStatusUpdateDto { ... }

    public interface IShippingService
    {
        /// <summary>
        /// Gọi API sandbox để tạo mã vận đơn (trackingNumber)
        /// (Hàm này cũng sẽ kích hoạt tiến trình Webhook giả lập)
        /// </summary>
        Task<CreateShipmentResponseDto> CreateShipmentAsync(int orderId, string buyerName, string fullAddress);

        /// <summary>
        /// (Hàm mới) Được gọi từ "Cổng ĐVVC" để mô phỏng yêu cầu Reroute.
        /// </summary>
        Task<RerouteResponseDto> RequestRerouteAsync(string trackingNumber, string newAddressNotes);


        // === HÀM CŨ (Bị xóa) ===
        // Task<ShipmentStatusUpdateDto> GetAndUpdateShipmentStatusAsync(string trackingNumber);
    }
}