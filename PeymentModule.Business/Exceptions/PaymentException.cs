using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PaymentModule.Business.Exceptions
{
    public class PaymentException : Exception
    {
        public string? TransactionID  { get; set; }
        public PaymentException(string transactionId, string message, Exception? inner = null)
            : base(message, inner)
        {
            TransactionID = transactionId;
        }
    }
}
