using System.Globalization;
using System.Text.Json;
using BrokerHub.Application.Abstractions.Identity;
using BrokerHub.Application.Authorization;
using BrokerHub.Application.DTOs.Commands;
using BrokerHub.Application.DTOs.Deals;
using BrokerHub.Application.Interfaces.Persistence;
using BrokerHub.Application.Interfaces.Services;
using BrokerHub.Domain.Entities;
using BrokerHub.Domain.ValueObjects;

namespace BrokerHub.Application.Services;

public sealed class DealService(ICrmCustomerRepository customers,ILeadRepository leads,IUnitRepository units,
    IBookingRepository bookings,IDealRepository repository,IDealQueries queries,IUnitPublicationRepository publications,
    ICommandExecutor executor,ICurrentUser user,TimeProvider clock):IDealService
{
    private Guid? Actor=>user.IsAuthenticated&&user.AccountId is { } actor&&actor!=Guid.Empty&&user.IsInRole(AppRoles.SalesManager)?actor:null;
    public Task<DealResult<DealPage<DealPreparationUnitDto>>> PreparationUnitsAsync(Guid customerId,Guid leadId,int page,int size,string? q,CancellationToken ct)=>
        Actor is not { } actor?Task.FromResult(new DealResult<DealPage<DealPreparationUnitDto>>(CommandStatus.Forbidden)):
        customerId==Guid.Empty||leadId==Guid.Empty||page is <=0 or >1000000||size is <=0 or >100||q is not null&&(string.IsNullOrWhiteSpace(q)||q.Length>200)
        ?Task.FromResult(new DealResult<DealPage<DealPreparationUnitDto>>(CommandStatus.Invalid))
        :queries.PreparationUnitsAsync(actor,customerId,leadId,page,size,q?.Trim(),ct);
    public Task<DealResult<DealPreparationUnitDto>> PreparationUnitAsync(Guid customerId,Guid leadId,Guid unitId,CancellationToken ct)=>
        Actor is not { } actor?Task.FromResult(new DealResult<DealPreparationUnitDto>(CommandStatus.Forbidden)):
        customerId==Guid.Empty||leadId==Guid.Empty||unitId==Guid.Empty?Task.FromResult(new DealResult<DealPreparationUnitDto>(CommandStatus.Invalid))
        :queries.PreparationUnitAsync(actor,customerId,leadId,unitId,ct);
    public Task<DealResult<ManagerDealDto>> GetAsync(Guid customerId,Guid id,CancellationToken ct)=>Actor is { } actor
        ?queries.GetAsync(actor,customerId,id,ct):Task.FromResult(new DealResult<ManagerDealDto>(CommandStatus.Forbidden));
    public Task<DealResult<CommissionSnapshotDto>> SnapshotAsync(Guid customerId,Guid id,CancellationToken ct)=>Actor is { } actor
        ?queries.SnapshotAsync(actor,customerId,id,ct):Task.FromResult(new DealResult<CommissionSnapshotDto>(CommandStatus.Forbidden));
    public Task<DealResult<DealPage<ManagerDealSummaryDto>>> ListAsync(DealFilterDto filter,CancellationToken ct)
    {
        if(Actor is not { } actor)return Task.FromResult(new DealResult<DealPage<ManagerDealSummaryDto>>(CommandStatus.Forbidden));
        var currency=filter.CurrencyCode?.Trim().ToUpperInvariant();
        if(filter.Page is <=0 or >1000000||filter.PageSize is <=0 or >100||
            filter.CustomerId==Guid.Empty||filter.ProjectId==Guid.Empty||filter.UnitId==Guid.Empty||
            filter.DeveloperId==Guid.Empty||filter.AgreementId==Guid.Empty||
            filter.SaleLocalFrom==default(DateOnly)||filter.SaleLocalThrough==default(DateOnly)||
            filter.SaleLocalFrom>filter.SaleLocalThrough||filter.CurrencyCode is not null&&
                (filter.CurrencyCode.Length>16||currency!.Length!=3||currency.Any(c=>c is < 'A' or > 'Z')))
            return Task.FromResult(new DealResult<DealPage<ManagerDealSummaryDto>>(CommandStatus.Invalid));
        return queries.ListAsync(actor,filter with{CurrencyCode=currency},ct);
    }
    public async Task<DealResult<DealReply>> AdoptAsync(Guid customerId,AdoptDealDto request,string key,CancellationToken ct)
    {
        if(Actor is not { } actor)return new(CommandStatus.Forbidden);
        var currency=request.CurrencyCode?.Trim().ToUpperInvariant();
        if(customerId==Guid.Empty||request.UnitId==Guid.Empty||request.LeadId==Guid.Empty||request.UnitVersion<=0||request.LeadVersion<=0||
            request.CurrencyCode is null||request.CurrencyCode.Length>16||currency is null||!BookingDeposit.ValidMoney(request.FinalUnitPrice,currency)||
            request.SaleOccurredAtUtc==default||request.SaleOccurredAtUtc.Offset!=TimeSpan.Zero||
            string.IsNullOrWhiteSpace(request.Source)||request.Source.Length>Deal.SourceMaxLength||
            string.IsNullOrWhiteSpace(request.Reference)||request.Reference.Length>Deal.ReferenceMaxLength||
            request.SelectionReason?.Length>Deal.SelectionReasonMaxLength||
            request.BookingId.HasValue!=request.BookingVersion.HasValue||request.BookingId==Guid.Empty||request.BookingVersion is <=0)
            return new(CommandStatus.Invalid);
        var reason=string.IsNullOrWhiteSpace(request.SelectionReason)?null:request.SelectionReason.Trim();
        Lead? lead=null;Booking? booking=null;
        var result=await executor.ExecuteAsync("Deal.Adopt",key,JsonSerializer.SerializeToElement(new
        {
            customerId,request.UnitId,request.UnitVersion,request.LeadId,request.LeadVersion,
            finalUnitPrice=request.FinalUnitPrice.ToString("0.00",CultureInfo.InvariantCulture),currencyCode=currency,
            saleOccurredAtUtc=request.SaleOccurredAtUtc.UtcDateTime,source=request.Source.Trim(),reference=request.Reference.Trim(),
            selectionReason=reason,request.BookingId,request.BookingVersion
        }),async token=>
        {
            lead=null;booking=null;
            var access=await customers.LockForSalesAsync(customerId,actor,true,token);
            if(access.Permission!=CommandPermission.Allowed)return access.Permission;
            lead=await leads.LockByIdAsync(customerId,request.LeadId,token);
            if(lead is null)return CommandPermission.NotFound;
            if(request.BookingId is { } bookingId)
            {
                booking=await bookings.LockAsync(customerId,bookingId,token);
                if(booking is null)return CommandPermission.NotFound;
            }
            return CommandPermission.Allowed;
        },async(context,token)=>
        {
            var original=lead??throw new InvalidOperationException("Current Manager/Customer/original Lead guard required.");
            if(original.Version!=request.LeadVersion||original.Status is not (Lead.Open or Lead.Closed))return new CommandEffect(CommandStatus.Conflict);
            var project=await leads.LockProjectOriginAsync(original.ProjectId,token);
            if(project is null)return new CommandEffect(CommandStatus.NotFound);
            var candidates=await leads.SaleCandidatesAsync(customerId,original.ProjectId,token);
            if(candidates.Count>1000||!candidates.Any(l=>l.Id==original.Id))return new CommandEffect(CommandStatus.Conflict);
            var agreements=new Dictionary<Guid,MarketingAgreement>();
            foreach(var agreementId in candidates.Select(l=>l.MarketingAgreementId).Distinct().Order())
            {
                var agreement=await leads.LockOriginalAgreementAsync(agreementId,project.DeveloperId,token);
                if(agreement is not null)agreements.Add(agreementId,agreement);
            }
            var unit=await units.LockAsync(request.UnitId,token);
            if(unit is null)return new CommandEffect(CommandStatus.NotFound);
            if(unit.ProjectId!=original.ProjectId||unit.Version!=request.UnitVersion||await bookings.HasAdoptedSaleAsync(unit.Id,token))
                return new CommandEffect(CommandStatus.Conflict);
            if(booking is { } candidate&&(candidate.UnitId!=unit.Id||candidate.ProjectId!=original.ProjectId||candidate.LeadId!=original.Id||
                candidate.Version!=request.BookingVersion||candidate.Status!=Booking.Confirmed||candidate.Version==long.MaxValue))
                return new CommandEffect(CommandStatus.Conflict);
            var claim=await bookings.LockClaimAsync(unit.Id,token);
            var observed=clock.GetUtcNow().UtcDateTime;
            // No foreign root UPDLOCK under Unit. All supported root state/deadline writes hold Unit
            // before SQL mutation; ordinary shared reads remain compatible with a waiting owner U lock.
            if(claim?.IsCurrent(observed)==true&&claim.BookingId!=booking?.Id||
                await bookings.HasOtherCurrentConfirmedAsync(unit.Id,booking?.Id,observed,token))return new CommandEffect(CommandStatus.Conflict);
            var now=clock.GetUtcNow().UtcDateTime;
            if(request.SaleOccurredAtUtc.UtcDateTime<original.OpenedAtUtc||request.SaleOccurredAtUtc.UtcDateTime>now)
                return new CommandEffect(CommandStatus.Invalid);
            if(now<unit.CreatedAtUtc||now<unit.UpdatedAtUtc||now<original.OpenedAtUtc||now<original.ClosedAtUtc||
                booking is { } b&&(!BookingDeposit.CanRecordAt(b,now)||b.IsDue(now)||claim is null||claim.BookingId!=b.Id||
                    claim.ExpiresAtUtc!=b.ReservationExpiresAtUtc||!claim.IsCurrent(now)))return new CommandEffect(CommandStatus.Conflict);
            var eligible=new Dictionary<Guid,CommissionCalculation>();
            foreach(var originCandidate in candidates)
            {
                if(request.SaleOccurredAtUtc.UtcDateTime<originCandidate.OpenedAtUtc||now<originCandidate.ClosedAtUtc||
                    !agreements.TryGetValue(originCandidate.MarketingAgreementId,out var agreement))continue;
                try
                {
                    var zone=TimeZoneInfo.FindSystemTimeZoneById(originCandidate.OpeningTimeZoneId);
                    var localDate=DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(request.SaleOccurredAtUtc,zone).DateTime);
                    eligible.Add(originCandidate.Id,CommissionCalculation.Calculate(originCandidate,agreement,unit.ProjectId,project.DeveloperId,localDate,request.FinalUnitPrice,currency));
                }
                catch(TimeZoneNotFoundException){ }
                catch(InvalidTimeZoneException){ }
                catch(ArgumentException){ }
                catch(InvalidOperationException){ }
            }
            if(!eligible.TryGetValue(original.Id,out var calculation)||eligible.Count>1&&reason is null)
                return new CommandEffect(CommandStatus.Conflict);
            // All rejecting checks precede staging. The shared executor rolls back exceptions.
            var deal=Deal.Adopt(context.CommandId,unit,original,calculation,request.SaleOccurredAtUtc.UtcDateTime,now,actor,request.Source,request.Reference,reason);
            UnitPublicationTransition? transition=null;
            if(unit.PublicationStatus==Unit.Published)
            {
                if(unit.Version==long.MaxValue)return new CommandEffect(CommandStatus.Conflict);
                transition=unit.ChangePublication(false,unit.Version,now,actor,"adopted-sale",null,null,null);
            }
            repository.Add(deal);
            if(booking is { } linked)
            {
                var before=linked.Version;linked.Complete(before,deal.Id,now,actor);
                repository.Add(DealBookingLink.Record(deal,linked,before));
                bookings.AddEvent(BookingEvent.Completion(context.CommandId,linked,deal));
                if(claim?.Release(linked.Id,now)!=true)throw new InvalidOperationException("Protected linked claim changed unexpectedly.");
                context.Emit("Booking.Completed",1,customerId,JsonSerializer.SerializeToElement(new{bookingId=linked.Id,customerId,unitId=unit.Id,dealId=deal.Id,status=linked.Status,version=linked.Version}));
            }
            if(transition is not null)
            {
                await publications.AddAsync(transition,token);
                context.Emit("Catalog.UnitUnpublished",1,unit.Id,JsonSerializer.SerializeToElement(new{unitId=unit.Id,projectId=unit.ProjectId,dealId=deal.Id,version=unit.Version,reasonCode="adopted-sale"}));
            }
            context.Emit("Deal.Adopted",1,customerId,JsonSerializer.SerializeToElement(new{dealId=deal.Id,customerId,unitId=unit.Id,leadId=original.Id,version=deal.Version,bookingId=booking?.Id,bookingVersion=booking?.Version}));
            return new CommandEffect(CommandStatus.Success,new CommandOutcome("Deal",deal.Id,deal.Version));
        },ct);
        return result.Status==CommandStatus.Success?new(CommandStatus.Success,new(result.Outcome!.ResourceId,result.Outcome.ResourceVersion,result.IsReplay)):new(result.Status);
    }
}
