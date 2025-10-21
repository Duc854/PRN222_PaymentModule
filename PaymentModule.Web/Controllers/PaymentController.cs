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
                    // 1. ĐỌC TẤT CẢ SESSION CẦN DÙNG
                    var userId = HttpContext.Session.GetInt32("UserId");
                    var addressId = HttpContext.Session.GetInt32("AddressId");
                    var totalStr = HttpContext.Session.GetString("Total") ?? "0";
                    var fullName = HttpContext.Session.GetString("FullName") ?? "";
                    decimal.TryParse(totalStr, out decimal total);

                    // 2. LẤY ĐƠN HÀNG "UNPAID"
                    var order = _context.OrderTables
                        .FirstOrDefault(o => o.BuyerId == userId && o.Status == "Unpaid");

                    // 3. CẬP NHẬT ĐƠN HÀNG
                    if (order != null && addressId != null && addressId.Value > 0)
                    {
                        order.Status = "Processing";
                        order.OrderDate = DateTime.Now;
                        order.AddressId = addressId.Value;
                        // Cập nhật tổng tiền cuối cùng (nếu logic của bạn đã cộng phí ship trong 'Total' ở Session)
                        order.TotalPrice = total;
                        _context.OrderTables.Update(order);
                    }
                    else
                    {
                        _logger.LogError("Lỗi thanh toán PayPal: Không tìm thấy Order, AddressId hoặc UserId. UserId: {uid}, AddressId: {aid}", userId, addressId);
                        return BadRequest("Lỗi xử lý đơn hàng, không tìm thấy địa chỉ hoặc người dùng.");
                    }

                    // 4. GHI LẠI LOGIC THANH TOÁN
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
                            TransactionId = executedPayment.id // <-- Lưu TransactionId
                        };
                        _context.Payments.Add(payment);
                    }

                    _context.SaveChanges(); // <-- Lưu Order và Payment vào DB

                    // 5. GỌI API VẬN CHUYỂN
                    try
                    {
                        await _orderTableService.CreateShipmentForOrderAsync(order.Id);
                    }
                    catch (Exception ex)
                    {
                        // Ghi log lỗi nhưng không chặn người dùng
                        _logger.LogError(ex, "Lỗi khi gọi API vận chuyển cho Order {OrderId} (PayPal)", order.Id);
                    }

                    // 6. "LÀM MỚI" (RE-SET) TOÀN BỘ SESSION TRƯỚC KHI REDIRECT
                    // Đây là bước quan trọng nhất để fix lỗi
                    HttpContext.Session.Remove("UserCart");
                    HttpContext.Session.SetString("PaymentProcessed", "true"); // Flag cho luồng PayPal
                    HttpContext.Session.SetInt32("UserId", userId.Value); // Ép lưu lại UserId
                    HttpContext.Session.SetInt32("AddressId", addressId.Value); // Ép lưu lại AddressId
                    HttpContext.Session.SetString("Total", totalStr); // Ép lưu lại Total
                    HttpContext.Session.SetString("FullName", fullName); // Ép lưu lại FullName

                    // 7. CHUYỂN ĐẾN TRANG CẢM ƠN (với OrderId)
                    return RedirectToAction("PaymentSuccess", "Order", new { orderId = order.Id });
                }

                return Content("❌ Thanh toán thất bại.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi nghiêm trọng trong Payment/Success (PayPal)");
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
