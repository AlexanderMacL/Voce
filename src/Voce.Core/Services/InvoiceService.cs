using Voce.Core.Data;
using Voce.Core.Models;

namespace Voce.Core.Services;

/// <summary>How to save changes to an invoice that already exists.</summary>
public enum AmendMode
{
    /// <summary>Keep the same number and overwrite the record and its PDF.</summary>
    Overwrite,
    /// <summary>Leave the original untouched and save the edits as a new invoice with a new number.</summary>
    Duplicate,
}

/// <summary>High-level invoice operations used by the UI: save, (re)generate PDF, mark paid, delete.</summary>
public sealed class InvoiceService(VoceDatabase db)
{
    public InvoiceRepository Invoices { get; } = new(db);
    public SettingsRepository Settings { get; } = new(db);

    /// <summary>Saves a brand-new invoice, allocating a number and writing its PDF. Returns the PDF path.</summary>
    public string CreateAndGenerate(Invoice inv)
    {
        inv.Id = 0;
        inv.Number = "";
        inv.PdfPath = null;
        Invoices.Insert(inv);
        return Regenerate(inv);
    }

    /// <summary>Saves edits to an existing invoice, either overwriting it or duplicating to a new number.</summary>
    public string SaveAmendment(Invoice inv, AmendMode mode)
    {
        if (inv.Id == 0 || mode == AmendMode.Duplicate)
        {
            inv.PaidDate = mode == AmendMode.Duplicate ? null : inv.PaidDate;
            return CreateAndGenerate(inv);
        }
        Invoices.Update(inv);
        return Regenerate(inv);
    }

    /// <summary>Writes (or rewrites) the PDF for a saved invoice and records its path.</summary>
    public string Regenerate(Invoice inv)
    {
        var path = InvoiceFileNamer.PathFor(Settings.PdfFolder, Settings.FileNamePattern, inv,
            p => Invoices.IsPdfPathUsedByOther(p, inv.Id));

        // If the name changed (e.g. employer renamed), remove the old file this invoice owned.
        if (inv.PdfPath is { } old && !string.Equals(old, path, StringComparison.OrdinalIgnoreCase)
            && File.Exists(old) && !Invoices.IsPdfPathUsedByOther(old, inv.Id))
        {
            TryDelete(old);
        }

        InvoicePdfGenerator.Generate(inv, path);
        inv.PdfPath = path;
        Invoices.SetPdfPath(inv.Id, path);
        return path;
    }

    public void MarkPaid(Invoice inv, DateOnly paidDate)
    {
        Invoices.SetPaid(inv.Id, paidDate);
        inv.PaidDate = paidDate;
    }

    public void MarkUnpaid(Invoice inv)
    {
        Invoices.SetPaid(inv.Id, null);
        inv.PaidDate = null;
    }

    /// <summary>Removes the invoice from the database. Its number stays used. Optionally deletes its PDF too.</summary>
    public void Delete(Invoice inv, bool deletePdf)
    {
        Invoices.Delete(inv.Id);
        if (deletePdf && inv.PdfPath is { } p && !Invoices.IsPdfPathUsedByOther(p, inv.Id))
            TryDelete(p);
    }

    /// <summary>Builds a fresh, unsaved invoice for the given profile/employer/event using the first-line template.</summary>
    public static Invoice NewDraft(Profile? profile, Employer? employer, EventType? ev, DateOnly issueDate, DateOnly? eventDate)
    {
        var inv = new Invoice { IssueDate = issueDate, EventDate = eventDate };
        if (profile is not null) inv.SnapshotProfile(profile);
        if (employer is not null) inv.SnapshotEmployer(employer);
        inv.DueDate = issueDate.AddDays(profile?.PaymentTermsDays ?? 30);
        inv.Lines.Add(LineTemplate.Expand(profile?.FirstLineTemplate ?? Profile.DefaultFirstLineTemplate, profile, employer, ev, eventDate));
        return inv;
    }

    private static void TryDelete(string path)
    {
        try { File.Delete(path); } catch (IOException) { } catch (UnauthorizedAccessException) { }
    }
}
