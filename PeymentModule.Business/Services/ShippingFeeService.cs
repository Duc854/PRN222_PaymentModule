using PaymentModule.Business.Abstraction;

namespace PaymentModule.Business.Services
{
    public class ShippingFeeService : IShippingFeeService
    {
        public decimal CalculateShippingFee(string city)
        {
            if (string.IsNullOrEmpty(city))
            {
                System.Console.WriteLine("City is null or empty. Defaulting shipping fee to 50000.");
                return 50000;
            }

            var normalizedCity = city.ToLower().Trim();

            if (normalizedCity == "hà nội" || normalizedCity == "hồ chí minh")
            {
                return 20000;
            }

            System.Console.WriteLine(normalizedCity + " is not a major city. Setting shipping fee to 50000.");
            return 50000;
        }
    }
}