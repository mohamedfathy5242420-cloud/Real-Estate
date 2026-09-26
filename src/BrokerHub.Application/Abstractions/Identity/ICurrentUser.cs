namespace BrokerHub.Application.Abstractions.Identity;

/// <summary>
/// Describes the authenticated actor without coupling Application to HTTP or a token format.
/// Resource ownership and current-assignment checks still belong to the use-case handler.
/// </summary>
public interface ICurrentUser
{
    bool IsAuthenticated { get; }

    Guid? AccountId { get; }

    bool IsInRole(string role);
}
