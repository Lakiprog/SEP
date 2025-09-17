using BankService.Data;
using BankService.Interfaces;
using BankService.Models;
using Microsoft.EntityFrameworkCore;

namespace BankService.Repository
{
    public class BankTransactionRepository : GenericRepository<BankTransaction>, IBankTransactionRepository
    {
        private readonly BankServiceDbContext _context;

        public BankTransactionRepository(BankServiceDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<BankTransaction?> GetByPaymentIdAsync(string paymentId)
        {
            return await _context.BankTransactions.FirstOrDefaultAsync(t => t.PaymentId == paymentId);
        }

        public async Task UpdateAsync(BankTransaction entity)
        {
            var existingEntity = _context.Entry(entity);
            if (existingEntity.State == EntityState.Detached)
            {
                var tracked = await _context.BankTransactions.FindAsync(entity.Id);
                if (tracked != null)
                {
                    _context.Entry(tracked).CurrentValues.SetValues(entity);
                }
                else
                {
                    _context.BankTransactions.Update(entity);
                }
            }
            await _context.SaveChangesAsync();
        }

        public async Task AddAsync(BankTransaction entity)
        {
            _context.BankTransactions.Add(entity);
            await _context.SaveChangesAsync();
        }
    }
}
