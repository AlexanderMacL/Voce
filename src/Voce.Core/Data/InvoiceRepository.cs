using System.Globalization;
using Microsoft.Data.Sqlite;
using Voce.Core.Models;

namespace Voce.Core.Data;

public sealed class InvoiceRepository(VoceDatabase db)
{
    /// <summary>Invoices whose issue date falls in [from, to] (inclusive), newest first. Nulls mean unbounded.</summary>
    public List<Invoice> GetAll(DateOnly? from = null, DateOnly? to = null)
    {
        using var c = db.Open();
        var invoices = c.Query(
            "SELECT * FROM invoices WHERE ($from IS NULL OR issue_date >= $from) AND ($to IS NULL OR issue_date <= $to) ORDER BY issue_date DESC, number DESC",
            Map, ("$from", from.ToDb()), ("$to", to.ToDb()));
        var ids = invoices.Select(i => i.Id).ToHashSet();
        var lines = c.Query("SELECT * FROM invoice_lines ORDER BY invoice_id, position", r => (InvoiceId: r.Long("invoice_id"), Line: MapLine(r)))
            .Where(x => ids.Contains(x.InvoiceId))
            .ToLookup(x => x.InvoiceId, x => x.Line);
        foreach (var i in invoices) i.Lines = lines[i.Id].ToList();
        return invoices;
    }

    public Invoice? Get(long id)
    {
        using var c = db.Open();
        var inv = c.Query("SELECT * FROM invoices WHERE id = $id", Map, ("$id", id)).FirstOrDefault();
        if (inv is not null)
            inv.Lines = c.Query("SELECT * FROM invoice_lines WHERE invoice_id = $id ORDER BY position", MapLine, ("$id", id));
        return inv;
    }

    public DateOnly? EarliestIssueDate()
    {
        using var c = db.Open();
        var s = c.Scalar<string>("SELECT MIN(issue_date) FROM invoices");
        return s is null ? null : Db.ParseDate(s);
    }

    /// <summary>
    /// Inserts a new invoice. If <see cref="Invoice.Number"/> is empty a fresh number is allocated
    /// from the issue date inside the same transaction.
    /// </summary>
    public void Insert(Invoice inv)
    {
        using var c = db.Open();
        using var tx = c.BeginTransaction();
        if (string.IsNullOrEmpty(inv.Number))
            inv.Number = InvoiceNumbering.Allocate(c, inv.IssueDate);
        inv.CreatedAt = inv.UpdatedAt = DateTime.Now;
        inv.Id = c.Insert("""
            INSERT INTO invoices (number, issue_date, due_date, event_date, employer_id, profile_id, bill_to_name, bill_to_address,
                from_singer_name, from_voice_part, from_address, from_email, from_phone, from_utr, from_bank_name,
                from_account_name, from_sort_code, from_account_number, notes, paid_date, pdf_path, created_at, updated_at)
            VALUES ($number, $issue, $due, $event, $emp, $prof, $btn, $bta, $fsn, $fvp, $fad, $fem, $fph, $futr, $fbank,
                $facn, $fsort, $facno, $notes, $paid, $pdf, $created, $updated)
            """, Args(inv));
        WriteLines(c, inv);
        tx.Commit();
    }

    /// <summary>Overwrites an existing invoice (same id and number) including its lines.</summary>
    public void Update(Invoice inv)
    {
        using var c = db.Open();
        using var tx = c.BeginTransaction();
        inv.UpdatedAt = DateTime.Now;
        c.Execute("""
            UPDATE invoices SET number=$number, issue_date=$issue, due_date=$due, event_date=$event, employer_id=$emp,
                profile_id=$prof, bill_to_name=$btn, bill_to_address=$bta, from_singer_name=$fsn, from_voice_part=$fvp,
                from_address=$fad, from_email=$fem, from_phone=$fph, from_utr=$futr, from_bank_name=$fbank,
                from_account_name=$facn, from_sort_code=$fsort, from_account_number=$facno, notes=$notes,
                paid_date=$paid, pdf_path=$pdf, updated_at=$updated
            WHERE id=$id
            """, Args(inv));
        c.Execute("DELETE FROM invoice_lines WHERE invoice_id = $id", ("$id", inv.Id));
        WriteLines(c, inv);
        tx.Commit();
    }

    public void SetPaid(long id, DateOnly? paidDate)
    {
        using var c = db.Open();
        c.Execute("UPDATE invoices SET paid_date = $p, updated_at = $u WHERE id = $id",
            ("$p", paidDate.ToDb()), ("$u", DateTime.Now.ToDb()), ("$id", id));
    }

    public void SetPdfPath(long id, string? path)
    {
        using var c = db.Open();
        c.Execute("UPDATE invoices SET pdf_path = $p WHERE id = $id", ("$p", path), ("$id", id));
    }

