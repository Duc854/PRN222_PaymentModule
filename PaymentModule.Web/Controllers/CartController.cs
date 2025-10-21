using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using PaymentModule.Business.Abstraction;
using PaymentModule.Business.Abstractions;
using PaymentModule.Business.Dtos.InputDtos;
using PaymentModule.Business.Dtos.OutputDtos;

namespace PaymentModule.Web.Controllers
{
    [Route("cart")]
    public class CartController : Controller
    {
        private readonly IOrderTableService _orderTableService;
        private readonly IUserService _userService;
        private readonly IAddressService _addressService;
        private readonly IShippingFeeService _shippingFeeService;

        public CartController(
            IOrderTableService orderTableService,
            IUserService userService,
            IAddressService addressService,
            IShippingFeeService shippingFeeService)
        {
            _orderTableService = orderTableService;
            _userService = userService;
            _addressService = addressService;
            _shippingFeeService = shippingFeeService;
        }

        [HttpGet("api/calculate-shipping")]
        public IActionResult CalculateShipping(string city)
        {
            var fee = _shippingFeeService.CalculateShippingFee(city);
            return Ok(new { fee = fee });
        }

        // =======================
        // 🛒 VIEW: Trang giỏ hàng
        // =======================
        [HttpGet("view")]
        public async Task<IActionResult> ViewCart()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim))
                return RedirectToAction("Login", "Account");

            int userId = int.Parse(userIdClaim);
            var result = await _orderTableService.GetCartByUserId(new UserCartInputDto { UserId = userId });

            // ✅ Nếu giỏ rỗng hoặc lỗi, tạo giỏ rỗng
            if (result == null || !result.Success)
            {
                result = new UserCartOutputDto
                {
                    Success = true,
                    Items = new List<PaymentModule.Business.Dtos.BusinessDtos.ItemInOrderTableDtos>(),
                    TotalPrice = 0m
                };
            }
            else
            {
                // ✅ Tính lại tổng giá nếu service chưa set
                if (result.Items != null && result.Items.Any())
                    result.TotalPrice = result.Items.Sum(i => i.TotalPrice);
            }

            return View(result);
        }

        // =======================
        // 🧾 API: Lấy giỏ hàng
        // =======================
        [HttpGet("GetCart")]
        public async Task<IActionResult> GetCart()
        {
            try
            {
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdClaim))
                {
                    return Json(new
                    {
                        success = true,
                        empty = true,
                        items = new List<object>(),
                        total = 0m,
                        count = 0
                    });
                }

                int userId = int.Parse(userIdClaim);
                var result = await _orderTableService.GetCartByUserId(new UserCartInputDto { UserId = userId });

                if (result == null || result.Items == null || result.Items.Count == 0)
                {
                    return Json(new
                    {
                        success = true,
                        empty = true,
                        items = new List<object>(),
                        total = 0m,
                        count = 0
                    });
                }

                result.TotalPrice = result.Items.Sum(i => i.TotalPrice);

                return Json(new
                {
                    success = true,
                    empty = false,
                    items = result.Items,
                    total = result.TotalPrice,
                    count = result.Items.Count
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Server error: " + ex.Message
                });
            }
        }

        // =======================
        // ➕ API: Thêm sản phẩm
        // =======================
        [HttpPost("AddToCart")]
        public async Task<IActionResult> AddToCart([FromBody] AddToCartInputDto input)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim))
                return Json(new { success = false, message = "Please sign in first!", total = 0m, count = 0 });

            input.UserId = int.Parse(userIdClaim);
            var success = await _orderTableService.AddToCartAsync(input);

            if (!success)
                return Json(new { success = false, message = "Failed to add to cart.", total = 0m, count = 0 });

            var updatedCart = await _orderTableService.GetCartByUserId(new UserCartInputDto { UserId = input.UserId });
            decimal total = updatedCart?.Items?.Sum(i => i.TotalPrice) ?? 0m;

            return Json(new
            {
                success = true,
                message = "Added to cart!",
                total = total,
                count = updatedCart?.Items?.Count ?? 0
            });
        }

        // =======================
        // ✏️ API: Cập nhật số lượng
        // =======================
        [HttpPost("UpdateItem")]
        public async Task<IActionResult> UpdateItem([FromBody] UpdateCartItemDto dto)
        {
            var success = await _orderTableService.UpdateCartItemAsync(dto);
            if (!success)
                return Json(new { success = false, total = 0m, count = 0, itemSubtotal = 0m });

            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim))
                return Json(new { success = false, total = 0m, count = 0, itemSubtotal = 0m });

            int userId = int.Parse(userIdClaim);
            var updatedCart = await _orderTableService.GetCartByUserId(new UserCartInputDto { UserId = userId });

            // ✅ Tính tổng và subtotal chính xác
            decimal totalPrice = 0m;
            if (updatedCart?.Items != null && updatedCart.Items.Count > 0)
                totalPrice = updatedCart.Items.Sum(i => i.TotalPrice);

            var item = updatedCart?.Items?.FirstOrDefault(i => i.OrderItemId == dto.OrderItemId);
            decimal itemSubtotal = item?.TotalPrice ?? 0m;

            return Json(new
            {
                success = true,
                total = totalPrice,
                count = updatedCart?.Items?.Count ?? 0,
                itemSubtotal = itemSubtotal
            });
        }

        // =======================
        // ❌ API: Xóa sản phẩm
        // =======================
        [HttpPost("DeleteItem")]
        public async Task<IActionResult> DeleteItem([FromBody] UpdateCartItemDto dto)
        {
            var success = await _orderTableService.DeleteCartItemAsync(dto);
            if (!success)
                return Json(new { success = false, total = 0m, count = 0 });

            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim))
                return Json(new { success = false, total = 0m, count = 0 });

            int userId = int.Parse(userIdClaim);
            var updatedCart = await _orderTableService.GetCartByUserId(new UserCartInputDto { UserId = userId });

            // ✅ Tính tổng sau khi xóa
            decimal totalPrice = 0m;
            if (updatedCart?.Items != null && updatedCart.Items.Count > 0)
                totalPrice = updatedCart.Items.Sum(i => i.TotalPrice);

            return Json(new
            {
                success = true,
                total = totalPrice,
                count = updatedCart?.Items?.Count ?? 0
            });
        }

        // =======================
        // 💳 VIEW: Checkout
        // =======================
        [HttpGet("checkout")]
        public async Task<IActionResult> Checkout()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim))
                return RedirectToAction("Login", "Account");

            int userId = int.Parse(userIdClaim);
            var result = await _orderTableService.GetCartByUserId(new UserCartInputDto { UserId = userId });
            var user = await _userService.GetNavbarInfoAsync(new UserNavbarInputDto { UserId = userId });

            if (result?.Items != null)
                result.TotalPrice = result.Items.Sum(i => i.TotalPrice);

            var cartJson = JsonSerializer.Serialize(result);
            HttpContext.Session.SetString("UserCart", cartJson);
            HttpContext.Session.SetInt32("UserId", userId);

            ViewBag.UserName = user.Username;
            ViewBag.AddressList = await _addressService.GetAddressesByUserId(userId);

            return View(result);
        }

        [HttpGet("Success")]
        public IActionResult Success() => View();
    }
}
