using Zapp.App.ViewModels;

namespace Zapp.App.Views.Mesh;

// render mesh transit queue monitor and relay metrics
public partial class MeshQueuePage : ContentPage
{
    public MeshQueuePage(MeshQueueViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
