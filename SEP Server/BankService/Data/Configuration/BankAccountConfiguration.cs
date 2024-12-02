using BankService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BankService.Data.Configuration
{
    public class BankAccountConfiguration : IEntityTypeConfiguration<BankAccount>
    {
        public void Configure(EntityTypeBuilder<BankAccount> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedOnAdd();

            builder.Property(x => x.Balance).HasDefaultValue(0);
            builder.Property(x => x.ReservedBalance).HasDefaultValue(0);

            builder.HasOne(x => x.RegularUser)
                .WithMany(x => x.BankAccounts)
                .HasForeignKey(x => x.RegularUserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Merchant)
                .WithMany(x => x.BankAccounts)
                .HasForeignKey(x => x.MerchantId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
