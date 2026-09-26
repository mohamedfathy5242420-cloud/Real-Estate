using Microsoft.AspNetCore.Identity;

namespace BrokerHub.Infrastructure.Identity;

/// <summary>
/// Persistence model for an authenticated account. The CRM Customer and Employee records are
/// separate domain concepts and will reference this identifier only through reviewed workflows.
/// </summary>
public sealed class ApplicationUser : IdentityUser<Guid>
{
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; }
}
