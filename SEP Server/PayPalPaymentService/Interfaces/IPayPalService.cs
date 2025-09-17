using PayPalPaymentService.Models;

namespace PayPalPaymentService.Interfaces
{
    public interface IPayPalService
    {
        Task<string> GetAccessTokenAsync();
        Task<PayPalOrderResponse> CreateOrderAsync(PayPalOrderRequest orderRequest);
        Task<PayPalOrderResponse> CaptureOrderAsync(string orderId);
        Task<PayPalOrderResponse> GetOrderAsync(string orderId);
        
        // Subscription methods
        Task<PayPalProductResponse> CreateProductAsync(PayPalProduct product);
        Task<PayPalPlan> CreatePlanAsync(PayPalPlan plan);
        Task<PayPalSubscriptionResponse> CreateSubscriptionAsync(PayPalSubscriptionRequest subscriptionRequest);
        Task<PayPalSubscriptionResponse> GetSubscriptionAsync(string subscriptionId);
        Task<bool> CancelSubscriptionAsync(string subscriptionId, string reason);
        
        // Webhook verification
        Task<bool> VerifyWebhookSignatureAsync(string webhookId, string headers, string body);
    }
}
