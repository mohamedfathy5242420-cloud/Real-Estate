using BrokerHub.Domain.Entities;
using BrokerHub.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BrokerHub.Infrastructure.Persistence.Configurations;

internal sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers", "Crm", table =>
        {
            table.HasCheckConstraint("CK_Customers_FullName_NotBlank", "LEN(LTRIM(RTRIM([FullName])))>0");
            table.HasCheckConstraint("CK_Customers_Phone_NotBlank", "LEN(LTRIM(RTRIM([Phone])))>0");
            table.HasCheckConstraint("CK_Customers_Version_Positive", "[Version]>0");
            table.HasCheckConstraint("CK_Customers_Assignment_Audit", "[AssignedEmployeeId] IS NULL OR [Version]>1");
            table.HasCheckConstraint("CK_Customers_Audit_Pair", "([Version]=1 AND [UpdatedAtUtc] IS NULL AND [UpdatedByAccountId] IS NULL) OR ([Version]>1 AND [UpdatedAtUtc] IS NOT NULL AND [UpdatedByAccountId] IS NOT NULL AND [UpdatedAtUtc]>=[CreatedAtUtc])");
        });
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();
        builder.Property(c => c.FullName).HasMaxLength(Customer.FullNameMaxLength).IsRequired();
        builder.Property(c => c.Phone).HasMaxLength(Customer.PhoneMaxLength).IsRequired();
        builder.Property(c => c.Email).HasMaxLength(Customer.EmailMaxLength);
        builder.Property(c => c.PreferredContactMethod).HasMaxLength(Customer.PreferredContactMethodMaxLength);
        builder.Property(c => c.Requirements).HasMaxLength(Customer.RequirementsMaxLength);
        builder.Property(c => c.CreatedAtUtc).HasPrecision(7).IsRequired();
        builder.Property(c => c.UpdatedAtUtc).HasPrecision(7);
        builder.Property(c => c.Version).HasDefaultValue(1L).IsConcurrencyToken().IsRequired();
        builder.HasIndex(c => c.AccountId).IsUnique().HasFilter("[AccountId] IS NOT NULL").HasDatabaseName("UX_Customers_AccountId");
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(c => c.AccountId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Employee>().WithMany().HasForeignKey(c => c.AssignedEmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(c => c.CreatedByAccountId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(c => c.UpdatedByAccountId).OnDelete(DeleteBehavior.Restrict);
    }
}
