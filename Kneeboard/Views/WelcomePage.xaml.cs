using Kneeboard.Models;
using Kneeboard.Services;
using Kneeboard.ViewModels;

namespace Kneeboard.Views;

public partial class WelcomePage : ContentPage
{
    private readonly WelcomeViewModel _viewModel;

    public WelcomePage() : this(ServiceHelper.GetRequired<WelcomeViewModel>()) { }

    public WelcomePage(WelcomeViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    // The recent-document row's BindingContext is the RecentDocument, not this page's
    // WelcomeViewModel, so reaching OpenRecentCommand needs an {x:Reference}-qualified binding —
    // which XamlC won't compile by default (XC0025). Handling Tapped here keeps every XAML
    // binding compiled.
    private void OnRecentDocumentTapped(object sender, TappedEventArgs e)
    {
        if (((Border)sender).BindingContext is RecentDocument doc)
            _viewModel.OpenRecentCommand.Execute(doc);
    }
}
