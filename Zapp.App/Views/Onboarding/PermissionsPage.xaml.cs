using Zapp.App.Services;

namespace Zapp.App.Views.Onboarding;

// display initial permission requests and trigger runtime verification
public partial class PermissionsPage : ContentPage
{
    public PermissionsPage()
    {
        InitializeComponent();
    }

    // handle user confirmation and request system bluetooth permissions
    private async void OnStartConfigurationClicked(object sender, EventArgs e)
    {
        // prompt user for required runtime bluetooth permissions
        var granted = await AndroidPermissionsHelper.EnsureBlePermissionsAsync();

        if (granted)
        {
            // navigate to main radar discovery interface
            await Shell.Current.GoToAsync("//RadarPage");
        }
        else
        {
            // display alert if permissions are rejected
            await DisplayAlert(
                "Autorisations requises",
                "Le Bluetooth et la localisation sont indispensables pour la découverte pair-à-pair hors ligne.",
                "OK"
            );
        }
    }
}
