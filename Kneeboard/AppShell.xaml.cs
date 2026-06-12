using Kneeboard.Views;

namespace Kneeboard;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute("kneeboard", typeof(KneeboardPage));
    }
}
