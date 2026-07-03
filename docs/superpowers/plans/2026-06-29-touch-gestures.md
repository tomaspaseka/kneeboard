# Touch Gesture Native Zoom Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace `CarouselView` with a custom `ZoomablePageView` backed by a native WinUI `ScrollViewer` that handles pinch-to-zoom and pan at OS speed, with edge-tap page navigation and double-tap zoom reset.

**Architecture:** A new `ZoomablePageView` MAUI control exposes `ImageSource`, `PreviousPageCommand`, and `NextPageCommand` bindable properties. On Windows its handler wraps a `ScrollViewer` (ZoomMode=Enabled, 1×–3×) containing an `Image` that is sized to the viewport so 1× = fit-to-page. Tap events on the `ScrollViewer` handle edge navigation and zoom reset; the `ScrollViewer` handles pinch/pan natively. A 250 ms timer disambiguates single-tap navigation from the first tap of a double-tap zoom-reset sequence.

**Tech Stack:** .NET MAUI 10.0.60, WinUI 3 (Windows App SDK via MAUI), CommunityToolkit.Mvvm 8.4.2, xUnit (tests)

## Global Constraints

- Target framework for build/run: `net10.0-windows10.0.19041.0`
- Target framework for tests: `net10.0-windows10.0.19041.0`
- Platform-specific handler lives under `Kneeboard/Platforms/Windows/` with `#if WINDOWS` guards in `MauiProgram.cs`
- `MaxZoomFactor = 3.0f`, `MinZoomFactor = 1.0f`
- Edge tap zones: left and right 20% of `ScrollViewer.ActualWidth`
- Double-tap disambiguation timer interval: 250 ms
- Branch: `feat/touch-gestures-native` (already created)
- Follow Conventional Commits: `feat`, `fix`, `refactor`, `test`, `chore`

---

## File Map

| Action | Path | Responsibility |
|---|---|---|
| **Modify** | `Kneeboard/ViewModels/KneeboardViewModel.cs` | Add `CurrentPageImagePath`, `PreviousPageCommand`, `NextPageCommand` |
| **Modify** | `Kneeboard.Tests/ViewModels/KneeboardViewModelTests.cs` | Tests for new ViewModel members |
| **Create** | `Kneeboard/Controls/ZoomablePageView.cs` | MAUI bindable control (shared) |
| **Create** | `Kneeboard/Platforms/Windows/ZoomablePageViewHandler.cs` | WinUI `ScrollViewer` handler |
| **Modify** | `Kneeboard/MauiProgram.cs` | Register handler |
| **Modify** | `Kneeboard/Views/KneeboardPage.xaml` | Replace `CarouselView` with `ZoomablePageView` |

---

### Task 1: ViewModel navigation commands and `CurrentPageImagePath`

**Files:**
- Modify: `Kneeboard/ViewModels/KneeboardViewModel.cs`
- Modify: `Kneeboard.Tests/ViewModels/KneeboardViewModelTests.cs`

**Interfaces:**
- Produces:
  - `string CurrentPageImagePath` — computed property; returns `CurrentPages[CurrentPageIndex]` or `string.Empty`
  - `IRelayCommand PreviousPageCommand` — decrements `CurrentPageIndex`, clamped to 0
  - `IRelayCommand NextPageCommand` — increments `CurrentPageIndex`, clamped to `CurrentPages.Count - 1`

- [ ] **Step 1: Write the failing tests**

Add these test methods to `KneeboardViewModelTests` in `Kneeboard.Tests/ViewModels/KneeboardViewModelTests.cs`:

