namespace BrokerHub.Domain.Entities;

public sealed class Notification
{
    private Notification() { }
    public Guid Id { get; private set; }
    public Guid InboxMessageId { get; private set; }
    public Guid RecipientAccountId { get; private set; }
    public Guid? CustomerId { get; private set; }
    public string EventName { get; private set; }="";
    public string ResourceKind { get; private set; }="";
    public Guid ResourceId { get; private set; }
    public Guid? ParentResourceId { get; private set; }
    public Guid? RelatedResourceId { get; private set; }
    public DateTime OccurredAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public static Notification Create(Guid id,InboxMessage inbox,Guid recipient,Guid? customerId,
        string resourceKind,Guid resourceId,Guid? parentResourceId=null,Guid? relatedResourceId=null)
    {
        ArgumentNullException.ThrowIfNull(inbox);
        if(id==Guid.Empty||recipient==Guid.Empty||resourceId==Guid.Empty||customerId==Guid.Empty||
            parentResourceId==Guid.Empty||relatedResourceId==Guid.Empty||resourceKind is not
            ("Customer" or "Viewing" or "Booking" or "BookingDeposit" or "BookingRefund" or "Deal" or "CommissionCollection" or "Unit"))
            throw new ArgumentException("Valid notification target and safe resource identities required.");
        if((resourceKind=="Unit")!=(!customerId.HasValue))
            throw new ArgumentException("Only catalog notifications have no Customer scope.");
        return new(){Id=id,InboxMessageId=inbox.Id,RecipientAccountId=recipient,CustomerId=customerId,
            EventName=inbox.EventName,ResourceKind=resourceKind,ResourceId=resourceId,
            ParentResourceId=parentResourceId,RelatedResourceId=relatedResourceId,
            OccurredAtUtc=inbox.OccurredAtUtc,CreatedAtUtc=inbox.ProcessedAtUtc};
    }
}
