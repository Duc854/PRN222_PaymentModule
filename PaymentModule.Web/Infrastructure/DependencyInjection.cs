using Microsoft.EntityFrameworkCore;
using PaymentModule.Business.Abstraction;
using PaymentModule.Business.Abstractions;
using PaymentModule.Business.Exception;
using PaymentModule.Business.Services;
using PaymentModule.Data;
using PaymentModule.Data.Abstractions;
using PaymentModule.Data.Repositories;
using Polly;

namespace PaymentModule.Web.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            //Add SqlServer
            var connectionString = configuration.GetConnectionString("SQLServerConnection");
            services.AddDbContext<CloneEbayDbContext>(options => options.UseSqlServer(connectionString));

            //Add Services
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IOrderTableService, OrderTableService>();
            services.AddScoped<IShippingService, MockShippingService>();
            //Add Repositories
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IOrderTableRepository, OrderTableRepository>();
            services.AddScoped<IOrderItemRepository, OrderItemRepository>();
            services.AddScoped<IOrderTableService, OrderTableService>();
            services.AddScoped<IProductRepository, ProductRepository>();
            services.AddScoped<IProductService, ProductService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IAddressService, AddressService>();
            services.AddScoped<IOrderTableService, OrderTableService>();
            services.AddScoped<IPaymentRepository, PaymentRepository>();
            services.AddScoped<IShippingInfoRepository, ShippingInfoRepository>();
            services.AddScoped<IAddressRepository, AddressRepository>();

            return services;
        }
    }
}
