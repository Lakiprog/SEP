using BankService.Data;
using BankService.Interfaces;
using BankService.Models;
using Microsoft.EntityFrameworkCore;

namespace BankService.Repository
{
    public class MerchantRepository : GenericRepository<Merchant>, IMerchantRepository
    {
        private readonly BankServiceDbContext _context;

        public MerchantRepository(BankServiceDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<Merchant?> GetByMerchantIdAsync(string merchantId)
        {
            return await _context.Merchants.FirstOrDefaultAsync(m => m.Merchant_Id == merchantId);
        }
    }
}
