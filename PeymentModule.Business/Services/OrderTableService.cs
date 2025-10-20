using PaymentModule.Business.Abstractions;
using PaymentModule.Business.Dtos.BusinessDtos;
using PaymentModule.Business.Dtos.InputDtos;
using PaymentModule.Business.Dtos.OutputDtos;
using PaymentModule.Data.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PaymentModule.Business.Services
{
    public class OrderTableService : IOrderTableService
    {
        private readonly IOrderTableRepository _orderTableRepo;
        private readonly IOrderItemRepository _orderItemRepo;

        private readonly IPaymentRepository _paymentRepo;

        public OrderTableService(
            IOrderTableRepository orderTableRepo,
            IOrderItemRepository orderItemRepo,
            IPaymentRepository paymentRepo)
        {
            _orderTableRepo = orderTableRepo;
            _orderItemRepo = orderItemRepo;
            _paymentRepo = paymentRepo;
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
        public async Task CompleteOrderAsync(int userId, string method = "COD")
        {
            var order = await _orderTableRepo.GetUnpaidOrderTableByBuyerId(userId);
            if (order == null) return;

            order.Status = "Paid";
            order.OrderDate = DateTime.Now;
            await _orderTableRepo.UpdateOrderTableAsync(order);

            // ✅ Chỉ thêm payment nếu là COD (tránh trùng với PayPal)
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
        }

        public Task CompleteOrderAsync(int userId)
        {
            throw new NotImplementedException();
        }
    }

}
