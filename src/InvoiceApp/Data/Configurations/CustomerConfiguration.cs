using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using InvoiceApp.Domain;

namespace InvoiceApp.Data.Configurations;

public sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(c => c.CompanyName)
            .HasMaxLength(150);

        builder.Property(c => c.Address)
            .HasMaxLength(300);

        builder.Property(c => c.Phone)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(c => c.Email)
            .HasMaxLength(150)
            .IsRequired();
    }
}
