namespace PaymentModule.Business.Dtos.BusinessDtos
{
    public class ProductDisplayDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string? Images { get; set; }
    }
}
