using BankService.Data;
using BankService.Interfaces;
using BankService.Models;

namespace BankService.Repository
{
    public class MerchantRepository : GenericRepository<Merchant>, IMerchantRepository
    {
        public MerchantRepository(BankServiceDbContext context) : base(context)
        {
        }
    }
}
