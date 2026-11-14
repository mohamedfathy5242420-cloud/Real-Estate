namespace BrokerHub.Api.Contracts.Accounts;

public sealed record AccountResponse(Guid AccountId, string Email, IReadOnlyCollection<string> Roles);

public sealed record AcceptedMessageResponse(string Message);
