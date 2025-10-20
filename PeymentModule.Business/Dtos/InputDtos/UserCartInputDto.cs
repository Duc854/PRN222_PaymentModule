using System;

namespace PaymentModule.Business.Dtos.InputDtos
{
    public class UserCartInputDto
    {
        public int UserId { get; set; }

        public UserCartInputDto() { }

        public UserCartInputDto(int userId)
        {
            UserId = userId;
        }
    }
}
