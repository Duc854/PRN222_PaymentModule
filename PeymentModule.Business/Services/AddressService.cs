using PaymentModule.Business.Abstractions;
using PaymentModule.Data;
using PaymentModule.Data.Abstractions;
using PaymentModule.Data.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PaymentModule.Business.Services
{
    public class AddressService : IAddressService
    {
        private readonly CloneEbayDbContext _dbContext;
        public AddressService(CloneEbayDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<Address>> GetAddressesByUserId(int userId)
        {
            return _dbContext.Addresses
                .Where(a => a.UserId == userId)
                .ToList();
        }
    }
}
