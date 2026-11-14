using BrokerHub.Domain.Entities;
using BrokerHub.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BrokerHub.Infrastructure.Persistence;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Developer> Developers => Set<Developer>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<CustomerAssignment> CustomerAssignments => Set<CustomerAssignment>();
    public DbSet<Lead> Leads => Set<Lead>();
    public DbSet<Viewing> Viewings => Set<Viewing>();
    public DbSet<ViewingEvent> ViewingEvents => Set<ViewingEvent>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<BookingEvent> BookingEvents => Set<BookingEvent>();
    public DbSet<BookingDeveloperResponse> BookingDeveloperResponses => Set<BookingDeveloperResponse>();
    public DbSet<BookingUnitClaim> BookingUnitClaims => Set<BookingUnitClaim>();
    public DbSet<BookingExtension> BookingExtensions => Set<BookingExtension>();
    public DbSet<BookingDeposit> BookingDeposits => Set<BookingDeposit>();
    public DbSet<BookingRefund> BookingRefunds => Set<BookingRefund>();
    public DbSet<Deal> Deals => Set<Deal>();
    public DbSet<DealBookingLink> DealBookingLinks => Set<DealBookingLink>();
    public DbSet<CommissionCollection> CommissionCollections => Set<CommissionCollection>();
    public DbSet<SalesNote> SalesNotes => Set<SalesNote>();
    public DbSet<CustomerNeedsChange> CustomerNeedsChanges => Set<CustomerNeedsChange>();
    public DbSet<LeadEvent> LeadEvents => Set<LeadEvent>();
    public DbSet<CommandReceipt> CommandReceipts => Set<CommandReceipt>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<SystemOutboxMessage> SystemOutboxMessages => Set<SystemOutboxMessage>();
    public DbSet<OutboxDelivery> OutboxDeliveries => Set<OutboxDelivery>();
    public DbSet<OutboxDispatchFence> OutboxDispatchFences => Set<OutboxDispatchFence>();
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<NotificationProcessingAudit> NotificationProcessingAudits => Set<NotificationProcessingAudit>();
    public DbSet<NotificationReadState> NotificationReadStates => Set<NotificationReadState>();
    public DbSet<NotificationEmailCapture> NotificationEmailCaptures => Set<NotificationEmailCapture>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<EmployeeRevision> EmployeeRevisions => Set<EmployeeRevision>();
    public DbSet<CustomerLinkRequest> CustomerLinkRequests => Set<CustomerLinkRequest>();
    public DbSet<CustomerLinkRequestEvent> CustomerLinkRequestEvents => Set<CustomerLinkRequestEvent>();

    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Unit> Units => Set<Unit>();
    public DbSet<UnitPublicationTransition> UnitPublicationTransitions => Set<UnitPublicationTransition>();
    public DbSet<UnitImage> UnitImages => Set<UnitImage>();
    public DbSet<UnitMarketConfirmation> UnitMarketConfirmations => Set<UnitMarketConfirmation>();
    public DbSet<MarketingAgreement> MarketingAgreements => Set<MarketingAgreement>();
    public DbSet<AgreementProject> AgreementProjects => Set<AgreementProject>();
    public DbSet<CommissionRule> CommissionRules => Set<CommissionRule>();
    public DbSet<MarketingAgreementRevision> MarketingAgreementRevisions => Set<MarketingAgreementRevision>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.HasDefaultSchema("Identity");

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.ToTable("Accounts");
            entity.Property(account => account.CreatedAtUtc).HasPrecision(7);
        });

        builder.Entity<IdentityRole<Guid>>().ToTable("Roles");
        builder.Entity<IdentityUserRole<Guid>>().ToTable("AccountRoles");
        builder.Entity<IdentityUserClaim<Guid>>().ToTable("AccountClaims");
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("AccountLogins");
        builder.Entity<IdentityRoleClaim<Guid>>().ToTable("RoleClaims");
        builder.Entity<IdentityUserToken<Guid>>().ToTable("AccountTokens");

        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
