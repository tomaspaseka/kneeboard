using Kneeboard.Services;
using Kneeboard.ViewModels;

namespace Kneeboard.Views;

public partial class WelcomePage : ContentPage
{
    public WelcomePage() : this(ServiceHelper.GetRequired<WelcomeViewModel>()) { }

    public WelcomePage(WelcomeViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
