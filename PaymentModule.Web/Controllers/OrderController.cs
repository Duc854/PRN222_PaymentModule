using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PaymentModule.Business.Abstraction;
using PaymentModule.Business.Abstractions;
using PaymentModule.Business.Dtos.OutputDtos;
using PaymentModule.Data;
using PaymentModule.Data.Abstractions;
using PaymentModule.Data.Entities;

namespace PaymentModule.Web.Controllers
{
    [Route("order")]
    public class OrderController : Controller
    {
        private readonly CloneEbayDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IShippingInfoRepository _shippingInfoRepo; // Inject MỚI
        private readonly IShippingService _shippingService; // Inject MỚI

        public OrderController(
            CloneEbayDbContext context,
            IEmailService emailService,
            IShippingInfoRepository shippingInfoRepo, // Inject MỚI
            IShippingService shippingService // Inject MỚI
            )
        {
            _context = context;
            _emailService = emailService;
            _shippingInfoRepo = shippingInfoRepo; // Inject MỚI
            _shippingService = shippingService; // Inject MỚI
        }

        // ✅ Receive checkout data and store in Session
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
                HttpContext.Session.SetInt32("AddressId", idProp.GetInt32());

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

        // ✅ Checkout confirmation page
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
            ViewBag.PaymentMethod = HttpContext.Session.GetString("PaymentMethod") ?? "COD";

            return View(cart);
        }

        // ✅ Payment success (PayPal or COD)
        [HttpGet("PaymentSuccess")]
        public async Task<IActionResult> PaymentSuccess(
            [FromServices] IOrderTableService orderTableService,
            [FromServices] ILogger<OrderController> logger,
            int? orderId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var addressId = HttpContext.Session.GetInt32("AddressId");
            var user = await _context.Users.FindAsync(userId);

            if (userId == null || user == null)
            {
                logger.LogWarning("PaymentSuccess loaded but no UserId found in Session.");
                return RedirectToAction("Index", "Home");
            }

            OrderTable completedOrder;
            string paymentMethodForEmail;
            string paymentProcessed = HttpContext.Session.GetString("PaymentProcessed");

            if (paymentProcessed == "true")
            {
                // === PayPal flow ===
                if (orderId == null)
                {
                    logger.LogError("PayPal flow error: missing OrderId after payment processed.");
                    return RedirectToAction("Index", "Home");
                }

                completedOrder = await orderTableService.GetOrderDetailsAsync(orderId.Value);
                paymentMethodForEmail = "PayPal";
                HttpContext.Session.Remove("PaymentProcessed");
            }
            else
            {
                // === COD flow ===
                if (addressId == null || addressId.Value == 0)
                {
                    logger.LogError("COD flow error: Missing AddressId in Session.");
                    return RedirectToAction("Checkout", "Cart");
                }

                completedOrder = await orderTableService.CompleteOrderAsync(userId.Value, addressId.Value, "COD");
                if (completedOrder == null)
                {
                    logger.LogWarning("COD flow error: No unpaid order found for User {UserId}", userId);
                    return RedirectToAction("Index", "Home");
                }

                try
                {
                    await orderTableService.CreateShipmentForOrderAsync(completedOrder.Id);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to create shipment for COD order {OrderId}", completedOrder.Id);
                }

                paymentMethodForEmail = "Cash on Delivery (COD)";
            }

            // === Shared logic: Send confirmation email ===
            var total = HttpContext.Session.GetString("Total") ?? "0";
            var fullName = HttpContext.Session.GetString("FullName") ?? user.Username;
            var email = user.Email;

            string subject = $"[CloneEbay] Order Confirmation – Order #{completedOrder.Id}";
            string body = $@"
                <h2>Thank you, {fullName}!</h2>
                <p>Your order <b>#{completedOrder.Id}</b> has been successfully confirmed.</p>
                <p><b>Total:</b> {total} USD</p>
                <p><b>Payment Method:</b> {paymentMethodForEmail}</p>
                <p>We will contact you soon to ship your order.</p>
                <hr/>
                <p>CloneEbay Team</p>
            ";

            await _emailService.SendEmailAsync(email, subject, body);

            // Clear all session data
            HttpContext.Session.Clear();

            ViewBag.OrderId = completedOrder.Id;
            ViewBag.PaymentMethod = paymentMethodForEmail;

            return View();
        }

