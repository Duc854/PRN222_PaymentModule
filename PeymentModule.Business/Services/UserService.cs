using PaymentModule.Business.Abstractions;
using PaymentModule.Business.Dtos.InputDtos;
using PaymentModule.Business.Dtos.OutputDtos;
using PaymentModule.Data.Abstractions;
using PaymentModule.Data.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PaymentModule.Business.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        public UserService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }
        public async Task<LoginOutputDto> UserLogin(LoginInputDto input)
        {
            var user = await _userRepository.GetUserInfoByEmail(input.Email);
            if (user == null) {
                return new LoginOutputDto
                {
                    Success = false,
                    Message = $"Cannot find account with email {input.Email}!!!",
                };
            }
            if(user.Password != input.Password)
            {
                return new LoginOutputDto
                {
                    Success = false,
                    Message = $"Password is incorrect! Please try again.",
                };
            }
            return new LoginOutputDto
            {
                Message = $"Login successful. Welcome {user.Role ?? "User"}",
                UserId = user.Id,
                Role = user.Role ?? string.Empty,
            };
        }
        public async Task<UserNavbarOuputDto> GetNavbarInfoAsync(UserNavbarInputDto input)
        {
            if (input == null || input.UserId <= 0)
            {
                return new UserNavbarOuputDto();
            }

            var user = await _userRepository.GetUserInfoById(input.UserId);
            if (user == null)
            {
                return new UserNavbarOuputDto
                {
                    Success = false,
                    Message = "User not found",
                };
            }

            return new UserNavbarOuputDto
            {
                Success = true,
                Username = user.Username!,
                AvatarUrl = string.IsNullOrEmpty(user.AvatarUrl)
                            ? "https://www.gravatar.com/avatar/?d=mp"
                            : user.AvatarUrl
            };
        }

    }
}
