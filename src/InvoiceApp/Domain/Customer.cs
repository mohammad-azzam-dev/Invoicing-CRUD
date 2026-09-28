namespace InvoiceApp.Domain;

public sealed class Customer
{
    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? CompanyName { get; private set; }
    public string? Address { get; private set; }
    public string Phone { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;

    // Navigation property (not stored)
    public ICollection<Invoice> Invoices { get; private set; } = new List<Invoice>();

    private Customer() { }

    public static Customer Create(
        string name,
        string phone,
        string email,
        string? companyName = null,
        string? address = null
    )
    {
        return new Customer
        {
            Name = name.Trim(),
            Phone = phone.Trim(),
            Email = email.Trim(),
            CompanyName = companyName?.Trim(),
            Address = address?.Trim(),
        };
    }

    public void Update(
        string name,
        string phone,
        string email,
        string? companyName = null,
        string? address = null
    )
    {
        Name = name.Trim();
        Phone = phone.Trim();
        Email = email.Trim();
        CompanyName = companyName?.Trim();
        Address = address?.Trim();
    }
}
