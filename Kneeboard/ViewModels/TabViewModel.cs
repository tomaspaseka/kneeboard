using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Kneeboard.ViewModels;

/// <summary>
/// One tab in the tab bar — a <see cref="SectionViewModel"/> or a <see cref="GlobalTabViewModel"/>
/// alike. The tab's pages, the page the pilot is on and how they have it framed all live in the
/// <see cref="Binder"/> instead — this, and its subtypes, are only what the tab itself draws.
/// </summary>
public abstract partial class TabViewModel : BaseViewModel
{
    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    public ICommand? SelectCommand { get; init; }
}
