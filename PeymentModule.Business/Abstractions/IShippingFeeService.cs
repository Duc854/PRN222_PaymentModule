namespace PaymentModule.Business.Abstraction
{
    public interface IShippingFeeService
    {
        decimal CalculateShippingFee(string city);
    }
}