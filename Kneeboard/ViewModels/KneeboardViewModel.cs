using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kneeboard.Models;
using Kneeboard.Services;

namespace Kneeboard.ViewModels;

[QueryProperty(nameof(Document), "Document")]
public partial class KneeboardViewModel : BaseViewModel
{
    private readonly IDocumentService _documentService;
    private readonly ISectionSource _sectionSource;

    [ObservableProperty]
    public partial KneeboardDocument? Document { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string? ErrorMessage { get; set; }

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

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
                OnPropertyChanged(nameof(CurrentPage));
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
                OnPropertyChanged(nameof(CurrentPage));
            }
        }
    }

    [ObservableProperty]
    public partial List<SectionViewModel> Sections { get; set; }

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    public IReadOnlyList<ReadOnlyMemory<byte>> CurrentPages =>
        SelectedSectionIndex >= 0 && SelectedSectionIndex < Sections.Count
            ? Sections[SelectedSectionIndex].Pages
            : [];

    public IReadOnlyList<bool> CurrentPageDots =>
        CurrentPages.Select((_, i) => i == CurrentPageIndex).ToList();

    public ReadOnlyMemory<byte> CurrentPage =>
        CurrentPages.Count > 0 && CurrentPageIndex < CurrentPages.Count
            ? CurrentPages[CurrentPageIndex]
            : ReadOnlyMemory<byte>.Empty;

    public KneeboardViewModel(IDocumentService documentService, ISectionSource sectionSource)
    {
        _documentService = documentService;
        _sectionSource = sectionSource;
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

        // Set the backing field directly so the CurrentPages/CurrentPageDots/CurrentPage
        // notifications raised by the caller are the only ones published, and they already reflect
        // the restored page of the new section — otherwise a stale-index/new-section combination is
        // briefly published first, which the native image view picks up as a real (if transient) wrong page.
        _currentPageIndex = value >= 0 && value < Sections.Count ? Sections[value].LastPageIndex : 0;
    }

    private async Task LoadDocumentAsync(KneeboardDocument doc)
    {
        IsLoading = true;
        ErrorMessage = null;
        Title = doc.Title;

        try
        {
            var sectionVMs = doc.Sections.Select(s => new SectionViewModel(s) { SelectCommand = SelectSectionCommand }).ToList();

            foreach (var vm in sectionVMs)
                vm.Pages = await _sectionSource.GetPagesAsync(vm.Section.Source);

            PublishSections(sectionVMs);
        }
        catch (Exception ex)
        {
            // OnDocumentChanged starts this without awaiting, so an escaping exception would go
            // unobserved and leave the kneeboard silently empty.
            ErrorMessage = ex.Message;
            PublishSections([]);
        }
        finally
        {
            // Alone in the finally, and after the notifications rather than before them: raising a
            // notification runs binding and template code, and a binding that throws must not be
            // able to skip this and strand the loading overlay over the page for the rest of the
            // flight. An escaping exception now costs the dots, not the whole kneeboard.
            IsLoading = false;
        }
    }

    private void PublishSections(List<SectionViewModel> sections)
    {
        Sections = sections;

        // Set backing fields directly to avoid no-change guard on index 0 → 0
        _selectedSectionIndex = 0;
        _currentPageIndex = 0;
        if (sections.Count > 0) sections[0].IsSelected = true;

        OnPropertyChanged(nameof(SelectedSectionIndex));
        OnPropertyChanged(nameof(CurrentPageIndex));
        OnPropertyChanged(nameof(CurrentPages));
        OnPropertyChanged(nameof(CurrentPageDots));
        OnPropertyChanged(nameof(CurrentPage));
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
