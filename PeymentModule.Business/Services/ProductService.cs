using PaymentModule.Business.Abstractions;
using PaymentModule.Business.Dtos.BusinessDtos;
using PaymentModule.Data.Abstractions;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PaymentModule.Business.Services
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _productRepo;

        public ProductService(IProductRepository productRepo)
        {
            _productRepo = productRepo;
        }

        public async Task<List<ProductDisplayDto>> GetTopProductsAsync(int count = 4)
        {
            var products = await _productRepo.GetAllProductsAsync();

            return products
                .Take(count)
                .Select(p => new ProductDisplayDto
                {
                    Id = p.Id,
                    Title = p.Title ?? "No title",
                    Price = p.Price ?? 0,
                    Images = string.IsNullOrEmpty(p.Images)
                        ? "https://via.placeholder.com/200x200.png?text=No+Image"
                        : p.Images
                })
                .ToList();
        }
    }
}
