using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Voce.App.Helpers;
using Voce.Core.Models;

namespace Voce.App.Pages;

public sealed partial class EmployersPage : Page
{
    private readonly ObservableCollection<EventItem> _events = new();
    private Employer? _current;
    private bool _loading;

    public EmployersPage()
    {
        InitializeComponent();
        EventsList.ItemsSource = _events;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e) => Reload();

    private void Reload(long? selectId = null)
    {
        _loading = true;
        var list = AppServices.Employers.GetAll(includeArchived: ShowArchived.IsChecked == true);
        EmployerList.ItemsSource = list;
        _loading = false;
        var select = list.FirstOrDefault(x => x.Id == selectId) ?? list.FirstOrDefault();
        EmployerList.SelectedItem = select;
        if (select is null) ShowForm(null);
    }

    private void ShowArchived_Toggled(object sender, RoutedEventArgs e) => Reload(_current?.Id);

    private void EmployerList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_loading && EmployerList.SelectedItem is Employer emp) ShowForm(emp);
    }

    private void Add_Click(object sender, RoutedEventArgs e)
    {
        EmployerList.SelectedItem = null;
        ShowForm(new Employer());
        NameBox.Focus(FocusState.Programmatic);
    }

    private void ShowForm(Employer? emp)
    {
        _current = emp;
        Form.Visibility = emp is null ? Visibility.Collapsed : Visibility.Visible;
        if (emp is null) return;
        NameBox.Text = emp.Name;
        ContactBox.Text = emp.ContactName;
        AddressBox.Text = emp.Address;
        EmailBox.Text = emp.Email;
        NotesBox.Text = emp.Notes;
        ArchivedCheck.IsChecked = emp.IsArchived;
        DeleteButton.IsEnabled = emp.Id != 0;
        _events.Clear();
        foreach (var ev in emp.Events)
            _events.Add(new EventItem { Id = ev.Id, Name = ev.Name, Fee = Ui.FromMoney(ev.DefaultFee), IsArchived = ev.IsArchived });
    }

    // ----- event rows -----

    private void EventName_Changed(object sender, TextChangedEventArgs e)
    {
        if (sender is TextBox { DataContext: EventItem item } box) item.Name = box.Text;
    }

    private void Fee_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is NumberBox box) Ui.UseMoneyFormat(box);
    }

    private void EventFee_Changed(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (sender.DataContext is EventItem item) item.Fee = sender.Value;
    }

    private void EventArchived_Changed(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox { DataContext: EventItem item } box) item.IsArchived = box.IsChecked;
    }

    private void RemoveEvent_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: EventItem item }) _events.Remove(item);
    }

    private void AddEvent_Click(object sender, RoutedEventArgs e) => _events.Add(new EventItem());

    // ----- save / delete -----

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        if (_current is null) return;
        if (string.IsNullOrWhiteSpace(NameBox.Text))
        {
            await Ui.Message(XamlRoot, "Name required", "Give the employer a name.");
            return;
        }
        _current.Name = NameBox.Text.Trim();
        _current.ContactName = ContactBox.Text.Trim();
        _current.Address = AddressBox.Text.Trim();
        _current.Email = EmailBox.Text.Trim();
        _current.Notes = NotesBox.Text.Trim();
        _current.IsArchived = ArchivedCheck.IsChecked == true;
        _current.Events = _events
            .Where(x => !string.IsNullOrWhiteSpace(x.Name))
            .Select(x => new EventType { Id = x.Id, Name = x.Name.Trim(), DefaultFee = Ui.ToMoney(x.Fee), IsArchived = x.IsArchived == true })
            .ToList();
        try
        {
            AppServices.Employers.Save(_current);
            Reload(_current.Id);
        }
        catch (Exception ex) { await Ui.Error(XamlRoot, ex); }
    }

    private async void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (_current is not { Id: > 0 } emp) return;
        var used = AppServices.Employers.CountInvoices(emp.Id);
        var text = $"Delete {emp.Name} and its events?";
        if (used > 0)
            text += $"\n\n{used} invoice(s) were issued to this employer. They will keep their printed details, " +
                    "but consider ticking \"Archived\" instead so it stays linked.";
        if (await Ui.Ask(XamlRoot, "Delete employer", new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap }, "Delete") != ContentDialogResult.Primary)
            return;
        AppServices.Employers.Delete(emp.Id);
        Reload();
    }
}
