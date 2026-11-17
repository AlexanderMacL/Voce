using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Globalization.NumberFormatting;

namespace Voce.App.Helpers;

public static class Ui
{
    /// <summary>The editor shows tab separators as " | " because TextBoxes cannot take a Tab key press.</summary>
    public const string ColumnSeparator = " | ";

    public static string TabsToBars(string s) => s.Replace("\t", ColumnSeparator);

    public static string BarsToTabs(string s) =>
        string.Join('\t', s.Split('|').Select(p => p.Trim()));

    public static DateOnly? ToDateOnly(DateTimeOffset? d) => d is { } v ? DateOnly.FromDateTime(v.Date) : null;

    public static DateTimeOffset? ToOffset(DateOnly? d) =>
        d is { } v ? new DateTimeOffset(v.ToDateTime(TimeOnly.MinValue)) : null;

    public static DateOnly Today => DateOnly.FromDateTime(DateTime.Today);

    public static decimal? ToMoney(double v) => double.IsNaN(v) ? null : Math.Round((decimal)v, 2);

    public static double FromMoney(decimal? v) => v is { } m ? (double)m : double.NaN;

    /// <summary>Formats a NumberBox as pounds and pence.</summary>
    public static void UseMoneyFormat(NumberBox box)
    {
        box.NumberFormatter = new DecimalFormatter
        {
            FractionDigits = 2,
            IntegerDigits = 1,
            IsGrouped = false,
            NumberRounder = new IncrementNumberRounder { Increment = 0.01, RoundingAlgorithm = RoundingAlgorithm.RoundHalfUp },
        };
    }

    public static void OpenFile(string path) =>
        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });

    public static void OpenFolder(string folder)
    {
        Directory.CreateDirectory(folder);
        Process.Start(new ProcessStartInfo("explorer.exe", $"\"{folder}\"") { UseShellExecute = true });
    }

    public static async Task<ContentDialogResult> Ask(XamlRoot root, string title, object content,
        string primary, string? secondary = null, string close = "Cancel")
    {
        var dlg = new ContentDialog
        {
            XamlRoot = root,
            Title = title,
            Content = content,
            PrimaryButtonText = primary,
            CloseButtonText = close,
            DefaultButton = ContentDialogButton.Primary,
        };
        if (secondary is not null) dlg.SecondaryButtonText = secondary;
        return await dlg.ShowAsync();
    }

    public static async Task Message(XamlRoot root, string title, string text)
    {
        var dlg = new ContentDialog
        {
            XamlRoot = root,
            Title = title,
            Content = new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap },
            CloseButtonText = "OK",
        };
        await dlg.ShowAsync();
    }

    /// <summary>Shows a friendly message for common failures (e.g. the PDF is open in a viewer).</summary>
    public static Task Error(XamlRoot root, Exception ex)
    {
        var text = ex is IOException
            ? "A file could not be written. If the invoice PDF is open in another program, close it and try again.\n\n" + ex.Message
            : ex.Message;
        return Message(root, "Something went wrong", text);
    }

    /// <summary>Lets a picker know which window it belongs to (required for unpackaged WinUI apps).</summary>
    public static void AttachToWindow(object picker) =>
        WinRT.Interop.InitializeWithWindow.Initialize(picker, WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow));
}
