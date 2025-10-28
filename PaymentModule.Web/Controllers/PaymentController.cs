using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PaymentModule.Business.Abstractions;
using PaymentModule.Business.Services;
using PaymentModule.Data;
using PayPal.Api;
using PaymentModule.Business.Exceptions;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

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

        public PaymentController(
            PayPalService paypalService,
            IOrderTableService orderTableService,
            ILogger<PaymentController> logger,
            CloneEbayDbContext context)
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
                _logger.LogError(ex, "❌ Lỗi khi tạo thanh toán PayPal.");
                throw new PaymentException("Không thể tạo thanh toán PayPal.", null);
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
                    var totalStr = HttpContext.Session.GetString("Total") ?? "0";
                    var fullName = HttpContext.Session.GetString("FullName") ?? "";
                    decimal.TryParse(totalStr, out decimal total);

                    var order = _context.OrderTables
                        .FirstOrDefault(o => o.BuyerId == userId && o.Status == "Unpaid");

                    if (order == null || addressId == null)
                    {
                        _logger.LogError("Lỗi thanh toán PayPal: Không tìm thấy Order, AddressId hoặc UserId. UserId: {uid}, AddressId: {aid}",
                            userId, addressId);
                        throw new PaymentException("Không tìm thấy đơn hàng hoặc địa chỉ giao hàng.", paymentId);
                    }

                    order.Status = "Processing";
                    order.OrderDate = DateTime.Now;
                    order.AddressId = addressId.Value;
                    order.TotalPrice = total;
                    _context.OrderTables.Update(order);

                    var exists = _context.Payments.Any(p => p.TransactionId == executedPayment.id);
                    if (!exists)
                    {
                        var payment = new PaymentModule.Data.Entities.Payment
                        {
                            OrderId = order.Id,
                            UserId = userId.Value,
                            Amount = total,
                            Method = "PayPal",
                            Status = "Completed",
                            PaidAt = DateTime.Now,
                            TransactionId = executedPayment.id
                        };
                        _context.Payments.Add(payment);
                    }

                    _context.SaveChanges();

                    // ✅ Log TransactionId khi thanh toán thành công
                    _logger.LogInformation(
                        "✅ Thanh toán PayPal thành công. TransactionId: {TransactionId}, OrderId: {OrderId}, UserId: {UserId}, Amount: {Amount}",
                        executedPayment.id, order.Id, userId, total);

                    try
                    {
                        await _orderTableService.CreateShipmentForOrderAsync(order.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "🚚 Lỗi khi gọi API vận chuyển cho Order {OrderId} (PayPal)", order.Id);
                    }

                    HttpContext.Session.Remove("UserCart");
                    HttpContext.Session.SetString("PaymentProcessed", "true");
                    HttpContext.Session.SetInt32("UserId", userId.Value);
                    HttpContext.Session.SetInt32("AddressId", addressId.Value);
                    HttpContext.Session.SetString("Total", totalStr);
                    HttpContext.Session.SetString("FullName", fullName);

                    return RedirectToAction("PaymentSuccess", "Order", new { orderId = order.Id });
                }

                _logger.LogWarning("⚠️ Thanh toán PayPal không được phê duyệt. PaymentId: {PaymentId}", paymentId);
                return Content("❌ Thanh toán thất bại.");
            }
            catch (PaymentException pex)
            {
                _logger.LogError(pex,
                    "💳 PaymentException trong Payment/Success. TransactionId: {TransactionId}", pex.TransactionID);
                throw; // Middleware sẽ bắt và ghi log file
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "🔥 Lỗi nghiêm trọng trong Payment/Success. PaymentId: {PaymentId}", paymentId);
                throw new PaymentException("Lỗi khi xác nhận thanh toán.", paymentId);
            }
        }

        [HttpGet]
        public IActionResult Cancel()
        {
            _logger.LogInformation("⚠️ Người dùng đã hủy thanh toán PayPal.");
            return Content("⚠️ Thanh toán đã bị hủy.");
        }
    }
}
