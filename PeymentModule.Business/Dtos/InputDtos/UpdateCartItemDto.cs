namespace PaymentModule.Business.Dtos.InputDtos
{
    public class UpdateCartItemDto
    {
        public int OrderItemId { get; set; }
        public int Quantity { get; set; } = 1;
    }
}
