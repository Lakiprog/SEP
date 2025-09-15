using BankService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BankService.Data.Configuration
{
    public class PaymentCardConfiguration : IEntityTypeConfiguration<PaymentCard>
    {
        public void Configure(EntityTypeBuilder<PaymentCard> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedOnAdd();

            builder.Property(x => x.CardNumber).IsRequired();
            builder.Property(x => x.CardHolderName).IsRequired();
            builder.Property(x => x.ExpiryDate).IsRequired();
            builder.Property(x => x.SecurityCode).IsRequired();

            builder.HasOne(x => x.BankAccount)
                .WithMany(x => x.PaymentCards)
                .HasForeignKey(x => x.BankAccountId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
