namespace PaymentModule.Business.Exception
{
    public class ShippingApiException : System.Exception
    {
        public ShippingApiException(string message) : base(message) { }
        public ShippingApiException(string message, System.Exception innerException)
            : base(message, innerException) { }
    }
}