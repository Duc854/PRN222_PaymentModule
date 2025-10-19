using PaymentModule.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PaymentModule.Data.Abstractions
{
    public interface IUserRepository
    {
        Task<User?> GetUserInfoByEmail(string email);
        Task<User?> GetUserInfoById(int id);
    }
}
