using Kneeboard.Views;

namespace Kneeboard;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute("kneeboard", typeof(KneeboardPage));
        Routing.RegisterRoute("settings", typeof(SettingsPage));
    }
}
