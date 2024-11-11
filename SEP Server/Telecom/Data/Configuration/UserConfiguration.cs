using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Telecom.Enums;
using Telecom.Models;

namespace Telecom.Data.Configuration
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id)
                .ValueGeneratedOnAdd();

            builder.Property(x => x.Email)
                .IsRequired()
                .HasMaxLength(64);

            builder.Property(x => x.Password)
                .IsRequired();

            builder.Property(x => x.UserType)
                .HasConversion<String>()
                .IsRequired();
        }
    }
}
