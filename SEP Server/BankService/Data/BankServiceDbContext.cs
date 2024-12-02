using BankService.Models;
using Microsoft.EntityFrameworkCore;

namespace BankService.Data
{
    public class BankServiceDbContext : DbContext
    {
        public BankServiceDbContext(DbContextOptions options) : base(options) { }

        public DbSet<BankAccount> BankAccounts { get; set; }
        public DbSet<BankTransaction> BankTransactions { get; set; }
        public DbSet<PaymentCard> PaymentCards { get; set; }
        public DbSet<Merchant> Merchants { get; set; }
        public DbSet<RegularUser> RegularUsers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(BankServiceDbContext).Assembly);
        }
    }
}
