using Voce.Core.Models;

namespace Voce.App.Pages;

/// <summary>
/// String-only view of an invoice for list templates. Keeps x:Bind away from Voce.Core types
/// (DateOnly etc.), which the XAML compiler's metadata loader can't always resolve.
/// </summary>
public sealed class InvoiceRow
{
    internal InvoiceRow(Invoice source)
    {
        Source = source;
        Number = source.Number;
        IssueDateText = source.IssueDateText;
        DueDateText = source.DueDateText;
        BillToName = source.BillToName;
        TotalText = source.TotalText;
        StatusText = source.StatusText;
    }

    internal Invoice Source { get; }
    public string Number { get; }
    public string IssueDateText { get; }
    public string DueDateText { get; }
    public string BillToName { get; }
    public string TotalText { get; }
    public string StatusText { get; }
}
