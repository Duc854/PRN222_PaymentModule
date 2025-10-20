using PaymentModule.Data.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PaymentModule.Data.Abstractions
{
    public interface IProductRepository
    {
        Task<Product?> GetProductInfoById(int id);
        Task<List<Product>> GetAllProductsAsync();
    }
}
