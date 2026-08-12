using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Maui.Storage;
using Kneeboard.Models;
using Kneeboard.Services;

namespace Kneeboard.ViewModels;

/// <summary>
/// Backs the Settings screen where the pilot configures the app-wide Global Tabs, independent of
/// any open document. Mirrors <see cref="WelcomeViewModel"/>'s shape: an <see cref="ObservableCollection{T}"/>
/// loaded async in the constructor, plus a small add/edit form held in a handful of properties.
/// </summary>
public partial class SettingsViewModel : BaseViewModel
{
    /// <summary>Mirrors the cap <see cref="IGlobalTabsSettingsService"/> enforces, so the UI can block a 4th entry up front.</summary>
    private const int MaxEntries = 3;

    private readonly IGlobalTabsSettingsService _globalTabsSettingsService;
    private readonly INavigationService _navigation;

    public ObservableCollection<GlobalTabSetting> Tabs { get; } = [];

    public bool CanAddMore => Tabs.Count < MaxEntries;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string? ErrorMessage { get; set; }

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    /// <summary>The label field of the add/edit form.</summary>
    [ObservableProperty]
    public partial string NewLabel { get; set; } = string.Empty;

    /// <summary>The source picked (via <see cref="PickPdfCommand"/>/<see cref="PickImageFolderCommand"/>) for the add/edit form.</summary>
    [ObservableProperty]
    public partial ContentSource? NewSource { get; set; }

    /// <summary>Human-readable summary of <see cref="NewSource"/>, e.g. "PDF: C:\checklist.pdf".</summary>
    [ObservableProperty]
    public partial string? NewSourceDisplay { get; set; }

    /// <summary>Id of the entry being edited, or null when the form is adding a new one.</summary>
    [ObservableProperty]
    public partial string? EditingId { get; set; }

    public bool IsEditing => EditingId is not null;

    public SettingsViewModel(IGlobalTabsSettingsService globalTabsSettingsService, INavigationService navigation)
    {
        _globalTabsSettingsService = globalTabsSettingsService;
        _navigation = navigation;
        Title = "Global Tabs";

        _ = ReloadTabsAsync();
    }

    private async Task ReloadTabsAsync()
    {
        var tabs = await _globalTabsSettingsService.GetTabsAsync();

        Tabs.Clear();
        foreach (var tab in tabs)
            Tabs.Add(tab);

        OnPropertyChanged(nameof(CanAddMore));
    }

    /// <summary>
    /// Applies a picked pdf file to the add/edit form. Pulled out of <see cref="PickPdfAsync"/> so
    /// tests can drive the form without going through the OS file picker.
    /// </summary>
    public void ApplyPdfPick(string path)
    {
        NewSource = new PdfSource { Path = path };
        NewSourceDisplay = $"PDF: {path}";
    }

    /// <summary>
    /// Applies a picked image folder to the add/edit form. Pulled out of <see cref="PickImageFolderAsync"/>
    /// so tests can drive the form without going through the OS folder picker.
    /// </summary>
    public void ApplyImageFolderPick(string path)
    {
        NewSource = new ImageFolderSource { Folder = path };
        NewSourceDisplay = $"Images: {path}";
    }

    [RelayCommand]
    private async Task PickPdfAsync()
    {
        var picked = await FilePicker.PickAsync(new PickOptions
        {
            PickerTitle = "Choose PDF",
            FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                { DevicePlatform.WinUI, [".pdf"] },
                { DevicePlatform.Android, ["application/pdf"] },
            })
        });

        if (picked is not null)
            ApplyPdfPick(picked.FullPath);
    }

    [RelayCommand]
    private async Task PickImageFolderAsync()
    {
        var result = await FolderPicker.Default.PickAsync();

        if (result.IsSuccessful)
            ApplyImageFolderPick(result.Folder.Path);
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        ErrorMessage = null;

        if (string.IsNullOrWhiteSpace(NewLabel))
        {
            ErrorMessage = "Enter a label for the Global Tab.";
            return;
        }

        if (NewSource is null)
        {
            ErrorMessage = "Pick a pdf file or image folder for the Global Tab.";
            return;
        }

        IsBusy = true;
        try
        {
            if (EditingId is { } id)
                await _globalTabsSettingsService.EditAsync(id, NewLabel, NewSource);
            else
                await _globalTabsSettingsService.AddAsync(NewLabel, NewSource);

            await ReloadTabsAsync();
            ResetForm();
        }
        catch (InvalidOperationException ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void Edit(GlobalTabSetting tab)
    {
        ErrorMessage = null;
        EditingId = tab.Id;
        NewLabel = tab.Label;
        NewSource = tab.Source;
        NewSourceDisplay = tab.Source switch
        {
            PdfSource pdf => $"PDF: {pdf.Path}",
            ImageFolderSource img => $"Images: {img.Folder}",
            _ => null
        };
    }

    [RelayCommand]
    private void CancelEdit() => ResetForm();

    private void ResetForm()
    {
        EditingId = null;
        NewLabel = string.Empty;
        NewSource = null;
        NewSourceDisplay = null;
        ErrorMessage = null;
    }

    [RelayCommand]
    private async Task RemoveAsync(GlobalTabSetting tab)
    {
        IsBusy = true;
        try
        {
            await _globalTabsSettingsService.RemoveAsync(tab.Id);
            if (EditingId == tab.Id)
                ResetForm();
            await ReloadTabsAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private Task MoveUpAsync(GlobalTabSetting tab) => ReorderAsync(tab, tab.Order - 1);

    [RelayCommand]
    private Task MoveDownAsync(GlobalTabSetting tab) => ReorderAsync(tab, tab.Order + 1);

    private async Task ReorderAsync(GlobalTabSetting tab, int newOrder)
    {
        IsBusy = true;
        try
        {
            await _globalTabsSettingsService.ReorderAsync(tab.Id, newOrder);
            await ReloadTabsAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ToggleEnabledAsync(GlobalTabSetting tab)
    {
        IsBusy = true;
        try
        {
            await _globalTabsSettingsService.SetEnabledAsync(tab.Id, !tab.Enabled);
            await ReloadTabsAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private Task BackAsync() => _navigation.GoToAsync("..");
}
