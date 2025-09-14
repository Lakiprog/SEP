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
                // QR payment is handled by the customer payment selection page
                // This plugin just confirms that QR payment is available
                
                await Task.Delay(100); // Minimal delay for simulation
                
                return new PaymentResponse
                {
                    Success = true,
                    Message = "QR payment method available",
                    PaymentUrl = null // QR payment doesn't redirect immediately
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
