using System.Collections.Frozen;

namespace InvoiceApp.Domain;

public static class InvoiceStatusRules
{
    private static readonly FrozenDictionary<InvoiceStatus, IReadOnlyList<InvoiceStatus>> AllowedTransitions =
        new Dictionary<InvoiceStatus, IReadOnlyList<InvoiceStatus>>
        {
            [InvoiceStatus.Draft] = [InvoiceStatus.Sent, InvoiceStatus.Cancelled],
            [InvoiceStatus.Sent] = [InvoiceStatus.Paid, InvoiceStatus.Cancelled],
            [InvoiceStatus.Paid] = [],
            [InvoiceStatus.Cancelled] = []
        }.ToFrozenDictionary();

    public static bool CanTransition(InvoiceStatus from, InvoiceStatus to)
    {
        return AllowedTransitions.TryGetValue(from, out var allowed) && allowed.Contains(to);
    }

    public static IReadOnlyList<InvoiceStatus> AllowedNext(InvoiceStatus from)
    {
        return AllowedTransitions.TryGetValue(from, out var allowed) ? allowed : [];
    }
}
