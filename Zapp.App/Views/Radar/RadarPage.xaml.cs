using Zapp.App.ViewModels;

namespace Zapp.App.Views.Radar;

// render radar discovery screen and bind viewmodel
public partial class RadarPage : ContentPage
{
    public RadarPage(RadarViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
