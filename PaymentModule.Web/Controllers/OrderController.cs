using Microsoft.AspNetCore.Mvc;
using PaymentModule.Business.Dtos.OutputDtos;
using PaymentModule.Data;
using System.Text.Json;

namespace PaymentModule.Web.Controllers
{
    [Route("order")]
    public class OrderController : Controller
    {
        private readonly CloneEbayDbContext _context;

        public OrderController(CloneEbayDbContext context)
        {
            _context = context;
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

            if (data.TryGetProperty("coupon", out var couponProp))
                HttpContext.Session.SetString("Coupon", couponProp.GetString() ?? "");

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

        // ✅ Trang xác nhận đơn hàng
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

            return View(cart);
        }

        [HttpGet("PaymentSuccess")]
        public async Task<IActionResult> PaymentSuccess([FromServices] PaymentModule.Business.Abstractions.IOrderTableService orderTableService)
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            if (userId != null)
            {
                // ✅ 1. Chuyển đơn hàng "Unpaid" → "Paid" trong DB
                await orderTableService.CompleteOrderAsync(userId.Value, "COD");


                // ✅ 2. Xóa session giỏ hàng để Checkout không hiện nữa
                HttpContext.Session.Remove("UserCart");
            }

            return View();
        }


        // ✅ Trang lịch sử đơn hàng
        [HttpGet("OrderHistory")]
        public IActionResult OrderHistory()
        {
            // sau này có thể lấy danh sách order từ DB, giờ chỉ load view
            return View();
        }

        // ✅ Trang theo dõi đơn hàng
        [HttpGet("OrderTracking")]
        public IActionResult OrderTracking()
        {
            return View();
        }
    }
}
