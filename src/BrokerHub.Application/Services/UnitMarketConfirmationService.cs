using BrokerHub.Application.Abstractions.Identity;
using BrokerHub.Application.Authorization;
using BrokerHub.Application.DTOs.UnitMarketConfirmations;
using BrokerHub.Application.DTOs.Units;
using BrokerHub.Application.Exceptions;
using BrokerHub.Application.Interfaces.Persistence;
using BrokerHub.Application.Interfaces.Services;
using BrokerHub.Application.Mappings;
using BrokerHub.Domain.Entities;
namespace BrokerHub.Application.Services;
public sealed class UnitMarketConfirmationService(IUnitRepository units, IUnitMarketConfirmationRepository confirmations,
    IUnitQueryService queries, IUnitOfWork unitOfWork, ICurrentUser user, TimeProvider clock) : IUnitMarketConfirmationService
{
    private bool Authorized => user.IsAuthenticated && user.AccountId.HasValue && user.IsInRole(AppRoles.SystemAdministrator);
    public async Task<UnitServiceResult<UnitMarketConfirmationMutationDto>> RecordAsync(Guid unitId, RecordUnitMarketConfirmationDto request, CancellationToken ct)
    {
        if (!Authorized) return UnitServiceResult<UnitMarketConfirmationMutationDto>.Forbidden();
        if (request.Version <= 0 || request.ConfirmedAtUtc.Offset != TimeSpan.Zero)
            return Invalid("Version/ConfirmedAtUtc", "Positive Version and explicit UTC external confirmation required.");
        var unit = await units.GetTrackedByIdAsync(unitId, ct);
        if (unit is null) return UnitServiceResult<UnitMarketConfirmationMutationDto>.NotFound("Unit", "Unit was not found.");
        if (unit.Version != request.Version || unit.Version == long.MaxValue)
            return UnitServiceResult<UnitMarketConfirmationMutationDto>.Conflict("Version", "Unit version is stale or exhausted. Reload.");
        UnitMarketConfirmation confirmation;
        try
        {
            var now = clock.GetUtcNow().UtcDateTime;
            confirmation = UnitMarketConfirmation.Create(Guid.NewGuid(), unitId, request.ConfirmedAtUtc.UtcDateTime,
                now, user.AccountId!.Value, request.Source, request.SourceReference, request.Notes,
                request.ConfirmedPrice, request.Currency, request.AvailabilityStatus);
            unit.RecordMarketConfirmation(confirmation, request.Version);
        }
        catch (ArgumentException e) { return Invalid(e.ParamName ?? "Confirmation", e.Message); }
        await confirmations.AddAsync(confirmation, ct);
        try
        {
            // One EF relational transaction includes the history insert and shared root Version/projection/audit.
            await unitOfWork.SaveChangesAsync(ct);
            return UnitServiceResult<UnitMarketConfirmationMutationDto>.Success(new(unit.ToDto(), confirmation.ToDto()));
        }
        catch (OptimisticConcurrencyException)
        { return UnitServiceResult<UnitMarketConfirmationMutationDto>.Conflict("Version", "Unit changed concurrently. Reload."); }
    }
    public async Task<UnitServiceResult<IReadOnlyList<UnitMarketConfirmationDto>>> ListAsync(Guid unitId, int skip, int take, CancellationToken ct)
    {
        if (!Authorized) return UnitServiceResult<IReadOnlyList<UnitMarketConfirmationDto>>.Forbidden();
        if (skip < 0 || take is < 1 or > 100)
            return UnitServiceResult<IReadOnlyList<UnitMarketConfirmationDto>>.Validation(new Dictionary<string,string[]> { ["Pagination"] = ["skip >=0 and take 1..100 required."] });
        if (await queries.GetByIdAsync(unitId, ct) is null)
            return UnitServiceResult<IReadOnlyList<UnitMarketConfirmationDto>>.NotFound("Unit", "Unit was not found.");
        return UnitServiceResult<IReadOnlyList<UnitMarketConfirmationDto>>.Success(await confirmations.ListAsync(unitId, skip, take, ct));
    }
    private static UnitServiceResult<UnitMarketConfirmationMutationDto> Invalid(string field, string message) =>
        UnitServiceResult<UnitMarketConfirmationMutationDto>.Validation(new Dictionary<string,string[]> { [field] = [message] });
}
