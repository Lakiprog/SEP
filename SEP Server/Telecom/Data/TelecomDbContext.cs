using Microsoft.EntityFrameworkCore;
using Telecom.Models;

namespace Telecom.Data
{
    public class TelecomDbContext : DbContext
    {
        public TelecomDbContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<PackageDeal> PackageDeals {  get; set; }
        public DbSet<PaymentType> PaymentTypes { get; set; }
        public DbSet<Subscription> Subscriptions { get; set; }
        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(TelecomDbContext).Assembly);
        }
    }
}
