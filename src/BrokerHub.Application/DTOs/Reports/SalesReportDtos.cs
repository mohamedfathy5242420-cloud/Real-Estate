namespace BrokerHub.Application.DTOs.Reports;

public sealed record ActivityReportFilterDto(DateOnly? FromUtcDate=null,DateOnly? ThroughUtcDate=null);
public sealed record CommissionReportFilterDto(int Page=1,int PageSize=20,Guid? CustomerId=null,Guid? ProjectId=null,
    Guid? DeveloperId=null,string? CurrencyCode=null,DateOnly? SaleLocalFrom=null,DateOnly? SaleLocalThrough=null);
public sealed record CrmActivityReportDto(DateTime ObservedAtUtc,string Scope,long CustomerCount,long UnassignedCustomerCount,
    long LeadCount,long OpenLeadCount,long UnitCount,long ViewingCount,long AwaitingViewingCount,long ConfirmedViewingCount,
    long BookingCount,long PendingBookingCount,long ConfirmedBookingCount,long DealCount);
public sealed record CommissionCurrencyTotalDto(string CurrencyCode,long DealCount,long CollectionCount,
    string EntitledAmount,string CollectedAmount,string RemainingAmount);
