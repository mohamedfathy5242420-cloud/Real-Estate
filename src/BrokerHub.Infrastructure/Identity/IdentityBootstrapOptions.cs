namespace BrokerHub.Infrastructure.Identity;

public sealed class IdentityBootstrapOptions
{
    public const string SectionName = "IdentityBootstrap";

    public bool Enabled { get; init; }

    public string? Email { get; init; }

    public string? Password { get; init; }
}
