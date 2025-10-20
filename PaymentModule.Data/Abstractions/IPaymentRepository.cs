using PaymentModule.Data.Entities;
using System.Threading.Tasks;

namespace PaymentModule.Data.Abstractions
{
    public interface IPaymentRepository
    {
        Task CreatePaymentAsync(Payment payment);
    }
}
