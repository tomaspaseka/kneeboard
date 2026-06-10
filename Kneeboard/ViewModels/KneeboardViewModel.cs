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
            if (SetProperty(ref _selectedSectionIndex, value))
            {
                OnPropertyChanged(nameof(CurrentPages));
                OnPropertyChanged(nameof(CurrentPageDots));
                OnSelectedSectionIndexChanged(value);
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
                OnPropertyChanged(nameof(CurrentPageDots));
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

    private void OnSelectedSectionIndexChanged(int value)
    {
        for (var i = 0; i < Sections.Count; i++)
            Sections[i].IsSelected = i == value;
        CurrentPageIndex = 0;
    }

    private async Task LoadDocumentAsync(KneeboardDocument doc)
    {
        IsLoading = true;
        Title = doc.Title;

        var sectionVMs = doc.Sections.Select(s => new SectionViewModel(s)).ToList();

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

        IsLoading = false;
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
}
