using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using PaymentServiceProvider.Models;

public class WebShopClientPaymentTypesConfiguration : IEntityTypeConfiguration<WebShopClientPaymentTypes>
{
    public void Configure(EntityTypeBuilder<WebShopClientPaymentTypes> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd();

        builder.HasOne(x => x.WebShopClient)
            .WithMany(x => x.PaymentTypes)
            .HasForeignKey(x => x.ClientId)
            .OnDelete(DeleteBehavior.Cascade); // You can adjust the delete behavior as needed

        builder.HasOne(x => x.PaymentType)
            .WithMany()
            .HasForeignKey(x => x.PaymentTypeId)
            .OnDelete(DeleteBehavior.Restrict); // Adjust if needed
    }
}
