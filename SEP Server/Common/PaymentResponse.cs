using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public class PaymentResponse
    {
        public string? PaymentUrl { get; set; }
        public string? PaymentId { get; set; }

        public PaymentResponse(string? paymentUrl, string? paymentId)
        {
            PaymentUrl = paymentUrl;
            PaymentId = paymentId;
        }
    }
}
