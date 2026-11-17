using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Voce.App.Helpers;
using Voce.Core.Models;

namespace Voce.App.Pages;

public sealed partial class ProfilesPage : Page
{
    private Profile? _current;
    private bool _loading;

    public ProfilesPage()
    {
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e) => Reload();

    private void Reload(long? selectId = null)
    {
        _loading = true;
        var list = AppServices.Profiles.GetAll();
        ProfileList.ItemsSource = list;
        _loading = false;
        var select = list.FirstOrDefault(p => p.Id == selectId) ?? list.FirstOrDefault();
        ProfileList.SelectedItem = select;
        if (select is null) ShowForm(null);
    }

    private void ProfileList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_loading && ProfileList.SelectedItem is Profile p) ShowForm(p);
    }

    private void Add_Click(object sender, RoutedEventArgs e)
    {
        ProfileList.SelectedItem = null;
        var template = AppServices.Profiles.GetDefault();
        // Start from a copy of the default so only what differs (e.g. bank account) needs typing.
        ShowForm(new Profile
        {
            Label = "New details",
            SingerName = template?.SingerName ?? "",
            VoicePart = template?.VoicePart ?? "",
            Address = template?.Address ?? "",
            Email = template?.Email ?? "",
            Phone = template?.Phone ?? "",
            Utr = template?.Utr ?? "",
            PaymentTermsDays = template?.PaymentTermsDays ?? 30,
            FirstLineTemplate = template?.FirstLineTemplate ?? Profile.DefaultFirstLineTemplate,
        });
        LabelBox.Focus(FocusState.Programmatic);
    }

    private void ShowForm(Profile? p)
    {
        _current = p;
        Form.Visibility = p is null ? Visibility.Collapsed : Visibility.Visible;
        if (p is null) return;
        LabelBox.Text = p.Label;
        DefaultCheck.IsChecked = p.IsDefault;
        SingerNameBox.Text = p.SingerName;
        VoicePartBox.Text = p.VoicePart;
        AddressBox.Text = p.Address;
        EmailBox.Text = p.Email;
        PhoneBox.Text = p.Phone;
        UtrBox.Text = p.Utr;
        AccountNameBox.Text = p.AccountName;
        BankNameBox.Text = p.BankName;
        SortCodeBox.Text = p.SortCode;
        AccountNumberBox.Text = p.AccountNumber;
        TermsBox.Value = p.PaymentTermsDays;
        TemplateBox.Text = Ui.TabsToBars(p.FirstLineTemplate);
        DeleteButton.IsEnabled = p.Id != 0;
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        if (_current is null) return;
        if (string.IsNullOrWhiteSpace(LabelBox.Text) || string.IsNullOrWhiteSpace(SingerNameBox.Text))
        {
            await Ui.Message(XamlRoot, "Details required", "Enter a label and your name.");
            return;
        }
        _current.Label = LabelBox.Text.Trim();
        _current.IsDefault = DefaultCheck.IsChecked == true;
        _current.SingerName = SingerNameBox.Text.Trim();
        _current.VoicePart = VoicePartBox.Text.Trim();
        _current.Address = AddressBox.Text.Trim();
        _current.Email = EmailBox.Text.Trim();
        _current.Phone = PhoneBox.Text.Trim();
        _current.Utr = UtrBox.Text.Trim();
        _current.AccountName = AccountNameBox.Text.Trim();
        _current.BankName = BankNameBox.Text.Trim();
        _current.SortCode = SortCodeBox.Text.Trim();
        _current.AccountNumber = AccountNumberBox.Text.Trim();
        _current.PaymentTermsDays = double.IsNaN(TermsBox.Value) ? 30 : (int)TermsBox.Value;
        _current.FirstLineTemplate = string.IsNullOrWhiteSpace(TemplateBox.Text)
            ? Profile.DefaultFirstLineTemplate
            : Ui.BarsToTabs(TemplateBox.Text);
        try
        {
            AppServices.Profiles.Save(_current);
            Reload(_current.Id);
        }
        catch (Exception ex) { await Ui.Error(XamlRoot, ex); }
    }

    private async void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (_current is not { Id: > 0 } p) return;
        if (AppServices.Profiles.GetAll().Count <= 1)
        {
            await Ui.Message(XamlRoot, "Can't delete", "You need at least one set of details.");
            return;
        }
        var text = $"Delete \"{p.Label}\"? Invoices already issued keep the details printed on them.";
        if (await Ui.Ask(XamlRoot, "Delete details", new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap }, "Delete") != ContentDialogResult.Primary)
            return;
        AppServices.Profiles.Delete(p.Id);
        Reload();
    }
}
