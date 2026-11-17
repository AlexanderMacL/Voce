using Voce.Core.Data;
using Voce.Core.Models;
using Voce.Core.Services;

namespace Voce.Core.Tests;

public class InvoiceTests
{
    private static Invoice Draft(TestDb t, DateOnly date)
    {
        var profile = new ProfileRepository(t.Db).GetDefault();
        var employer = new EmployerRepository(t.Db).GetAll().First(e => e.Name == "Church A");
        return InvoiceService.NewDraft(profile, employer, employer.Events.First(e => e.Name == "Evensong"), date, date);
    }

    [Fact]
    public void New_database_is_seeded_with_placeholders()
    {
        using var t = new TestDb(seed: true);
        Assert.Equal(2, new ProfileRepository(t.Db).GetAll().Count);
        Assert.Single(new ProfileRepository(t.Db).GetAll(), p => p.IsDefault);
        Assert.Equal(2, new EmployerRepository(t.Db).GetAll().Count);
        Assert.Equal(2, new InvoiceRepository(t.Db).GetAll().Count);
        Assert.False(t.Db.Initialize()); // second run: not new, no duplicate seed
        Assert.Equal(2, new InvoiceRepository(t.Db).GetAll().Count);
    }

    [Fact]
    public void Numbers_are_per_day_and_gaps_are_never_refilled()
    {
        using var t = new TestDb(seed: true);
        var svc = new InvoiceService(t.Db);
        var day = new DateOnly(2026, 11, 17);

        var a = Draft(t, day); svc.CreateAndGenerate(a);
        var b = Draft(t, day); svc.CreateAndGenerate(b);
        Assert.Equal("2026-11-17-001", a.Number);
        Assert.Equal("2026-11-17-002", b.Number);

        svc.Delete(b, deletePdf: true);
        var c = Draft(t, day); svc.CreateAndGenerate(c);
        Assert.Equal("2026-11-17-003", c.Number);

        var other = Draft(t, day.AddDays(1)); svc.CreateAndGenerate(other);
        Assert.Equal("2026-11-18-001", other.Number);
    }

    [Fact]
    public void Round_trips_lines_and_blank_fees()
    {
        using var t = new TestDb(seed: true);
        var svc = new InvoiceService(t.Db);
        var inv = Draft(t, new DateOnly(2026, 6, 1));
        inv.Lines.Add(new InvoiceLine { Description = "Note only" });
        inv.Lines.Add(new InvoiceLine { Description = "Travel", Fee = 12.34m });
        svc.CreateAndGenerate(inv);

        var loaded = svc.Invoices.Get(inv.Id)!;
        Assert.Equal(["Jane Placeholder\tSoprano", "Note only", "Travel"], loaded.Lines.Select(l => l.Description));
        Assert.Null(loaded.Lines[1].Fee);
        Assert.Equal(98.34m, loaded.Total);
        Assert.Equal("Church A", loaded.BillToName);
        Assert.Equal("12345678", loaded.FromAccountNumber);
    }

    [Fact]
    public void Pdf_named_by_pattern_with_number_suffix_on_clash()
    {
        using var t = new TestDb(seed: true);
        var svc = new InvoiceService(t.Db);
        var a = Draft(t, new DateOnly(2026, 11, 17)); svc.CreateAndGenerate(a);
        var b = Draft(t, new DateOnly(2026, 11, 17)); svc.CreateAndGenerate(b);

        Assert.Equal("Invoice-Church A-Jane Placeholder.pdf", Path.GetFileName(a.PdfPath));
        Assert.Equal("Invoice-Church A-Jane Placeholder (2026-11-17-002).pdf", Path.GetFileName(b.PdfPath));
        Assert.True(File.Exists(a.PdfPath));
        Assert.True(File.Exists(b.PdfPath));

        // Regenerating keeps the same file.
        Assert.Equal(a.PdfPath, svc.Regenerate(a));
    }

    [Fact]
    public void Amend_overwrite_keeps_number_duplicate_gets_new_one()
    {
        using var t = new TestDb(seed: true);
        var svc = new InvoiceService(t.Db);
        var inv = Draft(t, new DateOnly(2026, 11, 17));
        svc.CreateAndGenerate(inv);
        var originalId = inv.Id;

        inv.Lines[0].Fee = 90m;
        svc.SaveAmendment(inv, AmendMode.Overwrite);
        Assert.Equal("2026-11-17-001", inv.Number);
        Assert.Equal(90m, svc.Invoices.Get(originalId)!.Total);

        inv.Lines[0].Fee = 95m;
        svc.SaveAmendment(inv, AmendMode.Duplicate);
        Assert.NotEqual(originalId, inv.Id);
        Assert.Equal("2026-11-17-002", inv.Number);
        Assert.Equal(90m, svc.Invoices.Get(originalId)!.Total);
        Assert.Equal(95m, svc.Invoices.Get(inv.Id)!.Total);
    }

    [Fact]
    public void Paid_and_dashboard_totals_by_tax_year()
    {
        using var t = new TestDb();
        new ProfileRepository(t.Db).Save(new Profile { Label = "p", SingerName = "S" });
        new EmployerRepository(t.Db).Save(new Employer { Name = "Church A", Events = { new EventType { Name = "Evensong", DefaultFee = 86m } } });
        var svc = new InvoiceService(t.Db);

        var before = Draft(t, new DateOnly(2026, 4, 5)); svc.CreateAndGenerate(before);
        var inYear1 = Draft(t, new DateOnly(2026, 4, 6)); svc.CreateAndGenerate(inYear1);
        var inYear2 = Draft(t, new DateOnly(2026, 11, 1)); svc.CreateAndGenerate(inYear2);
        svc.MarkPaid(inYear1, new DateOnly(2026, 5, 1));

        var ty = new TaxYear(2026);
        var list = svc.Invoices.GetAll(ty.Start, ty.End);
        var s = DashboardSummary.From(list, new DateOnly(2026, 12, 31));
        Assert.Equal(2, s.Count);
        Assert.Equal(172m, s.TotalInvoiced);
        Assert.Equal(86m, s.TotalPaid);
        Assert.Equal(86m, s.Outstanding);
        Assert.Equal(1, s.OverdueCount);
    }

    [Fact]
    public void Csv_has_header_and_quotes()
    {
        var inv = new Invoice
        {
            Number = "2026-11-17-001", IssueDate = new DateOnly(2026, 11, 17), DueDate = new DateOnly(2026, 12, 17),
            BillToName = "Church A, Exampletown", FromSingerName = "Jane",
            Lines = { new InvoiceLine { Description = "Jane\tSoprano", Fee = 86m } },
        };
        var lines = InvoiceCsvExporter.ToCsv([inv]).Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        Assert.StartsWith("Invoice number,Issue date", lines[0]);
        Assert.Equal("2026-11-17-001,2026-11-17,2026-12-17,,\"Church A, Exampletown\",Jane Soprano,86.00,Unpaid,,Jane", lines[1]);
    }
}
