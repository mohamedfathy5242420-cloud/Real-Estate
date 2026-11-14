using BrokerHub.Api.Authorization;
using BrokerHub.Api.Contracts.UnitImages;
using BrokerHub.Application.DTOs.UnitImages;
using BrokerHub.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace BrokerHub.Api.Controllers;

[ApiController]
[Route("api/units/{unitId:guid}/images")]
[Authorize(Policy = AuthorizationPolicies.SystemAdministrator)]
public sealed class UnitImagesController(IUnitImageService images) : ControllerBase
{
    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(UnitImageLimits.MaximumRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = UnitImageLimits.MaximumRequestBytes, MemoryBufferThreshold = 65536,
        ValueLengthLimit = 1024, KeyLengthLimit = 128, ValueCountLimit = 16)]
    public async Task<IActionResult> Upload(Guid unitId, [FromForm] UploadUnitImageRequest request, CancellationToken cancellationToken)
    {
        var form=await Request.ReadFormAsync(cancellationToken);
        var names=new[]{"AltText","SortOrder","Version"};
        if(form.Keys.Any(key=>!names.Contains(key,StringComparer.OrdinalIgnoreCase))||form.Any(p=>p.Value.Count!=1)||
            form.Files.Count!=1||!string.Equals(form.Files[0].Name,"File",StringComparison.OrdinalIgnoreCase))
            return BadRequest(new ProblemDetails{Status=400,Title="Only the declared image fields and one File are permitted."});
        await using var stream = request.File.OpenReadStream();
        var result = await images.UploadAsync(unitId, new UploadUnitImageDto(stream, request.AltText,
            request.SortOrder!.Value, request.Version!.Value), cancellationToken);
        return result.Status == UnitImageStatus.Success
            ? CreatedAtAction(nameof(Read), new { unitId, imageId = result.Value!.Image.Id }, result.Value) : MapFailure(result);
    }
    [HttpGet]
    public async Task<IActionResult> List(Guid unitId, CancellationToken cancellationToken)
    {
        var result = await images.ListAsync(unitId, cancellationToken);
        return result.Status == UnitImageStatus.Success ? Ok(result.Value) : MapFailure(result);
    }
    [HttpPut("{imageId:guid}")]
    public async Task<IActionResult> Update(Guid unitId, Guid imageId, UpdateUnitImageRequest request, CancellationToken cancellationToken)
    {
        var result = await images.UpdateAsync(unitId, imageId, new(request.AltText, request.SortOrder!.Value, request.Version!.Value), cancellationToken);
        return result.Status == UnitImageStatus.Success ? Ok(result.Value) : MapFailure(result);
    }
    [HttpGet("{imageId:guid}/content")]
    public async Task<IActionResult> Read(Guid unitId, Guid imageId, CancellationToken cancellationToken)
    {
        var result = await images.ReadAsync(unitId, imageId, cancellationToken);
        if (result.Status != UnitImageStatus.Success) return MapFailure(result);
        Response.Headers.XContentTypeOptions = "nosniff";
        Response.Headers.CacheControl = "no-store";
        return File(result.Value!.Stream, result.Value.ContentType, enableRangeProcessing: false);
    }
    private IActionResult MapFailure<T>(UnitImageResult<T> result) => result.Status switch
    {
        UnitImageStatus.ValidationFailed => BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]> { [result.Field] = [result.Message] })),
        UnitImageStatus.NotFound => NotFound(new ProblemDetails { Status = 404, Title = "Unit image resource was not found.", Detail = result.Message }),
        UnitImageStatus.Conflict => Conflict(new ProblemDetails { Status = 409, Title = "Unit image collection conflict.", Detail = result.Message }),
        UnitImageStatus.Forbidden => Forbid(),
        UnitImageStatus.Unavailable => Problem(statusCode: 503, title: "Unit image operation unavailable.", detail: result.Message),
        _ => Problem(statusCode: 500)
    };
}
