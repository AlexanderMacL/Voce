using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Voce.App.Pages;

namespace Voce.App;

public sealed partial class MainWindow : Window
{
    private static readonly Dictionary<string, Type> Pages = new()
    {
        ["dashboard"] = typeof(DashboardPage),
        ["invoices"] = typeof(InvoicesPage),
        ["new"] = typeof(InvoiceEditorPage),
        ["employers"] = typeof(EmployersPage),
        ["profiles"] = typeof(ProfilesPage),
    };

    private bool _syncingSelection;

    public MainWindow()
    {
        InitializeComponent();
        Title = "Voce — singer's invoices";
        SystemBackdrop = new MicaBackdrop();
        AppWindow.Resize(new Windows.Graphics.SizeInt32(1280, 860));
        Nav.SelectedItem = Nav.MenuItems[0];
    }

    private void Nav_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (_syncingSelection) return;
        if (args.IsSettingsSelected)
        {
            ContentFrame.Navigate(typeof(SettingsPage));
            return;
        }
        if (args.SelectedItem is NavigationViewItem { Tag: string tag } && Pages.TryGetValue(tag, out var page))
            ContentFrame.Navigate(page, null);
    }

    /// <summary>Keeps the highlighted menu item in step when pages navigate each other.</summary>
    private void ContentFrame_Navigated(object sender, NavigationEventArgs e)
    {
        _syncingSelection = true;
        try
        {
            if (e.SourcePageType == typeof(SettingsPage))
                Nav.SelectedItem = Nav.SettingsItem;
            else
            {
                var tag = e.SourcePageType == typeof(InvoiceEditorPage)
                    ? (e.Parameter is long ? "invoices" : "new")
                    : Pages.FirstOrDefault(p => p.Value == e.SourcePageType).Key;
                Nav.SelectedItem = Nav.MenuItems.OfType<NavigationViewItem>().FirstOrDefault(i => (string)i.Tag == tag);
            }
        }
        finally { _syncingSelection = false; }
    }
}
