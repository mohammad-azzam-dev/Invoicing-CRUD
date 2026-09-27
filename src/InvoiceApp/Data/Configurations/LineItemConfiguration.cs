using InvoiceApp.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvoiceApp.Data.Configurations;

public sealed class LineItemConfiguration : IEntityTypeConfiguration<LineItem>
{
    public void Configure(EntityTypeBuilder<LineItem> builder)
    {
        builder.HasKey(l => l.Id);

        builder.Property(l => l.Description).HasMaxLength(200).IsRequired();

        builder.Property(l => l.Quantity).HasConversion<double>();

        builder.Property(l => l.UnitPrice).HasConversion<double>();

        builder.Property(l => l.DiscountPercent).HasConversion<double>();
    }
}
