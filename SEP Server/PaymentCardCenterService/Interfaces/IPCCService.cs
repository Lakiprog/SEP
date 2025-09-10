using PaymentCardCenterService.Dto;

namespace PaymentCardCenterService.Interfaces
{
    public interface IPCCService
    {
        Task<TransactionResponseDto> ProcessTransaction(TransactionRequestDto request);
        Task<PCCPaymentResponse> ProcessPayment(PCCPaymentRequest request);
        Task<PCCTransaction> RecordTransaction(PCCPaymentRequest request);
        Task<string> GetIssuerBankUrl(string pan);
        Task<IssuerBankResponse> ForwardToIssuerBank(string issuerBankUrl, PCCPaymentRequest request);
        Task<PCCPaymentResponse> ProcessIssuerResponse(IssuerBankResponse issuerResponse, PCCTransaction pccTransaction);
        Task<PCCTransaction> GetTransactionByAcquirerOrderId(string acquirerOrderId);
        Task<List<PCCTransaction>> GetAllTransactions();
    }
}
