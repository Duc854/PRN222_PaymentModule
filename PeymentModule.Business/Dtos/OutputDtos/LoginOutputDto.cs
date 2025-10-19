using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PaymentModule.Business.Dtos.OutputDtos
{
    public class LoginOutputDto : BaseOutputDto
    {
        public int UserId { get; set; }
        public string Role { get; set; } = string.Empty;
    }
}
