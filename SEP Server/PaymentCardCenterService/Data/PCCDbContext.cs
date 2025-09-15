using Microsoft.EntityFrameworkCore;
using PaymentCardCenterService.Models;

namespace PaymentCardCenterService.Data
{
    public class PCCDbContext : DbContext
    {
        public PCCDbContext(DbContextOptions<PCCDbContext> options) : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                // This should not happen as we configure in Program.cs, but just in case
                optionsBuilder.UseSqlServer("Server=localhost\\SQLEXPRESS;Database=PCCDB;Integrated Security=True;");
            }
            
            // Suppress the pending model changes warning 
            optionsBuilder.ConfigureWarnings(warnings => 
                warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
                
            base.OnConfiguring(optionsBuilder);
        }

        public DbSet<Bank> Banks { get; set; }
        public DbSet<BinRange> BinRanges { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Bank entity
            modelBuilder.Entity<Bank>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.ApiUrl).IsRequired().HasMaxLength(500);
                entity.Property(e => e.ContactEmail).HasMaxLength(100);
                entity.Property(e => e.ContactPhone).HasMaxLength(20);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // Configure BinRange entity
            modelBuilder.Entity<BinRange>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.BinCode).IsRequired().HasMaxLength(4);
                entity.Property(e => e.CardType).HasMaxLength(50);
                entity.Property(e => e.Description).HasMaxLength(100);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                
                // BinCode mora biti jedinstven
                entity.HasIndex(e => e.BinCode).IsUnique();
                
                // Foreign key relationship
                entity.HasOne(e => e.Bank)
                      .WithMany(b => b.BinRanges)
                      .HasForeignKey(e => e.BankId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Seed initial data
            SeedData(modelBuilder);
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            // Seed Banks
            modelBuilder.Entity<Bank>().HasData(
                new Bank
                {
                    Id = 1,
                    Name = "Bank1 (Primary Bank)",
                    ApiUrl = "http://localhost:7000",
                    ContactEmail = "contact@bank1.com",
                    ContactPhone = "+381-11-1234567",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new Bank
                {
                    Id = 2,
                    Name = "Bank2 (External Bank)",
                    ApiUrl = "http://localhost:7100",
                    ContactEmail = "info@bank2.com", 
                    ContactPhone = "+381-11-7654321",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                }
            );

            // Seed BIN Ranges
            modelBuilder.Entity<BinRange>().HasData(
                // Bank1 kartice
                new BinRange
                {
                    Id = 1,
                    BinCode = "4111",
                    CardType = "Visa",
                    Description = "Bank1 Visa kartice",
                    BankId = 1,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new BinRange
                {
                    Id = 2,
                    BinCode = "5555",
                    CardType = "MasterCard",
                    Description = "Bank1 MasterCard kartice",
                    BankId = 1,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                
                // Bank2 kartice
                new BinRange
                {
                    Id = 3,
                    BinCode = "4222",
                    CardType = "Visa",
                    Description = "Bank2 Visa kartice",
                    BankId = 2,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new BinRange
                {
                    Id = 4,
                    BinCode = "5444",
                    CardType = "MasterCard", 
                    Description = "Bank2 MasterCard kartice",
                    BankId = 2,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                }
            );
        }
    }
}
