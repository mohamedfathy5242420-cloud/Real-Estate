using BrokerHub.Domain.Entities;
using BrokerHub.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BrokerHub.Infrastructure.Persistence.Configurations;

internal sealed class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> b)
    {
        b.ToTable("Employees", "Crm", t =>
        {
            t.HasCheckConstraint("CK_Employees_Number", "LEN(LTRIM(RTRIM([EmployeeNumber])))>0");
            t.HasCheckConstraint("CK_Employees_Name", "LEN(LTRIM(RTRIM([DisplayName])))>0");
            t.HasCheckConstraint("CK_Employees_Version", "[Version]>0");
            t.HasCheckConstraint("CK_Employees_Audit", "([Version]=1 AND [UpdatedAtUtc] IS NULL AND [UpdatedByAccountId] IS NULL) OR ([Version]>1 AND [UpdatedAtUtc] IS NOT NULL AND [UpdatedByAccountId] IS NOT NULL AND [UpdatedAtUtc]>=[CreatedAtUtc])");
        });
        b.HasKey(e => e.Id); b.Property(e => e.Id).ValueGeneratedNever();
        b.Property(e => e.EmployeeNumber).HasMaxLength(Employee.NumberMaxLength).IsRequired();
        b.Property(e => e.DisplayName).HasMaxLength(Employee.NameMaxLength).IsRequired();
        b.Property(e => e.Version).IsConcurrencyToken();
        b.Property(e => e.CreatedAtUtc).HasPrecision(7); b.Property(e => e.UpdatedAtUtc).HasPrecision(7);
        b.HasIndex(e => e.AccountId).IsUnique(); b.HasIndex(e => e.EmployeeNumber).IsUnique();
        b.HasOne<ApplicationUser>().WithMany().HasForeignKey(e => e.AccountId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ApplicationUser>().WithMany().HasForeignKey(e => e.CreatedByAccountId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ApplicationUser>().WithMany().HasForeignKey(e => e.UpdatedByAccountId).OnDelete(DeleteBehavior.Restrict);
        b.HasMany(e => e.History).WithOne().HasForeignKey(r => r.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        b.Navigation(e => e.History).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
