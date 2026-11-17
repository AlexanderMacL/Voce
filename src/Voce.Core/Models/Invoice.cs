namespace Voce.Core.Models;

public sealed class Invoice
{
    public long Id { get; set; }
    public string Number { get; set; } = "";
    public DateOnly IssueDate { get; set; }
    public DateOnly DueDate { get; set; }
    public DateOnly? EventDate { get; set; }

    public long? EmployerId { get; set; }
    public long? ProfileId { get; set; }

    // Snapshot of who the invoice is addressed to.
    public string BillToName { get; set; } = "";
    public string BillToAddress { get; set; } = "";

    // Snapshot of the user's details at the time of issue.
    public string FromSingerName { get; set; } = "";
    public string FromVoicePart { get; set; } = "";
    public string FromAddress { get; set; } = "";
    public string FromEmail { get; set; } = "";
    public string FromPhone { get; set; } = "";
    public string FromUtr { get; set; } = "";
    public string FromBankName { get; set; } = "";
    public string FromAccountName { get; set; } = "";
    public string FromSortCode { get; set; } = "";
    public string FromAccountNumber { get; set; } = "";

    public string Notes { get; set; } = "";
    public DateOnly? PaidDate { get; set; }
    public string? PdfPath { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public List<InvoiceLine> Lines { get; set; } = new();

    public decimal Total => Lines.Sum(l => l.Fee ?? 0m);
    public bool IsPaid => PaidDate is not null;
    public bool IsOverdue(DateOnly today) => !IsPaid && DueDate < today;

    // Convenience text for list displays.
    public string TotalText => Money.Format(Total);
    public string IssueDateText => IssueDate.ToString("dd/MM/yyyy");
    public string DueDateText => DueDate.ToString("dd/MM/yyyy");
    public string StatusText => PaidDate is { } p ? $"Paid {p:dd/MM/yyyy}" : "Unpaid";

    /// <summary>Copies the bill-to fields from an employer.</summary>
    public void SnapshotEmployer(Employer employer)
    {
        EmployerId = employer.Id;
        BillToName = employer.Name;
        BillToAddress = employer.Address;
    }

    /// <summary>Copies the user's details from a profile.</summary>
    public void SnapshotProfile(Profile p)
    {
        ProfileId = p.Id;
        FromSingerName = p.SingerName;
        FromVoicePart = p.VoicePart;
        FromAddress = p.Address;
        FromEmail = p.Email;
        FromPhone = p.Phone;
        FromUtr = p.Utr;
        FromBankName = p.BankName;
        FromAccountName = p.AccountName;
        FromSortCode = p.SortCode;
        FromAccountNumber = p.AccountNumber;
    }
}

public sealed class InvoiceLine
{
    public long Id { get; set; }
    public string Description { get; set; } = "";
    /// <summary>Null means the fee column is left blank.</summary>
    public decimal? Fee { get; set; }
}
