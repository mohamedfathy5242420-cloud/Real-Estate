using System.ComponentModel.DataAnnotations;

namespace BrokerHub.Api.Configuration;

public sealed class BrokerHubOptions
{
    public const string SectionName = "BrokerHub";

    [Required]
    public string ApplicationName { get; init; } = string.Empty;
}
