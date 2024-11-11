using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Telecom.Models;

namespace Telecom.Data.Configuration
{
    public class PaymentTypeConfiguration : IEntityTypeConfiguration<PaymentType>
    {
        public void Configure(EntityTypeBuilder<PaymentType> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id)
                .ValueGeneratedOnAdd();

            builder.Property(x => x.Type)
                .IsRequired()
                .HasMaxLength(32);

            builder.Property(x => x.IsEnabled)
                .IsRequired();
        }
    }
}
