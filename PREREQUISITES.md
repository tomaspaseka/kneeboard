# Build Prerequisites

Install order for a clean machine (Windows). Run PowerShell as admin.

## 1. Shared (required)

```powershell
winget install Microsoft.DotNet.SDK.10   # .NET 10 SDK (fixes any broken/partial install)
dotnet workload install maui             # .NET MAUI workload
```

## 2. Windows target (`net10.0-windows10.0.19041.0`)

```powershell
winget install Microsoft.WindowsSDK.10.0.19041   # Windows 10 SDK (build + MSIX packaging/signing)
```

## 3. Android target (`net10.0-android`)

Skip if building the Windows app only.

```powershell
winget install Microsoft.OpenJDK.17      # JDK 17 (set JAVA_HOME)
dotnet workload install maui-android     # pulls Android SDK build packs
```

Then install the Android SDK (API >= 21) + platform-tools via Android Studio
(`winget install Google.AndroidStudio`) or the command-line tools, and set
`ANDROID_HOME`.

## Verify

```powershell
# Windows only (override TargetFrameworks so restore skips the android workload check)
dotnet build Kneeboard\Kneeboard.csproj -p:TargetFrameworks=net10.0-windows10.0.19041.0
dotnet build                                   # both targets (needs Android section installed)
```
