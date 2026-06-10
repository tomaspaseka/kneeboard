using Kneeboard.Services;

namespace Kneeboard;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
        => new Window(ServiceHelper.GetRequired<AppShell>());
}
