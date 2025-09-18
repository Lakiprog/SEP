using BitcoinPaymentService.Models;

namespace BitcoinPaymentService.Interfaces
{
    public interface ICoinPaymentsService
    {
        Task<CreateTransactionResponse?> CreateTransactionAsync(CreateTransactionRequest request);
        Task<GetTransactionInfoResponse?> GetTransactionInfoAsync(string txnId);
        Task<QRCodeResponse> GenerateQRCodeAsync(QRCodeRequest request);
        Task<bool> IsPaymentExpiredAsync(string transactionId);
        Task<string> GetPaymentStatusAsync(string transactionId);
        Task<CreateInvoiceResponse?> CreateInvoiceAsync(CreateInvoiceRequest request);
    }
}