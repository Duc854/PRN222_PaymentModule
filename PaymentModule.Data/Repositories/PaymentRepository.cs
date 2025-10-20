using PaymentModule.Data.Abstractions;
using PaymentModule.Data.Entities;
using System.Threading.Tasks;

namespace PaymentModule.Data.Repositories
{
    public class PaymentRepository : IPaymentRepository
    {
        private readonly CloneEbayDbContext _dbContext;
        public PaymentRepository(CloneEbayDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task CreatePaymentAsync(Payment payment)
        {
            await _dbContext.Payments.AddAsync(payment);
            await _dbContext.SaveChangesAsync();
        }
    }
}
