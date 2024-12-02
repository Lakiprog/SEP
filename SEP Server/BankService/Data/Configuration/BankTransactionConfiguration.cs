using BankService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BankService.Data.Configuration
{
    public class BankTransactionConfiguration : IEntityTypeConfiguration<BankTransaction>
    {
        public void Configure(EntityTypeBuilder<BankTransaction> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedOnAdd();

            builder.HasOne(x => x.RegularUser)
                .WithMany(x => x.BankTransactions)
                .HasForeignKey(x => x.RegularUserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Merchant)
                .WithMany(x => x.BankTransactions)
                .HasForeignKey(x => x.MerchantId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