```csharp
[Fact]
public void CurrentPageImagePath_ReturnsPathAtCurrentIndex()
{
    var folder = CreateTempFolder();
    File.WriteAllText(Path.Combine(folder, "p1.png"), "");
    File.WriteAllText(Path.Combine(folder, "p2.png"), "");
    var doc = new KneeboardDocument
    {
        Title = "T",
        Sections = [new() { Id = "x", Label = "X", Source = new ImageFolderSource { Folder = folder } }]
    };
    var vm = new KneeboardViewModel(new StubDocumentService(), new StubPdfService());
    vm.Document = doc;

    vm.CurrentPageIndex = 1;

    Assert.EndsWith("p2.png", vm.CurrentPageImagePath);
}

[Fact]
public void CurrentPageImagePath_EmptyWhenNoPages()
{
    var vm = new KneeboardViewModel(new StubDocumentService(), new StubPdfService());

    Assert.Equal(string.Empty, vm.CurrentPageImagePath);
}

[Fact]
public void NextPageCommand_AdvancesIndex()
{
    var folder = CreateTempFolder();
    File.WriteAllText(Path.Combine(folder, "p1.png"), "");
    File.WriteAllText(Path.Combine(folder, "p2.png"), "");
    var doc = new KneeboardDocument
    {
        Title = "T",
        Sections = [new() { Id = "x", Label = "X", Source = new ImageFolderSource { Folder = folder } }]
    };
    var vm = new KneeboardViewModel(new StubDocumentService(), new StubPdfService());
    vm.Document = doc;

    vm.NextPageCommand.Execute(null);

    Assert.Equal(1, vm.CurrentPageIndex);
}

[Fact]
public void NextPageCommand_ClampsAtLastPage()
{
    var folder = CreateTempFolder();
    File.WriteAllText(Path.Combine(folder, "p1.png"), "");
    File.WriteAllText(Path.Combine(folder, "p2.png"), "");
    var doc = new KneeboardDocument
    {
        Title = "T",
        Sections = [new() { Id = "x", Label = "X", Source = new ImageFolderSource { Folder = folder } }]
    };
    var vm = new KneeboardViewModel(new StubDocumentService(), new StubPdfService());
    vm.Document = doc;
    vm.CurrentPageIndex = 1;

    vm.NextPageCommand.Execute(null);

    Assert.Equal(1, vm.CurrentPageIndex);
}

[Fact]
public void PreviousPageCommand_DecrementsIndex()
{
    var folder = CreateTempFolder();
    File.WriteAllText(Path.Combine(folder, "p1.png"), "");
    File.WriteAllText(Path.Combine(folder, "p2.png"), "");
    var doc = new KneeboardDocument
    {
        Title = "T",
        Sections = [new() { Id = "x", Label = "X", Source = new ImageFolderSource { Folder = folder } }]
    };
    var vm = new KneeboardViewModel(new StubDocumentService(), new StubPdfService());
    vm.Document = doc;
    vm.CurrentPageIndex = 1;

    vm.PreviousPageCommand.Execute(null);

    Assert.Equal(0, vm.CurrentPageIndex);
}

[Fact]
public void PreviousPageCommand_ClampsAtZero()
{
    var folder = CreateTempFolder();
    File.WriteAllText(Path.Combine(folder, "p1.png"), "");
    var doc = new KneeboardDocument
    {
        Title = "T",
        Sections = [new() { Id = "x", Label = "X", Source = new ImageFolderSource { Folder = folder } }]
    };
    var vm = new KneeboardViewModel(new StubDocumentService(), new StubPdfService());
    vm.Document = doc;

    vm.PreviousPageCommand.Execute(null);

    Assert.Equal(0, vm.CurrentPageIndex);
}
```

- [ ] **Step 2: Run tests to confirm they fail**

```powershell
dotnet test --filter "FullyQualifiedName~KneeboardViewModelTests"
```

Expected: 6 failures — `CurrentPageImagePath` property not found, `NextPageCommand` not found, `PreviousPageCommand` not found.

- [ ] **Step 3: Add `CurrentPageImagePath` and update property notifications**

In `Kneeboard/ViewModels/KneeboardViewModel.cs`:

Add `CurrentPageImagePath` computed property after `CurrentPageDots`:

```csharp
public string CurrentPageImagePath =>
    CurrentPages.Count > 0 ? CurrentPages[CurrentPageIndex] : string.Empty;
```

In the `CurrentPageIndex` setter, add a notification for `CurrentPageImagePath`:

```csharp
private int _currentPageIndex;
public int CurrentPageIndex
{
    get => _currentPageIndex;
    set
    {
        if (SetProperty(ref _currentPageIndex, value))
        {
            OnPropertyChanged(nameof(CurrentPageDots));
            OnPropertyChanged(nameof(CurrentPageImagePath));
        }
    }
}
```

In the `SelectedSectionIndex` setter, add a notification for `CurrentPageImagePath`:

```csharp
private int _selectedSectionIndex;
public int SelectedSectionIndex
{
    get => _selectedSectionIndex;
    set
    {
        if (SetProperty(ref _selectedSectionIndex, value))
        {
            OnPropertyChanged(nameof(CurrentPages));
            OnPropertyChanged(nameof(CurrentPageDots));
            OnPropertyChanged(nameof(CurrentPageImagePath));
            OnSelectedSectionIndexChanged(value);
        }
    }
}
```

Also add `OnPropertyChanged(nameof(CurrentPageImagePath))` in `LoadDocumentAsync` alongside the other notifications at the end of the try block:

```csharp
OnPropertyChanged(nameof(SelectedSectionIndex));
OnPropertyChanged(nameof(CurrentPageIndex));
OnPropertyChanged(nameof(CurrentPages));
OnPropertyChanged(nameof(CurrentPageDots));
OnPropertyChanged(nameof(CurrentPageImagePath));
```

- [ ] **Step 4: Add `PreviousPageCommand` and `NextPageCommand`**

