using BankService.Models;
using Microsoft.EntityFrameworkCore;

namespace BankService.Data
{
    public class BankServiceDbContext : DbContext
    {
        public BankServiceDbContext(DbContextOptions<BankServiceDbContext> options) : base(options) { }

        public DbSet<BankAccount> BankAccounts { get; set; }
        public DbSet<BankTransaction> BankTransactions { get; set; }
        public DbSet<PaymentCard> PaymentCards { get; set; }
        public DbSet<Merchant> Merchants { get; set; }
        public DbSet<RegularUser> RegularUsers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Configure decimal precision for Amount fields
            modelBuilder.Entity<BankAccount>()
                .Property(e => e.Balance)
                .HasPrecision(18, 2);
                
            modelBuilder.Entity<BankAccount>()
                .Property(e => e.ReservedBalance)
                .HasPrecision(18, 2);
                
            modelBuilder.Entity<BankTransaction>()
                .Property(e => e.Amount)
                .HasPrecision(18, 2);
            
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(BankServiceDbContext).Assembly);
        }
    }
}
