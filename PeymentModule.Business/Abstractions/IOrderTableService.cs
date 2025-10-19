using PaymentModule.Business.Dtos.InputDtos;
using PaymentModule.Business.Dtos.OutputDtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PaymentModule.Business.Abstractions
{
    public interface IOrderTableService
    {
        Task<UserCartOutputDto> GetCartByUserId(UserCartInputDto input);
    }
}
