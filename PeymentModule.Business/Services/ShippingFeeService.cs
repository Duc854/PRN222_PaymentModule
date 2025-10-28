using System;
using System.Threading.Tasks;
using PaymentModule.Business.Abstraction;
using PaymentModule.Data.Abstractions;

namespace PaymentModule.Business.Services
{
    public class ShippingFeeService : IShippingFeeService
    {
        private readonly IOrderItemRepository _orderItemRepo;
        private readonly IAddressRepository _addressRepo;

        // Giả định tỷ giá USD/VND — có thể đọc từ appsettings nếu cần
        private const decimal ExchangeRate = 27000m;

        public ShippingFeeService(
            IOrderItemRepository orderItemRepo,
            IAddressRepository addressRepo)
        {
            _orderItemRepo = orderItemRepo;
            _addressRepo = addressRepo;
        }

        /// <summary>
        /// Tính phí vận chuyển (đơn vị USD) dựa trên khoảng cách giữa người mua và người bán.
        /// </summary>
        public async Task<decimal> CalculateShippingFeeAsync(int cartOrderId, int buyerAddressId)
        {
            // Lấy địa chỉ người mua
            var buyerAddress = await _addressRepo.GetByIdAsync(buyerAddressId);
            if (buyerAddress == null)
            {
                // Nếu không có địa chỉ -> phí mặc định (VND -> USD)
                return ConvertToUsd(50000);
            }

            // Lấy danh sách sản phẩm trong đơn hàng
            var items = await _orderItemRepo.GetOrderItemsByOrderId(cartOrderId);
            decimal totalShippingFeeVnd = 0;

            foreach (var item in items)
            {
                if (item.Product?.SellerId == null)
                {
                    totalShippingFeeVnd += 50000;
                    continue;
                }

                // Lấy địa chỉ người bán
                var sellerAddress = await _addressRepo.GetDefaultAddressByUserIdAsync(item.Product.SellerId.Value);

                if (sellerAddress == null)
                {
                    totalShippingFeeVnd += 50000;
                }
                else if (sellerAddress.City == buyerAddress.City)
                {
                    // Cùng thành phố → phí thấp
                    totalShippingFeeVnd += 20000;
                }
                else
                {
                    // Khác thành phố → phí cao
                    totalShippingFeeVnd += 50000;
                }
            }

            // Quy đổi toàn bộ sang USD
            return ConvertToUsd(totalShippingFeeVnd);
        }

        /// <summary>
        /// Quy đổi VND sang USD theo tỷ giá cố định.
        /// </summary>
        private decimal ConvertToUsd(decimal vndAmount)
        {
            return Math.Round(vndAmount / ExchangeRate, 2, MidpointRounding.AwayFromZero);
        }
    }
}
