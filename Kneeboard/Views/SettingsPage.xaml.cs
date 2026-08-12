using Kneeboard.Services;
using Kneeboard.ViewModels;

namespace Kneeboard.Views;

public partial class SettingsPage : ContentPage
{
    public SettingsPage() : this(ServiceHelper.GetRequired<SettingsViewModel>()) { }

    public SettingsPage(SettingsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
