using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Voce.App.Helpers;
using Voce.Core.Models;
using Voce.Core.Services;

namespace Voce.App.Pages;

public sealed partial class InvoiceEditorPage : Page
{
    private readonly ObservableCollection<LineItem> _lines = new();
    private Invoice? _existing;
    private List<Profile> _profiles = new();
    private List<Employer> _employers = new();

    private bool _loading;
    /// <summary>Once the user edits a line, changing employer/event no longer rewrites the first line.</summary>
    private bool _linesTouched;
    /// <summary>Once the user picks a due date, it is no longer derived from the invoice date.</summary>
    private bool _dueTouched;

    public InvoiceEditorPage()
    {
        InitializeComponent();
        LinesList.ItemsSource = _lines;
    }

    private Profile? SelectedProfile => ProfileCombo.SelectedItem as Profile;
    private Employer? SelectedEmployer => EmployerCombo.SelectedItem as Employer;
    private EventType? SelectedEvent => EventCombo.SelectedItem as EventType;

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        _loading = true;
        try
        {
            _existing = e.Parameter is long id ? AppServices.Invoices.Invoices.Get(id) : null;
            _profiles = AppServices.Profiles.GetAll();
            _employers = AppServices.Employers.GetAll()
                .Where(x => !x.IsArchived || x.Id == _existing?.EmployerId).ToList();
            ProfileCombo.ItemsSource = _profiles;
            EmployerCombo.ItemsSource = _employers;

            if (_existing is { } inv) LoadExisting(inv);
            else LoadNew();
        }
        finally { _loading = false; }
        UpdateTotal();
    }

    private void LoadNew()
    {
        TitleText.Text = "New invoice";
        ProfileCombo.SelectedItem = _profiles.FirstOrDefault(p => p.IsDefault) ?? _profiles.FirstOrDefault();
        IssueDatePicker.Date = Ui.ToOffset(Ui.Today);
        DueDatePicker.Date = Ui.ToOffset(Ui.Today.AddDays(SelectedProfile?.PaymentTermsDays ?? 30));
        _linesTouched = _dueTouched = false;
        ApplyLineTemplate();
    }

    private void LoadExisting(Invoice inv)
    {
        TitleText.Text = $"Amend invoice {inv.Number}";
        PaidInfo.IsOpen = inv.IsPaid;
        PaidInfo.Message = inv.IsPaid ? $"This invoice was marked paid on {inv.PaidDate:dd/MM/yyyy}." : "";

        ProfileCombo.SelectedItem = _profiles.FirstOrDefault(p => p.Id == inv.ProfileId);
        EmployerCombo.SelectedItem = _employers.FirstOrDefault(x => x.Id == inv.EmployerId);
        EventCombo.ItemsSource = SelectedEmployer?.Events.Where(ev => !ev.IsArchived).ToList();
        IssueDatePicker.Date = Ui.ToOffset(inv.IssueDate);
        DueDatePicker.Date = Ui.ToOffset(inv.DueDate);
        EventDatePicker.Date = Ui.ToOffset(inv.EventDate);
        BillToNameBox.Text = inv.BillToName;
        BillToAddressBox.Text = inv.BillToAddress;
        NotesBox.Text = inv.Notes;
        _lines.Clear();
        foreach (var l in inv.Lines)
            _lines.Add(new LineItem { Description = Ui.TabsToBars(l.Description), Fee = Ui.FromMoney(l.Fee) });
        _linesTouched = _dueTouched = true;
    }

    // ---------- reacting to header changes ----------

    private void Profile_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_loading) return;
        RecalculateDue();
        if (!_linesTouched) ApplyLineTemplate();
    }

    private void Employer_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_loading) return;
        var employer = SelectedEmployer;
        var events = employer?.Events.Where(ev => !ev.IsArchived).ToList() ?? new();
        EventCombo.ItemsSource = events;
        if (employer is not null)
        {
            BillToNameBox.Text = employer.Name;
            BillToAddressBox.Text = employer.Address;
        }
        _loading = true;
        EventCombo.SelectedItem = events.Count == 1 ? events[0] : null;
        _loading = false;
        if (!_linesTouched) ApplyLineTemplate();
    }

    private void Event_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (!_loading && !_linesTouched) ApplyLineTemplate();
    }

    private void EventDate_Changed(CalendarDatePicker sender, CalendarDatePickerDateChangedEventArgs args)
    {
        if (!_loading && !_linesTouched) ApplyLineTemplate();
    }

    private void IssueDate_Changed(CalendarDatePicker sender, CalendarDatePickerDateChangedEventArgs args)
    {
        if (!_loading) RecalculateDue();
    }

    private void DueDate_Changed(CalendarDatePicker sender, CalendarDatePickerDateChangedEventArgs args)
    {
        if (!_loading) _dueTouched = true;
    }

    private void RecalculateDue()
    {
        if (_dueTouched) return;
        var issue = Ui.ToDateOnly(IssueDatePicker.Date) ?? Ui.Today;
        _loading = true;
        DueDatePicker.Date = Ui.ToOffset(issue.AddDays(SelectedProfile?.PaymentTermsDays ?? 30));
        _loading = false;
    }

    /// <summary>Rebuilds the first line from the selected profile's template.</summary>
    private void ApplyLineTemplate()
    {
        var profile = SelectedProfile;
        var line = LineTemplate.Expand(profile?.FirstLineTemplate ?? Profile.DefaultFirstLineTemplate,
            profile, SelectedEmployer, SelectedEvent, Ui.ToDateOnly(EventDatePicker.Date));
        var item = new LineItem { Description = Ui.TabsToBars(line.Description), Fee = Ui.FromMoney(line.Fee) };
        if (_lines.Count == 0) _lines.Add(item);
        else _lines[0] = item;
        UpdateTotal();
    }

    // ---------- lines ----------

    private void LineText_Changed(object sender, TextChangedEventArgs e)
    {
        if (sender is TextBox { DataContext: LineItem item } box && item.Description != box.Text)
        {
            item.Description = box.Text;
            _linesTouched = true;
        }
    }

    private void Fee_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is NumberBox box) Ui.UseMoneyFormat(box);
    }

    private void Fee_Changed(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (sender.DataContext is not LineItem item) return;
        var same = item.Fee.Equals(sender.Value) || (double.IsNaN(item.Fee) && double.IsNaN(sender.Value));
        if (same) return;
        item.Fee = sender.Value;
        _linesTouched = true;
        UpdateTotal();
    }

    private void RemoveLine_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: LineItem item })
        {
            _lines.Remove(item);
            _linesTouched = true;
            UpdateTotal();
        }
    }

    private void AddLine_Click(object sender, RoutedEventArgs e)
    {
        _lines.Add(new LineItem());
        _linesTouched = true;
    }

    private void ResetFirstLine_Click(object sender, RoutedEventArgs e) => ApplyLineTemplate();

    private void UpdateTotal()
    {
        var total = _lines.Sum(l => Ui.ToMoney(l.Fee) ?? 0m);
        TotalText.Text = "Total " + Money.Format(total);
    }

    // ---------- save ----------

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        var profile = SelectedProfile;
        if (profile is null && _existing is null)
        {
            await Ui.Message(XamlRoot, "Missing details", "Choose which of your details (address and bank account) to use.");
            return;
        }
        if (string.IsNullOrWhiteSpace(BillToNameBox.Text))
        {
            await Ui.Message(XamlRoot, "Missing details", "Enter who the invoice is to (choose an employer or type a name).");
            return;
        }
        var lines = _lines
            .Select(l => new InvoiceLine { Description = Ui.BarsToTabs(l.Description).Trim(), Fee = Ui.ToMoney(l.Fee) })
            .Where(l => l.Description.Length > 0 || l.Fee is not null)
            .ToList();
        if (lines.Count == 0)
        {
            await Ui.Message(XamlRoot, "No lines", "Add at least one line to the invoice.");
            return;
        }

        var inv = _existing ?? new Invoice();
        inv.IssueDate = Ui.ToDateOnly(IssueDatePicker.Date) ?? Ui.Today;
        inv.DueDate = Ui.ToDateOnly(DueDatePicker.Date) ?? inv.IssueDate.AddDays(profile?.PaymentTermsDays ?? 30);
        inv.EventDate = Ui.ToDateOnly(EventDatePicker.Date);
        if (profile is not null) inv.SnapshotProfile(profile); // otherwise keep the stored snapshot
        inv.EmployerId = SelectedEmployer?.Id;
        inv.BillToName = BillToNameBox.Text.Trim();
        inv.BillToAddress = BillToAddressBox.Text.Trim();
        inv.Notes = NotesBox.Text.Trim();
        inv.Lines = lines;

        try
        {
            string path;
            if (_existing is null)
            {
                path = AppServices.Invoices.CreateAndGenerate(inv);
            }
            else
            {
                var choice = await Ui.Ask(XamlRoot, "Save changes",
                    new TextBlock
                    {
                        Text = $"Overwrite invoice {inv.Number} and its PDF, or keep it and save these changes as a new invoice with a new number?",
                        TextWrapping = TextWrapping.Wrap,
                    },
                    primary: $"Overwrite {inv.Number}", secondary: "Save as new invoice");
                if (choice == ContentDialogResult.None) return;
                path = AppServices.Invoices.SaveAmendment(inv, choice == ContentDialogResult.Primary ? AmendMode.Overwrite : AmendMode.Duplicate);
            }

            var open = await Ui.Ask(XamlRoot, $"Invoice {inv.Number} saved",
                new TextBlock { Text = $"Total {inv.TotalText}\nPDF: {path}", TextWrapping = TextWrapping.Wrap },
                primary: "Open PDF", close: "Close");
            if (open == ContentDialogResult.Primary) Ui.OpenFile(path);
            Frame.Navigate(typeof(InvoicesPage));
        }
        catch (Exception ex)
        {
            await Ui.Error(XamlRoot, ex);
            if (inv.Id != 0 && _existing is null)
            {
                // The record was saved but the PDF could not be written: don't let a second click create a duplicate.
                await Ui.Message(XamlRoot, "Invoice saved without PDF",
                    $"Invoice {inv.Number} was saved. Use \"Regenerate PDF\" on the Invoices page once the problem is fixed.");
                Frame.Navigate(typeof(InvoicesPage));
            }
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        if (Frame.CanGoBack) Frame.GoBack();
        else Frame.Navigate(typeof(InvoicesPage));
    }
}
