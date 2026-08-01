using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kneeboard.Models;
using Kneeboard.Services;

namespace Kneeboard.ViewModels;

public partial class WelcomeViewModel : BaseViewModel
{
    private readonly IDocumentService _documentService;
    private readonly INavigationService _navigation;
    private readonly IRecentDocumentsService _recentDocumentsService;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string? ErrorMessage { get; set; }

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public string AppVersion => $"v{AppInfo.Current.VersionString}";

    public ObservableCollection<RecentDocument> RecentDocuments { get; } = [];

    public bool HasRecentDocuments => RecentDocuments.Count > 0;

    public WelcomeViewModel(IDocumentService documentService, INavigationService navigation, IRecentDocumentsService recentDocumentsService)
    {
        _documentService = documentService;
        _navigation = navigation;
        _recentDocumentsService = recentDocumentsService;
        Title = "Kneeboard";

        _ = LoadRecentDocumentsAsync();
    }

    private async Task LoadRecentDocumentsAsync()
    {
        var recent = await _recentDocumentsService.GetRecentAsync();

        RecentDocuments.Clear();
        foreach (var doc in recent)
            RecentDocuments.Add(doc);

        OnPropertyChanged(nameof(HasRecentDocuments));
    }

    [RelayCommand]
    private async Task OpenFileAsync()
    {
        ErrorMessage = null;
        IsBusy = true;
        try
        {
            var result = await _documentService.PickAndLoadAsync();

            if (result.WasCancelled) return;

            if (!result.Success)
            {
                ErrorMessage = result.Error;
                return;
            }

            await _navigation.GoToAsync("kneeboard", new Dictionary<string, object>
            {
                ["Document"] = result.Document!
            });
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task OpenRecentAsync(RecentDocument doc)
    {
        ErrorMessage = null;
        IsBusy = true;
        try
        {
            var result = await _documentService.LoadFromPathAsync(doc.Path);

            if (!result.Success)
            {
                ErrorMessage = result.Error;
                await _recentDocumentsService.RemoveAsync(doc.Path);
                RecentDocuments.Remove(doc);
                OnPropertyChanged(nameof(HasRecentDocuments));
                return;
            }

            await _navigation.GoToAsync("kneeboard", new Dictionary<string, object>
            {
                ["Document"] = result.Document!
            });
        }
        finally
        {
            IsBusy = false;
        }
    }
}
