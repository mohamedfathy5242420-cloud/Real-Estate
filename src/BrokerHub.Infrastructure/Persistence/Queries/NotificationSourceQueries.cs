using System.Security.Cryptography;
using System.Text.Json;
using BrokerHub.Application.DTOs.Messaging;
using BrokerHub.Infrastructure.Messaging;
using BrokerHub.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace BrokerHub.Infrastructure.Persistence.Queries;

internal sealed class NotificationSourceQueries(ApplicationDbContext db)
{
    private static DateTime Utc(DateTime at)=>DateTime.SpecifyKind(at,DateTimeKind.Utc);
    public async Task<NotificationSourceFacts?> ReadAsync(ValidatedNotificationEnvelope input,CancellationToken ct)
    {
        var delivery=await db.OutboxDeliveries.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==input.Envelope.MessageId,ct);
        if(delivery is null||delivery.AttemptCount<=0)return null;
        OutboxEnvelope expected;
        if(delivery.HumanOutboxId is { } humanId)
        {
            var e=await db.OutboxMessages.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==humanId,ct);if(e is null)return null;
            expected=new(1,delivery.Id,"OutboxMessages",e.Id,e.Name,e.SchemaVersion,e.AggregateId,e.ActorAccountId,
                null,null,null,Utc(e.OccurredAtUtc),Utc(e.RecordedAtUtc),e.PayloadJson);
        }
        else if(delivery.SystemOutboxId is { } systemId)
        {
            var e=await db.SystemOutboxMessages.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==systemId,ct);if(e is null)return null;
            expected=new(1,delivery.Id,"SystemOutboxMessages",e.Id,e.Name,e.SchemaVersion,e.AggregateId,null,
                e.Source,e.ResourceId,e.ResourceVersion,Utc(e.OccurredAtUtc),Utc(e.RecordedAtUtc),e.PayloadJson);
        }
        else return null;
        var definition=NotificationEventCatalog.Find(expected.Name);
        if(definition is null||expected.EventSchemaVersion!=1||expected.SystemSource!=definition.SystemSource||!expected.IsValid())return null;
        var hash=Convert.ToHexString(SHA256.HashData(OutboxEnvelopeWire.Serialize(expected)));
        if(hash!=input.BodyHash)return null;
        using var document=JsonDocument.Parse(expected.PayloadJson);var payload=document.RootElement;
        if(!Id(payload,definition.ResourceIdProperty,out var resourceId))return null;
        Guid? parent=null,related=null;
        if(definition.ParentIdProperty is { } parentName){if(!Id(payload,parentName,out var parentId))return null;parent=parentId;}
        if(definition.RelatedIdProperty is { } relatedName){if(!Id(payload,relatedName,out var relatedId))return null;related=relatedId;}
        Guid? customerId=definition.ResourceKind=="Unit"?null:expected.AggregateId;
        if(customerId.HasValue&&(!Id(payload,"customerId",out var suppliedCustomer)||suppliedCustomer!=customerId.Value||
            !await db.Customers.AsNoTracking().AnyAsync(x=>x.Id==customerId.Value,ct)))return null;
        var linked=definition.ResourceKind switch
        {
            "Customer"=>resourceId==customerId&&await CrmResourceExists(expected.Name,payload,customerId!.Value,ct),
            "Viewing"=>await db.Viewings.AsNoTracking().AnyAsync(x=>x.Id==resourceId&&x.CustomerId==customerId,ct),
            "Booking"=>await db.Bookings.AsNoTracking().AnyAsync(x=>x.Id==resourceId&&x.CustomerId==customerId,ct),
            "BookingDeposit"=>await db.BookingDeposits.AsNoTracking().AnyAsync(x=>x.Id==resourceId&&x.CustomerId==customerId&&x.BookingId==parent,ct),
            "BookingRefund"=>await db.BookingRefunds.AsNoTracking().AnyAsync(x=>x.Id==resourceId&&x.CustomerId==customerId&&x.BookingId==parent&&x.DepositId==related,ct),
            "Deal"=>await db.Deals.AsNoTracking().AnyAsync(x=>x.Id==resourceId&&x.CustomerId==customerId,ct),
            "CommissionCollection"=>await db.CommissionCollections.AsNoTracking().AnyAsync(x=>x.Id==resourceId&&x.CustomerId==customerId&&x.DealId==parent,ct),
            "Unit"=>resourceId==expected.AggregateId&&await db.Units.AsNoTracking().AnyAsync(x=>x.Id==resourceId,ct),
            _=>false
        };
        if(!linked)return null;
        if(expected.ResourceId.HasValue&&(expected.ResourceId!=resourceId||!payload.TryGetProperty("version",out var version)||
            !version.TryGetInt64(out var originalVersion)||expected.ResourceVersion!=originalVersion))return null;
        return new(expected,definition,hash,customerId,resourceId,parent,related);
    }
    private async Task<bool> CrmResourceExists(string name,JsonElement payload,Guid customerId,CancellationToken ct)
    {
        if(name is "Crm.CustomerAssigned" or "Crm.CustomerTransferred")return Id(payload,"assignmentId",out var assignmentId)&&
            await db.CustomerAssignments.AsNoTracking().AnyAsync(x=>x.Id==assignmentId&&x.CustomerId==customerId,ct);
        if(name is "Crm.NoteAdded" or "Crm.NoteCorrected")return Id(payload,"noteId",out var noteId)&&
            await db.SalesNotes.AsNoTracking().AnyAsync(x=>x.Id==noteId&&x.CustomerId==customerId,ct);
        if(name is "Crm.LeadOpened" or "Crm.LeadClosed")return Id(payload,"leadId",out var leadId)&&
            await db.Leads.AsNoTracking().AnyAsync(x=>x.Id==leadId&&x.CustomerId==customerId,ct);
        if(name=="Crm.CustomerRequirementsChanged")return Id(payload,"changeId",out var changeId)&&
            await db.CustomerNeedsChanges.AsNoTracking().AnyAsync(x=>x.Id==changeId&&x.CustomerId==customerId,ct);
        return name=="Crm.CustomerCreated";
    }
    private static bool Id(JsonElement payload,string name,out Guid id)
    {id=Guid.Empty;return payload.TryGetProperty(name,out var value)&&value.ValueKind==JsonValueKind.String&&value.TryGetGuid(out id)&&id!=Guid.Empty;}
    public async Task<Guid[]> RecipientsAsync(NotificationSourceFacts source,CancellationToken ct)
    {
        var audience=source.Definition.Audience;if(audience==NotificationAudience.None)return [];
        Guid? owner=null,employeeAccount=null;
        if(source.CustomerId is { } customerId)
        {
            var customer=await db.Customers.AsNoTracking().Where(x=>x.Id==customerId).Select(x=>new{x.AccountId,x.AssignedEmployeeId}).SingleAsync(ct);
            owner=customer.AccountId;
            if(customer.AssignedEmployeeId is { } employeeId)employeeAccount=await db.Employees.AsNoTracking()
                .Where(x=>x.Id==employeeId).Select(x=>(Guid?)x.AccountId).SingleOrDefaultAsync(ct);
        }
        var owners=AccountsWithRole("CUSTOMER");var employees=AccountsWithRole("SALESEMPLOYEE");var managers=AccountsWithRole("SALESMANAGER");
        var wantOwner=audience.HasFlag(NotificationAudience.CustomerOwner);var wantEmployee=audience.HasFlag(NotificationAudience.AssignedEmployee);
        var wantManager=audience.HasFlag(NotificationAudience.Manager);
        return await db.Users.AsNoTracking().Where(a=>a.IsActive&&a.EmailConfirmed&&
            (wantOwner&&a.Id==owner&&owners.Contains(a.Id)||wantEmployee&&a.Id==employeeAccount&&employees.Contains(a.Id)||wantManager&&managers.Contains(a.Id)))
            .Select(a=>a.Id).ToArrayAsync(ct);
    }
    private IQueryable<Guid> AccountsWithRole(string normalized)=>from ar in db.UserRoles join role in db.Roles on ar.RoleId equals role.Id
        where role.NormalizedName==normalized select ar.UserId;
}
