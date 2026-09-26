using InvoiceApp.Domain;
using Shouldly;

namespace InvoiceApp.Tests.Unit;

public sealed class InvoiceTests
{
    private static readonly DateOnly Today = new(2024, 1, 15);
    private static readonly DateOnly DueDate = Today.AddDays(30);
    private const int TestCustomerId = 1;

    [Fact]
    public void Create_ReturnsInvoiceWithDraftStatus()
    {
        var result = Invoice.Create(TestCustomerId, Today, DueDate, 21m);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Status.ShouldBe(InvoiceStatus.Draft);
        result.Value.CustomerId.ShouldBe(TestCustomerId);
        result.Value.IssueDate.ShouldBe(Today);
        result.Value.DueDate.ShouldBe(DueDate);
        result.Value.TaxRate.ShouldBe(21m);
    }

    [Fact]
    public void MarkAsSent_WhenDraftWithItems_ReturnsSuccess()
    {
        var invoice = CreateDraftInvoice();

        var result = invoice.MarkAsSent(itemCount: 3);

        result.IsSuccess.ShouldBeTrue();
        invoice.Status.ShouldBe(InvoiceStatus.Sent);
    }

    [Fact]
    public void MarkAsSent_WhenDraftWithNoItems_ReturnsFailure()
    {
        var invoice = CreateDraftInvoice();

        var result = invoice.MarkAsSent(itemCount: 0);

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error.ShouldContain("line items");
        invoice.Status.ShouldBe(InvoiceStatus.Draft);
    }

    [Fact]
    public void MarkAsSent_WhenNotDraft_ReturnsFailure()
    {
        var invoice = CreateSentInvoice();

        var result = invoice.MarkAsSent(itemCount: 1);

        result.IsSuccess.ShouldBeFalse();
        invoice.Status.ShouldBe(InvoiceStatus.Sent);
    }

    [Fact]
    public void MarkAsPaid_WhenSent_ReturnsSuccess()
    {
        var invoice = CreateSentInvoice();

        var result = invoice.MarkAsPaid();

        result.IsSuccess.ShouldBeTrue();
        invoice.Status.ShouldBe(InvoiceStatus.Paid);
    }

    [Fact]
    public void MarkAsPaid_WhenDraft_ReturnsFailure()
    {
        var invoice = CreateDraftInvoice();

        var result = invoice.MarkAsPaid();

        result.IsSuccess.ShouldBeFalse();
        invoice.Status.ShouldBe(InvoiceStatus.Draft);
    }

    [Fact]
    public void Cancel_WhenDraft_ReturnsSuccess()
    {
        var invoice = CreateDraftInvoice();

        var result = invoice.Cancel();

        result.IsSuccess.ShouldBeTrue();
        invoice.Status.ShouldBe(InvoiceStatus.Cancelled);
    }

    [Fact]
    public void Cancel_WhenSent_ReturnsSuccess()
    {
        var invoice = CreateSentInvoice();

        var result = invoice.Cancel();

        result.IsSuccess.ShouldBeTrue();
        invoice.Status.ShouldBe(InvoiceStatus.Cancelled);
    }

    [Fact]
    public void Cancel_WhenPaid_ReturnsFailure()
    {
        var invoice = CreatePaidInvoice();

        var result = invoice.Cancel();

        result.IsSuccess.ShouldBeFalse();
        invoice.Status.ShouldBe(InvoiceStatus.Paid);
    }

    [Fact]
    public void CanEdit_WhenDraft_ReturnsTrue()
    {
        var invoice = CreateDraftInvoice();

        invoice.CanEdit().ShouldBeTrue();
    }

    [Theory]
    [InlineData(InvoiceStatus.Sent)]
    [InlineData(InvoiceStatus.Paid)]
    [InlineData(InvoiceStatus.Cancelled)]
    public void CanEdit_WhenNotDraft_ReturnsFalse(InvoiceStatus status)
    {
        var invoice = CreateInvoiceWithStatus(status);

        invoice.CanEdit().ShouldBeFalse();
    }

    [Fact]
    public void CanDelete_WhenDraft_ReturnsTrue()
    {
        var invoice = CreateDraftInvoice();

        invoice.CanDelete().ShouldBeTrue();
    }

    [Theory]
    [InlineData(InvoiceStatus.Sent)]
    [InlineData(InvoiceStatus.Paid)]
    [InlineData(InvoiceStatus.Cancelled)]
    public void CanDelete_WhenNotDraft_ReturnsFalse(InvoiceStatus status)
    {
        var invoice = CreateInvoiceWithStatus(status);

        invoice.CanDelete().ShouldBeFalse();
    }

    [Fact]
    public void Update_WhenDraft_ReturnsSuccessAndUpdatesFields()
    {
        var invoice = CreateDraftInvoice();
        var newIssueDate = Today.AddDays(1);
        var newDueDate = Today.AddDays(45);
        const int newCustomerId = 2;

        var result = invoice.Update(newCustomerId, newIssueDate, newDueDate, 15m);

        result.IsSuccess.ShouldBeTrue();
        invoice.CustomerId.ShouldBe(newCustomerId);
        invoice.IssueDate.ShouldBe(newIssueDate);
        invoice.DueDate.ShouldBe(newDueDate);
        invoice.TaxRate.ShouldBe(15m);
    }

    [Fact]
    public void Update_WhenNotDraft_ReturnsFailure()
    {
        var invoice = CreateSentInvoice();

        var result = invoice.Update(2, Today, DueDate, 15m);

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error.ShouldContain("draft");
    }

    private static Invoice CreateDraftInvoice()
    {
        return Invoice.Create(TestCustomerId, Today, DueDate, 21m).Value!;
    }

    private static Invoice CreateSentInvoice()
    {
        var invoice = CreateDraftInvoice();
        invoice.MarkAsSent(1);
        return invoice;
    }

    private static Invoice CreatePaidInvoice()
    {
        var invoice = CreateSentInvoice();
        invoice.MarkAsPaid();
        return invoice;
    }

    private static Invoice CreateInvoiceWithStatus(InvoiceStatus status)
    {
        var invoice = CreateDraftInvoice();

        if (status == InvoiceStatus.Sent || status == InvoiceStatus.Paid)
        {
            invoice.MarkAsSent(1);
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
}
