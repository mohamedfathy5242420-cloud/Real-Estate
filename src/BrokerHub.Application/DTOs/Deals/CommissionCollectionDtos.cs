namespace BrokerHub.Application.DTOs.Deals;

public sealed record RecordCommissionCollectionDto(long Version,decimal Amount,string CurrencyCode,
    DateTimeOffset CollectedAtUtc,string Source,string Reference,string? Notes=null);
public sealed record CommissionCollectionLedger(decimal RecordedAmount,DateTime? LastRecordedAtUtc,int CollectionCount);
public sealed record ManagerCommissionCollectionDto(Guid Id,Guid DealId,Guid CustomerId,Guid UnitId,long RequestedVersion,
    string Amount,string CurrencyCode,string EntitledAmount,DateTime SaleOccurredAtUtc,DateTime CollectedAtUtc,
    DateTime RecordedAtUtc,Guid RecorderAccountId,string Source,string Reference,string? Notes,long Version);
public sealed record CommissionCollectionSummaryDto(Guid DealId,Guid CustomerId,Guid UnitId,string CurrencyCode,
    string EntitledAmount,string CollectedAmount,string RemainingAmount,int CollectionCount,DateTime? LastRecordedAtUtc);
