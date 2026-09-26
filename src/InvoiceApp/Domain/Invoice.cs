namespace InvoiceApp.Domain;

public sealed class Invoice
{
    public int Id { get; private set; }
    public int CustomerId { get; private set; }
    public DateOnly IssueDate { get; private set; }
    public DateOnly DueDate { get; private set; }
    public InvoiceStatus Status { get; private set; }
    public decimal TaxRate { get; private set; }

    // Navigation properties (not stored)
    public Customer Customer { get; private set; } = null!;
    public ICollection<LineItem> LineItems { get; private set; } = new List<LineItem>();

    private Invoice() { }

    public static Result<Invoice> Create(int customerId, DateOnly issueDate, DateOnly dueDate, decimal taxRate)
    {
        var invoice = new Invoice
        {
            CustomerId = customerId,
            IssueDate = issueDate,
            DueDate = dueDate,
            TaxRate = taxRate,
            Status = InvoiceStatus.Draft
        };

        return Result<Invoice>.Success(invoice);
    }

    public Result Update(int customerId, DateOnly issueDate, DateOnly dueDate, decimal taxRate)
    {
        if (!CanEdit())
        {
            return Result.Failure("Only draft invoices can be edited.");
        }

        CustomerId = customerId;
        IssueDate = issueDate;
        DueDate = dueDate;
        TaxRate = taxRate;

        return Result.Success();
    }

    public Result MarkAsSent(int itemCount)
    {
        if (itemCount == 0)
        {
            return Result.Failure("Cannot send an invoice without line items.");
        }

        if (!InvoiceStatusRules.CanTransition(Status, InvoiceStatus.Sent))
        {
            return Result.Failure($"Cannot transition from {Status} to Sent.");
        }

        Status = InvoiceStatus.Sent;
        return Result.Success();
    }

    public Result MarkAsPaid()
    {
        if (!InvoiceStatusRules.CanTransition(Status, InvoiceStatus.Paid))
        {
            return Result.Failure($"Cannot transition from {Status} to Paid.");
        }

        Status = InvoiceStatus.Paid;
        return Result.Success();
    }

    public Result Cancel()
    {
        if (!InvoiceStatusRules.CanTransition(Status, InvoiceStatus.Cancelled))
        {
            return Result.Failure($"Cannot transition from {Status} to Cancelled.");
        }

        Status = InvoiceStatus.Cancelled;
        return Result.Success();
    }

    public bool CanEdit() => Status == InvoiceStatus.Draft;

    public bool CanDelete() => Status == InvoiceStatus.Draft;
}
