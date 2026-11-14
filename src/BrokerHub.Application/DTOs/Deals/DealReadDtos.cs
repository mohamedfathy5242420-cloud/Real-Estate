namespace BrokerHub.Application.DTOs.Deals;

public sealed record DealFilterDto(int Page=1,int PageSize=20,Guid? CustomerId=null,Guid? ProjectId=null,
    Guid? UnitId=null,Guid? DeveloperId=null,Guid? AgreementId=null,string? CurrencyCode=null,
    DateOnly? SaleLocalFrom=null,DateOnly? SaleLocalThrough=null);
public sealed record DealPage<T>(int Page,int PageSize,int Total,IReadOnlyList<T> Items);
public sealed record ManagerDealSummaryDto(Guid Id,Guid CustomerId,Guid UnitId,Guid ProjectId,Guid LeadId,
    Guid AgreementId,DateTime SaleOccurredAtUtc,DateTime AdoptedAtUtc,string FinalUnitPrice,
    string CommissionAmount,string CurrencyCode,long Version);
