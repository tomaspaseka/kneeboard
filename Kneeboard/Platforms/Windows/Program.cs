using System.Runtime.InteropServices;

namespace Kneeboard.WinUI;

public static class Program
{
    [DllImport("Microsoft.WindowsAppRuntime.Bootstrap.dll")]
    private static extern int MddBootstrapInitialize(uint majorMinorVersion, string? versionTag, ulong minVersion);

    // The auto-generated module initializer is disabled (DISABLE_XAML_GENERATED_MAIN +
    // WindowsAppSdkDeploymentManagerInitialize=false) to prevent it from firing when
    // Kneeboard.dll is loaded by the test runner.
    [System.STAThread]
    static void Main(string[] args)
    {
        // Unpackaged (debug) runs need WinAppSDK bootstrapped explicitly. Packaged MSIX
        // resolves WinAppSDK via the framework package in the manifest — calling Bootstrap
        // there loads the wrong DLL and FailFasts.
        if (!IsRunningPackaged())
            Marshal.ThrowExceptionForHR(MddBootstrapInitialize(0x00010008, null, 0));

        WinRT.ComWrappersSupport.InitializeComWrappers();
        Microsoft.UI.Xaml.Application.Start(p =>
        {
            var context = new Microsoft.UI.Dispatching.DispatcherQueueSynchronizationContext(
                Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread());
            System.Threading.SynchronizationContext.SetSynchronizationContext(context);
            new App();
        });
    }

    static bool IsRunningPackaged()
    {
        try { _ = Windows.ApplicationModel.Package.Current; return true; }
        catch { return false; }
    }
}
