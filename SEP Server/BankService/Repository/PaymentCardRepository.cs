using BankService.Data;
using BankService.Interfaces;
using BankService.Models;
using Microsoft.EntityFrameworkCore;

namespace BankService.Repository
{
    public class PaymentCardRepository : GenericRepository<PaymentCard>, IPaymentCardRepository
    {
        private readonly BankServiceDbContext _context;

        public PaymentCardRepository(BankServiceDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<PaymentCard?> GetByPANAsync(string pan)
        {
            return await _context.PaymentCards.FirstOrDefaultAsync(c => c.CardNumber == pan);
        }
    }
}
