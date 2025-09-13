using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public class PaymentRequest
    {
        public string? MerchantId { get; set; }
        public string? MerchantPassword { get; set; }
        public double Amount { get; set; }
        public Guid MerchantOrderId { get; set; }
        public DateTime MerchantTimestamp { get; set; }
        public string? SuccessUrl { get; set; }
        public string? FailedUrl { get; set; }
        public string? ErrorUrl { get; set; }
        public PaymentRequest(string? merchantId, string? merchantPassword, double amount, Guid merchantOrderId, DateTime merchantTimestamp, string? successUrl, string? failedUrl, string? errorUrl)
        {
            MerchantId = merchantId;
            MerchantPassword = merchantPassword;
            Amount = amount;
            MerchantOrderId = merchantOrderId;
            MerchantTimestamp = merchantTimestamp;
            SuccessUrl = successUrl;
            FailedUrl = failedUrl;
            ErrorUrl = errorUrl;
        }
    }
}
