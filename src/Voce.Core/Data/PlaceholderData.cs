using Voce.Core.Models;

namespace Voce.Core.Data;

/// <summary>Sample records written into a brand-new database so every screen has something to show.</summary>
public static class PlaceholderData
{
    public static void Seed(VoceDatabase db)
    {
        var profiles = new ProfileRepository(db);
        var main = new Profile
        {
            Label = "Main account",
            SingerName = "Jane Placeholder",
            VoicePart = "Soprano",
            Address = "1 Example Street\nExampletown\nEX1 2PL",
            Email = "jane@example.com",
            Phone = "07700 900000",
            BankName = "Example Bank",
            AccountName = "J Placeholder",
            SortCode = "00-00-00",
            AccountNumber = "12345678",
            PaymentTermsDays = 30,
            IsDefault = true,
        };
        profiles.Save(main);
        profiles.Save(new Profile
        {
            Label = "Savings account",
            SingerName = "Jane Placeholder",
            VoicePart = "Soprano",
            Address = main.Address,
            Email = main.Email,
            BankName = "Other Example Bank",
            AccountName = "J Placeholder",
            SortCode = "11-11-11",
            AccountNumber = "87654321",
            PaymentTermsDays = 14,
        });

        var employers = new EmployerRepository(db);
        var church = new Employer
        {
            Name = "Church A",
            ContactName = "The Director of Music",
            Address = "St Example's Church\nChurch Lane\nExampletown\nEX1 1AA",
            Email = "music@example.org",
            Events =
            {
                new EventType { Name = "Evensong", DefaultFee = 86m },
                new EventType { Name = "Sung Eucharist", DefaultFee = 70m },
            },
        };
        var society = new Employer
        {
            Name = "Example Choral Society",
            ContactName = "The Treasurer",
            Address = "c/o 5 Sample Road\nExampleton\nEX2 3CS",
            Events =
            {
                new EventType { Name = "Concert", DefaultFee = 150m },
                new EventType { Name = "Rehearsal", DefaultFee = 40m },
            },
        };
        employers.Save(church);
        employers.Save(society);

        var invoices = new InvoiceRepository(db);
        var today = DateOnly.FromDateTime(DateTime.Today);

        var paid = Services.InvoiceService.NewDraft(main, church, church.Events[0], today.AddDays(-45), today.AddDays(-47));
        paid.PaidDate = today.AddDays(-20);
        invoices.Insert(paid);

        var unpaid = Services.InvoiceService.NewDraft(main, society, society.Events[0], today.AddDays(-3), today.AddDays(-4));
        unpaid.Lines.Add(new InvoiceLine { Description = "Rehearsal", Fee = 40m });
        unpaid.Lines.Add(new InvoiceLine { Description = "Travel expenses", Fee = 12.50m });
        invoices.Insert(unpaid);
    }
}
