using System.IO;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using PaymentModule.Business.Abstractions;
using PaymentModule.Business.Services;
using PaymentModule.Web.Infrastructure;

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
            });

            // 4️⃣ Fix Data Protection Key (để không lỗi payload invalid)
            builder.Services.AddDataProtection()
                .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(Directory.GetCurrentDirectory(), "DataProtectionKeys")))
                .SetApplicationName("PaymentModuleWeb");

            // 5️⃣ Cookie Authentication
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Account/Login"; // ✅ đúng controller
                    options.AccessDeniedPath = "/Shared/AccessDenied";
                    options.Cookie.HttpOnly = true;
                    options.Cookie.SameSite = SameSiteMode.Lax;
                    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                    options.SlidingExpiration = true;
                    options.ExpireTimeSpan = TimeSpan.FromHours(2);
                });

            //đăng kí service gửi mail
            builder.Services.AddScoped<IEmailService, EmailService>();

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

            // 7️⃣ Middlewares
            app.UseSession();
            app.UseAuthentication();
            app.UseAuthorization();

            // 8️⃣ Default route
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }

    }
}
