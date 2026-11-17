using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Voce.App.Helpers;
using Voce.Core.Models;
using Voce.Core.Services;
using Windows.Storage.Pickers;

namespace Voce.App.Pages;

public sealed partial class DashboardPage : Page
{
    private const string CustomLabel = "Custom range";
    private List<TaxYear> _years = new();
    private bool _loading;

    public DashboardPage()
    {
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        _loading = true;
        _years = TaxYear.Range(AppServices.Invoices.Invoices.EarliestIssueDate(), Ui.Today);
        PeriodCombo.Items.Clear();
        foreach (var y in _years) PeriodCombo.Items.Add("Tax year " + y);
        PeriodCombo.Items.Add(CustomLabel);
        PeriodCombo.SelectedIndex = 0; // current tax year
        ApplyPeriodToPickers();
        _loading = false;
        Refresh();
    }

    private TaxYear? SelectedYear =>
        PeriodCombo.SelectedIndex >= 0 && PeriodCombo.SelectedIndex < _years.Count ? _years[PeriodCombo.SelectedIndex] : null;

    private (DateOnly From, DateOnly To) Period =>
        SelectedYear is { } y
            ? (y.Start, y.End)
            : (Ui.ToDateOnly(FromDate.Date) ?? DateOnly.MinValue, Ui.ToDateOnly(ToDate.Date) ?? DateOnly.MaxValue);

    private void ApplyPeriodToPickers()
    {
        var custom = SelectedYear is null;
        FromDate.IsEnabled = ToDate.IsEnabled = custom;
        if (SelectedYear is { } y)
        {
            FromDate.Date = Ui.ToOffset(y.Start);
            ToDate.Date = Ui.ToOffset(y.End);
        }
    }

    private void Period_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_loading) return;
        _loading = true;
        ApplyPeriodToPickers();
        _loading = false;
        Refresh();
    }

    private void Custom_DateChanged(CalendarDatePicker sender, CalendarDatePickerDateChangedEventArgs args)
    {
        if (!_loading && SelectedYear is null) Refresh();
    }

    private void Refresh()
    {
        var (from, to) = Period;
        var invoices = AppServices.Invoices.Invoices.GetAll(from, to);
        var s = DashboardSummary.From(invoices, Ui.Today);
        CountText.Text = s.Count.ToString();
        TotalText.Text = Money.Format(s.TotalInvoiced);
        PaidText.Text = Money.Format(s.TotalPaid);
        OutstandingText.Text = Money.Format(s.Outstanding);
        OverdueText.Text = s.OverdueCount > 0 ? $"{s.OverdueCount} overdue ({Money.Format(s.Overdue)})" : "";

        var unpaid = AppServices.Invoices.Invoices.GetAll().Where(i => !i.IsPaid).OrderBy(i => i.DueDate).ToList();
        UnpaidList.ItemsSource = unpaid;
        NoUnpaidText.Visibility = unpaid.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void Unpaid_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is Invoice inv) Frame.Navigate(typeof(InvoiceEditorPage), inv.Id);
    }

    private async void ExportCsv_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var (from, to) = Period;
            var name = SelectedYear is { } y ? $"Invoices {y.Label.Replace('/', '-')}" : $"Invoices {from:yyyy-MM-dd} to {to:yyyy-MM-dd}";
            var picker = new FileSavePicker { SuggestedFileName = name, SuggestedStartLocation = PickerLocationId.DocumentsLibrary };
            picker.FileTypeChoices.Add("CSV file", new List<string> { ".csv" });
            Ui.AttachToWindow(picker);
            var file = await picker.PickSaveFileAsync();
            if (file is null) return;

            var invoices = AppServices.Invoices.Invoices.GetAll(from, to);
            InvoiceCsvExporter.Write(file.Path, invoices);
            await Ui.Message(XamlRoot, "Exported", $"{invoices.Count} invoice(s) written to\n{file.Path}");
        }
        catch (Exception ex)
        {
            await Ui.Error(XamlRoot, ex);
        }
    }
}
