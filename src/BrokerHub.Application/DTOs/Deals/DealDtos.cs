using BrokerHub.Application.DTOs.Commands;

namespace BrokerHub.Application.DTOs.Deals;

public sealed record DealResult<T>(CommandStatus Status,T? Value=default);
public sealed record AdoptDealDto(Guid UnitId,long UnitVersion,Guid LeadId,long LeadVersion,decimal FinalUnitPrice,
    string CurrencyCode,DateTimeOffset SaleOccurredAtUtc,string Source,string Reference,string? SelectionReason=null,
    Guid? BookingId=null,long? BookingVersion=null);
public sealed record DealReply(Guid Id,long Version,bool IsReplay);
public sealed record ManagerDealDto(Guid Id,Guid CustomerId,Guid UnitId,Guid ProjectId,Guid LeadId,
    DateTime SaleOccurredAtUtc,DateOnly SaleLocalDate,string OpeningTimeZoneId,decimal FinalUnitPrice,string CurrencyCode,
    DateTime AdoptedAtUtc,Guid AdoptedByAccountId,Guid? BookingId,long Version);
