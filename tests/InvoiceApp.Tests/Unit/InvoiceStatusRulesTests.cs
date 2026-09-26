using InvoiceApp.Domain;
using Shouldly;

namespace InvoiceApp.Tests.Unit;

public sealed class InvoiceStatusRulesTests
{
    [Theory]
    [InlineData(InvoiceStatus.Draft, InvoiceStatus.Sent)]
    [InlineData(InvoiceStatus.Draft, InvoiceStatus.Cancelled)]
    [InlineData(InvoiceStatus.Sent, InvoiceStatus.Paid)]
    [InlineData(InvoiceStatus.Sent, InvoiceStatus.Cancelled)]
    public void CanTransition_ValidTransitions_ReturnsTrue(InvoiceStatus from, InvoiceStatus to)
    {
        InvoiceStatusRules.CanTransition(from, to).ShouldBeTrue();
    }

    [Theory]
    [InlineData(InvoiceStatus.Draft, InvoiceStatus.Paid)]
    [InlineData(InvoiceStatus.Draft, InvoiceStatus.Draft)]
    [InlineData(InvoiceStatus.Sent, InvoiceStatus.Draft)]
    [InlineData(InvoiceStatus.Sent, InvoiceStatus.Sent)]
    [InlineData(InvoiceStatus.Paid, InvoiceStatus.Draft)]
    [InlineData(InvoiceStatus.Paid, InvoiceStatus.Sent)]
    [InlineData(InvoiceStatus.Paid, InvoiceStatus.Cancelled)]
    [InlineData(InvoiceStatus.Paid, InvoiceStatus.Paid)]
    [InlineData(InvoiceStatus.Cancelled, InvoiceStatus.Draft)]
    [InlineData(InvoiceStatus.Cancelled, InvoiceStatus.Sent)]
    [InlineData(InvoiceStatus.Cancelled, InvoiceStatus.Paid)]
    [InlineData(InvoiceStatus.Cancelled, InvoiceStatus.Cancelled)]
    public void CanTransition_InvalidTransitions_ReturnsFalse(InvoiceStatus from, InvoiceStatus to)
    {
        InvoiceStatusRules.CanTransition(from, to).ShouldBeFalse();
    }

    [Fact]
    public void AllowedNext_Draft_ReturnsSentAndCancelled()
    {
        var allowed = InvoiceStatusRules.AllowedNext(InvoiceStatus.Draft);

        allowed.ShouldBe([InvoiceStatus.Sent, InvoiceStatus.Cancelled]);
    }

    [Fact]
    public void AllowedNext_Sent_ReturnsPaidAndCancelled()
    {
        var allowed = InvoiceStatusRules.AllowedNext(InvoiceStatus.Sent);

        allowed.ShouldBe([InvoiceStatus.Paid, InvoiceStatus.Cancelled]);
    }

    [Fact]
    public void AllowedNext_Paid_ReturnsEmpty()
    {
        var allowed = InvoiceStatusRules.AllowedNext(InvoiceStatus.Paid);

        allowed.ShouldBeEmpty();
    }

    [Fact]
    public void AllowedNext_Cancelled_ReturnsEmpty()
    {
        var allowed = InvoiceStatusRules.AllowedNext(InvoiceStatus.Cancelled);

        allowed.ShouldBeEmpty();
    }
}
