namespace YourNamespace.Models
{
    public class ProductViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public decimal Price { get; set; }
        public decimal? OldPrice { get; set; }
        public string ImageUrl { get; set; } = "/images/placeholder.png";
        public string TagLine { get; set; } = "";
    }
}
