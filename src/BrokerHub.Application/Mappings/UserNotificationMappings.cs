using System.Collections.ObjectModel;
using BrokerHub.Application.DTOs.Notifications;
using BrokerHub.Domain.Entities;

namespace BrokerHub.Application.Mappings;

public static class UserNotificationMappings
{
    public static IReadOnlyDictionary<string,(string Title,string Message)> Templates { get; }=
        new ReadOnlyDictionary<string,(string,string)>(new Dictionary<string,(string,string)>(StringComparer.Ordinal)
        {
            ["Crm.CustomerAssigned"]=("Customer assignment","A customer assignment was recorded."),
            ["Crm.CustomerTransferred"]=("Customer responsibility transferred","A customer responsibility transfer was recorded."),
            ["Viewing.Requested"]=("Request viewing","A viewing request was recorded."),
            ["Viewing.Confirmed"]=("Confirm viewing","A viewing confirmation was recorded."),
            ["Viewing.AlternativeOffered"]=("Alternative viewing time","An alternative viewing time was proposed."),
            ["Viewing.Rescheduled"]=("Reschedule viewing","A viewing reschedule was recorded."),
            ["Viewing.Cancelled"]=("Cancel viewing","A viewing cancellation was recorded."),
            ["Viewing.OutcomeRecorded"]=("Viewing outcome","A viewing outcome was recorded."),
            ["Viewing.Expired"]=("Viewing deadline expired","A viewing request deadline expired."),
            ["Booking.Requested"]=("Request booking","A booking request was recorded."),
            ["Booking.Confirmed"]=("Confirm booking","An external booking confirmation was recorded."),
            ["Booking.Rejected"]=("Reject booking","A booking request rejection was recorded."),
            ["Booking.DeveloperResponseRecorded"]=("Developer booking response","An external response to a booking request was recorded."),
            ["Booking.Extended"]=("Extend booking","A booking extension was recorded."),
            ["Booking.Cancelled"]=("Cancel booking","A booking cancellation was recorded."),
            ["Booking.DepositRecorded"]=("Record deposit","An externally paid deposit was recorded."),
            ["Booking.RefundRecorded"]=("Record refund","An external refund was recorded."),
            ["Booking.Completed"]=("Link booking to sale","A booking was completed and linked to an external sale."),
            ["Booking.Expired"]=("Booking expired","A booking period expired."),
            ["Booking.ResponseTimedOut"]=("Booking response deadline expired","The waiting period for a booking response expired."),
            ["Catalog.UnitUnpublished"]=("Unpublish unit","A unit was unpublished."),
            ["Deal.Adopted"]=("Record external sale","An external sale was recorded."),
            ["Commission.CollectionRecorded"]=("Record commission collection","An external commission collection was recorded.")
        });
    public static DateTimeOffset Utc(DateTime at)=>new(DateTime.SpecifyKind(at,DateTimeKind.Utc));
    public static UserNotificationDto ToUserNotification(this Notification fact,DateTime? readAtUtc)
    {
        if(!Templates.TryGetValue(fact.EventName,out var template))throw new ArgumentException("Unsupported notification template.");
        return new(fact.Id,fact.EventName,template.Title,template.Message,Utc(fact.OccurredAtUtc),Utc(fact.CreatedAtUtc),
            readAtUtc.HasValue?Utc(readAtUtc.Value):null,new(fact.ResourceKind,fact.ResourceId,fact.CustomerId,fact.ParentResourceId,fact.RelatedResourceId));
    }
}
