namespace InvoiceApp.Domain;

public static class InvoiceNumber
{
    public const string Prefix = "INV-";

    public static string Format(int id) => $"{Prefix}{id:D5}";

    public static int? TryParse(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return null;
        }

        string trimmed = input.Trim();

        if (trimmed.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
        {
            string numberPart = trimmed[Prefix.Length..];
            return int.TryParse(numberPart, out int id) ? id : null;
        }

        return int.TryParse(trimmed, out int directId) ? directId : null;
    }
}
