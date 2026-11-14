using BrokerHub.Domain.Entities;
using BrokerHub.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BrokerHub.Infrastructure.Persistence.Configurations;

internal sealed class EmployeeRevisionConfiguration : IEntityTypeConfiguration<EmployeeRevision>
{
    public void Configure(EntityTypeBuilder<EmployeeRevision> b)
    {
        b.ToTable("EmployeeRevisions", "Crm", t =>
        {
            t.HasCheckConstraint("CK_EmployeeRevisions_Version", "[Version]>0");
            t.HasCheckConstraint("CK_EmployeeRevisions_Name", "LEN(LTRIM(RTRIM([DisplayName])))>0");
        });
        b.HasKey(r => r.Id); b.Property(r => r.Id).ValueGeneratedNever();
        b.Property(r => r.DisplayName).HasMaxLength(Employee.NameMaxLength).IsRequired();
        b.Property(r => r.AtUtc).HasPrecision(7);
        b.HasIndex(r => new { r.EmployeeId, r.Version }).IsUnique();
        b.HasOne<ApplicationUser>().WithMany().HasForeignKey(r => r.ActorAccountId).OnDelete(DeleteBehavior.Restrict);
    }
}
