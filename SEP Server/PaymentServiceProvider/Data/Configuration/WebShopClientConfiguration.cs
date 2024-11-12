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

            builder.HasMany(x => x.PaymentTypes)
               .WithMany() // No back reference in PaymentType
               .UsingEntity(j => j.ToTable("WebShopClientPaymentTypes")); // Join table
        }
    }
}
