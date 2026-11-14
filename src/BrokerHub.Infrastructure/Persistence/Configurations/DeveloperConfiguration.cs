using BrokerHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BrokerHub.Infrastructure.Persistence.Configurations;

internal sealed class DeveloperConfiguration : IEntityTypeConfiguration<Developer>
{
    public void Configure(EntityTypeBuilder<Developer> builder)
    {
        builder.ToTable("Developers", "Catalog", table =>
        {
            table.HasCheckConstraint(
                "CK_Developers_Code_NotBlank",
                "LEN(LTRIM(RTRIM([Code]))) > 0");
            table.HasCheckConstraint(
                "CK_Developers_Name_NotBlank",
                "LEN(LTRIM(RTRIM([Name]))) > 0");
            table.HasCheckConstraint("CK_Developers_Version_Positive", "[Version] > 0");
        });

        builder.HasKey(developer => developer.Id);

        builder.Property(developer => developer.Code)
            .HasMaxLength(Developer.CodeMaxLength)
            .IsRequired();
        builder.HasIndex(developer => developer.Code)
            .IsUnique()
            .HasDatabaseName("UX_Developers_Code");

        builder.Property(developer => developer.Name)
            .HasMaxLength(Developer.NameMaxLength)
            .IsRequired();
        builder.Property(developer => developer.LegalName)
            .HasMaxLength(Developer.LegalNameMaxLength);
        builder.Property(developer => developer.ExternalContactName)
            .HasMaxLength(Developer.ContactNameMaxLength);
        builder.Property(developer => developer.ExternalContactEmail)
            .HasMaxLength(Developer.ContactEmailMaxLength);
        builder.Property(developer => developer.ExternalContactPhone)
            .HasMaxLength(Developer.ContactPhoneMaxLength);

        builder.Property(developer => developer.IsActive)
            .HasDefaultValue(true)
            .IsRequired();
        builder.HasIndex(developer => developer.IsActive)
            .HasDatabaseName("IX_Developers_IsActive");

        builder.Property(developer => developer.CreatedAtUtc)
            .HasPrecision(7)
            .IsRequired();
        builder.Property(developer => developer.CreatedByAccountId)
            .IsRequired();
        builder.Property(developer => developer.UpdatedAtUtc)
            .HasPrecision(7);

        builder.Property(developer => developer.Version)
            .HasDefaultValue(1L)
            .IsConcurrencyToken()
            .IsRequired();
    }
}
