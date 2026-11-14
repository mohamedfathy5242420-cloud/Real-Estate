using BrokerHub.Domain.Entities;
using BrokerHub.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BrokerHub.Infrastructure.Persistence.Configurations;
internal sealed class LeadEventConfiguration : IEntityTypeConfiguration<LeadEvent>
{
    public void Configure(EntityTypeBuilder<LeadEvent> b)
    {
        b.ToTable("LeadEvents", "Crm", t => t.HasCheckConstraint("CK_LeadEvents_Shape", "([EventType]=N'Opened' AND [LeadVersion]=1 AND [Reason] IS NULL) OR ([EventType]=N'Closed' AND [LeadVersion]>1 AND [Reason] IS NOT NULL AND LEN(LTRIM(RTRIM([Reason])))>0)"));
        b.HasKey(e => e.Id); b.HasIndex(e => new { e.LeadId, e.LeadVersion }).IsUnique();
        b.HasOne<Lead>().WithMany().HasForeignKey(e => new { e.LeadId, e.CustomerId }).HasPrincipalKey(l => new { l.Id, l.CustomerId }).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ApplicationUser>().WithMany().HasForeignKey(e => e.ActorAccountId).OnDelete(DeleteBehavior.Restrict);
        b.Property(e => e.EventType).HasMaxLength(16).IsRequired(); b.Property(e => e.Reason).HasMaxLength(Lead.ReasonMaxLength); b.Property(e => e.AtUtc).HasPrecision(7);
    }
}
