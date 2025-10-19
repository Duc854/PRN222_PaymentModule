using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PaymentModule.Business.Dtos.OutputDtos
{
    public class UserNavbarOuputDto : BaseOutputDto
    {
        public string Username { get; set; } = "Guest";
        public string AvatarUrl { get; set; } = "https://www.gravatar.com/avatar/?d=mp";
    }
}
