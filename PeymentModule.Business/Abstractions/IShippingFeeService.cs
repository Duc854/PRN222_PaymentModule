namespace PaymentModule.Business.Abstraction
{
    public interface IShippingFeeService
    {
        Task<decimal> CalculateShippingFeeAsync(int cartOrderId, int buyerAddressId);
    }
}