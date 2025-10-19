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

        public OrderTableService(
            IOrderTableRepository orderTableRepo,
            IOrderItemRepository orderItemRepo)
        {
            _orderTableRepo = orderTableRepo;
            _orderItemRepo = orderItemRepo;
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
                TotalPrice = order.TotalPrice.Value,
                Items = itemDtos
            };
        }
    }
}
