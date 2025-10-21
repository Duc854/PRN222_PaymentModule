using System.Threading.Tasks;
using PaymentModule.Business.Abstraction;
using PaymentModule.Data.Abstractions;

namespace PaymentModule.Business.Services
{
    public class ShippingFeeService : IShippingFeeService
    {
        private readonly IOrderItemRepository _orderItemRepo;
        private readonly IAddressRepository _addressRepo;

        public ShippingFeeService(
            IOrderItemRepository orderItemRepo,
            IAddressRepository addressRepo)
        {
            _orderItemRepo = orderItemRepo;
            _addressRepo = addressRepo;
        }

        public async Task<decimal> CalculateShippingFeeAsync(int cartOrderId, int buyerAddressId)
        {
            var buyerAddress = await _addressRepo.GetByIdAsync(buyerAddressId);
            if (buyerAddress == null) return 50000;

            var items = await _orderItemRepo.GetOrderItemsByOrderId(cartOrderId);
            decimal totalShippingFee = 0;

            foreach (var item in items)
            {
                if (item.Product?.SellerId == null)
                {
                    totalShippingFee += 50000;
                    continue;
                }

                var sellerAddress = await _addressRepo.GetDefaultAddressByUserIdAsync(item.Product.SellerId.Value);

                if (sellerAddress == null)
                {
                    totalShippingFee += 50000;
                }
                else if (sellerAddress.City == buyerAddress.City)
                {
                    totalShippingFee += 20000;
                }
                else
                {
                    totalShippingFee += 50000;
                }
            }

            return totalShippingFee;
        }
    }
}