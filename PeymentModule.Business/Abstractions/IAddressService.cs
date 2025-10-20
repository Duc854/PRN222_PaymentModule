using PaymentModule.Data.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PaymentModule.Business.Abstractions
{
    public interface IAddressService
    {
        Task<List<Address>> GetAddressesByUserId(int userId);
    }
}
