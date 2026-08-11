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
    private readonly IGlobalTabsSettingsService _globalTabsSettingsService;

    // Everything about where the pilot is in the document, in one field. No notification can catch the
    // screen holding a page from one tab and a framing or a highlighted tab from another, because
    // there is no second field to disagree with — which is the whole reason paging lives in a binder.
    private Binder _binder = Binder.Empty;

    // Sections followed by enabled Global Tabs, in that order — the same order their pages were handed
    // to Binder.Of, so index i here is exactly the tab Binder.SelectedTabIndex == i means. Kept
    // separately from Sections/GlobalTabs because the screen needs those as two distinct bindable
    // groups, but the binder only knows one combined index space.
    private List<TabViewModel> _tabs = [];

    [ObservableProperty]
    public partial KneeboardDocument? Document { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string? ErrorMessage { get; set; }

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    [ObservableProperty]
    public partial List<SectionViewModel> Sections { get; set; }

    [ObservableProperty]
    public partial List<GlobalTabViewModel> GlobalTabs { get; set; }

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    /// <summary>The page on screen.</summary>
    public ReadOnlyMemory<byte> CurrentPage => _binder.CurrentPage;

    /// <summary>One entry per page of the tab on screen, lit for the page the pilot is on.</summary>
    public IReadOnlyList<bool> CurrentPageDots => _binder.PageDots;

    /// <summary>
    /// How the tab on screen is framed. Two-way: the image view reads its zoom and pan from here,
    /// and reports the pilot's own gestures back through the setter as each one settles.
    /// </summary>
    public Framing CurrentFraming
    {
        get => _binder.CurrentFraming;
        set => Publish(_binder.Framed(value));
    }

    public KneeboardViewModel(
        IDocumentService documentService,
        ISectionSource sectionSource,
        IGlobalTabsSettingsService globalTabsSettingsService)
    {
        _documentService = documentService;
        _sectionSource = sectionSource;
        _globalTabsSettingsService = globalTabsSettingsService;
        Sections = [];
        GlobalTabs = [];
    }

    partial void OnDocumentChanged(KneeboardDocument? value)
    {
        if (value is not null)
            _ = LoadDocumentAsync(value);
    }

    private async Task LoadDocumentAsync(KneeboardDocument doc)
    {
        IsLoading = true;
        ErrorMessage = null;
        Title = doc.Title;

        try
        {
            var sections = doc.Sections
                .Select(s => new SectionViewModel(s) { SelectCommand = SelectSectionCommand })
                .ToList();

            var allGlobalTabs = await _globalTabsSettingsService.GetTabsAsync();
            var globalTabs = allGlobalTabs
                .Where(t => t.Enabled)
                .OrderBy(t => t.Order)
                .Select(t => new GlobalTabViewModel(t) { SelectCommand = SelectSectionCommand })
                .ToList();

            // Sections first, then enabled Global Tabs in configured order — this is the combined
            // index space Binder.SelectedTabIndex indexes into, so it has to match the order their
            // pages are handed to Binder.Of below exactly.
            var tabs = new List<TabViewModel>(sections.Count + globalTabs.Count);
            tabs.AddRange(sections);
            tabs.AddRange(globalTabs);

            var pages = new List<IReadOnlyList<ReadOnlyMemory<byte>>>(tabs.Count);
            foreach (var section in sections)
                pages.Add(await _sectionSource.GetPagesAsync(section.Section.Source));
            foreach (var globalTab in globalTabs)
                pages.Add(await _sectionSource.GetPagesAsync(globalTab.Tab.Source));

            // Tabs first: Publish lights one of them, so they have to exist by then.
            Sections = sections;
            GlobalTabs = globalTabs;
            _tabs = tabs;
            Publish(Binder.Of(pages));
        }
        catch (Exception ex)
        {
            // OnDocumentChanged starts this without awaiting, so an escaping exception would go
            // unobserved and leave the kneeboard silently empty. No special-casing by tab origin: a
            // broken Global Tab source fails the whole load exactly like a broken Section would.
            ErrorMessage = ex.Message;
            Sections = [];
            GlobalTabs = [];
            _tabs = [];
            Publish(Binder.Empty);
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

    /// <summary>
    /// Puts <paramref name="next"/> on screen. The binder is swapped in before anything is announced,
    /// so every binding that reacts reads one consistent place in the document; and the page is
    /// announced before the framing, because a framing is measured against the page it frames — sent
    /// the other way round it would land against the page the pilot just left.
    /// </summary>
    private void Publish(Binder next)
    {
        // Reference identity, not equality: a binder returns itself for anything that changed nothing,
        // and that is the signal. Turning at the end of a tab, re-tapping the open tab and the
        // image view echoing back the framing it was just handed all arrive here as the same instance.
        if (ReferenceEquals(_binder, next))
            return;

        _binder = next;

        for (var i = 0; i < _tabs.Count; i++)
            _tabs[i].IsSelected = i == next.SelectedTabIndex;

        OnPropertyChanged(nameof(CurrentPage));
        OnPropertyChanged(nameof(CurrentPageDots));
        OnPropertyChanged(nameof(CurrentFraming));
    }

    [RelayCommand]
    private void SelectSection(TabViewModel tab) =>
        // A tab that isn't in the list indexes to -1, which the binder treats as nothing to select.
        Publish(_binder.Select(_tabs.IndexOf(tab)));

    [RelayCommand]
    private async Task OpenFileAsync()
    {
        var result = await _documentService.PickAndLoadAsync();
        if (result.Success)
            Document = result.Document;
    }

    [RelayCommand]
    private void PreviousPage() => Publish(_binder.Previous());

    [RelayCommand]
    private void NextPage() => Publish(_binder.Next());
}
