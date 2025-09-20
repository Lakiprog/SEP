using Microsoft.EntityFrameworkCore;
using BitcoinPaymentService.Data.Entities;
using BitcoinPaymentService.Models;

namespace BitcoinPaymentService.Data
{
    public class BitcoinPaymentDbContext : DbContext
    {
        public BitcoinPaymentDbContext(DbContextOptions<BitcoinPaymentDbContext> options) : base(options)
        {
        }

        public DbSet<Transaction> Transactions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Transaction entity
            modelBuilder.Entity<Transaction>(entity =>
            {
                entity.HasKey(e => e.Id);

                // Configure GUID properties for SQL Server
                entity.Property(e => e.Id)
                    .HasColumnType("uniqueidentifier")
                    .HasDefaultValueSql("NEWID()");

                entity.Property(e => e.TelecomServiceId)
                    .HasColumnType("uniqueidentifier");

                entity.HasIndex(e => e.TransactionId)
                    .IsUnique()
                    .HasDatabaseName("IX_transactions_transaction_id");

                entity.HasIndex(e => e.BuyerEmail)
                    .HasDatabaseName("IX_transactions_buyer_email");

                entity.HasIndex(e => e.Status)
                    .HasDatabaseName("IX_transactions_status");

                entity.HasIndex(e => e.CreatedAt)
                    .HasDatabaseName("IX_transactions_created_at");

                entity.HasIndex(e => e.TelecomServiceId)
                    .HasDatabaseName("IX_transactions_telecom_service_id");

                // Configure enum to string conversion
                entity.Property(e => e.Status)
                    .HasConversion<string>()
                    .HasMaxLength(20);

                // Configure decimal precision
                entity.Property(e => e.Amount)
                    .HasColumnType("decimal(18,8)");

                // Configure datetime columns
                entity.Property(e => e.CreatedAt)
                    .HasColumnType("datetime2");

                entity.Property(e => e.UpdatedAt)
                    .HasColumnType("datetime2");
            });
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                // Fallback connection string (should be configured in Program.cs)
                optionsBuilder.UseSqlServer("Server=localhost;Database=CryptoDB;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False");
            }
        }
    }
}