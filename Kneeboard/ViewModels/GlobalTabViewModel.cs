using Kneeboard.Models;

namespace Kneeboard.ViewModels;

/// <summary>The tab for one enabled Global Tab, configured from Settings.</summary>
public partial class GlobalTabViewModel : TabViewModel
{
    public GlobalTabSetting Tab { get; }

    public GlobalTabViewModel(GlobalTabSetting tab)
    {
        Tab = tab;
        Title = tab.Label;
    }
}
