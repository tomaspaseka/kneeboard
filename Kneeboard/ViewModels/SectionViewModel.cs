using CommunityToolkit.Mvvm.ComponentModel;
using Kneeboard.Models;

namespace Kneeboard.ViewModels;

public partial class SectionViewModel : BaseViewModel
{
    public KneeboardSection Section { get; }

    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    public IReadOnlyList<ImageSource> Pages { get; set; } = [];

    public SectionViewModel(KneeboardSection section)
    {
        Section = section;
        Title = section.Label;
    }
}
