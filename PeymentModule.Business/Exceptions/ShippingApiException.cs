namespace PaymentModule.Business.Exceptions
{
    public class ShippingApiException : Exception
    {
        public string? TransactionID { get; set; }
        public ShippingApiException(string message) : base(message) { }

        public ShippingApiException(string transactionId, string message) : base(message) { }
        public ShippingApiException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}