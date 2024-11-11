using Microsoft.EntityFrameworkCore;
using PaymentServiceProvider.Models;

namespace PaymentServiceProvider.Data
{
    public class PaymentServiceProviderDbContext : DbContext
    {
        public PaymentServiceProviderDbContext(DbContextOptions options) : base(options) { }
        
        public DbSet<PaymentType> PaymentTypes { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<WebShopClient> WebShopClients { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(PaymentServiceProviderDbContext).Assembly);
        }
    }
}
