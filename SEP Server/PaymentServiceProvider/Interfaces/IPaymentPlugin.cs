using PaymentServiceProvider.Models;

namespace PaymentServiceProvider.Interfaces
{
    public interface IPaymentPlugin
    {
        string Name { get; }
        string Type { get; }
        bool IsEnabled { get; }
        
        Task<PaymentResponse> ProcessPaymentAsync(PaymentRequest request, Transaction transaction);
        Task<PaymentStatusUpdate> GetPaymentStatusAsync(string externalTransactionId);
        Task<bool> RefundPaymentAsync(string externalTransactionId, decimal amount);
        Task<PaymentCallback> ProcessCallbackAsync(Dictionary<string, object> callbackData);
        bool ValidateConfiguration(string configuration);
    }
}
