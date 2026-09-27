using InvoiceApp.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace InvoiceApp.Data;

public static class DbSeeder
{
    public const string DemoEmail = "admin@test.com";
    public const string DemoPassword = "12345678";

    public static async Task SeedAsync(IServiceProvider services)
    {
        await SeedUserAsync(services);
        await SeedCustomersAndInvoicesAsync(services);
    }

    private static async Task SeedUserAsync(IServiceProvider services)
    {
        var userManager = services.GetRequiredService<UserManager<IdentityUser>>();

        var existingUser = await userManager.FindByEmailAsync(DemoEmail);
        if (existingUser is not null)
        {
            return;
        }

        var user = new IdentityUser
        {
            UserName = DemoEmail,
            Email = DemoEmail,
            EmailConfirmed = true,
        };

        await userManager.CreateAsync(user, DemoPassword);
    }

    private static async Task SeedCustomersAndInvoicesAsync(IServiceProvider services)
    {
        var dbFactory = services.GetRequiredService<IDbContextFactory<AppDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();

        if (await db.Customers.AnyAsync())
        {
            return;
        }

        // Create 10 customers
        var customers = CreateSeedCustomers();
        db.Customers.AddRange(customers);
        await db.SaveChangesAsync();

        // Create invoices referencing the customers
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var invoices = CreateSeedInvoices(today, customers);

        db.Invoices.AddRange(invoices);
        await db.SaveChangesAsync();

        var lineItems = CreateSeedLineItems(invoices);
        db.LineItems.AddRange(lineItems);
        await db.SaveChangesAsync();
    }

    private static List<Customer> CreateSeedCustomers()
    {
        return
        [
            Customer.Create(
                "John Smith",
                "555-0101",
                "john.smith@acme.com",
                "Acme Corporation",
                "123 Main St, New York, NY 10001"
            ),
            Customer.Create(
                "Sarah Johnson",
                "555-0102",
                "sarah@techstart.io",
                "TechStart Inc.",
                "456 Innovation Blvd, San Francisco, CA 94102"
            ),
            Customer.Create(
                "Michael Chen",
                "555-0103",
                "mchen@globalsolutions.com",
                "Global Solutions Ltd.",
                "789 Enterprise Ave, Chicago, IL 60601"
            ),
            Customer.Create(
                "Emily Davis",
                "555-0104",
                "emily.davis@digitaldynamics.net",
                "Digital Dynamics",
                "321 Tech Park Dr, Austin, TX 78701"
            ),
            Customer.Create(
                "Robert Wilson",
                "555-0105",
                "rwilson@smithpartners.com",
                "Smith & Partners",
                "555 Legal Way, Boston, MA 02101"
            ),
            Customer.Create(
                "Jennifer Lee",
                "555-0106",
                "jlee@innovationlabs.co",
                "Innovation Labs",
                "777 Research Rd, Seattle, WA 98101"
            ),
            Customer.Create(
                "David Martinez",
                "555-0107",
                "david@enterprise-sys.com",
                "Enterprise Systems",
                "888 Corporate Blvd, Denver, CO 80201"
            ),
            Customer.Create(
                "Lisa Anderson",
                "555-0108",
                "lisa@cloudnine.services",
                "Cloud Nine Services",
                "999 Cloud St, Portland, OR 97201"
            ),
            Customer.Create(
                "James Taylor",
                "555-0109",
                "jtaylor@futuretech.corp",
                "Future Tech Corp",
                "111 Future Lane, Miami, FL 33101"
            ),
            Customer.Create(
                "Amanda Brown",
                "555-0110",
                "amanda.brown@gmail.com",
                null,
                "222 Freelance Ave, Los Angeles, CA 90001"
            ),
        ];
    }

    private static List<Invoice> CreateSeedInvoices(DateOnly today, List<Customer> customers)
    {
        var invoices = new List<Invoice>();
        var random = new Random(42); // Fixed seed for reproducibility

        var taxRates = new[] { 0m, 5m, 10m, 15m, 20m, 21m };

        // Generate 100 invoices with varied distribution:
        // ~15 Draft, ~25 Sent (10 overdue), ~45 Paid, ~15 Cancelled
        for (var i = 0; i < 100; i++)
        {
            var customer = customers[random.Next(customers.Count)];
            var taxRate = taxRates[random.Next(taxRates.Length)];
            var daysAgo = random.Next(1, 180); // Issue date 1-180 days ago
            var paymentTerms = random.Next(15, 45); // 15-45 day payment terms
            var issueDate = today.AddDays(-daysAgo);
            var dueDate = issueDate.AddDays(paymentTerms);

            var status = i switch
            {
                < 15 => InvoiceStatus.Draft,
                < 40 => InvoiceStatus.Sent,
                < 85 => InvoiceStatus.Paid,
                _ => InvoiceStatus.Cancelled,
            };

            // Adjust dates for realistic scenarios
            if (status == InvoiceStatus.Sent && i >= 30 && i < 40)
            {
                // Make some Sent invoices overdue
                dueDate = today.AddDays(-random.Next(1, 30));
            }

            if (status == InvoiceStatus.Paid)
            {
                // Paid invoices should have past due dates
                dueDate = today.AddDays(-random.Next(10, 90));
                issueDate = dueDate.AddDays(-paymentTerms);
            }

            invoices.Add(CreateInvoice(customer.Id, issueDate, dueDate, taxRate, status));
        }

        return invoices;
    }

    private static Invoice CreateInvoice(
        int customerId,
        DateOnly issueDate,
        DateOnly dueDate,
        decimal taxRate,
        InvoiceStatus status
    )
    {
        var result = Invoice.Create(customerId, issueDate, dueDate, taxRate);
        var invoice = result.Value!;

        // Transition to target status
        if (status == InvoiceStatus.Sent || status == InvoiceStatus.Paid)
        {
            invoice.MarkAsSent(1); // Pretend there's at least 1 item for transition
        }

        if (status == InvoiceStatus.Paid)
        {
            invoice.MarkAsPaid();
        }

        if (status == InvoiceStatus.Cancelled)
        {
            invoice.Cancel();
        }

        return invoice;
    }

    private static List<LineItem> CreateSeedLineItems(List<Invoice> invoices)
    {
        var lineItems = new List<LineItem>();
        var descriptions = new[]
        {
            "Consulting Services",
            "Software Development",
            "UI/UX Design",
            "Project Management",
            "Technical Support",
            "Data Analysis",
            "System Integration",
            "Training Session",
            "Code Review",
            "Architecture Planning",
            "Security Audit",
            "Performance Optimization",
        };

        var random = new Random(42); // Fixed seed for reproducibility

        foreach (var invoice in invoices)
        {
            var itemCount = random.Next(1, 6); // 1-5 items per invoice
            for (var i = 0; i < itemCount; i++)
            {
                var description = descriptions[random.Next(descriptions.Length)];
                var quantity = Math.Round((decimal)(random.NextDouble() * 9 + 1), 2); // 1-10
                var unitPrice = Math.Round((decimal)(random.NextDouble() * 490 + 10), 2); // 10-500
                var discountPercent = random.Next(0, 21); // 0-20%

                lineItems.Add(
                    LineItem.Create(invoice.Id, description, quantity, unitPrice, discountPercent)
                );
            }
        }

        return lineItems;
    }
}
