using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kneeboard.Models;
using Kneeboard.Services;

namespace Kneeboard.ViewModels;

[QueryProperty(nameof(Document), "Document")]
public partial class KneeboardViewModel : BaseViewModel
{
    private readonly IDocumentService _documentService;
    private readonly IPdfService _pdfService;

    [ObservableProperty]
    public partial KneeboardDocument? Document { get; set; }

    private int _selectedSectionIndex;
    public int SelectedSectionIndex
    {
        get => _selectedSectionIndex;
        set
        {
            var previousIndex = _selectedSectionIndex;
            if (SetProperty(ref _selectedSectionIndex, value))
            {
                OnSelectedSectionIndexChanged(previousIndex, value);
                OnPropertyChanged(nameof(CurrentPageIndex));
                OnPropertyChanged(nameof(CurrentPages));
                OnPropertyChanged(nameof(CurrentPageDots));
                OnPropertyChanged(nameof(CurrentPageImagePath));
            }
        }
    }

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

    [ObservableProperty]
    public partial List<SectionViewModel> Sections { get; set; }

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    public IReadOnlyList<string> CurrentPages =>
        SelectedSectionIndex >= 0 && SelectedSectionIndex < Sections.Count
            ? Sections[SelectedSectionIndex].Pages
            : [];

    public IReadOnlyList<bool> CurrentPageDots =>
        CurrentPages.Select((_, i) => i == CurrentPageIndex).ToList();

    public string CurrentPageImagePath =>
        CurrentPages.Count > 0 && CurrentPageIndex < CurrentPages.Count
            ? CurrentPages[CurrentPageIndex]
            : string.Empty;

    public KneeboardViewModel(IDocumentService documentService, IPdfService pdfService)
    {
        _documentService = documentService;
        _pdfService = pdfService;
        Sections = [];
    }

    partial void OnDocumentChanged(KneeboardDocument? value)
    {
        if (value is not null)
            _ = LoadDocumentAsync(value);
    }

    private void OnSelectedSectionIndexChanged(int previousIndex, int value)
    {
        if (previousIndex >= 0 && previousIndex < Sections.Count)
            Sections[previousIndex].LastPageIndex = _currentPageIndex;

        for (var i = 0; i < Sections.Count; i++)
            Sections[i].IsSelected = i == value;

        // Set the backing field directly so the CurrentPages/CurrentPageDots/CurrentPageImagePath
        // notifications raised by the caller are the only ones published, and they already reflect
        // the restored page of the new section — otherwise a stale-index/new-section combination is
        // briefly published first, which the native image view picks up as a real (if transient) wrong page.
        _currentPageIndex = value >= 0 && value < Sections.Count ? Sections[value].LastPageIndex : 0;
    }

    private async Task LoadDocumentAsync(KneeboardDocument doc)
    {
        IsLoading = true;
        Title = doc.Title;

        try
        {
            var sectionVMs = doc.Sections.Select(s => new SectionViewModel(s) { SelectCommand = SelectSectionCommand }).ToList();

            foreach (var vm in sectionVMs)
            {
                vm.Pages = vm.Section.Source switch
                {
                    PdfSource pdf => await _pdfService.RenderAllPagesAsync(pdf.Path),
                    ImageFolderSource img => LoadImagesFromFolder(img.Folder),
                    _ => []
                };
            }

            Sections = sectionVMs;

            // Set backing fields directly to avoid no-change guard on index 0 → 0
            _selectedSectionIndex = 0;
            _currentPageIndex = 0;
            if (sectionVMs.Count > 0) sectionVMs[0].IsSelected = true;

            OnPropertyChanged(nameof(SelectedSectionIndex));
            OnPropertyChanged(nameof(CurrentPageIndex));
            OnPropertyChanged(nameof(CurrentPages));
            OnPropertyChanged(nameof(CurrentPageDots));
            OnPropertyChanged(nameof(CurrentPageImagePath));
        }
        finally
        {
            IsLoading = false;
        }
    }

    private static IReadOnlyList<string> LoadImagesFromFolder(string folder)
    {
        string[] extensions = [".png", ".jpg", ".jpeg", ".bmp", ".gif", ".webp"];
        return [.. Directory.GetFiles(folder)
            .Where(f => extensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase))
            .Order()];
    }

    [RelayCommand]
    private void SelectSection(SectionViewModel section)
    {
        var index = Sections.IndexOf(section);
        if (index >= 0)
            SelectedSectionIndex = index;
    }

    [RelayCommand]
    private async Task OpenFileAsync()
    {
        var result = await _documentService.PickAndLoadAsync();
        if (result.Success)
            Document = result.Document;
    }

    [RelayCommand]
    private void PreviousPage() => CurrentPageIndex = Math.Max(0, CurrentPageIndex - 1);

    [RelayCommand]
    private void NextPage() => CurrentPageIndex = Math.Min(CurrentPages.Count - 1, CurrentPageIndex + 1);
}
