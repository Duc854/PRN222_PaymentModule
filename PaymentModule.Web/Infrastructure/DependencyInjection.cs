using Microsoft.EntityFrameworkCore;
using PaymentModule.Data.Entities;

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

            //Add Repositories

            return services;
        }
    }
}
