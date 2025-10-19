using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using PaymentModule.Business.Abstractions;
using PaymentModule.Business.Dtos.InputDtos;
using System.Security.Claims;
using PaymentModule.Business.Dtos.OutputDtos;

namespace PaymentModule.Web.Controllers
{
    public class UserController : Controller
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

            [HttpPost]
        public async Task<IActionResult> Login(string themViewModelVaoDay)
        {
            var input = new LoginInputDto();
            var loginResult = await _userService.UserLogin(input);

            if (!loginResult.Success)
            {
                //Đã có sẵn msg trong service tự thiết kế UI hiển thị msg
                ModelState.AddModelError("", loginResult.Message);
                return View();
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, loginResult.UserId.ToString()),
                new Claim(ClaimTypes.Role, loginResult.Role)
            };

            var claimsIdentity = new ClaimsIdentity(
                claims, CookieAuthenticationDefaults.AuthenticationScheme);;
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity)
            );
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }


        //Trả về json navbar tự xử lý với AJAX nhé
        [HttpGet]
        public async Task<ActionResult<UserNavbarOuputDto>> GetNavbarInfo()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int userId = 0;
            if (!string.IsNullOrEmpty(userIdClaim))
            {
                int.TryParse(userIdClaim, out userId);
            }
            var inputDto = new UserNavbarInputDto
            {
                UserId = userId
            };
            var result = await _userService.GetNavbarInfoAsync(inputDto);
            return Ok(result);
        }
    }
}
