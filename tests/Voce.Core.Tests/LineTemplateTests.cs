using Voce.Core.Models;
using Voce.Core.Services;

namespace Voce.Core.Tests;

public class LineTemplateTests
{
    private static readonly Profile P = new() { SingerName = "Jane Smith", VoicePart = "Soprano" };
    private static readonly Employer E = new() { Name = "Church A" };
    private static readonly EventType Ev = new() { Name = "Evensong", DefaultFee = 86m };

    [Fact]
    public void Default_template_gives_name_voice_and_fee()
    {
        var line = LineTemplate.Expand(Profile.DefaultFirstLineTemplate, P, E, Ev, null);
        Assert.Equal("Jane Smith\tSoprano", line.Description);
        Assert.Equal(86m, line.Fee);
    }

    [Fact]
    public void Without_fee_token_fee_is_blank()
    {
        var line = LineTemplate.Expand("{EVENT} at {EMPLOYER}, {DATE}", P, E, Ev, new DateOnly(2026, 11, 15));
        Assert.Equal("Evensong at Church A, 15 November 2026", line.Description);
        Assert.Null(line.Fee);
    }
}
