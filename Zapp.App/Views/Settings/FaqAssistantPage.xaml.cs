using Zapp.App.ViewModels;

namespace Zapp.App.Views.Settings;

// render faq directory and offline support assistant
public partial class FaqAssistantPage : ContentPage
{
    public FaqAssistantPage(FaqViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
