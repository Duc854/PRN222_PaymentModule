using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PaymentModule.Business.Abstractions;
using PaymentModule.Business.Dtos.InputDtos;

namespace PaymentModule.Web.Controllers
{
    [ApiController]
    [Route("api/shipping")]
    public class WebhookController : ControllerBase
    {
        private readonly IOrderTableService _orderTableService;
        private readonly ILogger<WebhookController> _logger;

        public WebhookController(
            IOrderTableService orderTableService,
            ILogger<WebhookController> logger)
        {
            _orderTableService = orderTableService;
            _logger = logger;
        }

        /// <summary>
        /// Endpoint này được gọi bởi Simulator (ĐVVC)
        /// </summary>
        [HttpPost("webhook")]
        public async Task<IActionResult> HandleShippingUpdate([FromBody] ShippingWebhookPayload payload)
        {
            if (payload == null || string.IsNullOrEmpty(payload.TrackingNumber))
            {
                _logger.LogWarning("[Webhook] Nhận được payload không hợp lệ.");
                return BadRequest("Invalid payload.");
            }

            _logger.LogInformation($"[Webhook] Nhận được cập nhật cho {payload.TrackingNumber}: {payload.NewStatus}");

            try
            {
                // Gọi service để cập nhật CSDL
                await _orderTableService.UpdateOrderStatusFromWebhookAsync(
                    payload.TrackingNumber,
                    payload.NewStatus,
                    payload.Message
                );

                // Phải trả về 200 OK ngay lập tức
                return Ok();
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, $"[Webhook] Lỗi khi xử lý {payload.TrackingNumber}.");
                return StatusCode(500, "Internal server error processing webhook.");
            }
        }
    }

    public class ShippingWebhookPayload
    {
        public string TrackingNumber { get; set; }
        public string NewStatus { get; set; }
        public string Message { get; set; }
        public System.DateTime Timestamp { get; set; }
    }
}