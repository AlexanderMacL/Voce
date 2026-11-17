namespace Voce.Core.Models;

/// <summary>The user's own name, address and bank details. Several may exist; one is the default.</summary>
public sealed class Profile
{
    public const string DefaultFirstLineTemplate = "{NAME}\t{VOICE}\t{FEE}";

    public long Id { get; set; }
    public string Label { get; set; } = "";
    public string SingerName { get; set; } = "";
    public string VoicePart { get; set; } = "";
    public string Address { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Utr { get; set; } = "";
    public string BankName { get; set; } = "";
    public string AccountName { get; set; } = "";
    public string SortCode { get; set; } = "";
    public string AccountNumber { get; set; } = "";
    public int PaymentTermsDays { get; set; } = 30;
    public string FirstLineTemplate { get; set; } = DefaultFirstLineTemplate;
    public bool IsDefault { get; set; }

    public string DisplayName => IsDefault ? $"{Label} (default)" : Label;
    public override string ToString() => DisplayName;
}
