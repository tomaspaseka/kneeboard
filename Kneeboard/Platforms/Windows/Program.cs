using System.Runtime.InteropServices;

namespace Kneeboard.WinUI;

public static class Program
{
    // Bootstrap must be called before Application.Start() so WinRT COM classes
    // (Microsoft.UI.Xaml.*) can be activated. The auto-generated module initializer
    // is disabled (DISABLE_XAML_GENERATED_MAIN + WindowsAppSdkDeploymentManagerInitialize=false)
    // to prevent it from firing when Kneeboard.dll is loaded by the test runner.
    [DllImport("Microsoft.WindowsAppRuntime.Bootstrap.dll")]
    private static extern int MddBootstrapInitialize(uint majorMinorVersion, string? versionTag, ulong minVersion);

    [System.STAThread]
    static void Main(string[] args)
    {
        int hr = MddBootstrapInitialize(0x00010008, null, 0); // WinAppSDK 1.8
        Marshal.ThrowExceptionForHR(hr);

        WinRT.ComWrappersSupport.InitializeComWrappers();
        Microsoft.UI.Xaml.Application.Start(p =>
        {
            var context = new Microsoft.UI.Dispatching.DispatcherQueueSynchronizationContext(
                Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread());
            System.Threading.SynchronizationContext.SetSynchronizationContext(context);
            new App();
        });
    }
}
