using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using Kneeboard.Models;

namespace Kneeboard.ViewModels;

/// <summary>
/// One tab in the tab bar. The section's pages, the page the pilot is on and how they have it framed
/// all live in the <see cref="Binder"/> instead — this is only what the tab itself draws.
/// </summary>
public partial class SectionViewModel : BaseViewModel
{
    public KneeboardSection Section { get; }

    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    public ICommand? SelectCommand { get; init; }

    public SectionViewModel(KneeboardSection section)
    {
        Section = section;
        Title = section.Label;
    }
}
