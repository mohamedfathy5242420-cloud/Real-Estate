using System.Collections.ObjectModel;

namespace BrokerHub.Application.DTOs.Messaging;

[Flags]
public enum NotificationAudience { None=0, CustomerOwner=1, AssignedEmployee=2, Manager=4 }
public sealed record NotificationEventDefinition(string Name,string ResourceKind,string ResourceIdProperty,
    NotificationAudience Audience,string? SystemSource=null,string? ParentIdProperty=null,string? RelatedIdProperty=null);

public static class NotificationEventCatalog
{
    private const NotificationAudience All=NotificationAudience.CustomerOwner|NotificationAudience.AssignedEmployee|NotificationAudience.Manager;
    private const NotificationAudience Staff=NotificationAudience.AssignedEmployee|NotificationAudience.Manager;
    public static IReadOnlyDictionary<string,NotificationEventDefinition> Events { get; }=Build();
    public static NotificationEventDefinition? Find(string name)=>Events.GetValueOrDefault(name);
    private static IReadOnlyDictionary<string,NotificationEventDefinition> Build()
    {
        NotificationEventDefinition[] definitions=[
            new("Crm.CustomerCreated","Customer","customerId",NotificationAudience.None),
            new("Crm.CustomerRequirementsChanged","Customer","customerId",NotificationAudience.None),
            new("Crm.NoteAdded","Customer","customerId",NotificationAudience.None),
            new("Crm.NoteCorrected","Customer","customerId",NotificationAudience.None),
            new("Crm.CustomerAssigned","Customer","customerId",All),
            new("Crm.CustomerTransferred","Customer","customerId",All),
            new("Crm.LeadOpened","Customer","customerId",NotificationAudience.None),
            new("Crm.LeadClosed","Customer","customerId",NotificationAudience.None),
            new("Viewing.Requested","Viewing","viewingId",All),
            new("Viewing.Confirmed","Viewing","viewingId",All),
            new("Viewing.AlternativeOffered","Viewing","viewingId",All),
            new("Viewing.Rescheduled","Viewing","viewingId",All),
            new("Viewing.Cancelled","Viewing","viewingId",All),
            new("Viewing.OutcomeRecorded","Viewing","viewingId",All),
            new("Viewing.Expired","Viewing","viewingId",All,"System.ViewingExpiry"),
            new("Booking.Requested","Booking","bookingId",All),
            new("Booking.Confirmed","Booking","bookingId",All),
            new("Booking.Rejected","Booking","bookingId",All),
            new("Booking.DeveloperResponseRecorded","Booking","bookingId",Staff),
            new("Booking.Extended","Booking","bookingId",All),
            new("Booking.Cancelled","Booking","bookingId",All),
            new("Booking.DepositRecorded","BookingDeposit","depositId",NotificationAudience.Manager,ParentIdProperty:"bookingId"),
            new("Booking.RefundRecorded","BookingRefund","refundId",NotificationAudience.Manager,ParentIdProperty:"bookingId",RelatedIdProperty:"depositId"),
            new("Booking.Completed","Booking","bookingId",All),
            new("Booking.Expired","Booking","bookingId",All,"System.BookingExpiry"),
            new("Booking.ResponseTimedOut","Booking","bookingId",All,"System.BookingResponseTimeout"),
            new("Catalog.UnitUnpublished","Unit","unitId",NotificationAudience.Manager),
            new("Deal.Adopted","Deal","dealId",NotificationAudience.Manager),
            new("Commission.CollectionRecorded","CommissionCollection","collectionId",NotificationAudience.Manager,ParentIdProperty:"dealId")
        ];
        return new ReadOnlyDictionary<string,NotificationEventDefinition>(definitions.ToDictionary(x=>x.Name,StringComparer.Ordinal));
    }
}
