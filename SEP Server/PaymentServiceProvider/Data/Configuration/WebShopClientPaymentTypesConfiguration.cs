using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PaymentServiceProvider.Models;
using System.Reflection.Emit;

namespace PaymentServiceProvider.Data.Configuration
{
    public class WebShopClientPaymentTypesConfiguration : IEntityTypeConfiguration<WebShopClientPaymentTypes>
    {
        public void Configure(EntityTypeBuilder<WebShopClientPaymentTypes> builder)
        {
            builder.HasKey(x => new { x.ClientId, x.PaymentTypeId });

            builder.HasOne(x => x.WebShopClient)
                .WithMany(x => x.PaymentTypes)
                .HasForeignKey(x => x.ClientId);

            builder.HasOne(x => x.PaymentType)
                .WithMany()
                .HasForeignKey(x => x.PaymentTypeId);
        }
    }
}
