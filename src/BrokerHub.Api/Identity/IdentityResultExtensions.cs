using Microsoft.AspNetCore.Identity;

namespace BrokerHub.Api.Identity;

internal static class IdentityResultExtensions
{
    internal static Dictionary<string, string[]> ToValidationErrors(this IdentityResult result)
    {
        return result.Errors
            .GroupBy(error => error.Code, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.Select(error => error.Description).ToArray(),
                StringComparer.Ordinal);
    }
}