    /// <summary>True if another invoice (not <paramref name="exceptId"/>) already uses this PDF path.</summary>
    public bool IsPdfPathUsedByOther(string path, long exceptId)
    {
        using var c = db.Open();
        return c.Scalar<long>("SELECT COUNT(*) FROM invoices WHERE pdf_path = $p COLLATE NOCASE AND id <> $id",
            ("$p", path), ("$id", exceptId)) > 0;
    }

    /// <summary>Deletes the invoice row and lines. Its number is NOT freed (see number_sequences).</summary>
    public void Delete(long id)
    {
        using var c = db.Open();
        c.Execute("DELETE FROM invoices WHERE id = $id", ("$id", id));
    }

    private static void WriteLines(SqliteConnection c, Invoice inv)
    {
        var pos = 0;
        foreach (var line in inv.Lines)
        {
            line.Id = c.Insert("INSERT INTO invoice_lines (invoice_id, position, description, fee_pence) VALUES ($inv, $pos, $desc, $fee)",
                ("$inv", inv.Id), ("$pos", pos++), ("$desc", line.Description), ("$fee", Money.ToPence(line.Fee)));
        }
    }

    private static (string, object?)[] Args(Invoice i) =>
    [
        ("$id", i.Id), ("$number", i.Number), ("$issue", i.IssueDate.ToDb()), ("$due", i.DueDate.ToDb()),
        ("$event", i.EventDate.ToDb()), ("$emp", i.EmployerId), ("$prof", i.ProfileId),
        ("$btn", i.BillToName), ("$bta", i.BillToAddress),
        ("$fsn", i.FromSingerName), ("$fvp", i.FromVoicePart), ("$fad", i.FromAddress), ("$fem", i.FromEmail),
        ("$fph", i.FromPhone), ("$futr", i.FromUtr), ("$fbank", i.FromBankName), ("$facn", i.FromAccountName),
        ("$fsort", i.FromSortCode), ("$facno", i.FromAccountNumber), ("$notes", i.Notes),
        ("$paid", i.PaidDate.ToDb()), ("$pdf", i.PdfPath),
        ("$created", i.CreatedAt.ToDb()), ("$updated", i.UpdatedAt.ToDb()),
    ];

    private static Invoice Map(SqliteDataReader r) => new()
    {
        Id = r.Long("id"),
        Number = r.Str("number"),
        IssueDate = r.Date("issue_date"),
        DueDate = r.Date("due_date"),
        EventDate = r.NDate("event_date"),
        EmployerId = r.NLong("employer_id"),
        ProfileId = r.NLong("profile_id"),
        BillToName = r.Str("bill_to_name"),
        BillToAddress = r.Str("bill_to_address"),
        FromSingerName = r.Str("from_singer_name"),
        FromVoicePart = r.Str("from_voice_part"),
        FromAddress = r.Str("from_address"),
        FromEmail = r.Str("from_email"),
        FromPhone = r.Str("from_phone"),
        FromUtr = r.Str("from_utr"),
        FromBankName = r.Str("from_bank_name"),
        FromAccountName = r.Str("from_account_name"),
        FromSortCode = r.Str("from_sort_code"),
        FromAccountNumber = r.Str("from_account_number"),
        Notes = r.Str("notes"),
        PaidDate = r.NDate("paid_date"),
        PdfPath = r.NStr("pdf_path"),
        CreatedAt = r.DateTime("created_at"),
        UpdatedAt = r.DateTime("updated_at"),
    };

    private static InvoiceLine MapLine(SqliteDataReader r) => new()
    {
        Id = r.Long("id"),
        Description = r.Str("description"),
        Fee = Money.FromPence(r.NLong("fee_pence")),
    };
}

/// <summary>Allocates numbers of the form yyyy-MM-dd-NNN. Numbers are never reused, even after deletion.</summary>
public static class InvoiceNumbering
{
    public static string Format(DateOnly date, int sequence) =>
        $"{date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}-{sequence:000}";

    internal static string Allocate(SqliteConnection c, DateOnly date)
    {
        var key = date.ToDb();
        var last = c.Scalar<long?>("SELECT last_value FROM number_sequences WHERE date = $d", ("$d", key)) ?? 0;

        // Also respect any invoices with this date's prefix (e.g. imported data that bypassed number_sequences).
        var existing = c.Query("SELECT number FROM invoices WHERE number LIKE $p", r => r.GetString(0), ("$p", key + "-%"));
        foreach (var n in existing)
            if (int.TryParse(n.AsSpan(key.Length + 1), NumberStyles.None, CultureInfo.InvariantCulture, out var seq) && seq > last)
                last = seq;

        var next = last + 1;
        c.Execute("INSERT INTO number_sequences (date, last_value) VALUES ($d, $v) ON CONFLICT(date) DO UPDATE SET last_value = excluded.last_value",
            ("$d", key), ("$v", next));
        return Format(date, (int)next);
    }
}
