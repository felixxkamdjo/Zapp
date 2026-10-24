namespace Zapp.Core.Models;

// represent lifecycle state of stored offline message
public enum MessageDeliveryStatus
{
    Pending,
    RelayedInTransit,
    Delivered,
    Failed
}

// encapsulate local message entity with routing metadata
public sealed record LocalMessage(
    string Id,
    string SenderKey,
    string RecipientKey,
    string EncryptedContent,
    long Timestamp,
    MessageDeliveryStatus DeliveryStatus,
    int HopsCount
);