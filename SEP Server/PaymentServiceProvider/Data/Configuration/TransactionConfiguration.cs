using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PaymentServiceProvider.Models;

namespace PaymentServiceProvider.Data.Configuration
{
    public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
    {
        public void Configure(EntityTypeBuilder<Transaction> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id)
                .ValueGeneratedOnAdd();

            builder.HasOne(x => x.WebShopClient)
               .WithMany(x => x.Transactions)
               .HasForeignKey(x => x.WebShopClientId)
               .IsRequired()
               .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
