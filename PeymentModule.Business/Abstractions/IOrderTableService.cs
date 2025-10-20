using PaymentModule.Business.Dtos.InputDtos;
using PaymentModule.Business.Dtos.OutputDtos;
using System.Threading.Tasks;

namespace PaymentModule.Business.Abstractions
{
    public interface IOrderTableService
    {
        Task<UserCartOutputDto> GetCartByUserId(UserCartInputDto input);
        Task<bool> AddToCartAsync(AddToCartInputDto input); // ✅ thêm dòng này
        Task<bool> UpdateCartItemAsync(UpdateCartItemDto input);
        Task<bool> DeleteCartItemAsync(UpdateCartItemDto input);
        Task CompleteOrderAsync(int userId, string method = "COD");

    }
}
