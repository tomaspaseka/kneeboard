# Kneeboard Empty Project Scaffold — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Scaffold an empty .NET MAUI 10 project for the Kneeboard app with MVVM architecture, targeting Windows 11 and Android only, with CommunityToolkit packages wired up and no business logic.

**Architecture:** MVVM pattern using CommunityToolkit.Mvvm for source-generated ViewModels. CommunityToolkit.Maui registered in MauiProgram.cs for extended controls and touch behaviors. Folder structure separates Views, ViewModels, Models, and Services from day one.

**Tech Stack:** .NET 10, C#, .NET MAUI 10, CommunityToolkit.Maui 10.x, CommunityToolkit.Mvvm 8.x

---

## File Map

| File | Action | Responsibility |
|---|---|---|
| `Kneeboard.sln` | Create | Solution file |
| `Kneeboard/Kneeboard.csproj` | Create then modify | Project definition, platform targets, packages |
| `Kneeboard/MauiProgram.cs` | Modify | App entry point, DI container, toolkit registration |
| `Kneeboard/App.xaml` | Keep | App resources |
| `Kneeboard/AppShell.xaml` | Keep minimal | Shell navigation host |
| `Kneeboard/MainPage.xaml` | Simplify | Placeholder page |
| `Kneeboard/MainPage.xaml.cs` | Simplify | Code-behind (nearly empty) |
| `Kneeboard/ViewModels/BaseViewModel.cs` | Create | ObservableObject base for all ViewModels |
| `Kneeboard/ViewModels/MainViewModel.cs` | Create | Placeholder ViewModel for MainPage |
| `Kneeboard/Views/` | Create folder | Future XAML pages live here |
| `Kneeboard/Models/` | Create folder | Future data models live here |
| `Kneeboard/Services/` | Create folder | Future services live here |
| `Kneeboard/Platforms/Android/` | Keep | Android entry point (auto-generated) |
| `Kneeboard/Platforms/Windows/` | Keep | Windows entry point (auto-generated) |
| `Kneeboard/Platforms/iOS/` | Delete | Not targeted |
| `Kneeboard/Platforms/MacCatalyst/` | Delete | Not targeted |
| `Kneeboard/Platforms/Tizen/` | Delete | Not targeted |
| `.gitignore` | Create | Standard .NET gitignore |

---

## Task 1: Verify Prerequisites

**Files:** none

- [ ] **Step 1: Check .NET 9 SDK is installed**

Run:
```powershell
dotnet --version
```
Expected: `9.0.x` — if not, install from https://dotnet.microsoft.com/download/dotnet/9

- [ ] **Step 2: Check MAUI workload is installed**

Run:
```powershell
dotnet workload list
```
Expected: `maui` appears in the list. If not, run:
```powershell
dotnet workload install maui
```

- [ ] **Step 3: Check Android workload is installed**

Run:
```powershell
dotnet workload list
```
Expected: `android` appears in the list. If not, run:
```powershell
dotnet workload install android
```

---

## Task 2: Initialize Git and Create Solution

**Files:**
- Create: `.gitignore`
- Create: `Kneeboard.sln`

- [ ] **Step 1: Initialize git repository**

Run from `C:\Users\tpase\src\kneeboard`:
```powershell
git init
```
Expected: `Initialized empty Git repository in C:/Users/tpase/src/kneeboard/.git/`

- [ ] **Step 2: Add .NET gitignore**

Run:
```powershell
dotnet new gitignore
```
Expected: `.gitignore` file created in the current directory.

- [ ] **Step 3: Create solution file**

Run:
```powershell
dotnet new sln -n Kneeboard
```
Expected: `Kneeboard.sln` created.

---

## Task 3: Create MAUI Project

**Files:**
- Create: `Kneeboard/Kneeboard.csproj` and all default MAUI scaffolding

- [ ] **Step 1: Scaffold the MAUI project**

Run from `C:\Users\tpase\src\kneeboard`:
```powershell
dotnet new maui -n Kneeboard -o Kneeboard
```
Expected: Project created in `Kneeboard/` folder with default MAUI structure.

- [ ] **Step 2: Add project to solution**

Run:
```powershell
dotnet sln add Kneeboard/Kneeboard.csproj
```
Expected: `Project 'Kneeboard/Kneeboard.csproj' added to the solution.`

---

## Task 4: Strip Unused Platform Targets

**Files:**
- Modify: `Kneeboard/Kneeboard.csproj`
- Delete: `Kneeboard/Platforms/iOS/`
- Delete: `Kneeboard/Platforms/MacCatalyst/`
- Delete: `Kneeboard/Platforms/Tizen/`

- [ ] **Step 1: Edit the csproj to target only Windows and Android**

Open `Kneeboard/Kneeboard.csproj`. Find the line:
```xml
<TargetFrameworks>net10.0-android;net10.0-ios;net10.0-maccatalyst;net10.0-windows10.0.19041.0</TargetFrameworks>
```
Replace with:
```xml
<TargetFrameworks>net10.0-android;net10.0-windows10.0.19041.0</TargetFrameworks>
```

Also find and remove this block entirely if present (iOS/Mac runtime identifiers):
```xml
<TargetFrameworks Condition="$([MSBuild]::IsOSPlatform('windows'))">$(TargetFrameworks);net10.0-windows10.0.19041.0</TargetFrameworks>
```
(The above conditional block is sometimes generated — remove it since we now list targets explicitly.)

- [ ] **Step 2: Delete unused platform folders**

