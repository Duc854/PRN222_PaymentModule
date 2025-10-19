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
    public class ProductRepository : IProductRepository
    {
        private readonly CloneEbayDbContext _dbContext;
        public ProductRepository(CloneEbayDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        public async Task<Product?> GetProductInfoById(int id)
        {
            return await _dbContext.Products.FirstOrDefaultAsync(p => p.Id == id);
        }
    }
}
