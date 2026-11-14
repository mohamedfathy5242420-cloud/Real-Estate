using BrokerHub.Domain.Entities;
using BrokerHub.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BrokerHub.Infrastructure.Persistence.Configurations;
internal sealed class ViewingEventConfiguration : IEntityTypeConfiguration<ViewingEvent>
{
    public void Configure(EntityTypeBuilder<ViewingEvent> b)
    {
        const string noConfirmation = "[ConfirmedStartUtc] IS NULL AND [ConfirmedEndUtc] IS NULL AND [ConfirmedEmployeeId] IS NULL";
        const string hasConfirmation = "[ConfirmedStartUtc] IS NOT NULL AND [ConfirmedEndUtc] IS NOT NULL AND [ConfirmedEmployeeId] IS NOT NULL AND [ConfirmedStartUtc]=[ProposedStartUtc] AND [ConfirmedEndUtc]>[ConfirmedStartUtc]";
        const string noCoordination = "[CoordinatedAtUtc] IS NULL AND [CoordinationSource] IS NULL AND [CoordinationReference] IS NULL";
        const string hasCoordination = "[CoordinatedAtUtc] IS NOT NULL AND [CoordinationSource] IS NOT NULL AND [CoordinatedAtUtc]>'00010101' AND [CoordinatedAtUtc]<=[AtUtc] AND LEN(LTRIM(RTRIM([CoordinationSource])))>0 AND ([CoordinationReference] IS NULL OR LEN(LTRIM(RTRIM([CoordinationReference])))>0)";
        const string noPrevious = "[PreviousProposedStartUtc] IS NULL AND [PreviousProposedEndUtc] IS NULL AND [Reason] IS NULL";
        const string noLifecycle = "[PreviousStatus] IS NULL AND [PreviousConfirmedStartUtc] IS NULL AND [PreviousConfirmedEndUtc] IS NULL AND [PreviousConfirmedEmployeeId] IS NULL AND [CancellationKind] IS NULL";
        const string noOldConfirmation = "[PreviousConfirmedStartUtc] IS NULL AND [PreviousConfirmedEndUtc] IS NULL AND [PreviousConfirmedEmployeeId] IS NULL";
        const string oldConfirmation = "[PreviousConfirmedStartUtc] IS NOT NULL AND [PreviousConfirmedEndUtc] IS NOT NULL AND [PreviousConfirmedEmployeeId] IS NOT NULL AND [PreviousConfirmedStartUtc]>[AtUtc] AND [PreviousConfirmedEndUtc]>[PreviousConfirmedStartUtc]";
        const string changeReason = "[Reason] IS NOT NULL AND LEN(LTRIM(RTRIM([Reason])))>0";
        var humanKinds =
            $"([Kind]=N'Requested' AND [ViewingVersion]=1 AND {noConfirmation} AND {noCoordination} AND {noPrevious} AND {noLifecycle} AND [OfferedEmployeeId] IS NULL AND [ProposedEndUtc]=DATEADD(minute,60,[ProposedStartUtc])) OR " +
            $"([Kind]=N'Confirmed' AND [ViewingVersion]>1 AND {hasConfirmation} AND {hasCoordination} AND {noPrevious} AND {noLifecycle} AND [OfferedEmployeeId] IS NULL AND [ProposedEndUtc]=DATEADD(minute,60,[ProposedStartUtc])) OR " +
            $"([Kind]=N'AlternativeOffered' AND [ViewingVersion]>1 AND {noConfirmation} AND {hasCoordination} AND {noLifecycle} AND [OfferedEmployeeId] IS NOT NULL AND [PreviousProposedStartUtc] IS NOT NULL AND [PreviousProposedEndUtc] IS NOT NULL AND [PreviousProposedStartUtc]>'00010101' AND [PreviousProposedEndUtc]>[PreviousProposedStartUtc] AND {changeReason}) OR " +
            $"([Kind]=N'AlternativeAccepted' AND [ViewingVersion]>=3 AND {hasConfirmation} AND {hasCoordination} AND {noPrevious} AND {noLifecycle} AND [OfferedEmployeeId] IS NOT NULL AND [OfferedEmployeeId]=[ConfirmedEmployeeId] AND [ConfirmedEndUtc]=[ProposedEndUtc]) OR " +
            $"([Kind]=N'Rescheduled' AND [ViewingVersion]>1 AND [PreviousStatus] IS NOT NULL AND {noConfirmation} AND {noCoordination} AND {changeReason} AND [OfferedEmployeeId] IS NULL AND [CancellationKind] IS NULL AND [ProposedEndUtc]=DATEADD(minute,60,[ProposedStartUtc]) AND [PreviousProposedStartUtc] IS NOT NULL AND [PreviousProposedEndUtc] IS NOT NULL AND [PreviousProposedStartUtc]>[AtUtc] AND [PreviousProposedEndUtc]>[PreviousProposedStartUtc] AND (([PreviousStatus] IN(N'PendingConfirmation',N'AwaitingCustomerAcceptance') AND {noOldConfirmation}) OR ([PreviousStatus]=N'Confirmed' AND {oldConfirmation}))) OR " +
            $"([Kind]=N'Cancelled' AND [ViewingVersion]>1 AND [PreviousStatus] IS NOT NULL AND {changeReason} AND [PreviousProposedStartUtc] IS NULL AND [PreviousProposedEndUtc] IS NULL AND {noOldConfirmation} AND [CancellationKind] IS NOT NULL AND [CancellationKind] IN(N'Customer',N'Broker',N'Developer') AND (([PreviousStatus]=N'PendingConfirmation' AND {noConfirmation} AND {noCoordination} AND [OfferedEmployeeId] IS NULL) OR ([PreviousStatus]=N'AwaitingCustomerAcceptance' AND {noConfirmation} AND {hasCoordination} AND [OfferedEmployeeId] IS NOT NULL) OR ([PreviousStatus]=N'Confirmed' AND {hasConfirmation} AND {hasCoordination} AND ([OfferedEmployeeId] IS NULL OR [OfferedEmployeeId]=[ConfirmedEmployeeId]))))";
        const string noOutcome = "[OutcomeOccurredAtUtc] IS NULL AND [OutcomeNoteId] IS NULL";
        var humanAudit = "[ActorKind]=N'User' AND [ActorAccountId] IS NOT NULL AND [EffectiveCutoffUtc] IS NULL";
        var outcome = $"[Kind] IN(N'Completed',N'CustomerNoShow') AND [ViewingVersion]>2 AND {hasConfirmation} AND {hasCoordination} AND {noPrevious} AND {noLifecycle} AND [OutcomeOccurredAtUtc] IS NOT NULL AND [OutcomeNoteId] IS NOT NULL AND [OutcomeOccurredAtUtc]>=[ConfirmedStartUtc] AND [OutcomeOccurredAtUtc]<=[AtUtc] AND ([OfferedEmployeeId] IS NULL OR [OfferedEmployeeId]=[ConfirmedEmployeeId])";
        var systemExpiry = $"[Kind]=N'Expired' AND [ViewingVersion]>1 AND [ActorKind]=N'System' AND [ActorAccountId] IS NULL AND [EffectiveCutoffUtc] IS NOT NULL AND [EffectiveCutoffUtc]>'00010101' AND [EffectiveCutoffUtc]<=[AtUtc] AND [EffectiveCutoffUtc]<=[ProposedStartUtc] AND [PreviousStatus] IS NOT NULL AND [Reason] IS NOT NULL AND {noConfirmation} AND {noOldConfirmation} AND [PreviousProposedStartUtc] IS NULL AND [PreviousProposedEndUtc] IS NULL AND [CancellationKind] IS NULL AND (([PreviousStatus]=N'PendingConfirmation' AND {noCoordination} AND [OfferedEmployeeId] IS NULL AND [Reason]=N'CoordinationDeadline') OR ([PreviousStatus]=N'AwaitingCustomerAcceptance' AND {hasCoordination} AND [OfferedEmployeeId] IS NOT NULL AND [EffectiveCutoffUtc]=[ProposedStartUtc] AND [Reason]=N'AlternativeStart'))";
        b.ToTable("ViewingEvents", "Crm", t => t.HasCheckConstraint("CK_ViewingEvents_Shape",
            $"[AtUtc]>'00010101' AND [ProposedStartUtc]>'00010101' AND [ProposedEndUtc]>[ProposedStartUtc] AND ((({humanAudit}) AND {noOutcome} AND [ProposedStartUtc]>[AtUtc] AND [ProposedStartUtc]<=DATEADD(day,30,[AtUtc]) AND ({humanKinds})) OR ({systemExpiry} AND {noOutcome}) OR ({humanAudit} AND {outcome}))"));
        b.HasKey(e => e.Id); b.Property(e => e.Id).ValueGeneratedNever(); b.HasIndex(e => new { e.ViewingId, e.ViewingVersion }).IsUnique();
        b.HasOne<Viewing>().WithMany().HasForeignKey(e => new { e.ViewingId, e.CustomerId })
            .HasPrincipalKey(v => new { v.Id, v.CustomerId }).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ApplicationUser>().WithMany().HasForeignKey(e => e.ActorAccountId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<SalesNote>().WithMany().HasForeignKey(e => new { e.OutcomeNoteId, e.CustomerId })
            .HasPrincipalKey(n => new { n.Id, n.CustomerId }).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Employee>().WithMany().HasForeignKey(e => e.ConfirmedEmployeeId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Employee>().WithMany().HasForeignKey(e => e.OfferedEmployeeId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Employee>().WithMany().HasForeignKey(e => e.PreviousConfirmedEmployeeId).OnDelete(DeleteBehavior.Restrict);
        b.Property(e => e.ActorKind).HasMaxLength(16).HasDefaultValue("User").IsRequired();
        b.Property(e => e.EffectiveCutoffUtc).HasPrecision(7);
        b.Property(e => e.OutcomeOccurredAtUtc).HasPrecision(7);
        b.Property(e => e.Kind).HasMaxLength(32).IsRequired(); b.Property(e => e.AtUtc).HasPrecision(7);
        b.Property(e => e.ProposedStartUtc).HasPrecision(7); b.Property(e => e.ProposedEndUtc).HasPrecision(7);
        b.Property(e => e.ConfirmedStartUtc).HasPrecision(7); b.Property(e => e.ConfirmedEndUtc).HasPrecision(7); b.Property(e => e.CoordinatedAtUtc).HasPrecision(7);
        b.Property(e => e.PreviousProposedStartUtc).HasPrecision(7); b.Property(e => e.PreviousProposedEndUtc).HasPrecision(7);
        b.Property(e => e.CoordinationSource).HasMaxLength(Viewing.CoordinationSourceMaxLength);
        b.Property(e => e.CoordinationReference).HasMaxLength(Viewing.CoordinationReferenceMaxLength);
        b.Property(e => e.Reason).HasMaxLength(Viewing.AlternativeReasonMaxLength);
        b.Property(e => e.PreviousStatus).HasMaxLength(32); b.Property(e => e.CancellationKind).HasMaxLength(16);
        b.Property(e => e.PreviousConfirmedStartUtc).HasPrecision(7); b.Property(e => e.PreviousConfirmedEndUtc).HasPrecision(7);
    }
}
