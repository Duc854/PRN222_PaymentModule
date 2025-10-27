using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PaymentModule.Business.Exceptions
{
    public class EmailException : Exception
    {
        public string? TransactionID { get; set; }
        public EmailException(string message) : base(message) { }
        public EmailException(string transcationId, string message) : base(message) { }
    }
}
