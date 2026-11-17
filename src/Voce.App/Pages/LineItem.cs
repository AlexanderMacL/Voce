namespace Voce.App.Pages;

/// <summary>Editable row in the invoice editor. Description uses " | " in place of tabs.</summary>
public sealed class LineItem
{
    public string Description { get; set; } = "";
    /// <summary>NaN means blank.</summary>
    public double Fee { get; set; } = double.NaN;
}
