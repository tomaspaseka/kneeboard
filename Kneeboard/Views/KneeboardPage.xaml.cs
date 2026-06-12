using Kneeboard.Services;
using Kneeboard.ViewModels;

namespace Kneeboard.Views;

public partial class KneeboardPage : ContentPage
{
    public KneeboardPage() : this(ServiceHelper.GetRequired<KneeboardViewModel>()) { }

    public KneeboardPage(KneeboardViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
