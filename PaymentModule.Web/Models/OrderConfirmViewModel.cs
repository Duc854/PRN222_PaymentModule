using System.Collections.Generic;

namespace CloneEbaySolution.Models.ViewModels
{
    public class OrderConfirmViewModel
    {
        public List<CartItem> Items { get; set; } = new List<CartItem>();
        public string FullName { get; set; }
        public string Street { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string PostalCode { get; set; }
        public string Country { get; set; }
        public string Phone { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Shipping { get; set; } = 5.00m;
        public decimal Discount { get; set; }
        public decimal Total => Subtotal + Shipping - Discount;
        public string PaymentMethod { get; set; }
    }

    public class CartItem
    {
        public string Title { get; set; }
        public string Image { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal Total => Price * Quantity;
    }
}
