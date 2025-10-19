using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PaymentModule.Business.Dtos
{
    public class BaseOutputDto
    {
        public bool Success { get; set; } = true;
        public string Message { get; set; } = string.Empty;
    }
}
