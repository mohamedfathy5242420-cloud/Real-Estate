using BrokerHub.Domain.Entities;
using BrokerHub.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BrokerHub.Infrastructure.Persistence.Configurations;
internal sealed class ViewingConfiguration : IEntityTypeConfiguration<Viewing>
{
    public void Configure(EntityTypeBuilder<Viewing> b)
    {
        const string noConfirmation = "[ConfirmedStartUtc] IS NULL AND [ConfirmedEndUtc] IS NULL AND [ConfirmedEmployeeId] IS NULL AND [ConfirmedAtUtc] IS NULL AND [ConfirmedByAccountId] IS NULL";
        const string hasConfirmation = "[ConfirmedStartUtc] IS NOT NULL AND [ConfirmedEndUtc] IS NOT NULL AND [ConfirmedEmployeeId] IS NOT NULL AND [ConfirmedAtUtc] IS NOT NULL AND [ConfirmedByAccountId] IS NOT NULL";
        const string noCoordination = "[CoordinatedAtUtc] IS NULL AND [CoordinationSource] IS NULL AND [CoordinationReference] IS NULL";
        const string hasCoordination = "[CoordinatedAtUtc] IS NOT NULL AND [CoordinationSource] IS NOT NULL";
        const string noOffer = "[OfferedAtUtc] IS NULL AND [OfferedByAccountId] IS NULL AND [OfferedEmployeeId] IS NULL";
        const string hasOffer = "[OfferedAtUtc] IS NOT NULL AND [OfferedByAccountId] IS NOT NULL AND [OfferedEmployeeId] IS NOT NULL";
        const string noCancellation = "[CancelledAtUtc] IS NULL AND [CancelledByAccountId] IS NULL AND [CancellationKind] IS NULL AND [CancellationReason] IS NULL AND [CancelledFromStatus] IS NULL";
        const string hasCancellation = "[CancelledAtUtc] IS NOT NULL AND [CancelledByAccountId] IS NOT NULL AND [CancellationKind] IS NOT NULL AND [CancellationReason] IS NOT NULL AND [CancelledFromStatus] IS NOT NULL";
        const string noExpiry = "[ExpiredAtUtc] IS NULL AND [ExpiredFromStatus] IS NULL";
        const string hasExpiry = "[ExpiredAtUtc] IS NOT NULL AND [ExpiredFromStatus] IS NOT NULL";
        const string noOutcome = "[OutcomeOccurredAtUtc] IS NULL AND [OutcomeRecordedAtUtc] IS NULL AND [OutcomeRecordedByAccountId] IS NULL AND [OutcomeNoteId] IS NULL";
        const string hasOutcome = "[OutcomeOccurredAtUtc] IS NOT NULL AND [OutcomeRecordedAtUtc] IS NOT NULL AND [OutcomeRecordedByAccountId] IS NOT NULL AND [OutcomeNoteId] IS NOT NULL";
        var pendingShape = $"[Version]>=1 AND {noConfirmation} AND {noCoordination} AND {noOffer}";
        var awaitingShape = $"[Version]>1 AND {noConfirmation} AND {hasCoordination} AND {hasOffer}";
        var confirmedShape = $"[Version]>1 AND {hasConfirmation} AND {hasCoordination} AND (({noOffer}) OR ({hasOffer} AND [OfferedEmployeeId]=[ConfirmedEmployeeId]))";
        b.ToTable("Viewings", "Crm", t =>
        {
            t.HasCheckConstraint("CK_Viewings_InitialState",
                $"([Status]=N'PendingConfirmation' AND {pendingShape} AND {noCancellation} AND {noExpiry} AND {noOutcome}) OR " +
                $"([Status]=N'AwaitingCustomerAcceptance' AND {awaitingShape} AND {noCancellation} AND {noExpiry} AND {noOutcome}) OR " +
                $"([Status]=N'Confirmed' AND {confirmedShape} AND {noCancellation} AND {noExpiry} AND {noOutcome}) OR " +
                $"([Status]=N'Cancelled' AND [Version]>1 AND {hasCancellation} AND {noExpiry} AND {noOutcome} AND (([CancelledFromStatus]=N'PendingConfirmation' AND {pendingShape}) OR ([CancelledFromStatus]=N'AwaitingCustomerAcceptance' AND {awaitingShape}) OR ([CancelledFromStatus]=N'Confirmed' AND {confirmedShape}))) OR " +
                $"([Status]=N'Expired' AND [Version]>1 AND {hasExpiry} AND {noCancellation} AND {noOutcome} AND (([ExpiredFromStatus]=N'PendingConfirmation' AND {pendingShape}) OR ([ExpiredFromStatus]=N'AwaitingCustomerAcceptance' AND {awaitingShape}))) OR " +
                $"([Status] IN(N'Completed',N'CustomerNoShow') AND [Version]>2 AND {confirmedShape} AND {noCancellation} AND {noExpiry} AND {hasOutcome})");
            t.HasCheckConstraint("CK_Viewings_Interval", "[RequestedAtUtc]>'00010101' AND [CurrentProposedStartUtc]>[PendingCycleStartedAtUtc] AND [CurrentProposedEndUtc]>[CurrentProposedStartUtc] AND (([OfferedAtUtc] IS NULL AND [CurrentProposedStartUtc]<=DATEADD(day,30,[PendingCycleStartedAtUtc]) AND [CurrentProposedEndUtc]=DATEADD(minute,60,[CurrentProposedStartUtc])) OR ([OfferedAtUtc] IS NOT NULL AND [CurrentProposedStartUtc]<=DATEADD(day,30,[OfferedAtUtc])))");
            t.HasCheckConstraint("CK_Viewings_Cycle", "[PendingCycleStartedAtUtc]>=[RequestedAtUtc]");
            t.HasCheckConstraint("CK_Viewings_LocalContext", "[SubmissionLocalDate]>'00010101' AND LEN(LTRIM(RTRIM([TimeZoneId])))>0");
            t.HasCheckConstraint("CK_Viewings_ConfirmationInterval", "[ConfirmedAtUtc] IS NULL OR ([ConfirmedStartUtc]=[CurrentProposedStartUtc] AND [ConfirmedEndUtc]>[ConfirmedStartUtc] AND [ConfirmedAtUtc]>=[PendingCycleStartedAtUtc] AND [ConfirmedAtUtc]<[ConfirmedStartUtc] AND (([OfferedAtUtc] IS NULL AND [ConfirmedAtUtc]<DATEADD(day,7,[PendingCycleStartedAtUtc])) OR ([OfferedAtUtc] IS NOT NULL AND [ConfirmedAtUtc]>=[OfferedAtUtc] AND [ConfirmedEndUtc]=[CurrentProposedEndUtc])))");
            t.HasCheckConstraint("CK_Viewings_Coordination", "[CoordinatedAtUtc] IS NULL OR ([CoordinatedAtUtc]>=[PendingCycleStartedAtUtc] AND [CoordinatedAtUtc]<=COALESCE([ConfirmedAtUtc],[OfferedAtUtc]) AND LEN(LTRIM(RTRIM([CoordinationSource])))>0 AND ([CoordinationReference] IS NULL OR LEN(LTRIM(RTRIM([CoordinationReference])))>0))");
            t.HasCheckConstraint("CK_Viewings_OfferAudit", "[OfferedAtUtc] IS NULL OR ([OfferedAtUtc]>=[RequestedAtUtc] AND [OfferedAtUtc]<[CurrentProposedStartUtc] AND [CoordinatedAtUtc]<=[OfferedAtUtc])");
            t.HasCheckConstraint("CK_Viewings_CancellationAudit", "[CancelledAtUtc] IS NULL OR ([CancelledAtUtc]>=[PendingCycleStartedAtUtc] AND [CancelledAtUtc]<[CurrentProposedStartUtc] AND LEN(LTRIM(RTRIM([CancellationReason])))>0 AND [CancellationKind] IN(N'Customer',N'Broker',N'Developer') AND ([CancelledFromStatus]<>N'PendingConfirmation' OR [CancelledAtUtc]<DATEADD(day,7,[PendingCycleStartedAtUtc])))");
            t.HasCheckConstraint("CK_Viewings_ExpiryAudit", "[ExpiredAtUtc] IS NULL OR ([ExpiredAtUtc]>=[PendingCycleStartedAtUtc] AND [ExpiredAtUtc]>=CASE WHEN [ExpiredFromStatus]=N'AwaitingCustomerAcceptance' OR [CurrentProposedStartUtc]<DATEADD(day,7,[PendingCycleStartedAtUtc]) THEN [CurrentProposedStartUtc] ELSE DATEADD(day,7,[PendingCycleStartedAtUtc]) END)");
            t.HasCheckConstraint("CK_Viewings_OutcomeAudit", "[OutcomeOccurredAtUtc] IS NULL OR ([ConfirmedStartUtc] IS NOT NULL AND [OutcomeRecordedAtUtc] IS NOT NULL AND [OutcomeOccurredAtUtc]>=[ConfirmedStartUtc] AND [OutcomeOccurredAtUtc]<=[OutcomeRecordedAtUtc])");
        });
        b.HasKey(v => v.Id); b.Property(v => v.Id).ValueGeneratedNever(); b.HasAlternateKey(v => new { v.Id, v.CustomerId });
        b.HasOne<Customer>().WithMany().HasForeignKey(v => v.CustomerId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Lead>().WithMany().HasForeignKey(v => new { v.LeadId, v.CustomerId, v.ProjectId })
            .HasPrincipalKey(l => new { l.Id, l.CustomerId, l.ProjectId }).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Unit>().WithMany().HasForeignKey(v => new { v.UnitId, v.ProjectId })
            .HasPrincipalKey(u => new { u.Id, u.ProjectId }).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ApplicationUser>().WithMany().HasForeignKey(v => v.RequestedByAccountId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ApplicationUser>().WithMany().HasForeignKey(v => v.ConfirmedByAccountId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ApplicationUser>().WithMany().HasForeignKey(v => v.OfferedByAccountId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ApplicationUser>().WithMany().HasForeignKey(v => v.CancelledByAccountId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ApplicationUser>().WithMany().HasForeignKey(v => v.OutcomeRecordedByAccountId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<SalesNote>().WithMany().HasForeignKey(v => new { v.OutcomeNoteId, v.CustomerId })
            .HasPrincipalKey(n => new { n.Id, n.CustomerId }).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Employee>().WithMany().HasForeignKey(v => v.ConfirmedEmployeeId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Employee>().WithMany().HasForeignKey(v => v.OfferedEmployeeId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<MarketingAgreement>().WithMany().HasForeignKey(v => v.SubmissionAgreementId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(v => new { v.CustomerId, v.RequestedAtUtc, v.Id });
        b.HasIndex(v => new { v.Status, v.CurrentProposedStartUtc, v.PendingCycleStartedAtUtc, v.Id })
            .HasFilter("[Status] IN (N'PendingConfirmation',N'AwaitingCustomerAcceptance')")
            .IncludeProperties(v => new { v.CustomerId, v.Version }).HasDatabaseName("IX_Viewings_PendingExpiry");
        b.HasIndex(v => new { v.UnitId, v.ConfirmedStartUtc, v.ConfirmedEndUtc }).HasFilter("[Status] = N'Confirmed'").HasDatabaseName("IX_Viewings_ConfirmedUnitInterval");
        b.HasIndex(v => new { v.ConfirmedEmployeeId, v.ConfirmedStartUtc, v.ConfirmedEndUtc }).HasFilter("[Status] = N'Confirmed'").HasDatabaseName("IX_Viewings_ConfirmedEmployeeInterval");
        b.Property(v => v.Status).HasMaxLength(32).IsRequired(); b.Property(v => v.TimeZoneId).HasMaxLength(100).IsRequired();
        b.Property(v => v.Version).IsConcurrencyToken(); b.Ignore(v => v.PendingExpiresAtUtc);
        b.Property(v => v.CurrentProposedStartUtc).HasPrecision(7); b.Property(v => v.CurrentProposedEndUtc).HasPrecision(7);
        b.Property(v => v.RequestedAtUtc).HasPrecision(7); b.Property(v => v.PendingCycleStartedAtUtc).HasPrecision(7);
        b.Property(v => v.ConfirmedStartUtc).HasPrecision(7); b.Property(v => v.ConfirmedEndUtc).HasPrecision(7);
        b.Property(v => v.ConfirmedAtUtc).HasPrecision(7); b.Property(v => v.CoordinatedAtUtc).HasPrecision(7); b.Property(v => v.OfferedAtUtc).HasPrecision(7);
        b.Property(v => v.CoordinationSource).HasMaxLength(Viewing.CoordinationSourceMaxLength);
        b.Property(v => v.CoordinationReference).HasMaxLength(Viewing.CoordinationReferenceMaxLength);
        b.Property(v => v.CancelledAtUtc).HasPrecision(7);
        b.Property(v => v.CancellationKind).HasMaxLength(16); b.Property(v => v.CancelledFromStatus).HasMaxLength(32);
        b.Property(v => v.CancellationReason).HasMaxLength(Viewing.ChangeReasonMaxLength);
        b.Property(v => v.ExpiredAtUtc).HasPrecision(7); b.Property(v => v.ExpiredFromStatus).HasMaxLength(32);
        b.Property(v => v.OutcomeOccurredAtUtc).HasPrecision(7); b.Property(v => v.OutcomeRecordedAtUtc).HasPrecision(7);
    }
}
