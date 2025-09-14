using PaymentServiceProvider.Interfaces;
using PaymentServiceProvider.Models;
using System.Text.Json;

namespace PaymentServiceProvider.Services.Plugins
{
    public class QRPaymentPlugin : IPaymentPlugin
    {
        public string Name => "QR Code Payment";
        public string Type => "qr";
        public bool IsEnabled => true;

        public async Task<PaymentResponse> ProcessPaymentAsync(PaymentRequest request, Transaction transaction)
        {
            try
            {
                // Generate redirect URL to bank React application QR payment page
                // Pass transaction details as query parameters
                var bankFrontendUrl = "http://localhost:3002"; // Bank React app URL
                var bankQRUrl = $"{bankFrontendUrl}/qr-payment" +
                    $"?amount={transaction.Amount}" +
                    $"&currency=RSD" +
                    $"&merchantId={request.MerchantId}" +
                    $"&orderId={transaction.MerchantOrderId}" +
                    $"&pspTransactionId={transaction.PSPTransactionId}" +
                    $"&returnUrl={request.ReturnURL}" +
                    $"&cancelUrl={request.CancelURL}";
                
                Console.WriteLine($"[DEBUG] QR Payment Plugin redirecting to: {bankQRUrl}");
                
                return new PaymentResponse
                {
                    Success = true,
                    Message = "Redirecting to bank for QR payment",
                    PaymentUrl = bankQRUrl,
                    PSPTransactionId = transaction.PSPTransactionId
                };
            }
            catch (Exception ex)
            {
                return new PaymentResponse
                {
                    Success = false,
                    Message = $"QR payment processing error: {ex.Message}",
                    ErrorCode = "QR_PAYMENT_FAILED"
                };
            }
        }

        public async Task<PaymentStatusUpdate> GetPaymentStatusAsync(string externalTransactionId)
        {
            await Task.Delay(100);
            
            return new PaymentStatusUpdate
            {
                PSPTransactionId = externalTransactionId,
                Status = TransactionStatus.Pending,
                StatusMessage = "QR payment pending",
                UpdatedAt = DateTime.UtcNow
            };
        }

        public async Task<bool> RefundPaymentAsync(string externalTransactionId, decimal amount)
        {
            // Simulate QR payment refund
            await Task.Delay(100);
            return true;
        }

        public async Task<PaymentCallback> ProcessCallbackAsync(Dictionary<string, object> callbackData)
        {
            await Task.Delay(100);
            
            return new PaymentCallback
            {
                PSPTransactionId = callbackData.ContainsKey("transactionId") ? callbackData["transactionId"].ToString() : "",
                Status = TransactionStatus.Completed,
                StatusMessage = "QR payment completed",
                Timestamp = DateTime.UtcNow
            };
        }

        public bool ValidateConfiguration(string configuration)
        {
            // QR payment doesn't require special configuration
            return true;
        }
    }
}
