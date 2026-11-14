using BrokerHub.Application.Abstractions.Identity;
using BrokerHub.Application.DTOs.Commands;
using BrokerHub.Application.DTOs.Notifications;
using BrokerHub.Application.Interfaces.Persistence;
using BrokerHub.Application.Interfaces.Services;

namespace BrokerHub.Application.Services;

public sealed class UserNotificationService(IUserNotificationQueries queries,INotificationReadStore reads,ICurrentUser user):IUserNotificationService
{
    private Guid? Actor=>user.IsAuthenticated&&user.AccountId is { } actor&&actor!=Guid.Empty?actor:null;
    public Task<bool> CanAccessAsync(CancellationToken ct)=>Actor is { } actor?queries.CanAccessAsync(actor,ct):Task.FromResult(false);
    public Task<NotificationResult<UserNotificationPage>> ListAsync(NotificationListFilter filter,CancellationToken ct)=>
        Actor is not { } actor?Task.FromResult(new NotificationResult<UserNotificationPage>(CommandStatus.Forbidden)):
        !filter.IsValid?Task.FromResult(new NotificationResult<UserNotificationPage>(CommandStatus.Invalid)):queries.ListAsync(actor,filter,ct);
    public Task<NotificationResult<UserNotificationDto>> GetAsync(Guid id,CancellationToken ct)=>
        Actor is not { } actor?Task.FromResult(new NotificationResult<UserNotificationDto>(CommandStatus.Forbidden)):
        id==Guid.Empty?Task.FromResult(new NotificationResult<UserNotificationDto>(CommandStatus.Invalid)):queries.GetAsync(actor,id,ct);
    public Task<NotificationResult<NotificationUnreadCountDto>> UnreadCountAsync(CancellationToken ct)=>
        Actor is { } actor?queries.UnreadCountAsync(actor,ct):Task.FromResult(new NotificationResult<NotificationUnreadCountDto>(CommandStatus.Forbidden));
    public Task<NotificationResult<NotificationReadReply>> MarkReadAsync(Guid id,CancellationToken ct)=>
        Actor is not { } actor?Task.FromResult(new NotificationResult<NotificationReadReply>(CommandStatus.Forbidden)):
        id==Guid.Empty?Task.FromResult(new NotificationResult<NotificationReadReply>(CommandStatus.Invalid)):reads.MarkReadAsync(actor,id,ct);
}
