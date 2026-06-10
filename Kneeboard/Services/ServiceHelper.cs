namespace Kneeboard.Services;

public static class ServiceHelper
{
    private static IServiceProvider? _services;

    public static IServiceProvider Services =>
        _services ?? throw new InvalidOperationException("ServiceHelper not initialized. Call Initialize() in MauiProgram.");

    public static void Initialize(IServiceProvider services) => _services = services;

    public static T GetRequired<T>() where T : notnull =>
        Services.GetRequiredService<T>();
}
