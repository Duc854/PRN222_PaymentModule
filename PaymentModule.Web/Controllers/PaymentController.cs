using Microsoft.AspNetCore.Mvc;
using PaymentModule.Business.Services;
using PaymentModule.Data;
using PayPal.Api;
using System;
using System.Linq;

// Alias để tránh nhầm giữa PayPalPayment và Payment Entity
using PayPalPayment = PayPal.Api.Payment;

namespace PaymentModule.Web.Controllers
{
    [Route("[controller]/[action]")]
    public class PaymentController : Controller
    {
        private readonly PayPalService _paypalService;

        public PaymentController(PayPalService paypalService)
        {
            _paypalService = paypalService;
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
        public IActionResult Success(string paymentId, string token, string PayerID, [FromServices] CloneEbayDbContext _context)
        {
            try
            {
                var executedPayment = _paypalService.ExecutePayment(paymentId, PayerID);

                if (executedPayment.state.ToLower() == "approved")
                {
                    var userId = HttpContext.Session.GetInt32("UserId");
                    var totalStr = HttpContext.Session.GetString("Total");
                    decimal.TryParse(totalStr, out decimal total);

                    // ✅ Lấy đơn hàng "Unpaid" hiện tại
                    var order = _context.OrderTables
                        .FirstOrDefault(o => o.BuyerId == userId && o.Status == "Unpaid");

                    if (order != null)
                    {
                        order.Status = "Paid";
                        order.OrderDate = DateTime.Now;
                        _context.OrderTables.Update(order);
                    }

                    // ✅ Ghi Payment bản duy nhất
                    var exists = _context.Payments.Any(p => p.TransactionId == executedPayment.id);
                    if (!exists)
                    {
                        var entity = new PaymentModule.Data.Entities.Payment
                        {
                            OrderId = order?.Id,
                            UserId = userId ?? 0,
                            Amount = total,
                            Method = "PayPal",
                            Status = "Completed",
                            PaidAt = DateTime.Now,
                            TransactionId = executedPayment.id
                        };
                        _context.Payments.Add(entity);
                    }

                    _context.SaveChanges();
                    HttpContext.Session.Remove("UserCart");

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
