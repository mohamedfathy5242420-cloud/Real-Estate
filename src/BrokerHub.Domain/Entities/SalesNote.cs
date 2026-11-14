namespace BrokerHub.Domain.Entities;

public sealed class SalesNote
{
    public const int BodyMaxLength = 2000;
    private SalesNote() { }
    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }
    public Guid? LeadId { get; private set; }
    public Guid AuthorAccountId { get; private set; }
    public Guid? AuthorEmployeeId { get; private set; }
    public string Body { get; private set; } = "";
    public DateTime CreatedAtUtc { get; private set; }
    public Guid? CorrectsNoteId { get; private set; }
    public static SalesNote Create(Guid id, Guid customerId, Guid? leadId, Guid actor, Guid? employeeId, string body, DateTime atUtc, Guid? corrects)
    {
        if (id == Guid.Empty || customerId == Guid.Empty || leadId == Guid.Empty || actor == Guid.Empty || employeeId == Guid.Empty || corrects == Guid.Empty || corrects == id)
            throw new ArgumentException("Note identities required.");
        if (atUtc == default || atUtc.Kind != DateTimeKind.Utc || string.IsNullOrWhiteSpace(body) || body.Trim().Length > BodyMaxLength)
            throw new ArgumentException("Bounded nonblank body and explicit UTC required.");
        return new() { Id = id, CustomerId = customerId, LeadId = leadId, AuthorAccountId = actor, AuthorEmployeeId = employeeId,
            Body = body.Trim(), CreatedAtUtc = atUtc, CorrectsNoteId = corrects };
    }
}
