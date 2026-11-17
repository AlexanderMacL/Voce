using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using Voce.App.Helpers;
using Voce.Core.Models;
using Voce.Core.Services;

namespace Voce.App.Pages;

public sealed partial class InvoicesPage : Page
{
    private List<Invoice> _all = new();
    private List<TaxYear> _years = new();
    private bool _loading;

    public InvoicesPage()
    {
        InitializeComponent();
    }

    private Invoice? Selected => (List.SelectedItem as InvoiceRow)?.Source;

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        _loading = true;
        _years = TaxYear.Range(AppServices.Invoices.Invoices.EarliestIssueDate(), Ui.Today);
        YearCombo.Items.Clear();
        YearCombo.Items.Add("All tax years");
        foreach (var y in _years) YearCombo.Items.Add("Tax year " + y.Label);
        YearCombo.SelectedIndex = 0;
        _loading = false;
        Reload();
    }

    private void Reload(long? selectId = null)
    {
        _all = AppServices.Invoices.Invoices.GetAll();
        ApplyFilter(selectId);
    }

    private void ApplyFilter(long? selectId = null)
    {
        selectId ??= Selected?.Id;
        IEnumerable<Invoice> q = _all;
        if (YearCombo.SelectedIndex > 0)
        {
            var y = _years[YearCombo.SelectedIndex - 1];
            q = q.Where(i => y.Contains(i.IssueDate));
        }
        if (UnpaidOnly.IsChecked == true) q = q.Where(i => !i.IsPaid);
        var text = SearchBox.Text.Trim();
        if (text.Length > 0)
            q = q.Where(i => i.Number.Contains(text, StringComparison.OrdinalIgnoreCase)
                          || i.BillToName.Contains(text, StringComparison.OrdinalIgnoreCase)
                          || i.Notes.Contains(text, StringComparison.OrdinalIgnoreCase)
                          || i.Lines.Any(l => l.Description.Contains(text, StringComparison.OrdinalIgnoreCase)));
        var items = q.Select(i => new InvoiceRow(i)).ToList();
        List.ItemsSource = items;
        List.SelectedItem = items.FirstOrDefault(r => r.Source.Id == selectId);
        UpdateButtons();
    }

    private void UpdateButtons()
    {
        var s = Selected;
        EditButton.IsEnabled = RegenButton.IsEnabled = DeleteButton.IsEnabled = s is not null;
        OpenButton.IsEnabled = s?.PdfPath is { } p && File.Exists(p);
        PaidButton.IsEnabled = s is { IsPaid: false };
        UnpaidButton.IsEnabled = s is { IsPaid: true };
    }

    private void Search_TextChanged(object sender, TextChangedEventArgs e) { if (!_loading) ApplyFilter(); }
    private void Year_SelectionChanged(object sender, SelectionChangedEventArgs e) { if (!_loading) ApplyFilter(); }
    private void Unpaid_Toggled(object sender, RoutedEventArgs e) { if (!_loading) ApplyFilter(); }

    private void List_SelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateButtons();

    private void List_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        if (Selected is { } inv) Frame.Navigate(typeof(InvoiceEditorPage), inv.Id);
    }

    private void New_Click(object sender, RoutedEventArgs e) => Frame.Navigate(typeof(InvoiceEditorPage), null);

    private void Edit_Click(object sender, RoutedEventArgs e)
    {
        if (Selected is { } inv) Frame.Navigate(typeof(InvoiceEditorPage), inv.Id);
    }

    private async void Open_Click(object sender, RoutedEventArgs e)
    {
        if (Selected?.PdfPath is not { } path) return;
        try { Ui.OpenFile(path); }
        catch (Exception ex) { await Ui.Error(XamlRoot, ex); }
    }

    private async void Regenerate_Click(object sender, RoutedEventArgs e)
    {
        if (Selected is not { } inv) return;
        try
        {
            var path = AppServices.Invoices.Regenerate(inv);
            Reload(inv.Id);
            if (await Ui.Ask(XamlRoot, "PDF regenerated", new TextBlock { Text = path, TextWrapping = TextWrapping.Wrap }, "Open PDF", close: "Close") == ContentDialogResult.Primary)
                Ui.OpenFile(path);
        }
        catch (Exception ex) { await Ui.Error(XamlRoot, ex); }
    }

    private async void Paid_Click(object sender, RoutedEventArgs e)
    {
        if (Selected is not { } inv) return;
        var picker = new CalendarDatePicker
        {
            Header = "Date paid",
            Date = Ui.ToOffset(Ui.Today),
            DateFormat = "{day.integer}/{month.integer}/{year.full}",
        };
        var content = new StackPanel { Spacing = 8 };
        content.Children.Add(new TextBlock { Text = $"{inv.Number} — {inv.BillToName} — {inv.TotalText}" });
        content.Children.Add(picker);
        if (await Ui.Ask(XamlRoot, "Mark as paid", content, "Mark paid") != ContentDialogResult.Primary) return;
        AppServices.Invoices.MarkPaid(inv, Ui.ToDateOnly(picker.Date) ?? Ui.Today);
        Reload(inv.Id);
    }

    private void Unpaid_Click(object sender, RoutedEventArgs e)
    {
        if (Selected is not { } inv) return;
        AppServices.Invoices.MarkUnpaid(inv);
        Reload(inv.Id);
    }

    private async void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (Selected is not { } inv) return;
        var alsoPdf = new CheckBox { Content = "Also delete the PDF file", IsChecked = true, IsEnabled = inv.PdfPath is not null };
        var content = new StackPanel { Spacing = 8 };
        content.Children.Add(new TextBlock
        {
            Text = $"Delete invoice {inv.Number} to {inv.BillToName} ({inv.TotalText}) from the database?\n\n" +
                   "This cannot be undone. The number will not be reused.",
            TextWrapping = TextWrapping.Wrap,
        });
        content.Children.Add(alsoPdf);
        if (await Ui.Ask(XamlRoot, "Delete invoice", content, "Delete") != ContentDialogResult.Primary) return;
        AppServices.Invoices.Delete(inv, alsoPdf.IsChecked == true);
        Reload();
    }
}
