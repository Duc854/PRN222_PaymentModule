using Microsoft.EntityFrameworkCore;
using PaymentModule.Data.Abstractions;
using PaymentModule.Data.Entities;

namespace PaymentModule.Data.Repositories
{
    public class AddressRepository : IAddressRepository
    {
        private readonly CloneEbayDbContext _context;

        public AddressRepository(CloneEbayDbContext context)
        {
            _context = context;
        }

        public async Task<Address> GetByIdAsync(int id)
        {
            return await _context.Addresses.FindAsync(id);
        }

        public async Task<Address?> GetDefaultAddressByUserIdAsync(int userId)
        {
            return await _context.Addresses
                .FirstOrDefaultAsync(a => a.UserId == userId && a.IsDefault == true) ??
                   await _context.Addresses.FirstOrDefaultAsync(a => a.UserId == userId);
        }
    }
}