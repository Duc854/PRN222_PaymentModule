namespace PaymentModule.Business.Abstraction
{
    // DTO cho response khi tạo vận đơn
    public class CreateShipmentResponseDto
    {
        public bool Success { get; set; }
        public string TrackingNumber { get; set; }
        public string Carrier { get; set; }
        public string InitialStatus { get; set; }
        public DateTime? EstimatedArrival { get; set; }
        public string Message { get; set; }
    }

    // DTO cho response khi cập nhật trạng thái
    public class ShipmentStatusUpdateDto
    {
        public bool Success { get; set; }
        public string NewStatus { get; set; }
        public DateTime? EstimatedArrival { get; set; }
        public string Message { get; set; }
    }

    public interface IShippingService
    {
        /// <summary>
        /// Gọi API sandbox để tạo mã vận đơn (trackingNumber)
        /// </summary>
        Task<CreateShipmentResponseDto> CreateShipmentAsync(int orderId, string buyerName, string fullAddress);

        /// <summary>
        /// Gọi API sandbox để kiểm tra và cập nhật trạng thái
        /// </summary>
        Task<ShipmentStatusUpdateDto> GetAndUpdateShipmentStatusAsync(string trackingNumber);
    }
}