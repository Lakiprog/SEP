using BankService.Data;
using BankService.Interfaces;
using BankService.Models;

namespace BankService.Repository
{
    public class BankTransactionRepository : GenericRepository<BankTransaction>, IBankTransactionRepository
    {
        public BankTransactionRepository(BankServiceDbContext context) : base(context)
        {
        }
    }
}