Add after `OpenFileAsync` in `KneeboardViewModel.cs`:

```csharp
[RelayCommand]
private void PreviousPage() => CurrentPageIndex = Math.Max(0, CurrentPageIndex - 1);

[RelayCommand]
private void NextPage() => CurrentPageIndex = Math.Min(CurrentPages.Count - 1, CurrentPageIndex + 1);
```

- [ ] **Step 5: Run tests to confirm they pass**

```powershell
dotnet test --filter "FullyQualifiedName~KneeboardViewModelTests"
```

Expected: all tests pass.

- [ ] **Step 6: Commit**

```powershell
git add Kneeboard/ViewModels/KneeboardViewModel.cs Kneeboard.Tests/ViewModels/KneeboardViewModelTests.cs
git commit -m "feat(vm): add PreviousPageCommand, NextPageCommand, CurrentPageImagePath"
```

---

### Task 2: `ZoomablePageView` control, Windows handler, and handler registration

**Files:**
- Create: `Kneeboard/Controls/ZoomablePageView.cs`
- Create: `Kneeboard/Platforms/Windows/ZoomablePageViewHandler.cs`
- Modify: `Kneeboard/MauiProgram.cs`

**Interfaces:**
- Consumes: `CurrentPageImagePath` (string), `PreviousPageCommand` (ICommand), `NextPageCommand` (ICommand) from Task 1
- Produces: `ZoomablePageView` bindable control registered with MAUI handler infrastructure

- [ ] **Step 1: Create `ZoomablePageView.cs`**

Create `Kneeboard/Controls/ZoomablePageView.cs`:

```csharp
using System.Windows.Input;

namespace Kneeboard.Controls;

public class ZoomablePageView : View
{
    public static readonly BindableProperty ImageSourceProperty =
        BindableProperty.Create(nameof(ImageSource), typeof(string), typeof(ZoomablePageView), default(string));

    public static readonly BindableProperty PreviousPageCommandProperty =
        BindableProperty.Create(nameof(PreviousPageCommand), typeof(ICommand), typeof(ZoomablePageView), null);

    public static readonly BindableProperty NextPageCommandProperty =
        BindableProperty.Create(nameof(NextPageCommand), typeof(ICommand), typeof(ZoomablePageView), null);

    public string? ImageSource
    {
        get => (string?)GetValue(ImageSourceProperty);
        set => SetValue(ImageSourceProperty, value);
    }

    public ICommand? PreviousPageCommand
    {
        get => (ICommand?)GetValue(PreviousPageCommandProperty);
        set => SetValue(PreviousPageCommandProperty, value);
    }

    public ICommand? NextPageCommand
    {
        get => (ICommand?)GetValue(NextPageCommandProperty);
        set => SetValue(NextPageCommandProperty, value);
    }
}
```

- [ ] **Step 2: Create `ZoomablePageViewHandler.cs`**

Create `Kneeboard/Platforms/Windows/ZoomablePageViewHandler.cs`:

```csharp
using Kneeboard.Controls;
using Microsoft.Maui.Handlers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;

namespace Kneeboard.Platforms.Windows;

public class ZoomablePageViewHandler : ViewHandler<ZoomablePageView, ScrollViewer>
{
    public static PropertyMapper<ZoomablePageView, ZoomablePageViewHandler> Mapper =
        new(ViewMapper)
        {
            [nameof(ZoomablePageView.ImageSource)] = MapImageSource,
        };

    private Microsoft.UI.Xaml.Controls.Image _image = null!;
    private DispatcherTimer? _tapTimer;
    private global::Windows.Foundation.Point _pendingTapPos;

    public ZoomablePageViewHandler() : base(Mapper) { }

    protected override ScrollViewer CreatePlatformView()
    {
        _image = new Microsoft.UI.Xaml.Controls.Image
        {
            Stretch = Microsoft.UI.Xaml.Media.Stretch.Uniform,
        };

        var scrollViewer = new ScrollViewer
        {
            ZoomMode = ZoomMode.Enabled,
            HorizontalScrollMode = ScrollMode.Enabled,
            VerticalScrollMode = ScrollMode.Enabled,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden,
            VerticalScrollBarVisibility = ScrollBarVisibility.Hidden,
            MinZoomFactor = 1.0f,
            MaxZoomFactor = 3.0f,
            Content = _image,
        };

        return scrollViewer;
    }

    protected override void ConnectHandler(ScrollViewer platformView)
    {
        base.ConnectHandler(platformView);
        platformView.SizeChanged += OnSizeChanged;
        platformView.Tapped += OnTapped;
        platformView.DoubleTapped += OnDoubleTapped;
    }

    protected override void DisconnectHandler(ScrollViewer platformView)
    {
        platformView.SizeChanged -= OnSizeChanged;
        platformView.Tapped -= OnTapped;
        platformView.DoubleTapped -= OnDoubleTapped;
        _tapTimer?.Stop();
        _tapTimer = null;
        base.DisconnectHandler(platformView);
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        var sv = PlatformView;
        _image.Width = sv.ViewportWidth;
        _image.Height = sv.ViewportHeight;
        sv.ChangeView(0, 0, 1.0f, disableAnimation: true);
    }

    private void OnTapped(object sender, TappedRoutedEventArgs e)
    {
        _pendingTapPos = e.GetPosition(PlatformView);
        _tapTimer?.Stop();
        _tapTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        _tapTimer.Tick += OnTapTimerTick;
        _tapTimer.Start();
    }

    private void OnTapTimerTick(object? sender, object e)
    {
        _tapTimer?.Stop();
        _tapTimer = null;

        var width = PlatformView.ActualWidth;
        if (_pendingTapPos.X < width * 0.2)
            VirtualView?.PreviousPageCommand?.Execute(null);
        else if (_pendingTapPos.X > width * 0.8)
            VirtualView?.NextPageCommand?.Execute(null);
    }

    private void OnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        _tapTimer?.Stop();
        _tapTimer = null;
        PlatformView.ChangeView(0, 0, 1.0f, disableAnimation: false);
    }

    private static void MapImageSource(ZoomablePageViewHandler handler, ZoomablePageView view)
    {
        var path = view.ImageSource;
        if (string.IsNullOrEmpty(path))
        {
            handler._image.Source = null;
        }
        else
        {
            handler._image.Source = new BitmapImage(new Uri(path));
            handler.PlatformView.ChangeView(0, 0, 1.0f, disableAnimation: true);
        }
    }
}
```

