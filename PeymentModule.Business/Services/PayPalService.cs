using PayPal.Api;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

// Alias để tránh trùng với Payment entity trong Data layer
using PayPalPayment = PayPal.Api.Payment;

namespace PaymentModule.Business.Services
{
    public class PayPalService
    {
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly string _mode;
        private readonly string _returnUrl;
        private readonly string _cancelUrl;

        public PayPalService(IConfiguration configuration)
        {
            var section = configuration.GetSection("PayPal");
            _clientId = section["ClientId"];
            _clientSecret = section["ClientSecret"];
            _mode = section["Mode"];
            _returnUrl = section["ReturnUrl"];
            _cancelUrl = section["CancelUrl"];
        }

        private APIContext GetAPIContext()
        {
            var config = new Dictionary<string, string>
            {
                { "mode", _mode },
                { "clientId", _clientId },
                { "clientSecret", _clientSecret }
            };

            var accessToken = new OAuthTokenCredential(_clientId, _clientSecret, config).GetAccessToken();
            return new APIContext(accessToken) { Config = config };
        }

        public PayPalPayment CreatePayment(decimal amount)
        {
            var apiContext = GetAPIContext();

            var payer = new Payer { payment_method = "paypal" };

            var redirectUrls = new RedirectUrls
            {
                cancel_url = _cancelUrl,
                return_url = _returnUrl
            };

            var amountObj = new Amount
            {
                currency = "USD",
                total = amount.ToString("F2")
            };

            var transactionList = new List<Transaction>
            {
                new Transaction
                {
                    description = "CloneEbay Payment",
                    amount = amountObj
                }
            };

            var payment = new PayPalPayment
            {
                intent = "sale",
                payer = payer,
                transactions = transactionList,
                redirect_urls = redirectUrls
            };

            return payment.Create(apiContext);
        }

        public PayPalPayment ExecutePayment(string paymentId, string payerId)
        {
            var apiContext = GetAPIContext();
            var paymentExecution = new PaymentExecution { payer_id = payerId };
            var payment = new PayPalPayment { id = paymentId };
            return payment.Execute(apiContext, paymentExecution);
        }
    }
}
