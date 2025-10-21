using System.Text.Json;
using Microsoft.Extensions.Logging;
using PaymentModule.Business.Abstraction;
using PaymentModule.Business.Exception;
using PaymentModule.Data.Entities;

namespace PaymentModule.Business.Services
{
    public class MockShippingService : IShippingService
    {
        private readonly ILogger<MockShippingService> _logger;

        // Database giả lập (lưu trạng thái của mã vận đơn)
        private static Dictionary<string, string> _mockDatabase = new Dictionary<string, string>();

        public MockShippingService(ILogger<MockShippingService> logger)
        {
            _logger = logger;
        }

        public async Task<CreateShipmentResponseDto> CreateShipmentAsync(int orderId, string buyerName, string fullAddress)
        {
            var requestPayload = JsonSerializer.Serialize(new { orderId, buyerName, fullAddress });
            _logger.LogInformation($"[Shipping API] Gọi CreateShipmentAsync cho Order: {orderId}...");
            _logger.LogInformation($"[Shipping API] Request: {requestPayload}");

            await Task.Delay(TimeSpan.FromSeconds(1)); // Giả lập chờ API

            // === GIẢ LẬP LỖI ===
            if (new Random().Next(0, 5) == 0) // 1/5 lỗi
            {
                _logger.LogError($"[Shipping API] Lỗi khi tạo vận đơn cho Order: {orderId}. Giả lập lỗi 500.");
                throw new ShippingApiException("API Sandbox Error: 500 Internal Server Error. Failed to create label.");
            }

            // === GIẢ LẬP THÀNH CÔNG ===
            var trackingNumber = $"SHIP{DateTime.UtcNow:yyyyMMdd}{orderId:D4}";
            var initialStatus = ShippingStatusConstants.Processing;

            _mockDatabase[trackingNumber] = initialStatus;

            var response = new CreateShipmentResponseDto
            {
                Success = true,
                TrackingNumber = trackingNumber,
                Carrier = "Sandbox Express",
                InitialStatus = initialStatus,
                EstimatedArrival = DateTime.UtcNow.AddDays(5),
                Message = $"[Sandbox] Shipment created successfully. Status: {initialStatus}"
            };

            _logger.LogInformation($"[Shipping API] Response: {JsonSerializer.Serialize(response)}");
            return response;
        }

        public async Task<ShipmentStatusUpdateDto> GetAndUpdateShipmentStatusAsync(string trackingNumber)
        {
            _logger.LogInformation($"[Shipping API] Gọi GetAndUpdateShipmentStatusAsync cho mã: {trackingNumber}...");
            await Task.Delay(TimeSpan.FromMilliseconds(500));

            if (!_mockDatabase.ContainsKey(trackingNumber))
            {
                throw new ShippingApiException($"API Sandbox Error: 404 Not Found. Tracking number {trackingNumber} does not exist.");
            }

            // Giả lập trạng thái tự động thay đổi
            var currentStatus = _mockDatabase[trackingNumber];
            string newStatus = currentStatus;

            if (currentStatus == ShippingStatusConstants.Processing)
                newStatus = ShippingStatusConstants.InTransit;
            else if (currentStatus == ShippingStatusConstants.InTransit)
                newStatus = ShippingStatusConstants.Delivered;

            _mockDatabase[trackingNumber] = newStatus;

            var response = new ShipmentStatusUpdateDto
            {
                Success = true,
                NewStatus = newStatus,
                EstimatedArrival = (newStatus == ShippingStatusConstants.Delivered) ? DateTime.UtcNow : DateTime.UtcNow.AddDays(2),
                Message = $"[Sandbox] Status updated from {currentStatus} to {newStatus}."
            };

            _logger.LogInformation($"[Shipping API] Response: {JsonSerializer.Serialize(response)}");
            return response;
        }
    }
}