using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using PaymentModule.Business.Abstractions;
using PaymentModule.Business.Dtos.OutputDtos;
using PaymentModule.Business.Services;
using PaymentModule.Data;

namespace PaymentModule.Web.Controllers
{
    [Route("order")]
    public class OrderController : Controller
    {
        private readonly CloneEbayDbContext _context;
        private readonly IEmailService _emailService;

        public OrderController(CloneEbayDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        // ✅ Nhận dữ liệu từ Checkout và lưu vào Session
        [HttpPost("ConfirmFromCheckout")]
        public IActionResult ConfirmFromCheckout([FromBody] JsonElement data)
        {
            if (data.ValueKind != JsonValueKind.Object)
                return BadRequest("Invalid checkout data.");

            HttpContext.Session.SetString("Subtotal", data.GetProperty("subtotal").GetDecimal().ToString());
            HttpContext.Session.SetString("Shipping", data.GetProperty("shipping").GetDecimal().ToString());
            HttpContext.Session.SetString("Discount", data.GetProperty("discount").GetDecimal().ToString());
            HttpContext.Session.SetString("Total", data.GetProperty("total").GetDecimal().ToString());

            if (data.TryGetProperty("addressId", out var idProp) && idProp.GetInt32() > 0)
            {
                HttpContext.Session.SetInt32("AddressId", idProp.GetInt32());
            }

            if (data.TryGetProperty("coupon", out var couponProp))
                HttpContext.Session.SetString("Coupon", couponProp.GetString() ?? "");

            if (data.TryGetProperty("paymentMethod", out var paymentProp))
                HttpContext.Session.SetString("PaymentMethod", paymentProp.GetString() ?? "COD");

            if (data.TryGetProperty("address", out var addrProp))
            {
                HttpContext.Session.SetString("FullName", addrProp.GetProperty("fullName").GetString() ?? "");
                HttpContext.Session.SetString("Street", addrProp.GetProperty("street").GetString() ?? "");
                HttpContext.Session.SetString("CityLine", addrProp.GetProperty("cityLine").GetString() ?? "");
                HttpContext.Session.SetString("Country", addrProp.GetProperty("country").GetString() ?? "");
                HttpContext.Session.SetString("Phone", addrProp.GetProperty("phone").GetString() ?? "");
            }


            return Ok();
        }

        [HttpGet("confirm")]
        public IActionResult Confirm()
        {
            var cartJson = HttpContext.Session.GetString("UserCart");
            if (string.IsNullOrEmpty(cartJson))
                return RedirectToAction("Checkout", "Cart");

            var cart = JsonSerializer.Deserialize<UserCartOutputDto>(cartJson);

            var fullName = HttpContext.Session.GetString("FullName");
            dynamic address = null;

            if (!string.IsNullOrEmpty(fullName))
            {
                address = new
                {
                    FullName = fullName,
                    Street = HttpContext.Session.GetString("Street"),
                    City = HttpContext.Session.GetString("CityLine"),
                    Country = HttpContext.Session.GetString("Country"),
                    Phone = HttpContext.Session.GetString("Phone")
                };
            }
            else
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                address = _context.Addresses.FirstOrDefault(a => a.UserId == userId && a.IsDefault == true);
            }

            ViewBag.Subtotal = HttpContext.Session.GetString("Subtotal");
            ViewBag.Shipping = HttpContext.Session.GetString("Shipping");
            ViewBag.Discount = HttpContext.Session.GetString("Discount");
            ViewBag.Total = HttpContext.Session.GetString("Total");
            ViewBag.Coupon = HttpContext.Session.GetString("Coupon");
            ViewBag.Address = address;

            // ✅ Thêm dòng này
            ViewBag.PaymentMethod = HttpContext.Session.GetString("PaymentMethod") ?? "COD";

            return View(cart);
        }


        [HttpGet("PaymentSuccess")]
        public async Task<IActionResult> PaymentSuccess(
            [FromServices] PaymentModule.Business.Abstractions.IOrderTableService orderTableService,
            [FromServices] ILogger<OrderController> logger) // <-- Thêm Logger
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var addressId = HttpContext.Session.GetInt32("AddressId");
            var user = await _context.Users.FindAsync(userId);

            if (userId != null && addressId != null && addressId.Value > 0)
            {
                // ✅ 1. Chuyển đơn hàng "Unpaid" → "Paid"
                var completedOrder = await orderTableService.CompleteOrderAsync(userId.Value, addressId.Value, "COD");

                if (completedOrder == null)
                {
                    logger.LogWarning("Không tìm thấy đơn hàng Unpaid để hoàn tất cho User {UserId}", userId);
                    return RedirectToAction("Index", "Home");
                }

                // ✅ 2. GỌI API VẬN CHUYỂN 
                try
                {
                    await orderTableService.CreateShipmentForOrderAsync(completedOrder.Id);
                }
                catch (Exception ex)
                {
                    // Ghi log lỗi nhưng không chặn người dùng
                    logger.LogError(ex, "Lỗi khi gọi API vận chuyển cho Order {OrderId} (COD)", completedOrder.Id);
                    // Dù API vận chuyển lỗi, đơn hàng vẫn thành công, email vẫn gửi
                }

                // ✅ 3. Gửi mail xác nhận
                var total = HttpContext.Session.GetString("Total") ?? "0";
                var fullName = HttpContext.Session.GetString("FullName") ?? "Khách hàng";
                var email = user?.Email;

                // Sửa lại Subject để dùng Order ID thật
                string subject = $"[CloneEbay] Thanh toán thành công – Mã đơn hàng #{completedOrder.Id}";
                string body = $@"
                            <h2>Cảm ơn {fullName} đã đặt hàng!</h2>
                            <p>Đơn hàng của bạn (mã #{completedOrder.Id}) đã được xác nhận.</p>
                            <p><b>Tổng tiền:</b> ${total}</p>
                            <p>Phương thức thanh toán: Thanh toán khi nhận hàng (COD)</p>
                            <p>Chúng tôi sẽ liên hệ sớm để giao hàng.</p>
                            <hr/>
                            <p>CloneEbay Team</p>
                        ";

                await _emailService.SendEmailAsync(email, subject, body);

                // ✅ 4. Xóa session giỏ hàng
                HttpContext.Session.Remove("UserCart");
            }

            return View();
        }

        [HttpGet("OrderHistory")]
        public async Task<IActionResult> OrderHistory(
            [FromServices] PaymentModule.Business.Abstractions.IOrderTableService orderTableService) // Inject
        {
            // Lấy userId của người đang đăng nhập
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim))
                return RedirectToAction("Login", "User");

            int userId = int.Parse(userIdClaim);

            // Gọi service để lấy lịch sử
            var orders = await orderTableService.GetOrderHistoryAsync(userId);

            return View(orders); // Truyền danh sách order vào View
        }

        [HttpGet("OrderTracking/{id}")]
        public async Task<IActionResult> OrderTracking(int id,
            [FromServices] PaymentModule.Business.Abstractions.IOrderTableService orderTableService)
        {
            var order = await orderTableService.GetOrderDetailsAsync(id);

            if (order == null || order.ShippingInfos == null)
            {
                ViewBag.ErrorMessage = "Không tìm thấy thông tin vận chuyển cho đơn hàng này.";
                return View("OrderTrackingError");
            }

            return View(order);
        }

        [HttpPost("SyncStatus/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SyncShippingStatus(int id,
            [FromServices] PaymentModule.Business.Abstractions.IOrderTableService orderTableService)
        {
            await orderTableService.SyncShipmentStatusAsync(id);

            return RedirectToAction("OrderTracking", new { id = id });
        }
    }
}
