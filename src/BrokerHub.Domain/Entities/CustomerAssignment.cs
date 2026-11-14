namespace BrokerHub.Domain.Entities;

// Immutable ownership transition. Customer's single FK slot is the current-owner authority.
public sealed class CustomerAssignment
{
    public const int ReasonMaxLength = 2000;
    private CustomerAssignment() { }
    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }
    public Guid? PreviousEmployeeId { get; private set; }
    public Guid NewEmployeeId { get; private set; }
    public Guid ActorAccountId { get; private set; }
    public DateTime ChangedAtUtc { get; private set; }
    public string? Reason { get; private set; }
    public long CustomerVersion { get; private set; }
    public static CustomerAssignment Record(Guid id, Guid customerId, Guid? previous, Guid next, Guid actor,
        DateTime atUtc, string? reason, long customerVersion)
    {
        if (id == Guid.Empty || customerId == Guid.Empty || previous == Guid.Empty || next == Guid.Empty || actor == Guid.Empty ||
            previous == next || customerVersion <= 1) throw new ArgumentException("Invalid assignment identities/version.");
        if (atUtc == default || atUtc.Kind != DateTimeKind.Utc) throw new ArgumentException("Explicit UTC assignment time required.");
        reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        if (reason?.Length > ReasonMaxLength || (previous.HasValue && reason is null)) throw new ArgumentException("Transfer reason required, max2000.");
        return new() { Id = id, CustomerId = customerId, PreviousEmployeeId = previous, NewEmployeeId = next,
            ActorAccountId = actor, ChangedAtUtc = atUtc, Reason = reason, CustomerVersion = customerVersion };
    }
}
