using BrokerHub.Application.DTOs.Commands;

namespace BrokerHub.Application.DTOs.Viewings;
public sealed record ViewingResult<T>(CommandStatus Status, T? Value = default);
public sealed record ViewingPage<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize);
public sealed record SubmitViewingDto(Guid UnitId, DateTimeOffset ProposedStartUtc);
public sealed record ViewingReply(Guid Id, long Version, bool IsReplay);
public sealed record CustomerViewingDto(Guid Id, Guid UnitId, Guid ProjectId, string Status,
    DateTime CurrentProposedStartUtc, DateTime CurrentProposedEndUtc, string TimeZoneId,
    DateTime RequestedAtUtc, DateTime PendingCycleStartedAtUtc, DateTime PendingExpiresAtUtc, long Version,
    DateTime? ConfirmedStartUtc, DateTime? ConfirmedEndUtc);
public sealed record CustomerViewingEventDto(Guid Id, Guid ViewingId, long ViewingVersion, string Kind,
    DateTime AtUtc, DateTime ProposedStartUtc, DateTime ProposedEndUtc, DateTime? ConfirmedStartUtc, DateTime? ConfirmedEndUtc);
