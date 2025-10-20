namespace PaymentModule.Business.Dtos.InputDtos
{
    public class AddToCartInputDto
    {
        public int UserId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; } = 1;
    }
}
