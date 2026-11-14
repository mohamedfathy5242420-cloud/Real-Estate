using System.Globalization;
using System.Text.Json;
using BrokerHub.Application.Abstractions.Identity;
using BrokerHub.Application.Authorization;
using BrokerHub.Application.DTOs.Commands;
using BrokerHub.Application.DTOs.Deals;
using BrokerHub.Application.Interfaces.Persistence;
using BrokerHub.Application.Interfaces.Services;
using BrokerHub.Domain.Entities;

namespace BrokerHub.Application.Services;

public sealed class CommissionCollectionService(ICrmCustomerRepository scope,IDealRepository repository,
    IDealQueries queries,ICommandExecutor executor,ICurrentUser user,TimeProvider clock):ICommissionCollectionService
{
    private Guid? Actor=>user.IsAuthenticated&&user.AccountId is { } actor&&actor!=Guid.Empty&&user.IsInRole(AppRoles.SalesManager)?actor:null;
    public Task<DealResult<DealPage<ManagerCommissionCollectionDto>>> ListAsync(Guid customerId,Guid dealId,int page,int size,CancellationToken ct)=>
        Actor is not { } actor?Task.FromResult(new DealResult<DealPage<ManagerCommissionCollectionDto>>(CommandStatus.Forbidden)):
        page is <=0 or >1000000||size is <=0 or >100?Task.FromResult(new DealResult<DealPage<ManagerCommissionCollectionDto>>(CommandStatus.Invalid)):
        queries.CollectionsAsync(actor,customerId,dealId,page,size,ct);
    public Task<DealResult<ManagerCommissionCollectionDto>> GetAsync(Guid customerId,Guid dealId,Guid id,CancellationToken ct)=>Actor is { } actor
        ?queries.CollectionAsync(actor,customerId,dealId,id,ct):Task.FromResult(new DealResult<ManagerCommissionCollectionDto>(CommandStatus.Forbidden));
    public Task<DealResult<CommissionCollectionSummaryDto>> SummaryAsync(Guid customerId,Guid dealId,CancellationToken ct)=>Actor is { } actor
        ?queries.CollectionSummaryAsync(actor,customerId,dealId,ct):Task.FromResult(new DealResult<CommissionCollectionSummaryDto>(CommandStatus.Forbidden));
    public async Task<DealResult<DealReply>> RecordAsync(Guid customerId,Guid dealId,RecordCommissionCollectionDto request,string key,CancellationToken ct)
    {
        if(Actor is not { } actor)return new(CommandStatus.Forbidden);
        var raw=request.CurrencyCode?.Trim();
        if(customerId==Guid.Empty||dealId==Guid.Empty||request.Version<=0||request.CollectedAtUtc==default||request.CollectedAtUtc.Offset!=TimeSpan.Zero||
            request.CurrencyCode is null||request.CurrencyCode.Length>16||raw is null||raw.Length!=3||raw.Any(c=>c is not (>= 'A' and <= 'Z' or >= 'a' and <= 'z'))||
            string.IsNullOrWhiteSpace(request.Source)||request.Source.Length>CommissionCollection.SourceMaxLength||
            string.IsNullOrWhiteSpace(request.Reference)||request.Reference.Length>CommissionCollection.ReferenceMaxLength||
            request.Notes?.Length>CommissionCollection.NotesMaxLength)return new(CommandStatus.Invalid);
        var currency=raw.ToUpperInvariant();
        if(!BookingDeposit.ValidMoney(request.Amount,currency))return new(CommandStatus.Invalid);
        var notes=string.IsNullOrWhiteSpace(request.Notes)?null:request.Notes.Trim();Deal? deal=null;
        var result=await executor.ExecuteAsync("Deal.RecordCollection",key,JsonSerializer.SerializeToElement(new
        {
            customerId,dealId,version=request.Version,amount=request.Amount.ToString("0.00",CultureInfo.InvariantCulture),
            currencyCode=currency,collectedAtUtc=request.CollectedAtUtc.UtcDateTime,source=request.Source.Trim(),reference=request.Reference.Trim(),notes
        }),async token=>
        {
            deal=null;var access=await scope.LockForSalesAsync(customerId,actor,true,token);
            if(access.Permission!=CommandPermission.Allowed)return access.Permission;
            deal=await repository.LockAsync(customerId,dealId,token);
            return deal is null?CommandPermission.NotFound:CommandPermission.Allowed;
        },async(context,token)=>
        {
            var parent=deal??throw new InvalidOperationException("Current Manager/Customer/Deal guard required.");
            if(parent.Version!=request.Version)return new CommandEffect(CommandStatus.Conflict);
            var ledger=await repository.CollectionLedgerAsync(parent.Id,token);var now=clock.GetUtcNow().UtcDateTime;
            if(!CommissionCollection.CanRecordAt(parent,ledger.LastRecordedAtUtc,now)||ledger.RecordedAmount<0m||ledger.RecordedAmount>parent.Amount)
                return new CommandEffect(CommandStatus.Conflict);
            if(currency!=parent.CurrencyCode||request.CollectedAtUtc.UtcDateTime<parent.SaleOccurredAtUtc||request.CollectedAtUtc.UtcDateTime>now)
                return new CommandEffect(CommandStatus.Invalid);
            if(request.Amount>parent.Amount-ledger.RecordedAmount)return new CommandEffect(CommandStatus.Conflict);
            try
            {
                var fact=CommissionCollection.Record(context.CommandId,parent,request.Version,ledger.RecordedAmount,ledger.LastRecordedAtUtc,
                    request.Amount,currency,request.CollectedAtUtc.UtcDateTime,now,actor,request.Source,request.Reference,notes);
                repository.Add(fact);
                context.Emit("Commission.CollectionRecorded",1,parent.CustomerId,JsonSerializer.SerializeToElement(new
                {
                    collectionId=fact.Id,dealId=parent.Id,customerId=parent.CustomerId,unitId=parent.UnitId,
                    dealVersion=parent.Version,collectionVersion=fact.Version
                }));
                return new CommandEffect(CommandStatus.Success,new CommandOutcome("CommissionCollection",fact.Id,fact.Version));
            }
            catch(ArgumentException){return new CommandEffect(CommandStatus.Invalid);}
            catch(InvalidOperationException){return new CommandEffect(CommandStatus.Conflict);}
        },ct);
        return result.Status==CommandStatus.Success?new(CommandStatus.Success,new(result.Outcome!.ResourceId,result.Outcome.ResourceVersion,result.IsReplay)):new(result.Status);
    }
}
