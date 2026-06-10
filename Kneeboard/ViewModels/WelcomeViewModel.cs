using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kneeboard.Services;

namespace Kneeboard.ViewModels;

public partial class WelcomeViewModel : BaseViewModel
{
    private readonly IDocumentService _documentService;
    private readonly INavigationService _navigation;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string? ErrorMessage { get; set; }

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public WelcomeViewModel(IDocumentService documentService, INavigationService navigation)
    {
        _documentService = documentService;
        _navigation = navigation;
        Title = "Kneeboard";
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
}
