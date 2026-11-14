using BrokerHub.Application.DTOs.Commands;
using BrokerHub.Application.DTOs.Deals;
using BrokerHub.Application.Interfaces.Persistence;
using System.Globalization;
using BrokerHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BrokerHub.Infrastructure.Persistence.Queries;

internal sealed class DealQueries(ApplicationDbContext db):IDealQueries
{
    private IQueryable<Lead> PreparationOrigin(Guid actor,Guid customerId,Guid leadId)=>
        from lead in db.Leads.AsNoTracking() join customer in db.Customers.AsNoTracking() on lead.CustomerId equals customer.Id
        where customer.Id==customerId&&lead.Id==leadId&&CurrentManagers(actor).Any() select lead;
    private IQueryable<Unit> PreparationUnits(Guid actor,Guid customerId,Guid leadId)=>
        from unit in db.Units.AsNoTracking()
        where PreparationOrigin(actor,customerId,leadId).Any(lead=>lead.ProjectId==unit.ProjectId)
        select unit;
    private IQueryable<DealPreparationUnitDto> PreparationProjection(IQueryable<Unit> units)=>
        units.Select(unit=>new DealPreparationUnitDto(unit.Id,unit.ProjectId,unit.Code,unit.Title,unit.PublicationStatus,
            unit.AvailabilityStatus,db.Deals.Any(deal=>deal.UnitId==unit.Id),unit.Version));
    public async Task<DealResult<DealPage<DealPreparationUnitDto>>> PreparationUnitsAsync(Guid actor,Guid customerId,Guid leadId,int page,int size,string? q,CancellationToken ct)
    {
        if(!await CurrentManagers(actor).AnyAsync(ct))return new(CommandStatus.Forbidden);
        if(!await PreparationOrigin(actor,customerId,leadId).AnyAsync(ct))return new(CommandStatus.NotFound);
        var units=PreparationUnits(actor,customerId,leadId);
        if(q is not null)units=units.Where(unit=>unit.Code.Contains(q)||unit.Title.Contains(q));
        var total=await units.CountAsync(ct);
        var rows=await PreparationProjection(units.OrderBy(unit=>unit.Code).ThenBy(unit=>unit.Id).Skip((page-1)*size).Take(size)).ToArrayAsync(ct);
        if(!await CurrentManagers(actor).AnyAsync(ct))return new(CommandStatus.Forbidden);
        return new(CommandStatus.Success,new(page,size,total,rows));
    }
    public async Task<DealResult<DealPreparationUnitDto>> PreparationUnitAsync(Guid actor,Guid customerId,Guid leadId,Guid unitId,CancellationToken ct)
    {
        if(!await CurrentManagers(actor).AnyAsync(ct))return new(CommandStatus.Forbidden);
        var row=await PreparationProjection(PreparationUnits(actor,customerId,leadId).Where(unit=>unit.Id==unitId)).SingleOrDefaultAsync(ct);
        if(!await CurrentManagers(actor).AnyAsync(ct))return new(CommandStatus.Forbidden);
        return row is null?new(CommandStatus.NotFound):new(CommandStatus.Success,row);
    }
    public async Task<DealResult<ManagerDealDto>> GetAsync(Guid actor,Guid customerId,Guid id,CancellationToken ct)
    {
        var managers=from ar in db.UserRoles join r in db.Roles on ar.RoleId equals r.Id where r.NormalizedName=="SALESMANAGER" select ar.UserId;
        var current=db.Users.Where(u=>u.Id==actor&&u.IsActive&&u.EmailConfirmed&&managers.Contains(u.Id));
        if(!await current.AnyAsync(ct))return new(CommandStatus.Forbidden);
        var row=await(from d in db.Deals.AsNoTracking() join c in db.Customers.AsNoTracking() on d.CustomerId equals c.Id
            where c.Id==customerId&&d.Id==id&&current.Any()
            select new{Deal=d,BookingId=db.DealBookingLinks.Where(l=>l.DealId==d.Id&&l.CustomerId==c.Id&&l.UnitId==d.UnitId).Select(l=>(Guid?)l.BookingId).SingleOrDefault()}).SingleOrDefaultAsync(ct);
        if(row is null)return new(CommandStatus.NotFound);
        var deal=row.Deal;return new(CommandStatus.Success,new(deal.Id,deal.CustomerId,deal.UnitId,deal.ProjectId,deal.LeadId,
            DateTime.SpecifyKind(deal.SaleOccurredAtUtc,DateTimeKind.Utc),deal.SaleLocalDate,deal.OpeningTimeZoneId,deal.FinalUnitPrice,deal.CurrencyCode,
            DateTime.SpecifyKind(deal.AdoptedAtUtc,DateTimeKind.Utc),deal.AdoptedByAccountId,row.BookingId,deal.Version));
    }
    public async Task<DealResult<CommissionSnapshotDto>> SnapshotAsync(Guid actor,Guid customerId,Guid id,CancellationToken ct)
    {
        var current=CurrentManagers(actor);
        if(!await current.AnyAsync(ct))return new(CommandStatus.Forbidden);
        var d=await(from deal in db.Deals.AsNoTracking() join c in db.Customers.AsNoTracking() on deal.CustomerId equals c.Id
            where c.Id==customerId&&deal.Id==id&&current.Any() select deal).SingleOrDefaultAsync(ct);
        if(d is null)return new(CommandStatus.NotFound);
        return new(CommandStatus.Success,new(d.Id,d.UnitId,d.LeadId,d.LeadVersion,d.CustomerId,d.ProjectId,d.DeveloperId,
            d.AgreementId,d.AgreementVersion,d.AgreementNumber,d.AgreementScope.ToString(),d.AgreementStartsOn,d.AgreementEndsOn,
            d.ProtectionPeriodDays,d.ProtectionEndsOn,d.LeadOpenedOnLocalDate,d.OpeningTimeZoneId,d.RuleId,d.RuleVersion,
            d.RuleProjectId,d.RuleType.ToString(),d.Percentage?.ToString("0.0000",CultureInfo.InvariantCulture),
            d.FixedAmount?.ToString("0.00",CultureInfo.InvariantCulture),d.CurrencyCode,d.SaleLocalDate,
            d.FinalUnitPrice.ToString("0.00",CultureInfo.InvariantCulture),d.Amount.ToString("0.00",CultureInfo.InvariantCulture),
            DateTime.SpecifyKind(d.SaleOccurredAtUtc,DateTimeKind.Utc),d.Source,d.Reference,d.SelectionReason,
            DateTime.SpecifyKind(d.AdoptedAtUtc,DateTimeKind.Utc),d.AdoptedByAccountId,d.Version));
    }
    public async Task<DealResult<DealPage<ManagerDealSummaryDto>>> ListAsync(Guid actor,DealFilterDto filter,CancellationToken ct)
    {
        var current=CurrentManagers(actor);
        if(!await current.AnyAsync(ct))return new(CommandStatus.Forbidden);
        var query=db.Deals.AsNoTracking().Where(d=>current.Any());
        if(filter.CustomerId is { } customer)query=query.Where(d=>d.CustomerId==customer);
        if(filter.ProjectId is { } project)query=query.Where(d=>d.ProjectId==project);
        if(filter.UnitId is { } unit)query=query.Where(d=>d.UnitId==unit);
        if(filter.DeveloperId is { } developer)query=query.Where(d=>d.DeveloperId==developer);
        if(filter.AgreementId is { } agreement)query=query.Where(d=>d.AgreementId==agreement);
        if(filter.CurrencyCode is { } currency)query=query.Where(d=>d.CurrencyCode==currency);
        if(filter.SaleLocalFrom is { } from)query=query.Where(d=>d.SaleLocalDate>=from);
        if(filter.SaleLocalThrough is { } through)query=query.Where(d=>d.SaleLocalDate<=through);
        var total=await query.CountAsync(ct);
        var rows=await query.OrderByDescending(d=>d.AdoptedAtUtc).ThenBy(d=>d.Id)
            .Skip((filter.Page-1)*filter.PageSize).Take(filter.PageSize)
            .Select(d=>new{d.Id,d.CustomerId,d.UnitId,d.ProjectId,d.LeadId,d.AgreementId,d.SaleOccurredAtUtc,
                d.AdoptedAtUtc,d.FinalUnitPrice,d.Amount,d.CurrencyCode,d.Version}).ToArrayAsync(ct);
        var items=rows.Select(d=>new ManagerDealSummaryDto(d.Id,d.CustomerId,d.UnitId,d.ProjectId,d.LeadId,d.AgreementId,
            DateTime.SpecifyKind(d.SaleOccurredAtUtc,DateTimeKind.Utc),DateTime.SpecifyKind(d.AdoptedAtUtc,DateTimeKind.Utc),
            d.FinalUnitPrice.ToString("0.00",CultureInfo.InvariantCulture),d.Amount.ToString("0.00",CultureInfo.InvariantCulture),d.CurrencyCode,d.Version)).ToArray();
        return new(CommandStatus.Success,new(filter.Page,filter.PageSize,total,items));
    }
    public async Task<DealResult<DealPage<ManagerCommissionCollectionDto>>> CollectionsAsync(Guid actor,Guid customerId,Guid dealId,int page,int size,CancellationToken ct)
    {
        var current=CurrentManagers(actor);
        if(!await current.AnyAsync(ct))return new(CommandStatus.Forbidden);
        if(!await db.Deals.AnyAsync(d=>d.Id==dealId&&d.CustomerId==customerId&&current.Any(),ct))return new(CommandStatus.NotFound);
        var query=db.CommissionCollections.AsNoTracking().Where(c=>c.DealId==dealId&&c.CustomerId==customerId&&current.Any());
        var total=await query.CountAsync(ct);
        var rows=await query.OrderBy(c=>c.RecordedAtUtc).ThenBy(c=>c.Id).Skip((page-1)*size).Take(size).ToArrayAsync(ct);
        return new(CommandStatus.Success,new(page,size,total,rows.Select(MapCollection).ToArray()));
    }
    public async Task<DealResult<ManagerCommissionCollectionDto>> CollectionAsync(Guid actor,Guid customerId,Guid dealId,Guid id,CancellationToken ct)
    {
        var current=CurrentManagers(actor);
        if(!await current.AnyAsync(ct))return new(CommandStatus.Forbidden);
        var row=await db.CommissionCollections.AsNoTracking().Where(c=>c.Id==id&&c.DealId==dealId&&c.CustomerId==customerId&&current.Any()).SingleOrDefaultAsync(ct);
        return row is null?new(CommandStatus.NotFound):new(CommandStatus.Success,MapCollection(row));
    }
    public async Task<DealResult<CommissionCollectionSummaryDto>> CollectionSummaryAsync(Guid actor,Guid customerId,Guid dealId,CancellationToken ct)
    {
        var current=CurrentManagers(actor);
        if(!await current.AnyAsync(ct))return new(CommandStatus.Forbidden);
        var row=await db.Deals.AsNoTracking().Where(d=>d.Id==dealId&&d.CustomerId==customerId&&current.Any())
            .Select(d=>new{d.Id,d.CustomerId,d.UnitId,d.CurrencyCode,EntitledAmount=d.Amount,
                CollectedAmount=db.CommissionCollections.Where(c=>c.DealId==d.Id).Sum(c=>(decimal?)c.Amount)??0m,
                Count=db.CommissionCollections.Count(c=>c.DealId==d.Id),
                Last=db.CommissionCollections.Where(c=>c.DealId==d.Id).Max(c=>(DateTime?)c.RecordedAtUtc)}).SingleOrDefaultAsync(ct);
        if(row is null)return new(CommandStatus.NotFound);
        if(row.CollectedAmount<0m||row.CollectedAmount>row.EntitledAmount)return new(CommandStatus.Conflict);
        return new(CommandStatus.Success,new(row.Id,row.CustomerId,row.UnitId,row.CurrencyCode,
            row.EntitledAmount.ToString("0.00",CultureInfo.InvariantCulture),row.CollectedAmount.ToString("0.00",CultureInfo.InvariantCulture),
            (row.EntitledAmount-row.CollectedAmount).ToString("0.00",CultureInfo.InvariantCulture),row.Count,
            row.Last is { } last?DateTime.SpecifyKind(last,DateTimeKind.Utc):null));
    }
    private static ManagerCommissionCollectionDto MapCollection(CommissionCollection c)=>new(c.Id,c.DealId,c.CustomerId,c.UnitId,c.RequestedVersion,
        c.Amount.ToString("0.00",CultureInfo.InvariantCulture),c.CurrencyCode,c.EntitledAmount.ToString("0.00",CultureInfo.InvariantCulture),
        DateTime.SpecifyKind(c.SaleOccurredAtUtc,DateTimeKind.Utc),DateTime.SpecifyKind(c.CollectedAtUtc,DateTimeKind.Utc),
        DateTime.SpecifyKind(c.RecordedAtUtc,DateTimeKind.Utc),c.RecorderAccountId,c.Source,c.Reference,c.Notes,c.Version);
    private IQueryable<Guid> CurrentManagers(Guid actor)=>db.Users.Where(u=>u.Id==actor&&u.IsActive&&u.EmailConfirmed&&
        db.UserRoles.Any(ar=>ar.UserId==u.Id&&db.Roles.Any(r=>r.Id==ar.RoleId&&r.NormalizedName=="SALESMANAGER"))).Select(u=>u.Id);
}
