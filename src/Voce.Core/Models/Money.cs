using System.Globalization;

namespace Voce.Core.Models;

public static class Money
{
    public static readonly CultureInfo UkCulture = CultureInfo.GetCultureInfo("en-GB");

    public static string Format(decimal? amount) =>
        amount is null ? "" : "£" + amount.Value.ToString("#,##0.00", CultureInfo.InvariantCulture);

    public static long? ToPence(decimal? amount) =>
        amount is null ? null : (long)Math.Round(amount.Value * 100m, MidpointRounding.AwayFromZero);

    public static decimal? FromPence(long? pence) => pence is null ? null : pence.Value / 100m;
}
