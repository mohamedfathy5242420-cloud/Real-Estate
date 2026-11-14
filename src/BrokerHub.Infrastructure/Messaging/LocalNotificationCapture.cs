using BrokerHub.Application.DTOs.Notifications;
using BrokerHub.Application.Interfaces.Persistence;
using BrokerHub.Application.Interfaces.Services;
using BrokerHub.Infrastructure.Storage;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace BrokerHub.Infrastructure.Messaging;

internal sealed class LocalNotificationCapture(INotificationCaptureStore store,IOptions<LocalNotificationCaptureOptions> options,
    IHostEnvironment environment):ILocalNotificationCapture
{
    public Task<LocalNotificationCaptureResult> CaptureAsync(Guid recipientAccountId,Guid notificationId,CancellationToken ct)
    {
        if(!environment.IsDevelopment())return Task.FromResult(new LocalNotificationCaptureResult(LocalNotificationCaptureStatus.Disabled));
        var o=options.Value;
        if(!o.Enabled)return Task.FromResult(new LocalNotificationCaptureResult(LocalNotificationCaptureStatus.Disabled));
        if(recipientAccountId==Guid.Empty||notificationId==Guid.Empty)return Task.FromResult(new LocalNotificationCaptureResult(LocalNotificationCaptureStatus.Invalid));
        if(!o.IsValid||!o.AllowedAccountIds.Contains(recipientAccountId))return Task.FromResult(new LocalNotificationCaptureResult(LocalNotificationCaptureStatus.Forbidden));
        return store.CaptureAsync(recipientAccountId,notificationId,ct);
    }
}
