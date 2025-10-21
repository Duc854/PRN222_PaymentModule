using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PaymentModule.Business.Abstraction;
using PaymentModule.Business.Abstractions;
using PaymentModule.Business.Dtos.BusinessDtos;
using PaymentModule.Business.Dtos.InputDtos;
using PaymentModule.Business.Dtos.OutputDtos;
using PaymentModule.Business.Exception;
using PaymentModule.Data.Abstractions;
using PaymentModule.Data.Entities;
using Polly;
using Polly.Retry;

namespace PaymentModule.Business.Services
{
    public class OrderTableService : IOrderTableService
    {
        private readonly IOrderTableRepository _orderTableRepo;
        private readonly IOrderItemRepository _orderItemRepo;
        private readonly IPaymentRepository _paymentRepo;
        private readonly IAddressRepository _addressRepo;
        private readonly IUserRepository _userRepo;
        private readonly IShippingService _shippingService;
        private readonly IShippingInfoRepository _shippingInfoRepo;
        private readonly ILogger<OrderTableService> _logger;
        private readonly AsyncRetryPolicy _shippingRetryPolicy;

        public OrderTableService(
            IOrderTableRepository orderTableRepo,
            IOrderItemRepository orderItemRepo,
            IPaymentRepository paymentRepo,
            IAddressRepository addressRepo,
            IUserRepository userRepo,
            IShippingService shippingService,
            IShippingInfoRepository shippingInfoRepo,
            ILogger<OrderTableService> logger
            )
        {
            _orderTableRepo = orderTableRepo;
            _orderItemRepo = orderItemRepo;
            _paymentRepo = paymentRepo;
            _addressRepo = addressRepo;
            _userRepo = userRepo;
            _shippingService = shippingService;
            _shippingInfoRepo = shippingInfoRepo;
            _logger = logger;

            // Cấu hình Retry Policy (Thử lại 3 lần, 1s, 2s, 4s)
            _shippingRetryPolicy = Policy
                .Handle<ShippingApiException>()
                .WaitAndRetryAsync(3, retryAttempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt - 1)),
                    (exception, timeSpan, retryCount, context) =>
                    {
                        _logger.LogWarning(exception, $"[Retry {retryCount}] Lỗi gọi API vận chuyển. Thử lại sau {timeSpan.TotalSeconds}s. Lỗi: {exception.Message}");
                    }
                );
        }

        public async Task<bool> AddToCartAsync(AddToCartInputDto input)
        {
            var order = await _orderTableRepo.GetUnpaidOrderTableByBuyerId(input.UserId);

            // Nếu chưa có giỏ hàng -> tạo mới
            if (order == null)
            {
                order = new PaymentModule.Data.Entities.OrderTable
                {
                    BuyerId = input.UserId,
                    Status = "Unpaid",
                    OrderDate = DateTime.Now,
                    TotalPrice = 0
                };

                await _orderTableRepo.CreateOrderTableAsync(order);
            }

            // Kiểm tra sản phẩm có sẵn trong OrderItem
            var existingItems = await _orderItemRepo.GetOrderItemsByOrderId(order.Id);
            var existing = existingItems.FirstOrDefault(i => i.ProductId == input.ProductId);

            if (existing != null)
            {
                existing.Quantity += input.Quantity;
                existing.UnitPrice = existing.UnitPrice; // giữ nguyên
                await _orderItemRepo.UpdateOrderItemAsync(existing);
            }
            else
            {
                var product = await _orderItemRepo.GetProductByIdAsync(input.ProductId);
                if (product == null) return false;

                var newItem = new PaymentModule.Data.Entities.OrderItem
                {
                    OrderId = order.Id,
                    ProductId = input.ProductId,
                    Quantity = input.Quantity,
                    UnitPrice = product.Price
                };
                await _orderItemRepo.CreateOrderItemAsync(newItem);
            }

            // Cập nhật tổng tiền
            var allItems = await _orderItemRepo.GetOrderItemsByOrderId(order.Id);
            order.TotalPrice = allItems.Sum(i => (i.UnitPrice ?? 0) * (i.Quantity ?? 0));
            await _orderTableRepo.UpdateOrderTableAsync(order);

            return true;
        }

        public async Task<UserCartOutputDto> GetCartByUserId(UserCartInputDto input)
        {
            var order = await _orderTableRepo.GetUnpaidOrderTableByBuyerId(input.UserId);
            if (order == null)
                return new UserCartOutputDto
                {
                    Success = false,
                    Message = "User currently don't have any product in cart",
                };

            var orderItems = await _orderItemRepo.GetOrderItemsByOrderId(order.Id);
            var itemDtos = new List<ItemInOrderTableDtos>();
            foreach (var item in orderItems)
            {
                if (item.Product != null && item.Quantity > 0 && item.Product.Title != null && item.Product.Price >= 0)
                {
                    itemDtos.Add(new ItemInOrderTableDtos
                    {
                        OrderItemId = item.Id,
                        Title = item.Product.Title,
                        Quantity = item.Quantity.Value,
                        Images = item.Product.Images ?? "No Image",
                        Price = item.Product.Price.Value,
                        TotalPrice = item.Quantity.Value * item.Product.Price.Value
                    });
                }
            }

            return new UserCartOutputDto
            {
                TotalPrice = order.TotalPrice.GetValueOrDefault(),
                Items = itemDtos
            };
        }

        public async Task<bool> UpdateCartItemAsync(UpdateCartItemDto input)
        {
            var item = await _orderItemRepo.GetOrderItemByIdAsync(input.OrderItemId);
            if (item == null) return false;

            item.Quantity = input.Quantity;
            await _orderItemRepo.UpdateOrderItemAsync(item);

            var order = await _orderTableRepo.GetOrderTableByBuyerId(item.Order.BuyerId ?? 0);
            var allItems = await _orderItemRepo.GetOrderItemsByOrderId(item.OrderId ?? 0);
            order.TotalPrice = allItems.Sum(i => (i.UnitPrice ?? 0) * (i.Quantity ?? 0));
            await _orderTableRepo.UpdateOrderTableAsync(order);

            return true;
        }

        public async Task<bool> DeleteCartItemAsync(UpdateCartItemDto input)
        {
            var item = await _orderItemRepo.GetOrderItemByIdAsync(input.OrderItemId);
            if (item == null) return false;

            await _orderItemRepo.DeleteOrderItemAsync(item);

            var order = await _orderTableRepo.GetOrderTableByBuyerId(item.Order.BuyerId ?? 0);
            var allItems = await _orderItemRepo.GetOrderItemsByOrderId(item.OrderId ?? 0);
            order.TotalPrice = allItems.Sum(i => (i.UnitPrice ?? 0) * (i.Quantity ?? 0));
            await _orderTableRepo.UpdateOrderTableAsync(order);

            return true;
        }

        public async Task<OrderTable> CompleteOrderAsync(int userId, int addressId, string method = "COD")
        {
            var order = await _orderTableRepo.GetUnpaidOrderTableByBuyerId(userId);
            if (order == null) return null;

            order.Status = "Processing";
            order.OrderDate = DateTime.Now;
            order.AddressId = addressId;
            await _orderTableRepo.UpdateOrderTableAsync(order);

            if (method == "COD")
            {
                var payment = new PaymentModule.Data.Entities.Payment
                {
                    UserId = userId,
                    OrderId = order.Id,
                    Amount = order.TotalPrice ?? 0,
                    Method = "COD",
                    PaidAt = DateTime.Now,
                    Status = "Completed"
                };

                await _paymentRepo.CreatePaymentAsync(payment);
            }

            return order;
        }

        public async Task CreateShipmentForOrderAsync(int orderId)
        {
            OrderTable order = null;
            try
            {
                order = await _orderTableRepo.GetOrderTableByIdAsync(orderId);
                if (order == null)
                {
                    _logger.LogError($"Không tìm thấy Order {orderId} để tạo vận đơn.");
                    return;
                }

                var address = await _addressRepo.GetByIdAsync(order.AddressId.Value);
                var user = await _userRepo.GetUserInfoById(order.BuyerId.Value);

                if (address == null || user == null)
                {
                    _logger.LogError($"Không tìm thấy Address hoặc User cho Order {orderId}.");
                    return;
                }

                var fullAddress = $"{address.Street}, {address.City}, {address.State}, {address.Country}";
                var buyerName = user.Username ?? address.FullName;

                // 2. Gọi API Vận chuyển (với Retry)
                CreateShipmentResponseDto shippingResponse = null;

                await _shippingRetryPolicy.ExecuteAsync(async () =>
                {
                    _logger.LogInformation($"Đang gọi API vận chuyển cho Order {orderId}...");
                    shippingResponse = await _shippingService.CreateShipmentAsync(orderId, buyerName, fullAddress);
                });

                if (shippingResponse != null && shippingResponse.Success)
                {
                    // 3. Lưu vào bảng ShippingInfo
                    var shippingInfo = new ShippingInfo
                    {
                        OrderId = orderId,
                        Carrier = shippingResponse.Carrier,
                        TrackingNumber = shippingResponse.TrackingNumber,
                        Status = shippingResponse.InitialStatus,
                        EstimatedArrival = shippingResponse.EstimatedArrival
                    };
                    await _shippingInfoRepo.CreateAsync(shippingInfo);

                    _logger.LogInformation($"Tạo vận đơn thành công cho Order {orderId}. Mã: {shippingResponse.TrackingNumber}");
                }
            }
            catch (ShippingApiException ex)
            {
                // LỖI (Sau khi đã retry 3 lần)
                _logger.LogError(ex, $"[Critical] KHÔNG THỂ tạo vận đơn cho Order {orderId} sau 3 lần thử. Lỗi: {ex.Message}");
                if (order != null)
                {
                    order.Status = "Shipping_Failed";
                    await _orderTableRepo.UpdateOrderTableAsync(order);
                }
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, $"Lỗi hệ thống khi tạo vận đơn cho Order {orderId}.");
                if (order != null && order.Status != "Shipping_Failed")
                {
                    order.Status = "Shipping_Error";
                    await _orderTableRepo.UpdateOrderTableAsync(order);
                }
            }
        }

        // HÀM LẤY LỊCH SỬ
        public async Task<List<OrderTable>> GetOrderHistoryAsync(int buyerId)
        {
            return await _orderTableRepo.GetOrderHistoryByBuyerIdAsync(buyerId);
        }

        // HÀM LẤY CHI TIẾT
        public async Task<OrderTable> GetOrderDetailsAsync(int orderId)
        {
            return await _orderTableRepo.GetOrderDetailsAsync(orderId);
        }

        // HÀM ĐỒNG BỘ TRẠNG THÁI
        public async Task<bool> SyncShipmentStatusAsync(int orderId)
        {
            _logger.LogInformation("Bắt đầu đồng bộ trạng thái cho Order {OrderId}", orderId);

            // 1. Lấy thông tin vận đơn từ DB
            var shippingInfo = await _shippingInfoRepo.GetByOrderIdAsync(orderId);
            if (shippingInfo == null)
            {
                _logger.LogWarning("Không tìm thấy ShippingInfo cho Order {OrderId}", orderId);
                return false;
            }

            try
            {
                // 2. Gọi API Giả lập để lấy trạng thái MỚI
                // (Hàm này chúng ta đã tạo ở MockShippingService)
                var update = await _shippingService.GetAndUpdateShipmentStatusAsync(shippingInfo.TrackingNumber);

                if (update.Success && update.NewStatus != shippingInfo.Status)
                {
                    // 3. Nếu trạng thái có thay đổi, cập nhật vào DB
                    _logger.LogInformation("Cập nhật trạng thái Order {OrderId} từ {OldStatus} sang {NewStatus}",
                        orderId, shippingInfo.Status, update.NewStatus);

                    await _shippingInfoRepo.UpdateStatusAsync(shippingInfo.Id, update.NewStatus);
                    return true;
                }

                _logger.LogInformation("Trạng thái Order {OrderId} không thay đổi.", orderId);
                return true;
            }
            catch (ShippingApiException ex)
            {
                _logger.LogError(ex, "Lỗi API khi đồng bộ trạng thái Order {OrderId}", orderId);
                return false;
            }
        }
    }
}