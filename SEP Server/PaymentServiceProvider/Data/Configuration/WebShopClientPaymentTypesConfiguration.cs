using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PaymentServiceProvider.Models;

namespace PaymentServiceProvider.Data.Configuration
{
    public class WebShopClientPaymentTypesConfiguration : IEntityTypeConfiguration<WebShopClientPaymentTypes>
    {
        public void Configure(EntityTypeBuilder<WebShopClientPaymentTypes> builder)
        {
            builder.HasKey(x => x.Id); // Primary key for the join table
            builder.Property(x => x.Id)
                .ValueGeneratedOnAdd();

            builder.HasOne(x => x.WebShopClient)
                .WithMany(x => x.WebShopClientPaymentTypes)  // Refers to the navigation property in WebShopClient
                .HasForeignKey(x => x.ClientId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.PaymentType)
                .WithMany(x => x.WebShopClientPaymentTypes)  // Refers to the navigation property in PaymentType
                .HasForeignKey(x => x.PaymentTypeId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
