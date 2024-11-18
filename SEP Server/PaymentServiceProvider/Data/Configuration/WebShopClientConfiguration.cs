using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PaymentServiceProvider.Models;

namespace PaymentServiceProvider.Data.Configuration
{
    public class WebShopClientConfiguration : IEntityTypeConfiguration<WebShopClient>
    {
        public void Configure(EntityTypeBuilder<WebShopClient> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id)
                .ValueGeneratedOnAdd();

            builder.HasMany(x => x.Transactions)
                .WithOne(x => x.WebShopClient)
                .HasForeignKey(x => x.WebShopClientId)
                .OnDelete(DeleteBehavior.Cascade);

            // Correctly configure the many-to-many relationship using the join table WebShopClientPaymentTypes
            builder.HasMany(x => x.WebShopClientPaymentTypes)
                .WithOne(x => x.WebShopClient)
                .HasForeignKey(x => x.ClientId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
