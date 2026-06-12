# DI Wiring

`MauiProgram.cs` is the single DI registration point. Services are singletons; pages and ViewModels are transient (fresh instance per navigation). `ServiceHelper` provides a service-locator fallback for cases where constructor injection isn't available.