        // ✅ Order history
        [HttpGet("OrderHistory")]
        public async Task<IActionResult> OrderHistory(
            [FromServices] IOrderTableService orderTableService)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim))
                return RedirectToAction("Login", "User");

            int userId = int.Parse(userIdClaim);
            var orders = await orderTableService.GetOrderHistoryAsync(userId);

            return View(orders);
        }

        // ✅ Order tracking
        [HttpGet("OrderTracking/{id}")]
        public async Task<IActionResult> OrderTracking(int id,
            [FromServices] IOrderTableService orderTableService)
        {
            var order = await orderTableService.GetOrderDetailsAsync(id);

            if (order == null)
            {
                ViewBag.ErrorMessage = "Không tìm thấy đơn hàng.";
                return View("OrderTrackingError");
            }

            // (Chuyển logic kiểm tra ShippingInfo vào View)

            return View(order);
        }

        [HttpGet("ManageShipment/{shippingInfoId}")]
        public async Task<IActionResult> ManageShipment(int shippingInfoId)
        {
            var shippingInfo = await _shippingInfoRepo.GetByIdAsync(shippingInfoId);
            if (shippingInfo == null) return NotFound();

            // Lấy địa chỉ gốc để hiển thị
            var order = await _context.OrderTables
                .Include(o => o.Address) // Phải Include Address
                .FirstOrDefaultAsync(o => o.Id == shippingInfo.OrderId);

            if (order != null && order.BuyerId.HasValue)
            {
                HttpContext.Session.SetInt32("UserId", order.BuyerId.Value);
            }

            ViewBag.OrderAddress = order?.Address;
            return View(shippingInfo); // Tạo một View tên là ManageShipment.cshtml
        }

        // === HÀM MỚI (Xử lý Reroute) ===
        [HttpPost("RequestReroute")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestReroute(int shippingInfoId, string newAddressNotes)
        {
            var shippingInfo = await _shippingInfoRepo.GetByIdAsync(shippingInfoId);
            if (shippingInfo == null) return NotFound();

            if (string.IsNullOrEmpty(newAddressNotes))
            {
                TempData["RerouteError"] = "Vui lòng nhập ghi chú địa chỉ mới.";
                return RedirectToAction("ManageShipment", new { shippingInfoId });
            }

            try
            {
                // Gọi service Reroute
                var response = await _shippingService.RequestRerouteAsync(shippingInfo.TrackingNumber, newAddressNotes);

                if (response.Success)
                {
                    TempData["RerouteSuccess"] = response.Message;
                }
                else
                {
                    TempData["RerouteError"] = response.Message;
                }
            }
            catch (Exception ex)
            {
                TempData["RerouteError"] = $"Lỗi hệ thống: {ex.Message}";
            }

            // Quay về trang tracking của đơn hàng
            return RedirectToAction("OrderTracking", new { id = shippingInfo.OrderId });
        }

        // ✅ Popup API: Get all addresses of current user
        [HttpGet("GetAddresses")]
        public IActionResult GetAddresses()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return Unauthorized();

            var addresses = _context.Addresses
                .Where(a => a.UserId == userId.Value)
                .Select(a => new
                {
                    a.Id,
                    a.FullName,
                    a.Street,
                    a.City,
                    a.State,
                    a.Country,
                    a.Phone
                })
                .ToList();

            return Json(addresses);
        }

        // ✅ Popup API: Add new address
        [HttpPost("SaveAddress")]
        public async Task<IActionResult> SaveAddress([FromBody] Address model)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return Unauthorized();

            if (string.IsNullOrEmpty(model.FullName) || string.IsNullOrEmpty(model.Street))
                return BadRequest("Invalid address details.");

            model.UserId = userId.Value;
            model.IsDefault = false;

            _context.Addresses.Add(model);
            await _context.SaveChangesAsync();

            HttpContext.Session.SetInt32("AddressId", model.Id);
            HttpContext.Session.SetString("AddressDisplay",
                $"{model.FullName}, {model.Street}, {model.City}, {model.Country}");

            return Ok(new { success = true, addressId = model.Id });
        }

        // ✅ Popup API: Change existing address (from popup)
        [HttpPost("ChangeAddress")]
        public IActionResult ChangeAddress([FromBody] int selectedAddressId)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return Unauthorized();

            var address = _context.Addresses
                .FirstOrDefault(a => a.Id == selectedAddressId && a.UserId == userId.Value);

            if (address == null)
                return BadRequest("Address not found.");

            HttpContext.Session.SetInt32("AddressId", address.Id);
            HttpContext.Session.SetString("AddressDisplay",
                $"{address.FullName}, {address.Street}, {address.City}, {address.Country}");

            return Ok(new { success = true });
        }
    }
}
