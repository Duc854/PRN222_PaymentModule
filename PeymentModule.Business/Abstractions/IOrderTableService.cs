using System.Threading.Tasks;
using PaymentModule.Business.Dtos.InputDtos;
using PaymentModule.Business.Dtos.OutputDtos;
using PaymentModule.Data.Entities;

namespace PaymentModule.Business.Abstractions
{
    public interface IOrderTableService
    {
        Task<UserCartOutputDto> GetCartByUserId(UserCartInputDto input);
        Task<bool> AddToCartAsync(AddToCartInputDto input); // ✅ thêm dòng này
        Task<bool> UpdateCartItemAsync(UpdateCartItemDto input);
        Task<bool> DeleteCartItemAsync(UpdateCartItemDto input);
        Task<OrderTable> CompleteOrderAsync(int userId, int addressId, string method = "COD");
        Task CreateShipmentForOrderAsync(int orderId);
        Task<List<OrderTable>> GetOrderHistoryAsync(int buyerId);
        Task<OrderTable> GetOrderDetailsAsync(int orderId);
        Task<bool> SyncShipmentStatusAsync(int orderId);
    }
}
