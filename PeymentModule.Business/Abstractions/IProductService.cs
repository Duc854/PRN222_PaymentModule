using PaymentModule.Business.Dtos.BusinessDtos;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PaymentModule.Business.Abstractions
{
    public interface IProductService
    {
        Task<List<ProductDisplayDto>> GetTopProductsAsync(int count = 4);
    }
}
