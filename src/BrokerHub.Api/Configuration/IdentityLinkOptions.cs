using System.ComponentModel.DataAnnotations;

namespace BrokerHub.Api.Configuration;

public sealed class IdentityLinkOptions
{
    public const string SectionName = "IdentityLinks";

    [Required]
    [Url]
    public string FrontendBaseUrl { get; init; } = string.Empty;
}
