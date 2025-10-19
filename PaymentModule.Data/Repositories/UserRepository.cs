using Microsoft.EntityFrameworkCore;
using PaymentModule.Data.Abstractions;
using PaymentModule.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PaymentModule.Data.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly CloneEbayDbContext _dbContext;
        public UserRepository(CloneEbayDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        public async Task<User?> GetUserInfoByEmail(string email)
        {
            return await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == email);
        }
        public async Task<User?> GetUserInfoById(int id)
        {
            return await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == id);
        }
    }
}
