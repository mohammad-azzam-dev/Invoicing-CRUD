using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using InvoiceApp.Domain;

namespace InvoiceApp.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<IdentityUser>(options)
{
    public DbSet<Customer> Customers { get; set; } = default!;
    public DbSet<Invoice> Invoices { get; set; } = default!;
    public DbSet<LineItem> LineItems { get; set; } = default!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
