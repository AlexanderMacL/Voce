using Voce.Core.Models;

namespace Voce.Core.Services;

/// <summary>
/// Expands a profile's first-line template.
/// Tokens: {NAME} {VOICE} {EMPLOYER} {EVENT} {DATE} {FEE}.
/// {FEE} is removed from the text and instead fills the line's fee with the event's default fee.
/// Tabs split the description into columns on the PDF.
/// </summary>
public static class LineTemplate
{
    public static InvoiceLine Expand(string template, Profile? profile, Employer? employer, EventType? ev, DateOnly? eventDate)
    {
        var text = string.IsNullOrEmpty(template) ? Profile.DefaultFirstLineTemplate : template;
        var usesFee = text.Contains("{FEE}", StringComparison.OrdinalIgnoreCase);

        text = Replace(text, "{NAME}", profile?.SingerName ?? "");
        text = Replace(text, "{VOICE}", profile?.VoicePart ?? "");
        text = Replace(text, "{EMPLOYER}", employer?.Name ?? "");
        text = Replace(text, "{EVENT}", ev?.Name ?? "");
        text = Replace(text, "{DATE}", eventDate?.ToString("d MMMM yyyy", Money.UkCulture) ?? "");
        text = Replace(text, "{FEE}", "");

        // Tidy up: drop empty tab-separated cells (e.g. a trailing "\t{FEE}") and stray spaces.
        var cells = text.Split('\t').Select(s => s.Trim()).Where(s => s.Length > 0);
        return new InvoiceLine
        {
            Description = string.Join('\t', cells),
            Fee = usesFee ? ev?.DefaultFee : null,
        };
    }

    private static string Replace(string s, string token, string value) =>
        s.Replace(token, value, StringComparison.OrdinalIgnoreCase);
}
