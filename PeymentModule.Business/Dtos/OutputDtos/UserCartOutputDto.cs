using System;
using System.Collections.Generic;
using PaymentModule.Business.Dtos.BusinessDtos;

namespace PaymentModule.Business.Dtos.OutputDtos
{
    public class UserCartOutputDto : BaseOutputDto
    {
        public UserCartOutputDto()
        {
            // ✅ luôn khởi tạo mặc định để tránh null reference
            Items = new List<ItemInOrderTableDtos>();
            TotalPrice = 0m;
            Success = true;
        }
        public int OrderTableId { get; set; }

        public decimal TotalPrice { get; set; }
        public List<ItemInOrderTableDtos> Items { get; set; }
    }
}
