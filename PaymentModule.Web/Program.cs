using System.IO;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using PaymentModule.Business.Services;
using PaymentModule.Web.Infrastructure;
using PaymentModule.Web.Middleware;

namespace PaymentModule.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Đọc PayPal section
            var paypalSettings = builder.Configuration.GetSection("PayPal");

            string clientId = paypalSettings["ClientId"];
            string clientSecret = paypalSettings["ClientSecret"];
            string mode = paypalSettings["Mode"];
            string returnUrl = paypalSettings["ReturnUrl"];
            string cancelUrl = paypalSettings["CancelUrl"];
            builder.Services.AddScoped<PayPalService>();

            // 1️⃣ Add MVC
            builder.Services.AddControllersWithViews();
            // 2️⃣ Add infrastructure (DbContext, Repository, Service)
            builder.Services.AddInfrastructure(builder.Configuration);

            // 3️⃣ Add Session
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
                options.Cookie.SameSite = SameSiteMode.None;
            });

            builder.Services.Configure<CookiePolicyOptions>(options =>
            {
                options.MinimumSameSitePolicy = SameSiteMode.None;
                // Khi SameSite=None, bắt buộc phải dùng Secure (HTTPS)
                options.Secure = CookieSecurePolicy.Always;
            });

            // 4️⃣ Fix Data Protection Key (để không lỗi payload invalid)
            builder.Services.AddDataProtection()
                .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(Directory.GetCurrentDirectory(), "DataProtectionKeys")))
                .SetApplicationName("PaymentModuleWeb");

            // 5️⃣ Cookie Authentication
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/User/Login";
                    options.AccessDeniedPath = "/Shared/AccessDenied";
                    options.Cookie.HttpOnly = true;
                    options.Cookie.SameSite = SameSiteMode.Lax;
                    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                    options.SlidingExpiration = true;
                    options.ExpireTimeSpan = TimeSpan.FromHours(2);
                });
            // Add user rate limiter
            builder.Services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = 429;
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                {
                    var user = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous";
                    return RateLimitPartition.GetSlidingWindowLimiter(
                        user,
                        _ => new SlidingWindowRateLimiterOptions
                        {
                            PermitLimit = 20,
                            Window = TimeSpan.FromSeconds(10),
                            SegmentsPerWindow = 5,
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = 0
                        });
                });
            });

            var app = builder.Build();

            // 6️⃣ Error handling
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseCustomExceptionLogging();

            app.UseCookiePolicy();

            // 7️⃣ Middlewares
            app.UseSession();
            app.UseAuthentication();
            app.UseRateLimiter();
            app.UseAuthorization();

            // 8️⃣ Default route
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }

    }
}
