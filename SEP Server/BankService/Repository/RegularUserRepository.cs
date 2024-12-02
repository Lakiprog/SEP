using BankService.Data;
using BankService.Interfaces;
using BankService.Models;

namespace BankService.Repository
{
    public class RegularUserRepository : GenericRepository<RegularUser>, IRegularUserRepository
    {
        public RegularUserRepository(BankServiceDbContext context) : base(context)
        {
        }
    }
}
