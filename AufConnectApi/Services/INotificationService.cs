using AufConnectApi.Models;

namespace AufConnectApi.Services;

public interface INotificationService
{
    Task SendNotificationAsync(string fcmTokenId, NotificationInfo info, IReadOnlyDictionary<string, string>? data = null);
}