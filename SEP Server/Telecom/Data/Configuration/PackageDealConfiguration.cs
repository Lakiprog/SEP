using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Telecom.Models;

namespace Telecom.Data.Configuration
{
    public class PackageDealConfiguration : IEntityTypeConfiguration<PackageDeal>
    {
        public void Configure(EntityTypeBuilder<PackageDeal> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id)
                .ValueGeneratedOnAdd();

            builder.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(32);

            builder.Property(x => x.Description)
                .HasMaxLength(256);

            builder.Property(x => x.Price)
                .IsRequired();

        }
    }
}
