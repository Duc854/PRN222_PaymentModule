using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using PaymentModule.Business.Abstractions;
using PaymentModule.Business.Services;
using PaymentModule.Data;
using PayPal.Api;
// Alias để tránh nhầm giữa PayPalPayment và Payment Entity
using PayPalPayment = PayPal.Api.Payment;

namespace PaymentModule.Web.Controllers
{
    [Route("[controller]/[action]")]
    public class PaymentController : Controller
    {
        private readonly PayPalService _paypalService;
        private readonly IOrderTableService _orderTableService;
        private readonly ILogger<PaymentController> _logger;
        private readonly CloneEbayDbContext _context;

        public PaymentController(PayPalService paypalService, IOrderTableService orderTableService, ILogger<PaymentController> logger, CloneEbayDbContext context)
        {
            _paypalService = paypalService;
            _orderTableService = orderTableService;
            _logger = logger;
            _context = context;
        }

        // Bắt đầu tạo thanh toán
        [HttpGet]
        public IActionResult Create(decimal amount)
        {
            try
            {
                var payment = _paypalService.CreatePayment(amount);
                var approvalUrl = payment.links.FirstOrDefault(
                    x => x.rel.Equals("approval_url", StringComparison.OrdinalIgnoreCase))?.href;

                if (approvalUrl != null)
                    return Redirect(approvalUrl);

                return BadRequest("Không thể tạo thanh toán PayPal.");
            }
            catch (Exception ex)
            {
                return BadRequest($"Lỗi khi tạo thanh toán: {ex.Message}");
            }
        }

        // PayPal redirect về đây khi thanh toán thành công
        [HttpGet]
        public async Task<IActionResult> Success(string paymentId, string token, string PayerID)
        {
            try
            {
                var executedPayment = _paypalService.ExecutePayment(paymentId, PayerID);

                if (executedPayment.state.ToLower() == "approved")
                {
                    var userId = HttpContext.Session.GetInt32("UserId");
                    var addressId = HttpContext.Session.GetInt32("AddressId");
                    var totalStr = HttpContext.Session.GetString("Total");
                    decimal.TryParse(totalStr, out decimal total);

                    // ✅ Lấy đơn hàng "Unpaid" hiện tại 
                    var order = _context.OrderTables
                        .FirstOrDefault(o => o.BuyerId == userId && o.Status == "Unpaid");

                    if (order != null && addressId != null && addressId.Value > 0)
                    {
                        order.Status = "Processing";
                        order.OrderDate = DateTime.Now;
                        order.AddressId = addressId.Value;
                        _context.OrderTables.Update(order);
                    }

                    // ... (Logic ghi Payment của bạn) ...
                    var exists = _context.Payments.Any(p => p.TransactionId == executedPayment.id);
                    if (!exists && order != null) { /* ... */ }

                    _context.SaveChanges(); // <-- Lưu đơn hàng & thanh toán

                    // ✅ GỌI API VẬN CHUYỂN
                    if (order != null)
                    {
                        try
                        {
                            await _orderTableService.CreateShipmentForOrderAsync(order.Id);
                        }
                        catch (Exception ex)
                        {
                            // Ghi log lỗi nhưng không chặn người dùng
                            _logger.LogError(ex, "Lỗi khi gọi API vận chuyển cho Order {OrderId} (PayPal)", order.Id);
                        }
                    }

                    HttpContext.Session.Remove("UserCart");

                    // Chuyển đến trang Cảm ơn (nơi sẽ gửi mail)
                    return RedirectToAction("PaymentSuccess", "Order");
                }

                return Content("❌ Thanh toán thất bại.");
            }
            catch (Exception ex)
            {
                return BadRequest($"Lỗi khi xác nhận thanh toán: {ex.Message}");
            }
        }


        // PayPal redirect về đây khi hủy thanh toán
        [HttpGet]
        public IActionResult Cancel()
        {
            return Content("⚠️ Thanh toán đã bị hủy.");
        }
    }
}
