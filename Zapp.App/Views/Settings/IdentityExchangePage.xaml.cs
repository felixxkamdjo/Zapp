using Zapp.App.ViewModels;
using ZXing.Net.Maui;

namespace Zapp.App.Views.Settings;

// handle qr optical interactions and event callbacks
public partial class IdentityExchangePage : ContentPage
{
    private readonly IdentityExchangeViewModel _viewModel;

    public IdentityExchangePage(IdentityExchangeViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    // handle optical barcode detection event
    private void OnBarcodesDetected(object sender, BarcodeDetectionEventArgs e)
    {
        var firstResult = e.Results.FirstOrDefault();
        if (firstResult is null)
        {
            // ignore empty detection
            return;
        }

        // process detection on main thread
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await _viewModel.ProcessScannedQrAsync(firstResult.Value);
        });
    }

    // navigate back to previous screen
    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}
