using BrokerHub.Application.DTOs.Customers;
using BrokerHub.Domain.Entities;

namespace BrokerHub.Application.Mappings;

public static class CustomerLinkMappings
{
    private static DateTime Utc(DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Utc);
    public static OwnCustomerLinkDto ToOwnDto(this CustomerLinkRequest r) => new(r.Id, r.Claim, r.Status.ToString(),
        Utc(r.CreatedAtUtc), r.DecidedAtUtc is { } at ? Utc(at) : null, r.Version);
    public static CustomerLinkReviewDto ToReviewDto(this CustomerLinkRequest r) => new(r.Id, r.AccountId, r.Claim,
        r.Status.ToString(), Utc(r.CreatedAtUtc), r.DecidedAtUtc is { } at ? Utc(at) : null, r.Version, r.DecisionReason,
        r.DecidedByAccountId, r.CustomerId, r.History.OrderBy(e => e.Sequence).Select(e => new CustomerLinkEventDto(
            e.Id, e.Kind, e.ActorAccountId, Utc(e.AtUtc), e.Reason, e.CustomerId)).ToArray());
}
