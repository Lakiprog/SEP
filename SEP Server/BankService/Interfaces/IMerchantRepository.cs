using BankService.Models;

namespace BankService.Interfaces
{
    public interface IMerchantRepository : IGenericRepository<Merchant>
    {
        Task<Merchant?> GetByMerchantIdAsync(string merchantId);
    }
}
