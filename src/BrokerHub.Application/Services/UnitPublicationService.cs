using BrokerHub.Application.Abstractions.Identity;
using BrokerHub.Application.Authorization;
using BrokerHub.Application.DTOs.Units;
using BrokerHub.Application.DTOs.UnitPublications;
using BrokerHub.Application.Exceptions;
using BrokerHub.Application.Interfaces.Persistence;
using BrokerHub.Application.Interfaces.Services;
using BrokerHub.Application.Mappings;
using BrokerHub.Domain.Entities;

namespace BrokerHub.Application.Services;

public sealed class UnitPublicationService(IUnitPublicationRepository publications, IUnitOfWork work,
    ICurrentUser user, TimeProvider clock) : IUnitPublicationService
{
    public async Task<UnitServiceResult<UnitPublicationMutationDto>> ChangeAsync(Guid unitId, bool publish,
        ChangeUnitPublicationDto request, CancellationToken ct)
    {
        if (!user.IsAuthenticated || !user.AccountId.HasValue || !user.IsInRole(AppRoles.SystemAdministrator))
            return UnitServiceResult<UnitPublicationMutationDto>.Forbidden();
        if (request.Version <= 0 || request.Reason?.Trim().Length > UnitPublicationTransition.ReasonMaxLength)
            return UnitServiceResult<UnitPublicationMutationDto>.Validation(new Dictionary<string,string[]>
                { ["Request"] = ["Positive Version and reason of at most 500 characters required."] });
        try
        {
            return await publications.ExecuteAsync(unitId, async (context, token) =>
            {
                var unit = context.Unit;
                if (unit is null) return UnitServiceResult<UnitPublicationMutationDto>.NotFound("Unit", "Unit was not found.");
                if (unit.Version != request.Version || unit.Version == long.MaxValue)
                    return Conflict("Unit version is stale or exhausted. Reload.");
                if (unit.PublicationStatus != (publish ? Unit.Unpublished : Unit.Published))
                    return Conflict("Invalid publication transition.");
                // Sample after acquiring locks. Never use the host's local time zone.
                var now = clock.GetUtcNow();
                Guid? agreement = null; DateOnly? date = null;
                if (publish)
                {
                    if (context.HasAdoptedSale) return Conflict("An adopted Unit cannot be published again.");
                    if (!context.DeveloperActive || !context.ProjectActive || context.TimeZoneId is null)
                        return Conflict("Active Developer and Project required for publication.");
                    date = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(now,
                        TimeZoneInfo.FindSystemTimeZoneById(context.TimeZoneId)).DateTime);
                    agreement = await publications.FindEligibleAgreementAsync(context.DeveloperId, unit.ProjectId, date.Value, token);
                    if (!agreement.HasValue) return Conflict("No currently effective Active marketing agreement covers the project.");
                }
                var transition = unit.ChangePublication(publish, request.Version, now.UtcDateTime,
                    user.AccountId.Value, request.Reason, agreement, date, publish ? context.TimeZoneId : null);
                await publications.AddAsync(transition, token);
                await work.SaveChangesAsync(token);
                return UnitServiceResult<UnitPublicationMutationDto>.Success(new(unit.ToDto(), new(
                    transition.Id, transition.UnitId, transition.FromStatus, transition.ToStatus, transition.UnitVersion,
                    transition.RecordedAtUtc, transition.RecordedByAccountId, transition.Reason,
                    transition.MarketingAgreementId, transition.EligibilityDate, transition.EligibilityTimeZoneId)));
            }, ct);
        }
        catch (OptimisticConcurrencyException) { return Conflict("Unit changed concurrently. Reload."); }
    }
    private static UnitServiceResult<UnitPublicationMutationDto> Conflict(string message) =>
        UnitServiceResult<UnitPublicationMutationDto>.Conflict("Publication", message);
}
