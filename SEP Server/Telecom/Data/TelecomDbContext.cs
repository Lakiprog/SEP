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
        public DbSet<Category> Categories { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure PackageDeal - Category relationship
            modelBuilder.Entity<PackageDeal>()
                .HasOne(p => p.Category)
                .WithMany()
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            // Configure Subscription - PackageDeal relationship
            modelBuilder.Entity<Subscription>()
                .HasOne(s => s.Package)
                .WithMany()
                .HasForeignKey(s => s.PackageId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure Subscription - User relationship
            modelBuilder.Entity<Subscription>()
                .HasOne(s => s.User)
                .WithMany()
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(TelecomDbContext).Assembly);
        }
    }
}