- [ ] **Step 3: Register the handler in `MauiProgram.cs`**

Add a `.ConfigureMauiHandlers` call inside `CreateMauiApp`, after `.UseMauiCommunityToolkit()`:

```csharp
.ConfigureMauiHandlers(handlers =>
{
#if WINDOWS
    handlers.AddHandler<Kneeboard.Controls.ZoomablePageView,
                        Kneeboard.Platforms.Windows.ZoomablePageViewHandler>();
#endif
})
```

- [ ] **Step 4: Build to confirm no errors**

```powershell
dotnet build --framework net10.0-windows10.0.19041.0
```

Expected: Build succeeded, 0 error(s).

- [ ] **Step 5: Commit**

```powershell
git add Kneeboard/Controls/ZoomablePageView.cs Kneeboard/Platforms/Windows/ZoomablePageViewHandler.cs Kneeboard/MauiProgram.cs
git commit -m "feat(ui): add ZoomablePageView control with native WinUI ScrollViewer handler"
```

---

### Task 3: Wire `KneeboardPage.xaml`

**Files:**
- Modify: `Kneeboard/Views/KneeboardPage.xaml`

**Interfaces:**
- Consumes: `ZoomablePageView` from Task 2; `CurrentPageImagePath`, `PreviousPageCommand`, `NextPageCommand` from Task 1

- [ ] **Step 1: Replace `CarouselView` in `KneeboardPage.xaml`**

Add the `controls` namespace to the `ContentPage` opening tag alongside the existing `xmlns` declarations:

```xml
xmlns:controls="clr-namespace:Kneeboard.Controls"
```

Replace the entire `<CarouselView>` block (lines 75–94) with:

```xml
<controls:ZoomablePageView
    ImageSource="{Binding CurrentPageImagePath}"
    PreviousPageCommand="{Binding PreviousPageCommand}"
    NextPageCommand="{Binding NextPageCommand}"
    HorizontalOptions="Fill"
    VerticalOptions="Fill" />
```

Leave the loading overlay `<Grid>` (lines 97–103) and the page indicator dots (lines 108–122) untouched.

- [ ] **Step 2: Build to confirm no XAML errors**

```powershell
dotnet build --framework net10.0-windows10.0.19041.0
```

Expected: Build succeeded, 0 error(s).

- [ ] **Step 3: Run the app and verify all gestures**

```powershell
dotnet run --project Kneeboard --framework net10.0-windows10.0.19041.0
```

Open a PDF or image folder. Verify:
- Pinch in/out zooms smoothly (1×–3×)
- Single-finger drag pans when zoomed in; does nothing at 1×
- Double-tap resets zoom to fit-page (animated)
- Tap left 20% navigates to previous page
- Tap right 20% navigates to next page
- Page indicator dots update on navigation
- Switching sections resets zoom to 1×

- [ ] **Step 4: Commit**

```powershell
git add Kneeboard/Views/KneeboardPage.xaml
git commit -m "feat(ui): replace CarouselView with ZoomablePageView for native touch gestures"
```
