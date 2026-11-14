using BrokerHub.Domain.Entities;
using BrokerHub.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BrokerHub.Infrastructure.Persistence.Configurations;
internal sealed class SalesNoteConfiguration : IEntityTypeConfiguration<SalesNote>
{
    public void Configure(EntityTypeBuilder<SalesNote> b)
    {
        b.ToTable("SalesNotes", "Crm", t =>
        {
            t.HasCheckConstraint("CK_SalesNotes_Body", "LEN(LTRIM(RTRIM([Body])))>0");
            t.HasCheckConstraint("CK_SalesNotes_Correction", "[CorrectsNoteId] IS NULL OR [CorrectsNoteId]<>[Id]");
        });
        b.HasKey(n => n.Id); b.HasAlternateKey(n => new { n.Id, n.CustomerId });
        b.HasIndex(n => new { n.CustomerId, n.CreatedAtUtc });
        b.HasOne<Customer>().WithMany().HasForeignKey(n => n.CustomerId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ApplicationUser>().WithMany().HasForeignKey(n => n.AuthorAccountId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Employee>().WithMany().HasForeignKey(n => n.AuthorEmployeeId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Lead>().WithMany().HasForeignKey(n => new { n.LeadId, n.CustomerId }).HasPrincipalKey(l => new { l.Id, l.CustomerId }).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<SalesNote>().WithMany().HasForeignKey(n => new { n.CorrectsNoteId, n.CustomerId }).HasPrincipalKey(n => new { n.Id, n.CustomerId }).OnDelete(DeleteBehavior.Restrict);
        b.Property(n => n.Body).HasMaxLength(SalesNote.BodyMaxLength).IsRequired(); b.Property(n => n.CreatedAtUtc).HasPrecision(7);
    }
}
