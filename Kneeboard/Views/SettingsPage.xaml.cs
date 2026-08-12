using Kneeboard.Models;
using Kneeboard.Services;
using Kneeboard.ViewModels;

namespace Kneeboard.Views;

public partial class SettingsPage : ContentPage
{
    private readonly SettingsViewModel _viewModel;

    public SettingsPage() : this(ServiceHelper.GetRequired<SettingsViewModel>()) { }

    public SettingsPage(SettingsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    // Row buttons live inside the CollectionView's DataTemplate, whose BindingContext is the
    // GlobalTabSetting, not this page's SettingsViewModel. Reaching the page-level command from
    // there would need an {x:Reference}-qualified binding, which XamlC won't compile by default
    // (XC0025) — routing through Clicked instead keeps every XAML binding compiled.
    private void OnToggleEnabledClicked(object sender, EventArgs e) => Run(sender, _viewModel.ToggleEnabledCommand);
    private void OnMoveUpClicked(object sender, EventArgs e) => Run(sender, _viewModel.MoveUpCommand);
    private void OnMoveDownClicked(object sender, EventArgs e) => Run(sender, _viewModel.MoveDownCommand);
    private void OnEditClicked(object sender, EventArgs e) => Run(sender, _viewModel.EditCommand);
    private void OnRemoveClicked(object sender, EventArgs e) => Run(sender, _viewModel.RemoveCommand);

    private static void Run(object sender, System.Windows.Input.ICommand command)
    {
        if (((Button)sender).BindingContext is GlobalTabSetting tab)
            command.Execute(tab);
    }
}
