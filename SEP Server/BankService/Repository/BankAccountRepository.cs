using BankService.Data;
using BankService.Interfaces;
using BankService.Models;
using Microsoft.EntityFrameworkCore;

namespace BankService.Repository
{
    public class BankAccountRepository : GenericRepository<BankAccount>, IBankAccountRepository
    {
        private readonly BankServiceDbContext _context;

        public BankAccountRepository(BankServiceDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<BankAccount?> GetByIdAsync(int id)
        {
            return await _context.BankAccounts.FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task UpdateAsync(BankAccount entity)
        {
            _context.BankAccounts.Update(entity);
            await _context.SaveChangesAsync();
        }

        public async Task<BankAccount?> GetAccountByCardNumber(string pan)
        {
            // Mock implementation - in real app this would query database
            return await Task.FromResult(new BankAccount
            {
                Id = 1,
                AccountNumber = "1234567890",
                Balance = 50000,
                RegularUserId = 1,
                BankId = 1
            });
        }

        public async Task<BankAccount?> GetMerchantAccount(string merchantId)
        {
            // Mock implementation - in real app this would query database
            return await Task.FromResult(new BankAccount
            {
                Id = 2,
                AccountNumber = "0987654321",
                Balance = 100000,
                MerchantId = 1,
                BankId = 1
            });
        }

        public async Task<Merchant?> GetMerchantByCredentials(string merchantId, string merchantPassword)
        {
            // Mock implementation - in real app this would query database
            if (merchantId == "TELECOM_001" && merchantPassword == "telecom123")
            {
                return await Task.FromResult(new Merchant
                {
                    Id = 1,
                    Merchant_Id = merchantId,
                    MerchantPassword = merchantPassword,
                    MerchantName = "Telecom Operator"
                });
            }
            return null;
        }

        public async Task UpdateAccount(BankAccount account)
        {
            // Mock implementation - in real app this would update database
            await Task.CompletedTask;
        }
    }
}
