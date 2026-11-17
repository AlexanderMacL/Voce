namespace Voce.App.Pages;

/// <summary>Editable event row on the Employers page.</summary>
public sealed class EventItem
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    /// <summary>NaN means no default fee.</summary>
    public double Fee { get; set; } = double.NaN;
    public bool IsArchived { get; set; }
}
