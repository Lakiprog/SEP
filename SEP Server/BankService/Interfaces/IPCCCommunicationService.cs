using BankService.Models;

namespace BankService.Interfaces
{
    public interface IPCCCommunicationService
    {
        Task<PCCResponse> SendTransactionToPCC(PCCRequest request);
        Task<PCCResponse> GetTransactionStatus(string acquirerOrderId);
        Task<PCCResponse> ProcessPaymentRequest(PCCRequest request);
    }
}
