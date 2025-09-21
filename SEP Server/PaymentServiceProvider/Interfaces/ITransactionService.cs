using PaymentServiceProvider.Models;

namespace PaymentServiceProvider.Interfaces
{
    public interface ITransactionService
    {
        Task<List<Transaction>> GetAllTransactions();
        Task<List<Transaction>> GetAllTransactionsByWebShopClientId(int webShopClientId);
        Task<List<Transaction>> GetTransactionsByClientId(int clientId, int page = 1, int pageSize = 10);
        Task<Transaction> GetById(int id);
        Task<Transaction> GetByPSPTransactionId(string pspTransactionId);
        Task<Transaction?> GetByExternalTransactionId(string externalTransactionId);
        Task<Transaction> GetByMerchantOrderId(string merchantOrderId);
        Task<Transaction> AddTransaction(Transaction transaction);
        Task<Transaction> UpdateTransaction(Transaction transaction);
        Task<bool> RemoveTransaction(int id);
        
        // New method for admin functionality
        Task<List<Transaction>> GetAllAsync();
    }
}
