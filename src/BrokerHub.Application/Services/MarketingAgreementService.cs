using System.Text.Json;
using BrokerHub.Application.Abstractions.Identity;
using BrokerHub.Application.Authorization;
using BrokerHub.Application.DTOs.MarketingAgreements;
using BrokerHub.Application.Exceptions;
using BrokerHub.Application.Interfaces.Persistence;
using BrokerHub.Application.Interfaces.Services;
using BrokerHub.Application.Mappings;
using BrokerHub.Domain.Entities;
using BrokerHub.Domain.Enums;
using BrokerHub.Domain.ValueObjects;

namespace BrokerHub.Application.Services;

public sealed class MarketingAgreementService(IMarketingAgreementRepository repository, IMarketingAgreementQueryService queries,
    IDeveloperRepository developers, IProjectRepository projects, IUnitOfWork unitOfWork, ICurrentUser user, TimeProvider clock)
    : IMarketingAgreementService
{
    public async Task<MarketingAgreementResult<MarketingAgreementDto>> CreateAsync(CreateMarketingAgreementDto request, CancellationToken ct)
    {
        if (!Authorized()) return MarketingAgreementResult<MarketingAgreementDto>.Forbidden();
        if (request.DeveloperId == Guid.Empty) return Failure(MarketingAgreementStatus.ValidationFailed, "DeveloperId", "Developer ID required.");
        MarketingAgreement agreement;
        try
        {
            agreement = MarketingAgreement.Create(Guid.NewGuid(), request.DeveloperId, request.AgreementNumber, ParseScope(request.ScopeType),
                request.StartsOn, request.EndsOn, request.ProtectionPeriodDays ?? 90, request.ProjectIds ?? [], ParseRules(request.CommissionRules),
                clock.GetUtcNow().UtcDateTime, user.AccountId!.Value);
        }
        catch (ArgumentException ex) { return Failure(MarketingAgreementStatus.ValidationFailed, "Agreement", ex.Message); }
        var developer = await developers.GetTrackedByIdAsync(request.DeveloperId, ct);
        if (developer is null) return Failure(MarketingAgreementStatus.NotFound, "DeveloperId", "Developer not found.");
        if (!developer.IsActive) return Failure(MarketingAgreementStatus.Conflict, "DeveloperId", "Developer must be active for new agreement work.");
        var invalidProject = await ValidateProjectsAsync(agreement, ct);
        if (invalidProject is not null) return invalidProject;
        if (await repository.NumberExistsAsync(agreement.DeveloperId, agreement.AgreementNumber, null, ct))
            return Failure(MarketingAgreementStatus.Conflict, "AgreementNumber", "Number already exists within Developer.");
        await repository.AddAsync(agreement, ct);
        return await SaveAsync(agreement, "Created", protectDeveloper: true, ct);
    }
    public async Task<MarketingAgreementResult<MarketingAgreementDto>> UpdateAsync(Guid id, UpdateMarketingAgreementDto request, CancellationToken ct)
    {
        if (!Authorized()) return MarketingAgreementResult<MarketingAgreementDto>.Forbidden();
        if (request.Version <= 0) return Failure(MarketingAgreementStatus.ValidationFailed, "Version", "Positive Version required.");
        var agreement = await repository.GetTrackedByIdAsync(id, ct);
        if (agreement is null) return Failure(MarketingAgreementStatus.NotFound, "Agreement", "Agreement not found.");
        if (agreement.Version != request.Version) return Failure(MarketingAgreementStatus.Conflict, "Version", "Stale Version; reload and retry.");
        if (agreement.Status != "Draft") return Failure(MarketingAgreementStatus.Conflict, "Status", "Only drafts can be edited.");
        try
        {
            agreement.UpdateDraft(request.AgreementNumber, ParseScope(request.ScopeType), request.StartsOn, request.EndsOn,
                request.ProtectionPeriodDays ?? 90, request.ProjectIds ?? [], ParseRules(request.CommissionRules), request.Version,
                clock.GetUtcNow().UtcDateTime, user.AccountId!.Value);
        }
        catch (ArgumentException ex) { return Failure(MarketingAgreementStatus.ValidationFailed, "Agreement", ex.Message); }
        var invalidProject = await ValidateProjectsAsync(agreement, ct);
        if (invalidProject is not null) return invalidProject;
        if (await repository.NumberExistsAsync(agreement.DeveloperId, agreement.AgreementNumber, agreement.Id, ct))
            return Failure(MarketingAgreementStatus.Conflict, "AgreementNumber", "Number already exists within Developer.");
        return await SaveAsync(agreement, "DraftUpdated", protectDeveloper: false, ct);
    }
    public async Task<MarketingAgreementResult<MarketingAgreementDto>> ActivateAsync(Guid id, long version, CancellationToken ct)
    {
        if (!Authorized()) return MarketingAgreementResult<MarketingAgreementDto>.Forbidden();
        if (version <= 0) return Failure(MarketingAgreementStatus.ValidationFailed, "Version", "Positive Version required.");
        // Ownership is immutable. This lookup routes the Developer lock, not an activation precheck.
        var route = await queries.GetByIdAsync(id, ct);
        if (route is null) return Failure(MarketingAgreementStatus.NotFound, "Agreement", "Agreement not found.");
        try
        {
            return await repository.ExecuteActivationAsync(route.DeveloperId, async token =>
            {
                var agreement = await repository.GetTrackedByIdAsync(id, token);
                if (agreement is null) return Failure(MarketingAgreementStatus.NotFound, "Agreement", "Agreement not found.");
                if (agreement.Version != version) return Failure(MarketingAgreementStatus.Conflict, "Version", "Stale Version; reload.");
                if (agreement.Status != "Draft") return Failure(MarketingAgreementStatus.Conflict, "Status", "Only drafts can be activated.");
                var invalidProject = await ValidateProjectsAsync(agreement, token);
                if (invalidProject is not null) return invalidProject;
                if (await repository.ActiveOverlapExistsAsync(agreement, token))
                    return Failure(MarketingAgreementStatus.Conflict, "Scope", "An Active agreement covers the same scope during an intersecting period.");
                try { agreement.Activate(version, clock.GetUtcNow().UtcDateTime, user.AccountId!.Value); }
                catch (ArgumentException ex) { return Failure(MarketingAgreementStatus.ValidationFailed, "Agreement", ex.Message); }
                var dto = agreement.ToDto();
                await repository.AddRevisionAsync(new MarketingAgreementRevision(agreement.Id, agreement.Version, "Activated",
                    JsonSerializer.Serialize(dto), dto.UpdatedAtUtc!.Value, user.AccountId!.Value), token);
                await unitOfWork.SaveChangesAsync(token);
                return MarketingAgreementResult<MarketingAgreementDto>.Success(dto);
            }, ct);
        }
        catch (DeveloperNotActiveException) { return Failure(MarketingAgreementStatus.Conflict, "DeveloperId", "Developer must still be active at activation commit."); }
        catch (OptimisticConcurrencyException) { return Failure(MarketingAgreementStatus.Conflict, "Version", "Agreement changed concurrently; reload."); }
        catch (UniqueConstraintViolationException) { return Failure(MarketingAgreementStatus.Conflict, "Version", "Concurrent agreement revision conflict; reload."); }
    }
    public async Task<MarketingAgreementResult<MarketingAgreementDto>> GetByIdAsync(Guid id, CancellationToken ct)
    {
        if (!Authorized()) return MarketingAgreementResult<MarketingAgreementDto>.Forbidden();
        var value = await queries.GetByIdAsync(id, ct);
        return value is null ? Failure(MarketingAgreementStatus.NotFound, "Agreement", "Agreement not found.") : MarketingAgreementResult<MarketingAgreementDto>.Success(value);
    }
    public async Task<MarketingAgreementResult<IReadOnlyList<MarketingAgreementDto>>> ListAsync(Guid? developerId, int skip, int take, CancellationToken ct)
    {
        if (!Authorized()) return MarketingAgreementResult<IReadOnlyList<MarketingAgreementDto>>.Forbidden();
        if (developerId == Guid.Empty || skip < 0 || take is < 1 or > 100)
            return MarketingAgreementResult<IReadOnlyList<MarketingAgreementDto>>.Failure(MarketingAgreementStatus.ValidationFailed, "Query", "Nonempty DeveloperId, skip >=0 and take 1..100 required.");
        return MarketingAgreementResult<IReadOnlyList<MarketingAgreementDto>>.Success(await queries.ListAsync(developerId, skip, take, ct));
    }
    private bool Authorized() => user.IsAuthenticated && user.AccountId.HasValue && user.IsInRole(AppRoles.SystemAdministrator);
    private static MarketingAgreementResult<MarketingAgreementDto> Failure(MarketingAgreementStatus status, string field, string message) =>
        MarketingAgreementResult<MarketingAgreementDto>.Failure(status, field, message);
    private async Task<MarketingAgreementResult<MarketingAgreementDto>?> ValidateProjectsAsync(MarketingAgreement agreement, CancellationToken ct)
    {
        var ids = agreement.Projects.Where(p => p.IsIncluded).Select(p => p.ProjectId)
            .Concat(agreement.Rules.Where(r => !r.IsArchived && r.ProjectId.HasValue).Select(r => r.ProjectId!.Value)).Distinct();
        foreach (var id in ids)
        {
            var project = await projects.GetTrackedByIdAsync(id, ct);
            if (project is null) return Failure(MarketingAgreementStatus.NotFound, "ProjectIds", "Project not found.");
            if (project.DeveloperId != agreement.DeveloperId) return Failure(MarketingAgreementStatus.ValidationFailed, "ProjectIds", "Project belongs to a different Developer.");
        }
        return null;
    }
    private async Task<MarketingAgreementResult<MarketingAgreementDto>> SaveAsync(MarketingAgreement agreement, string action, bool protectDeveloper, CancellationToken ct)
    {
        var dto = agreement.ToDto();
        await repository.AddRevisionAsync(new MarketingAgreementRevision(agreement.Id, agreement.Version, action,
            JsonSerializer.Serialize(dto), clock.GetUtcNow().UtcDateTime, user.AccountId!.Value), ct);
        try
        {
            if (protectDeveloper) await unitOfWork.SaveChangesWithActiveDeveloperAsync(agreement.DeveloperId, ct);
            else await unitOfWork.SaveChangesAsync(ct);
        }
        catch (DeveloperNotActiveException) { return Failure(MarketingAgreementStatus.Conflict, "DeveloperId", "Developer no longer active; reload."); }
        catch (OptimisticConcurrencyException) { return Failure(MarketingAgreementStatus.Conflict, "Version", "Agreement changed concurrently; reload."); }
        catch (UniqueConstraintViolationException) { return Failure(MarketingAgreementStatus.Conflict, "AgreementNumber", "Concurrent unique constraint conflict; reload."); }
        return MarketingAgreementResult<MarketingAgreementDto>.Success(dto);
    }
    private static AgreementScopeType ParseScope(string value) => Enum.TryParse<AgreementScopeType>(value, out var type) && type.ToString() == value
        ? type : throw new ArgumentException("ScopeType must be AllDeveloperProjects or SelectedProjects.");
    private static IReadOnlyList<CommissionRuleDefinition> ParseRules(IReadOnlyList<CommissionRuleInput>? rules)
    {
        if (rules is null || rules.Count > MarketingAgreement.MaxRules) throw new ArgumentException("CommissionRules required; max 101.");
        return rules.Select(r => r is not null && Enum.TryParse<CommissionRuleType>(r.RuleType, out var type) && type.ToString() == r.RuleType
            ? new CommissionRuleDefinition(r.ProjectId, type, r.Percentage, r.FixedAmount, r.Currency)
            : throw new ArgumentException("RuleType must be Percentage or FixedAmount.")).ToArray();
    }
}
