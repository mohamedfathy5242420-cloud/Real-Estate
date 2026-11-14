using BrokerHub.Application.DTOs.Catalog;
using BrokerHub.Application.Interfaces.Services;
using BrokerHub.Api.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BrokerHub.Api.Controllers;

[ApiController]
[AllowAnonymous]
[CatalogReadException]
[Route("api/catalog/units")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class PublicCatalogController(IPublicCatalogService catalog) : ControllerBase
{
    private static readonly HashSet<string> QueryNames = new(StringComparer.OrdinalIgnoreCase)
        { "q", "area", "unitType", "projectId", "developerId", "bedrooms", "minPrice", "maxPrice", "currency", "minArea", "maxArea", "sort", "page", "pageSize" };
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] CatalogSearchDto search, CancellationToken ct)
    {
        if (Request.Query.Any(p => !QueryNames.Contains(p.Key) || p.Value.Count != 1) ||
            Request.Query.Keys.Distinct(StringComparer.OrdinalIgnoreCase).Count() != Request.Query.Count)
            return BadRequest(new ProblemDetails { Status = 400, Title = "Unknown or duplicate catalog query parameter." });
        return Map(await catalog.ListAsync(search, ct));
    }
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Detail(Guid id, CancellationToken ct) => Map(await catalog.DetailAsync(id, ct));
    [HttpGet("{id:guid}/images/{imageId:guid}/content")]
    public async Task<IActionResult> Image(Guid id, Guid imageId, CancellationToken ct)
    {
        var result = await catalog.ImageAsync(id, imageId, ct);
        if (result.Status != PublicCatalogStatus.Success) return Map(result);
        Response.Headers.XContentTypeOptions = "nosniff";
        return File(result.Value!.Stream, result.Value.ContentType, enableRangeProcessing: false);
    }
    private IActionResult Map<T>(PublicCatalogResult<T> result) => result.Status switch
    {
        PublicCatalogStatus.Success => Ok(result.Value),
        PublicCatalogStatus.Invalid => BadRequest(new ValidationProblemDetails(result.Errors!.ToDictionary(p => p.Key, p => p.Value))),
        PublicCatalogStatus.NotFound => NotFound(new ProblemDetails { Status = 404, Title = "Catalog resource was not found." }),
        PublicCatalogStatus.Unavailable => Problem(statusCode: 503, title: "Catalog image is unavailable."),
        _ => Problem(statusCode: 500)
    };
}
