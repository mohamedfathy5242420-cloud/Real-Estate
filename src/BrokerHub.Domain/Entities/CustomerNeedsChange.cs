namespace BrokerHub.Domain.Entities;

public sealed class CustomerNeedsChange
{
    private CustomerNeedsChange() { }
    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }
    public string? PreviousRequirements { get; private set; }
    public string? Requirements { get; private set; }
    public long CustomerVersion { get; private set; }
    public Guid ActorAccountId { get; private set; }
    public DateTime AtUtc { get; private set; }
    public static CustomerNeedsChange Record(Guid id, Customer customer, string? before, DateTime atUtc, Guid actor)
    {
        if (id == Guid.Empty || actor == Guid.Empty || customer.Version <= 1 || atUtc == default || atUtc.Kind != DateTimeKind.Utc || atUtc < customer.CreatedAtUtc)
            throw new ArgumentException("Needs history audit/version required.");
        return new() { Id = id, CustomerId = customer.Id, PreviousRequirements = before, Requirements = customer.Requirements,
            CustomerVersion = customer.Version, ActorAccountId = actor, AtUtc = atUtc };
    }
}
