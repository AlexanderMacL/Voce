using System.Text;
using Voce.Core.Models;

namespace Voce.Core.Services;

/// <summary>
/// Builds PDF file names from a pattern. Tokens: {EMPLOYER} {NAME} {NUMBER} {DATE}.
/// Default pattern "Invoice-{EMPLOYER}-{NAME}".
/// </summary>
public static class InvoiceFileNamer
{
    public static string BaseName(string pattern, Invoice inv)
    {
        var s = pattern
            .Replace("{EMPLOYER}", inv.BillToName, StringComparison.OrdinalIgnoreCase)
            .Replace("{NAME}", inv.FromSingerName, StringComparison.OrdinalIgnoreCase)
            .Replace("{NUMBER}", inv.Number, StringComparison.OrdinalIgnoreCase)
            .Replace("{DATE}", inv.IssueDate.ToString("yyyy-MM-dd"), StringComparison.OrdinalIgnoreCase);
        s = Sanitize(s);
        return s.Length == 0 ? inv.Number : s;
    }

    /// <summary>
    /// Full path for an invoice's PDF. If the plain name is already taken by a different invoice
    /// (in the database or as an unknown file on disk), " (number)" is appended so nothing is overwritten.
    /// </summary>
    public static string PathFor(string folder, string pattern, Invoice inv, Func<string, bool> usedByOtherInvoice)
    {
        var plain = Path.Combine(folder, BaseName(pattern, inv) + ".pdf");
        var ownsPlain = string.Equals(inv.PdfPath, plain, StringComparison.OrdinalIgnoreCase);
        if (ownsPlain || (!usedByOtherInvoice(plain) && !File.Exists(plain)))
            return plain;
        return Path.Combine(folder, $"{BaseName(pattern, inv)} ({Sanitize(inv.Number)}).pdf");
    }

    public static string Sanitize(string s)
    {
        var invalid = Path.GetInvalidFileNameChars().Concat(['<', '>', ':', '"', '/', '\\', '|', '?', '*', '\t', '\r', '\n']).ToHashSet();
        var sb = new StringBuilder();
        foreach (var ch in s) sb.Append(invalid.Contains(ch) ? '_' : ch);
        return sb.ToString().Trim().TrimEnd('.');
    }
}
