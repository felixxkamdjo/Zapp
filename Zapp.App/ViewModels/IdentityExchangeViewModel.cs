using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Zapp.Core.Interfaces;
using Zapp.Core.Models;

namespace Zapp.App.ViewModels;

// manage peer identity qr generation and optical scanner validation
public partial class IdentityExchangeViewModel : BaseViewModel
{
    private readonly IMessageRepository _repository;

    [ObservableProperty]
    private string _qrCodeValue = string.Empty;

    [ObservableProperty]
    private string _statusMessage = "Prêt à scanner ou présenter votre code";

    [ObservableProperty]
    private bool _isScannerActive;

    public IdentityExchangeViewModel(IMessageRepository repository, string localPublicKey, string displayName)
    {
        // bind persistence dependency and build export string
        _repository = repository;
        Title = "Échange d'Identité";

        // generate serialized identity string for visual qr rendering
        var identity = new PeerIdentityQrPayload(
            PublicKeyBase64: localPublicKey,
            DisplayName: displayName,
            IssuedAt: DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        );

        QrCodeValue = JsonSerializer.Serialize(identity);
    }

    // toggle visual optical camera scanner
    [RelayCommand]
    private void ToggleScanner()
    {
        IsScannerActive = !IsScannerActive;
        StatusMessage = IsScannerActive ? "Pointez la caméra vers le QR Code du pair" : "Scanner en pause";
    }

    // parse scanned qr string and register paired peer node
    public async Task ProcessScannedQrAsync(string rawQrText)
    {
        if (string.IsNullOrWhiteSpace(rawQrText))
        {
            // reject empty detection
            return;
        }

        await RunSafeAsync(async () =>
        {
            try
            {
                // deserialize scanned identity payload
                var identity = JsonSerializer.Deserialize<PeerIdentityQrPayload>(rawQrText);
                if (identity is null || string.IsNullOrWhiteSpace(identity.PublicKeyBase64))
                {
                    StatusMessage = "Format de QR Code Zapp invalide";
                    return;
                }

                // create verified peer record marked as paired
                var pairedPeer = new PeerNode(
                    PublicKey: identity.PublicKeyBase64,
                    DisplayName: identity.DisplayName,
                    LastSeenTicks: DateTimeOffset.UtcNow.Ticks,
                    Rssi: -40,
                    IsPaired: true
                );

                // persist paired peer into sqlite store
                await _repository.UpsertPeerAsync(pairedPeer).ConfigureAwait(false);

                IsScannerActive = false;
                StatusMessage = $"Pair validé avec succès : {identity.DisplayName}";
            }
            catch (Exception ex)
            {
                // capture malformed payload exception
                StatusMessage = $"Erreur d'import : {ex.Message}";
            }
        });
    }
}
