using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PaymentModule.Business.Abstraction;
using PaymentModule.Business.Exceptions;
using PaymentModule.Data.Abstractions;
using PaymentModule.Data.Entities;

namespace PaymentModule.Business.Services
{
    public class MockShippingService : IShippingService
    {
        private readonly ILogger<MockShippingService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IShippingInfoRepository _shippingInfoRepo;
        private const string WebhookUrl = "http://localhost:5098/api/shipping/webhook";

        public MockShippingService(
            ILogger<MockShippingService> logger,
            IHttpClientFactory httpClientFactory,
            IShippingInfoRepository shippingInfoRepo
        )
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _shippingInfoRepo = shippingInfoRepo;
        }

        public async Task<CreateShipmentResponseDto> CreateShipmentAsync(int orderId, string buyerName, string fullAddress)
        {
            _logger.LogInformation($"[Shipping API] Gọi CreateShipmentAsync cho Order: {orderId}...");

            await Task.Delay(TimeSpan.FromSeconds(1)); // Giả lập chờ API

            if (new Random().Next(0, 5) == 0) // 1/5 lỗi
            {
                _logger.LogError($"[Shipping API] Lỗi khi tạo vận đơn cho Order: {orderId}. Giả lập lỗi 500.");
                throw new ShippingApiException("API Sandbox Error: 500 Internal Server Error. Failed to create label.");
            }

            var trackingNumber = $"SHIP{DateTime.UtcNow:yyyyMMdd}{orderId:D4}";
            var initialStatus = ShippingStatusConstants.Processing;

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

            _ = Task.Run(() => SimulateShipmentLifecycle(trackingNumber));

            return response;
        }

        private async Task SimulateShipmentLifecycle(string trackingNumber)
        {
            _logger.LogInformation($"[Simulator] Bắt đầu mô phỏng cho {trackingNumber}...");
            try
            {
                // === (60 giây) ===
                await Task.Delay(TimeSpan.FromSeconds(20));
                await SendWebhookUpdate(trackingNumber, ShippingStatusConstants.InTransit, "Hàng đã rời kho, đang trên đường vận chuyển.");

                // Đợi tiếp 60s
                await Task.Delay(TimeSpan.FromSeconds(20));
                await SendWebhookUpdate(trackingNumber, ShippingStatusConstants.OutForDelivery, "Tài xế đang trên đường giao hàng đến bạn.");

                // Đợi tiếp 30s
                await Task.Delay(TimeSpan.FromSeconds(10));
                await SendWebhookUpdate(trackingNumber, ShippingStatusConstants.Delivered, "Giao hàng thành công.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[Simulator] Lỗi khi mô phỏng {trackingNumber}.");
            }
        }

        public async Task<RerouteResponseDto> RequestRerouteAsync(string trackingNumber, string newAddressNotes)
        {
            var shippingInfo = await _shippingInfoRepo.GetByTrackingNumberAsync(trackingNumber);
            if (shippingInfo == null)
                return new RerouteResponseDto { Success = false, Message = "Không tìm thấy mã vận đơn." };

            // === [FIX] KIỂM TRA: NẾU ĐÃ GIAO HÀNG THÌ KHÔNG CHO ĐỔI ===
            if (shippingInfo.Status == ShippingStatusConstants.Delivered)
            {
                return new RerouteResponseDto
                {
                    Success = false,
                    Message = "Đơn hàng ĐÃ GIAO THÀNH CÔNG. Bạn không thể thay đổi địa chỉ lúc này."
                };
            }
            // ===========================================================

            string message = "";
            string statusDetail = "";

            // === XỬ LÝ LOGIC PHÍ $5 (LẦN 2) ===
            if (shippingInfo.ReroutedOnce)
            {
                message = $"Đổi địa chỉ thành công (LẦN 2). Phí thay đổi $5 đã được tính.";
                statusDetail = $"Reroute Lần 2 (Phí $5): {newAddressNotes}";
            }
            else
            {
                message = "Đổi địa chỉ thành công (Miễn phí lần đầu).";
                statusDetail = $"Reroute Lần 1: {newAddressNotes}";
                shippingInfo.ReroutedOnce = true;
            }

            // Cập nhật DB ngay lập tức
            shippingInfo.Notes = statusDetail;
            // Quan trọng: Status chuyển sang InTransit_Rerouted để quy trình tiếp tục
            shippingInfo.Status = ShippingStatusConstants.InTransit_Rerouted;
            await _shippingInfoRepo.UpdateAsync(shippingInfo);

            // Gửi Webhook để đồng bộ sang OrderTable (nếu cần thiết)
            await SendWebhookUpdate(trackingNumber, ShippingStatusConstants.InTransit_Rerouted, statusDetail);

            return new RerouteResponseDto
            {
                Success = true,
                Message = message,
                NewStatus = ShippingStatusConstants.InTransit_Rerouted
            };
        }

        /// <summary>
        /// (Hàm mới) Gửi một bản tin (payload) đến endpoint Webhook.
        /// </summary>
        private async Task SendWebhookUpdate(string trackingNumber, string newStatus, string message)
        {
            var payload = new
            {
                TrackingNumber = trackingNumber,
                NewStatus = newStatus,
                Message = message,
                Timestamp = DateTime.UtcNow
            };

            var jsonPayload = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            _logger.LogInformation($"[Simulator] Gửi Webhook cho {trackingNumber}: {newStatus}");

            try
            {
                // Bỏ qua kiểm tra chứng chỉ SSL (chỉ cho DEV localhost)
                var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
                };
                var client = new HttpClient(handler);

                var response = await client.PostAsync(WebhookUrl, content);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning($"[Simulator] Webhook gửi đi thất bại (Code: {response.StatusCode}). Endpoint của bạn có đang chạy không?");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[Simulator] Lỗi nghiêm trọng khi gọi Webhook: {ex.Message}. Đảm bảo URL '{WebhookUrl}' là đúng và ứng dụng đang chạy.");
            }
        }
    }
}