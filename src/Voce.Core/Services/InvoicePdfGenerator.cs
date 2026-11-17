using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Voce.Core.Models;

namespace Voce.Core.Services;

/// <summary>Renders an invoice to an A4 PDF in a fixed, standard layout.</summary>
public static class InvoicePdfGenerator
{
    static InvoicePdfGenerator()
    {
        // Free for individuals and small businesses: https://www.questpdf.com/license/
        QuestPDF.Settings.License = LicenseType.Community;
    }

    private const string Accent = "#2F3E5C";

    public static void Generate(Invoice inv, string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        File.WriteAllBytes(path, GenerateBytes(inv));
    }

    public static byte[] GenerateBytes(Invoice inv) => Document.Create(doc => doc.Page(page =>
    {
        page.Size(PageSizes.A4);
        page.Margin(2, Unit.Centimetre);
        page.DefaultTextStyle(t => t.FontSize(10).FontColor(Colors.Grey.Darken4));

        page.Header().Element(c => Header(c, inv));
        page.Content().PaddingVertical(20).Element(c => Content(c, inv));
        page.Footer().AlignCenter().Text(t =>
        {
            t.DefaultTextStyle(s => s.FontSize(8).FontColor(Colors.Grey.Darken1));
            t.Span($"Invoice {inv.Number} · page ");
            t.CurrentPageNumber();
            t.Span(" of ");
            t.TotalPages();
        });
    })).GeneratePdf();

    private static void Header(IContainer c, Invoice inv) => c.Column(col =>
    {
        col.Item().Row(row =>
        {
            row.RelativeItem().Column(left =>
            {
                left.Item().Text(inv.FromSingerName).FontSize(20).Bold().FontColor(Accent);
                if (!string.IsNullOrWhiteSpace(inv.FromVoicePart))
                    left.Item().Text(inv.FromVoicePart).FontSize(11).Italic();
                left.Item().PaddingTop(6).Text(inv.FromAddress.Trim());
                if (!string.IsNullOrWhiteSpace(inv.FromEmail)) left.Item().Text(inv.FromEmail);
                if (!string.IsNullOrWhiteSpace(inv.FromPhone)) left.Item().Text(inv.FromPhone);
                if (!string.IsNullOrWhiteSpace(inv.FromUtr)) left.Item().Text($"UTR: {inv.FromUtr}");
            });
            row.ConstantItem(200).AlignRight().Column(right =>
            {
                right.Item().AlignRight().Text("INVOICE").FontSize(26).Bold().FontColor(Accent);
                right.Item().PaddingTop(8).Element(e => KeyValue(e, "Invoice no.", inv.Number));
                right.Item().Element(e => KeyValue(e, "Date", inv.IssueDate.ToString("d MMMM yyyy", Money.UkCulture)));
                right.Item().Element(e => KeyValue(e, "Due", inv.DueDate.ToString("d MMMM yyyy", Money.UkCulture)));
                if (inv.EventDate is { } ed)
                    right.Item().Element(e => KeyValue(e, "Event date", ed.ToString("d MMMM yyyy", Money.UkCulture)));
            });
        });
        col.Item().PaddingTop(12).LineHorizontal(1).LineColor(Accent);
    });

    private static void KeyValue(IContainer c, string key, string value) => c.Row(r =>
    {
        r.RelativeItem().AlignRight().Text(key + ":").SemiBold();
        r.ConstantItem(110).AlignRight().Text(value);
    });

    private static void Content(IContainer c, Invoice inv) => c.Column(col =>
    {
        col.Spacing(18);

        col.Item().Column(bill =>
        {
            bill.Item().Text("Bill to").FontSize(9).SemiBold().FontColor(Colors.Grey.Darken1);
            bill.Item().Text(inv.BillToName).Bold();
            if (!string.IsNullOrWhiteSpace(inv.BillToAddress)) bill.Item().Text(inv.BillToAddress.Trim());
        });

        col.Item().Table(table =>
        {
            table.ColumnsDefinition(cd =>
            {
                cd.RelativeColumn();
                cd.ConstantColumn(90);
            });
            table.Header(h =>
            {
                h.Cell().Background(Accent).Padding(6).Text("Description").FontColor(Colors.White).SemiBold();
                h.Cell().Background(Accent).Padding(6).AlignRight().Text("Amount").FontColor(Colors.White).SemiBold();
            });
            foreach (var line in inv.Lines)
            {
                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(6).Row(r =>
                {
                    foreach (var cell in line.Description.Split('\t'))
                        r.RelativeItem().Text(cell);
                });
                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(6).AlignRight().Text(Money.Format(line.Fee));
            }
            table.Cell().Padding(6).AlignRight().Text("Total").Bold();
            table.Cell().Padding(6).AlignRight().Text(Money.Format(inv.Total)).Bold().FontSize(12);
        });

        if (!string.IsNullOrWhiteSpace(inv.Notes))
            col.Item().Text(inv.Notes.Trim());

        col.Item().Background(Colors.Grey.Lighten4).Padding(12).Column(pay =>
        {
            pay.Spacing(2);
            pay.Item().Text("Payment details").Bold().FontColor(Accent);
            pay.Item().PaddingBottom(4).Text($"Please pay by bank transfer by {inv.DueDate.ToString("d MMMM yyyy", Money.UkCulture)}, quoting invoice number {inv.Number}.");
            if (!string.IsNullOrWhiteSpace(inv.FromAccountName)) pay.Item().Element(e => PayRow(e, "Account name", inv.FromAccountName));
            if (!string.IsNullOrWhiteSpace(inv.FromBankName)) pay.Item().Element(e => PayRow(e, "Bank", inv.FromBankName));
            if (!string.IsNullOrWhiteSpace(inv.FromSortCode)) pay.Item().Element(e => PayRow(e, "Sort code", inv.FromSortCode));
            if (!string.IsNullOrWhiteSpace(inv.FromAccountNumber)) pay.Item().Element(e => PayRow(e, "Account number", inv.FromAccountNumber));
            pay.Item().Element(e => PayRow(e, "Reference", inv.Number));
        });

        col.Item().AlignCenter().Text("Thank you.").Italic().FontColor(Colors.Grey.Darken1);
    });

    private static void PayRow(IContainer c, string key, string value) => c.Row(r =>
    {
        r.ConstantItem(110).Text(key).SemiBold();
        r.RelativeItem().Text(value);
    });
}
