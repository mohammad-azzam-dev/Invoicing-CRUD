using InvoiceApp.Domain;
using Shouldly;

namespace InvoiceApp.Tests.Unit;

public sealed class InvoiceNumberTests
{
    [Fact]
    public void Prefix_IsINVDash()
    {
        InvoiceNumber.Prefix.ShouldBe("INV-");
    }

    [Fact]
    public void Format_WithId1_ReturnsINV00001()
    {
        InvoiceNumber.Format(1).ShouldBe("INV-00001");
    }

    [Fact]
    public void Format_WithId42_ReturnsINV00042()
    {
        InvoiceNumber.Format(42).ShouldBe("INV-00042");
    }

    [Fact]
    public void Format_WithId99999_ReturnsINV99999()
    {
        InvoiceNumber.Format(99999).ShouldBe("INV-99999");
    }

    [Fact]
    public void Format_WithLargeId_PadsCorrectly()
    {
        // With 6 digits, it exceeds the 5-digit padding but still formats correctly
        InvoiceNumber.Format(123456).ShouldBe("INV-123456");
    }

    [Fact]
    public void Format_WithZeroId_ReturnsINV00000()
    {
        InvoiceNumber.Format(0).ShouldBe("INV-00000");
    }
}
