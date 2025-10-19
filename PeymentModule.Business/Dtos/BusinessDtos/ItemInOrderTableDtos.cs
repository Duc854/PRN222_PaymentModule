using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PaymentModule.Business.Dtos.BusinessDtos
{
    public class ItemInOrderTableDtos
    {
        public int OrderItemId { get; set; }
        public string Title { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public string Images { get; set; } = string.Empty;
        public decimal TotalPrice { get; set; }
    }
}
