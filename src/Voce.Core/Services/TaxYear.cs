namespace Voce.Core.Services;

/// <summary>UK tax year: 6 April to 5 April. Identified by the calendar year it starts in.</summary>
public readonly record struct TaxYear(int StartYear)
{
    public DateOnly Start => new(StartYear, 4, 6);
    public DateOnly End => new(StartYear + 1, 4, 5);

    public static TaxYear Containing(DateOnly date) =>
        new(date < new DateOnly(date.Year, 4, 6) ? date.Year - 1 : date.Year);

    public static TaxYear Current => Containing(DateOnly.FromDateTime(DateTime.Today));

    public bool Contains(DateOnly date) => date >= Start && date <= End;

    /// <summary>e.g. "2026/27".</summary>
    public string Label => $"{StartYear}/{(StartYear + 1) % 100:00}";

    public override string ToString() => $"{Label} (6 Apr {StartYear} – 5 Apr {StartYear + 1})";

    /// <summary>Tax years from the one containing <paramref name="earliest"/> up to the current one, newest first.</summary>
    public static List<TaxYear> Range(DateOnly? earliest, DateOnly today)
    {
        var last = Containing(today).StartYear;
        var first = earliest is null ? last : Math.Min(Containing(earliest.Value).StartYear, last);
        var list = new List<TaxYear>();
        for (var y = last; y >= first; y--) list.Add(new TaxYear(y));
        return list;
    }
}
