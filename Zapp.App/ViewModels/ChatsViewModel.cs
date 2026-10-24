using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Zapp.Core.Interfaces;
using Zapp.Core.Models;

namespace Zapp.App.ViewModels;

// manage direct conversations and outgoing transmission pipeline
public partial class ChatsViewModel : BaseViewModel
{
    private readonly IMessageRepository _repository;
    private readonly ICryptoService _cryptoService;
    private readonly ITransportAdapter _transportAdapter;
    private readonly string _localPrivateKey;
    private readonly string _localPublicKey;

    public ObservableCollection<LocalMessage> Messages { get; } = [];

    [ObservableProperty]
    private string _outgoingText = string.Empty;

    public ChatsViewModel(
        IMessageRepository repository,
        ICryptoService cryptoService,
        ITransportAdapter transportAdapter,
        string localPrivateKey,
        string localPublicKey)
    {
        // bind service dependencies
        _repository = repository;
        _cryptoService = cryptoService;
        _transportAdapter = transportAdapter;
        _localPrivateKey = localPrivateKey;
        _localPublicKey = localPublicKey;
        Title = "Chats";
    }

    // load previous conversation messages from sqlite
    [RelayCommand]
    private async Task LoadMessagesAsync()
    {
        await RunSafeAsync(async () =>
        {
            var result = await _repository.GetPendingOutboxMessagesAsync().ConfigureAwait(false);
            if (result.IsSuccess && result.Value is not null)
            {
                // populate conversation list on ui thread
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Messages.Clear();
                    foreach (var msg in result.Value)
                    {
                        Messages.Add(msg);
                    }
                });
            }
        });
    }

    // execute message packaging signing and transmission pipeline
    [RelayCommand]
    private async Task SendMessageAsync(string recipientPublicKey)
    {
        if (string.IsNullOrWhiteSpace(OutgoingText))
        {
            // ignore empty input
            return;
        }

        await RunSafeAsync(async () =>
        {
            // sign raw message payload using local identity
            var signResult = _cryptoService.Sign(OutgoingText, _localPrivateKey);
            if (!signResult.IsSuccess)
            {
                // record signature failure
                ErrorMessage = signResult.ErrorMessage;
                return;
            }

            var packetId = Guid.NewGuid().ToString();
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            // construct outgoing network packet frame
            var packet = new NetworkPacket(
                PacketId: packetId,
                Type: PacketType.Direct,
                SenderPublicKey: _localPublicKey,
                RecipientPublicKey: recipientPublicKey,
                Ttl: 3,
                Payload: OutgoingText,
                Signature: signResult.Value!,
                Timestamp: timestamp
            );

            // save record in local outbox
            var localMessage = new LocalMessage(
                Id: packetId,
                SenderKey: _localPublicKey,
                RecipientKey: recipientPublicKey,
                EncryptedContent: OutgoingText,
                Timestamp: timestamp,
                DeliveryStatus: MessageDeliveryStatus.Pending,
                HopsCount: 0
            );

            await _repository.SaveMessageAsync(localMessage).ConfigureAwait(false);

            MainThread.BeginInvokeOnMainThread(() =>
            {
                // append sent message to reactive list
                Messages.Add(localMessage);
                OutgoingText = string.Empty;
            });

            // attempt direct radio delivery
            await _transportAdapter.SendDirectAsync(recipientPublicKey, packet).ConfigureAwait(false);
        });
    }
}