Run:
```powershell
Remove-Item -Recurse -Force Kneeboard/Platforms/iOS
Remove-Item -Recurse -Force Kneeboard/Platforms/MacCatalyst
Remove-Item -Recurse -Force Kneeboard/Platforms/Tizen
```
Expected: Only `Kneeboard/Platforms/Android/` and `Kneeboard/Platforms/Windows/` remain.

---

## Task 5: Add NuGet Packages

**Files:**
- Modify: `Kneeboard/Kneeboard.csproj` (packages added automatically)

- [ ] **Step 1: Add CommunityToolkit.Maui**

Run from `C:\Users\tpase\src\kneeboard`:
```powershell
dotnet add Kneeboard/Kneeboard.csproj package CommunityToolkit.Maui
```
Expected: Package added. Version 9.x will be resolved.

- [ ] **Step 2: Add CommunityToolkit.Mvvm**

Run:
```powershell
dotnet add Kneeboard/Kneeboard.csproj package CommunityToolkit.Mvvm
```
Expected: Package added. Version 8.x will be resolved.

- [ ] **Step 3: Restore packages**

Run:
```powershell
dotnet restore Kneeboard/Kneeboard.csproj
```
Expected: `Restore succeeded.`

---

## Task 6: Create Folder Structure

**Files:**
- Create: `Kneeboard/Views/.gitkeep`
- Create: `Kneeboard/Models/.gitkeep`
- Create: `Kneeboard/Services/.gitkeep`

- [ ] **Step 1: Create the folders with gitkeep placeholders**

Run:
```powershell
New-Item -ItemType File -Force Kneeboard/Views/.gitkeep
New-Item -ItemType File -Force Kneeboard/Models/.gitkeep
New-Item -ItemType File -Force Kneeboard/Services/.gitkeep
```
Expected: Three empty `.gitkeep` files created, one per folder.

---

## Task 7: Configure MauiProgram.cs

**Files:**
- Modify: `Kneeboard/MauiProgram.cs`

- [ ] **Step 1: Replace MauiProgram.cs with toolkit-wired version**

Replace the entire contents of `Kneeboard/MauiProgram.cs` with:

```csharp
using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;

namespace Kneeboard;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
```

---

## Task 8: Create BaseViewModel

**Files:**
- Create: `Kneeboard/ViewModels/BaseViewModel.cs`

- [ ] **Step 1: Create BaseViewModel using CommunityToolkit.Mvvm**

Create `Kneeboard/ViewModels/BaseViewModel.cs` with:

```csharp
using CommunityToolkit.Mvvm.ComponentModel;

namespace Kneeboard.ViewModels;

public partial class BaseViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    private bool isBusy;

    [ObservableProperty]
    private string title = string.Empty;

    public bool IsNotBusy => !IsBusy;
}
```

---

## Task 9: Create MainViewModel

**Files:**
- Create: `Kneeboard/ViewModels/MainViewModel.cs`

- [ ] **Step 1: Create placeholder MainViewModel**

Create `Kneeboard/ViewModels/MainViewModel.cs` with:

```csharp
using CommunityToolkit.Mvvm.ComponentModel;

namespace Kneeboard.ViewModels;

public partial class MainViewModel : BaseViewModel
{
    public MainViewModel()
    {
        Title = "Kneeboard";
    }
}
```

---

## Task 10: Simplify MainPage

**Files:**
- Modify: `Kneeboard/MainPage.xaml`
- Modify: `Kneeboard/MainPage.xaml.cs`

- [ ] **Step 1: Replace MainPage.xaml with a minimal placeholder**

Replace the entire contents of `Kneeboard/MainPage.xaml` with:

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:vm="clr-namespace:Kneeboard.ViewModels"
             x:Class="Kneeboard.MainPage"
             x:DataType="vm:MainViewModel"
             Title="{Binding Title}">

    <Label Text="Kneeboard"
           HorizontalOptions="Center"
           VerticalOptions="Center" />

</ContentPage>
```

- [ ] **Step 2: Replace MainPage.xaml.cs with minimal code-behind**

Replace the entire contents of `Kneeboard/MainPage.xaml.cs` with:

```csharp
namespace Kneeboard;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
    }
}
```

---

## Task 11: Build Verification — Windows

**Files:** none

- [ ] **Step 1: Build for Windows**

Run from `C:\Users\tpase\src\kneeboard`:
```powershell
dotnet build Kneeboard/Kneeboard.csproj -f net10.0-windows10.0.19041.0
```
Expected: `Build succeeded.` with 0 errors.

If you see errors about `UseMauiCommunityToolkit` not found, verify the package was added in Task 5 and `MauiProgram.cs` has `using CommunityToolkit.Maui;` at the top.

---

## Task 12: Build Verification — Android

**Files:** none

- [ ] **Step 1: Build for Android**

Run:
```powershell
dotnet build Kneeboard/Kneeboard.csproj -f net10.0-android
```
Expected: `Build succeeded.` with 0 errors.

If you see Android SDK errors, run `dotnet workload install android` and retry.

---

## Task 13: Initial Commit

**Files:** none (commit everything)

- [ ] **Step 1: Stage all files**

Run from `C:\Users\tpase\src\kneeboard`:
```powershell
git add .
```

- [ ] **Step 2: Commit**

Run:
```powershell
git commit -m "chore: scaffold empty Kneeboard MAUI project

- .NET MAUI 9 targeting Windows 11 and Android
- MVVM with CommunityToolkit.Maui + CommunityToolkit.Mvvm
- BaseViewModel with IsBusy/Title
- Views, ViewModels, Models, Services folder structure"
```
Expected: Commit created successfully.
