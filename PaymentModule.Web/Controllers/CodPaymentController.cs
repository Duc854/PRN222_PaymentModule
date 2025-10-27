using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PaymentModule.Business.Abstractions;
using System.Security.Claims;
using System.Threading.Tasks;

namespace PaymentModule.Web.Controllers
{
    [Authorize] // ✅ Bắt buộc đăng nhập
    [Route("[controller]/[action]")]
    public class CodPaymentController : Controller
    {
        private readonly ICodPaymentService _codPaymentService;

        public CodPaymentController(ICodPaymentService codPaymentService)
        {
            _codPaymentService = codPaymentService;
        }

        /// <summary>
        /// Hiển thị danh sách tất cả đơn COD của Seller
        /// </summary>
        public async Task<IActionResult> Manage()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim))
                return RedirectToAction("Login", "Account");

            int userId = int.Parse(userIdClaim);
            var orders = await _codPaymentService.GetCodOrdersBySellerAsync(userId);

            return View(orders);
        }

        /// <summary>
        /// Hiển thị chi tiết đơn COD
        /// </summary>
        public async Task<IActionResult> Detail(int id)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim))
                return RedirectToAction("Login", "Account");

            int userId = int.Parse(userIdClaim);
            var order = await _codPaymentService.GetCodOrderDetailAsync(id, userId);

            if (order == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đơn hàng hoặc bạn không có quyền xem.";
                return RedirectToAction(nameof(Manage));
            }

            return View(order);
        }

        /// <summary>
        /// Seller xác nhận đơn hàng (bắt đầu giao hàng)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirm(int id)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim))
                return RedirectToAction("Login", "Account");

            int userId = int.Parse(userIdClaim);
            var result = await _codPaymentService.ConfirmOrderAsync(id, userId);

            TempData[result ? "SuccessMessage" : "ErrorMessage"] =
                result
                ? "✅ Đã xác nhận đơn hàng thành công! Đơn hàng đang được chuẩn bị giao."
                : "❌ Không thể xác nhận đơn hàng. Vui lòng thử lại.";

            return RedirectToAction(nameof(Detail), new { id });
        }

        /// <summary>
        /// Seller đánh dấu đơn đã giao hàng
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deliver(int id)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim))
                return RedirectToAction("Login", "Account");

            int userId = int.Parse(userIdClaim);
            var result = await _codPaymentService.MarkAsDeliveredAsync(id, userId);

            TempData[result ? "SuccessMessage" : "ErrorMessage"] =
                result
                ? "🚚 Đã đánh dấu giao hàng thành công! Chờ xác nhận thanh toán COD."
                : "❌ Không thể cập nhật trạng thái giao hàng. Vui lòng thử lại.";

            return RedirectToAction(nameof(Detail), new { id });
        }

        /// <summary>
        /// Seller xác nhận người mua đã trả tiền COD
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Verify(int id)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim))
                return RedirectToAction("Login", "Account");

            int userId = int.Parse(userIdClaim);

            try
            {
                var result = await _codPaymentService.VerifyCodPaymentAsync(id, userId);
                TempData[result ? "SuccessMessage" : "ErrorMessage"] =
                    result
                    ? "💰 Đã xác nhận nhận tiền COD thành công! Đơn hàng hoàn thành."
                    : "❌ Không thể xác nhận thanh toán COD. Vui lòng thử lại.";
            }
            catch (System.InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToAction(nameof(Detail), new { id });
        }
    }
}
