using BankService.Data;
using BankService.Interfaces;
using BankService.Models;

namespace BankService.Repository
{
    public class BankAccountRepository : GenericRepository<BankAccount>, IBankAccountRepository
    {
        public BankAccountRepository(BankServiceDbContext context) : base(context)
        {
        }
    }
}
