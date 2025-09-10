using BankService.Models;

namespace BankService.Interfaces
{
    public interface IPaymentCardService
    {
        Task<CardValidationResult> ValidateCardAsync(string pan, string securityCode, string cardHolderName, string expiryDate);
        Task<bool> ProcessPaymentAsync(string pan, decimal amount);
        Task<PaymentRequest> GetPaymentRequest(string paymentId);
        Task StorePaymentRequest(PaymentRequest request);
        Task UpdatePaymentStatus(BankTransactionStatus status);
        Task<PaymentRequest> GetPaymentRequestByOrderId(string merchantOrderId);
    }

    public class CardValidationResult
    {
        public bool IsValid { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
