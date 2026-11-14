using BrokerHub.Domain.Enums;
using BrokerHub.Domain.ValueObjects;

namespace BrokerHub.Domain.Entities;

public sealed class MarketingAgreement
{
    public const int NumberMaxLength = 64;
    public const int MaxProjects = 100;
    public const int MaxRules = 101;
    private readonly List<AgreementProject> _projects = [];
    private readonly List<CommissionRule> _rules = [];
    private MarketingAgreement() { }
    public Guid Id { get; private set; }
    public Guid DeveloperId { get; private set; }
    public string AgreementNumber { get; private set; } = string.Empty;
    public AgreementScopeType ScopeType { get; private set; }
    public DateOnly StartsOn { get; private set; }
    public DateOnly EndsOn { get; private set; }
    public int ProtectionPeriodDays { get; private set; }
    public string Status { get; private set; } = "Draft";
    public DateTime CreatedAtUtc { get; private set; }
    public Guid CreatedByAccountId { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }
    public Guid? UpdatedByAccountId { get; private set; }
    public long Version { get; private set; }
    public IReadOnlyCollection<AgreementProject> Projects => _projects.AsReadOnly();
    public IReadOnlyCollection<CommissionRule> Rules => _rules.AsReadOnly();

    public static MarketingAgreement Create(Guid id, Guid developerId, string number, AgreementScopeType scope,
        DateOnly starts, DateOnly ends, int protection, IReadOnlyList<Guid> projects,
        IReadOnlyList<CommissionRuleDefinition> rules, DateTime at, Guid actor)
    {
        if (id == Guid.Empty || developerId == Guid.Empty) throw new ArgumentException("Agreement and Developer IDs required.");
        Validate(number, scope, starts, ends, protection, projects, rules, at, actor);
        var agreement = new MarketingAgreement { Id = id, DeveloperId = developerId, Version = 1,
            CreatedAtUtc = at, CreatedByAccountId = actor };
        agreement.Apply(number, scope, starts, ends, protection, projects, rules, at, actor);
        return agreement;
    }
    public void UpdateDraft(string number, AgreementScopeType scope, DateOnly starts, DateOnly ends, int protection,
        IReadOnlyList<Guid> projects, IReadOnlyList<CommissionRuleDefinition> rules, long expectedVersion, DateTime at, Guid actor)
    {
        if (expectedVersion <= 0 || expectedVersion != Version) throw new InvalidOperationException("Agreement version is stale.");
        if (Status != "Draft") throw new InvalidOperationException("Only drafts can be edited.");
        Validate(number, scope, starts, ends, protection, projects, rules, at, actor);
        Apply(number, scope, starts, ends, protection, projects, rules, at, actor);
        UpdatedAtUtc = at; UpdatedByAccountId = actor; Version++;
    }
    public void Activate(long expectedVersion, DateTime at, Guid actor)
    {
        if (expectedVersion <= 0 || expectedVersion != Version) throw new InvalidOperationException("Agreement version is stale.");
        if (Status != "Draft") throw new InvalidOperationException("Only drafts can be activated.");
        Validate(AgreementNumber, ScopeType, StartsOn, EndsOn, ProtectionPeriodDays,
            Projects.Where(p => p.IsIncluded).Select(p => p.ProjectId).ToArray(),
            Rules.Where(r => !r.IsArchived).Select(r => new CommissionRuleDefinition(r.ProjectId, r.RuleType,
                r.Percentage, r.FixedAmount, r.Currency)).ToArray(), at, actor);
        Status = "Active"; UpdatedAtUtc = at; UpdatedByAccountId = actor; Version++;
    }
    public static void Validate(string number, AgreementScopeType scope, DateOnly starts, DateOnly ends, int protection,
        IReadOnlyList<Guid> projects, IReadOnlyList<CommissionRuleDefinition> rules, DateTime at, Guid actor)
    {
        if (string.IsNullOrWhiteSpace(number) || number.Trim().Length > NumberMaxLength) throw new ArgumentException("AgreementNumber required, max 64 characters.");
        if (starts == default || ends == default || ends < starts) throw new ArgumentException("Dates required; EndsOn must be >= StartsOn.");
        if (protection < 0) throw new ArgumentException("ProtectionPeriodDays cannot be negative.");
        if (at.Kind != DateTimeKind.Utc || actor == Guid.Empty) throw new ArgumentException("UTC time and account actor required.");
        if (projects.Count > MaxProjects || rules.Count > MaxRules) throw new ArgumentException("Technical collection limits exceeded.");
        if (projects.Any(p => p == Guid.Empty) || projects.Distinct().Count() != projects.Count) throw new ArgumentException("Project IDs required and distinct.");
        if (scope == AgreementScopeType.AllDeveloperProjects && projects.Count != 0) throw new ArgumentException("All scope must not contain selected rows.");
        if (scope == AgreementScopeType.SelectedProjects && projects.Count == 0) throw new ArgumentException("Selected scope requires projects.");
        if (!Enum.IsDefined(scope)) throw new ArgumentException("Unknown scope.");
        if (rules.Count(r => r.ProjectId is null) != 1) throw new ArgumentException("Exactly one default rule required.");
        if (rules.Select(r => r.ProjectId).Distinct().Count() != rules.Count) throw new ArgumentException("Duplicate override/default rule.");
        foreach (var rule in rules)
        {
            rule.Validate();
            if (scope == AgreementScopeType.SelectedProjects && rule.ProjectId.HasValue && !projects.Contains(rule.ProjectId.Value))
                throw new ArgumentException("Override must be within selected scope.");
        }
    }
    private void Apply(string number, AgreementScopeType scope, DateOnly starts, DateOnly ends, int protection,
        IReadOnlyList<Guid> projects, IReadOnlyList<CommissionRuleDefinition> rules, DateTime at, Guid actor)
    {
        AgreementNumber = number.Trim(); ScopeType = scope; StartsOn = starts; EndsOn = ends; ProtectionPeriodDays = protection;
        foreach (var row in _projects) row.SetIncluded(projects.Contains(row.ProjectId), at, actor);
        foreach (var id in projects)
            if (_projects.All(p => p.ProjectId != id)) _projects.Add(new AgreementProject(Id, DeveloperId, id, at, actor));
        foreach (var row in _rules)
            if (!rules.Any(r => r.ProjectId == row.ProjectId)) row.Archive(at, actor);
        foreach (var rule in rules)
        {
            var existing = _rules.SingleOrDefault(r => r.ProjectId == rule.ProjectId);
            if (existing is null) _rules.Add(new CommissionRule(Id, DeveloperId, rule, at, actor));
            else existing.Update(rule, at, actor);
        }
    }
}
