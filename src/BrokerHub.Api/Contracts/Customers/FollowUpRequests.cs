using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace BrokerHub.Api.Contracts.Customers;
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class IntakeCustomerRequest
{
    [Required, StringLength(200)] public string FullName { get; init; } = "";
    [Required, StringLength(32)] public string Phone { get; init; } = "";
    [StringLength(320)] public string? Email { get; init; }
    [StringLength(100)] public string? PreferredContactMethod { get; init; }
    [StringLength(2000)] public string? Requirements { get; init; }
}
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class ChangeCustomerNeedsRequest
{
    [Required, Range(1, long.MaxValue)] public long? Version { get; init; }
    [JsonRequired, StringLength(2000)] public string? Requirements { get; init; }
}
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class AddSalesNoteRequest
{
    [Required, StringLength(2000)] public string Body { get; init; } = "";
    public Guid? LeadId { get; init; }
}
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class CorrectSalesNoteRequest
{
    [Required, StringLength(2000)] public string Body { get; init; } = "";
}
