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

            // === PayPal settings ===
            var paypalSettings = builder.Configuration.GetSection("PayPal");
            builder.Services.AddScoped<PayPalService>();

            // 1️⃣ MVC + Infrastructure
            builder.Services.AddControllersWithViews();
            builder.Services.AddInfrastructure(builder.Configuration);

            // ✅ Dùng In-Memory cache thay vì SQL (để tránh lỗi khi không có bảng SessionCache)
            builder.Services.AddDistributedMemoryCache();

            // ✅ Cấu hình Session
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
                options.Cookie.SameSite = SameSiteMode.Lax; // Lax để tránh lỗi cross-site khi deploy
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            });

            // ✅ Cookie policy
            builder.Services.Configure<CookiePolicyOptions>(options =>
            {
                options.MinimumSameSitePolicy = SameSiteMode.Lax;
                options.Secure = CookieSecurePolicy.SameAsRequest;
            });

            // ✅ Data Protection Key (để mã hóa cookie ổn định)
            builder.Services.AddDataProtection()
                .PersistKeysToFileSystem(new DirectoryInfo(
                    Path.Combine(Directory.GetCurrentDirectory(), "DataProtectionKeys")))
                .SetApplicationName("PaymentModuleWeb");

            // ✅ Cookie Authentication
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

            // ✅ Rate limiter (chống spam request)
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

            builder.Services.AddHttpClient();

            var app = builder.Build();

            // === Middleware pipeline ===
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

            // ✅ Phải đặt Session trước Authentication
            app.UseSession();

            app.UseAuthentication();
            app.UseRateLimiter();
            app.UseAuthorization();

            // === Default route ===
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}
