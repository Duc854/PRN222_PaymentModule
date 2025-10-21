using System.Threading.Tasks;
using PaymentModule.Data.Entities;

namespace PaymentModule.Data.Abstractions
{
    public interface IAddressRepository
    {
        Task<Address> GetByIdAsync(int id);
    }
}