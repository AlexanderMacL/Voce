using System.Globalization;
using System.Text;
using Voce.Core.Models;

namespace Voce.Core.Services;

public static class InvoiceCsvExporter
{
    public static readonly string[] Headers =
    [
        "Invoice number", "Issue date", "Due date", "Event date", "Employer", "Description", "Total (GBP)", "Status", "Paid date", "Profile name",
    ];

    public static string ToCsv(IEnumerable<Invoice> invoices)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(",", Headers.Select(Quote)));
        foreach (var i in invoices.OrderBy(i => i.IssueDate).ThenBy(i => i.Number, StringComparer.Ordinal))
        {
            var description = string.Join("; ", i.Lines.Select(l => l.Description.Replace('\t', ' ')).Where(s => s.Length > 0));
            sb.AppendLine(string.Join(",", new[]
            {
                i.Number,
                i.IssueDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                i.DueDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                i.EventDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "",
                i.BillToName,
                description,
                i.Total.ToString("0.00", CultureInfo.InvariantCulture),
                i.IsPaid ? "Paid" : "Unpaid",
                i.PaidDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "",
                i.FromSingerName,
            }.Select(Quote)));
        }
        return sb.ToString();
    }

    public static void Write(string path, IEnumerable<Invoice> invoices) =>
        File.WriteAllText(path, ToCsv(invoices), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true)); // BOM so Excel reads £ correctly

    private static string Quote(string s) =>
        s.IndexOfAny([',', '"', '\n', '\r']) >= 0 ? "\"" + s.Replace("\"", "\"\"") + "\"" : s;
}
