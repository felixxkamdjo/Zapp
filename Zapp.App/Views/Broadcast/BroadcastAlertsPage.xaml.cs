using Zapp.App.ViewModels;

namespace Zapp.App.Views.Broadcast;

// render public broadcast channel and emergency alert feeds
public partial class BroadcastAlertsPage : ContentPage
{
    public BroadcastAlertsPage(BroadcastViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
