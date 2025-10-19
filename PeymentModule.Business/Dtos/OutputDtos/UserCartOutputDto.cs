using PaymentModule.Business.Dtos.BusinessDtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PaymentModule.Business.Dtos.OutputDtos
{
    public class UserCartOutputDto : BaseOutputDto
    {
        public decimal TotalPrice { get; set; }
        public List<ItemInOrderTableDtos> Items { get; set; } = new List<ItemInOrderTableDtos>();
    }
}
