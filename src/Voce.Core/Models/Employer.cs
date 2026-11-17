namespace Voce.Core.Models;

public sealed class Employer
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public string ContactName { get; set; } = "";
    public string Address { get; set; } = "";
    public string Email { get; set; } = "";
    public string Notes { get; set; } = "";
    public bool IsArchived { get; set; }
    public List<EventType> Events { get; set; } = new();

    public override string ToString() => IsArchived ? $"{Name} (archived)" : Name;
}

/// <summary>A kind of engagement for an employer, e.g. "Evensong" at £86.</summary>
public sealed class EventType
{
    public long Id { get; set; }
    public long EmployerId { get; set; }
    public string Name { get; set; } = "";
    public decimal? DefaultFee { get; set; }
    public bool IsArchived { get; set; }

    public string FeeText => Money.Format(DefaultFee);
    public override string ToString() => DefaultFee is null ? Name : $"{Name} — {Money.Format(DefaultFee)}";
}
