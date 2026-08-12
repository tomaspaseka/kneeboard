using Kneeboard.Models;

namespace Kneeboard.ViewModels;

/// <summary>The tab for one of the open document's own sections.</summary>
public partial class SectionViewModel : TabViewModel
{
    public KneeboardSection Section { get; }

    public SectionViewModel(KneeboardSection section)
    {
        Section = section;
        Title = section.Label;
    }
}
