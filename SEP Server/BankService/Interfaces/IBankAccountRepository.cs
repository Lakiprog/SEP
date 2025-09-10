using BankService.Models;

namespace BankService.Interfaces
{
    public interface IBankAccountRepository : IGenericRepository<BankAccount>
    {
        Task<BankAccount?> GetByIdAsync(int id);
        Task UpdateAsync(BankAccount entity);
        Task<BankAccount?> GetAccountByCardNumber(string pan);
        Task<BankAccount?> GetMerchantAccount(string merchantId);
        Task<Merchant?> GetMerchantByCredentials(string merchantId, string merchantPassword);
        Task UpdateAccount(BankAccount account);
    }
}
