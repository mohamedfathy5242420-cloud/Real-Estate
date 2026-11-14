using System.ComponentModel.DataAnnotations;
namespace BrokerHub.Api.Contracts.UnitImages;
public sealed class UploadUnitImageRequest
{
    [Required] public IFormFile File { get; set; } = null!;
    public string? AltText { get; set; }
    [Required] public int? SortOrder { get; set; }
    [Required] public long? Version { get; set; }
}
public sealed record UpdateUnitImageRequest(string? AltText, [Required] int? SortOrder, [Required] long? Version);
