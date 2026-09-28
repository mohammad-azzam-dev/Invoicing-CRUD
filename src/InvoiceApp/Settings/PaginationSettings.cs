namespace InvoiceApp.Settings;

public sealed class PaginationSettings
{
    public const string SectionName = "Pagination";

    public int DefaultPage { get; set; } = 1;
    public int DefaultPageSize { get; set; } = 10;
    public int[] PageSizeOptions { get; set; } = [];
}
