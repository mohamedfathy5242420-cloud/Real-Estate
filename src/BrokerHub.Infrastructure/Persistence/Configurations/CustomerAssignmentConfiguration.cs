using BrokerHub.Domain.Entities;
using BrokerHub.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BrokerHub.Infrastructure.Persistence.Configurations;

internal sealed class CustomerAssignmentConfiguration : IEntityTypeConfiguration<CustomerAssignment>
{
    public void Configure(EntityTypeBuilder<CustomerAssignment> b)
    {
        b.ToTable("CustomerAssignments", "Crm", t =>
        {
            t.HasCheckConstraint("CK_CustomerAssignments_Version", "[CustomerVersion]>1");
            t.HasCheckConstraint("CK_CustomerAssignments_Transition", "[PreviousEmployeeId] IS NULL OR [PreviousEmployeeId]<>[NewEmployeeId]");
            t.HasCheckConstraint("CK_CustomerAssignments_Reason", "([Reason] IS NULL OR LEN(LTRIM(RTRIM([Reason])))>0) AND ([PreviousEmployeeId] IS NULL OR [Reason] IS NOT NULL)");
        });
        b.HasKey(a => a.Id); b.Property(a => a.Id).ValueGeneratedNever();
        b.Property(a => a.Reason).HasMaxLength(CustomerAssignment.ReasonMaxLength);
        b.Property(a => a.ChangedAtUtc).HasPrecision(7);
        b.HasIndex(a => new { a.CustomerId, a.CustomerVersion }).IsUnique();
        b.HasOne<Customer>().WithMany().HasForeignKey(a => a.CustomerId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Employee>().WithMany().HasForeignKey(a => a.PreviousEmployeeId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Employee>().WithMany().HasForeignKey(a => a.NewEmployeeId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ApplicationUser>().WithMany().HasForeignKey(a => a.ActorAccountId).OnDelete(DeleteBehavior.Restrict);
    }
}
