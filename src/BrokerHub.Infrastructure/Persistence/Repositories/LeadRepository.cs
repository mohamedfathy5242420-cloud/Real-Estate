using BrokerHub.Application.DTOs.Leads;
using BrokerHub.Application.Interfaces.Persistence;
using BrokerHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BrokerHub.Infrastructure.Persistence.Repositories;
internal sealed class LeadRepository(ApplicationDbContext db) : ILeadRepository
{
    public Task<Lead?> LockOpenAsync(Guid customerId, Guid projectId, CancellationToken ct)
    {
        RequireTransaction();
        return db.Leads.FromSqlInterpolated($"""
            SELECT * FROM [Crm].[Leads] WITH (UPDLOCK,HOLDLOCK,INDEX([UX_Leads_OpenCustomerProject]))
            WHERE [CustomerId]={customerId} AND [ProjectId]={projectId} AND [Status]=N'Open'
            """).SingleOrDefaultAsync(ct);
    }
    public Task<Lead?> LockByIdAsync(Guid customerId, Guid id, CancellationToken ct)
    {
        RequireTransaction();
        return db.Leads.FromSqlInterpolated($"""
            SELECT * FROM [Crm].[Leads] WITH (UPDLOCK,HOLDLOCK) WHERE [CustomerId]={customerId} AND [Id]={id}
            """).SingleOrDefaultAsync(ct);
    }
    public async Task<IReadOnlyList<Lead>> SaleCandidatesAsync(Guid customerId,Guid projectId,CancellationToken ct)
    {
        RequireTransaction();
        // Caller holds current Customer before this read. All supported Lead writers share that
        // lock; no foreign Lead UPDLOCK or unbounded/truncated eligibility decision is needed.
        return await db.Leads.AsNoTracking().Where(l=>l.CustomerId==customerId&&l.ProjectId==projectId)
            .OrderBy(l=>l.Id).Take(1001).ToArrayAsync(ct);
    }
    public async Task<LeadProjectOrigin?> LockProjectOriginAsync(Guid projectId, CancellationToken ct)
    {
        RequireTransaction();
        // Project.DeveloperId is immutable; protect Developer before Project, like P03 activation.
        var developer = await db.Projects.AsNoTracking().Where(p => p.Id == projectId).Select(p => (Guid?)p.DeveloperId).SingleOrDefaultAsync(ct);
        if (developer is null) return null;
        var active = await db.Database.SqlQuery<int>($"""
            SELECT CONVERT(int,[IsActive]) AS [Value] FROM [Catalog].[Developers] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={developer.Value}
            """).SingleOrDefaultAsync(ct);
        var project = await db.Projects.FromSqlInterpolated($"""
            SELECT * FROM [Catalog].[Projects] WITH (HOLDLOCK) WHERE [Id]={projectId} AND [DeveloperId]={developer.Value}
            """).AsNoTracking().SingleOrDefaultAsync(ct);
        return project is null ? null : new(developer.Value, active == 1, project.IsActive, project.TimeZoneId);
    }
    public async Task<IReadOnlyList<LeadAgreementOrigin>> LockAgreementsAsync(Guid developerId, Guid projectId, DateOnly localDate, CancellationToken ct)
    {
        RequireTransaction();
        return await db.Database.SqlQuery<LeadAgreementOrigin>($"""
            SELECT TOP(2) a.[Id],a.[Version],a.[StartsOn],a.[EndsOn],a.[ProtectionPeriodDays]
            FROM [Catalog].[MarketingAgreements] AS a WITH (HOLDLOCK)
            WHERE a.[DeveloperId]={developerId} AND a.[Status]=N'Active' AND a.[StartsOn]<={localDate} AND a.[EndsOn]>={localDate}
              AND (a.[ScopeType]=N'AllDeveloperProjects' OR (a.[ScopeType]=N'SelectedProjects' AND EXISTS(
                SELECT 1 FROM [Catalog].[AgreementProjects] AS ap WITH (HOLDLOCK)
                WHERE ap.[MarketingAgreementId]=a.[Id] AND ap.[DeveloperId]=a.[DeveloperId] AND ap.[ProjectId]={projectId} AND ap.[IsIncluded]=1)))
            ORDER BY a.[Id]
            """).ToArrayAsync(ct);
    }
    public void Add(Lead lead) { RequireTransaction(); db.Leads.Add(lead); }
    public Task<MarketingAgreement?> LockOriginalAgreementAsync(Guid id,Guid developerId,CancellationToken ct)
    {
        RequireTransaction();
        return db.MarketingAgreements.FromSqlInterpolated($"SELECT * FROM [Catalog].[MarketingAgreements] WITH (HOLDLOCK) WHERE [Id]={id} AND [DeveloperId]={developerId}")
            .Include(a=>a.Rules).Include(a=>a.Projects).SingleOrDefaultAsync(ct);
    }
    public void AddEvent(LeadEvent history) { RequireTransaction(); db.LeadEvents.Add(history); }
    private void RequireTransaction()
    { if (db.Database.CurrentTransaction is null) throw new InvalidOperationException("Lead staging requires the caller's live command transaction."); }
}
