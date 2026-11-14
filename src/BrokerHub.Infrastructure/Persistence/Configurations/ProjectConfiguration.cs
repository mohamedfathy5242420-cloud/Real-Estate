using BrokerHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BrokerHub.Infrastructure.Persistence.Configurations;

internal sealed class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("Projects", "Catalog", table =>
        {
            table.HasCheckConstraint(
                "CK_Projects_Code_NotBlank",
                "LEN(LTRIM(RTRIM([Code]))) > 0");
            table.HasCheckConstraint(
                "CK_Projects_Name_NotBlank",
                "LEN(LTRIM(RTRIM([Name]))) > 0");
            table.HasCheckConstraint(
                "CK_Projects_Area_NotBlank",
                "LEN(LTRIM(RTRIM([Area]))) > 0");
            table.HasCheckConstraint(
                "CK_Projects_TimeZoneId_NotBlank",
                "LEN(LTRIM(RTRIM([TimeZoneId]))) > 0");
            table.HasCheckConstraint("CK_Projects_Version_Positive", "[Version] > 0");
        });

        builder.HasKey(project => project.Id);

        builder.Property(project => project.DeveloperId)
            .IsRequired();
        builder.HasOne<Developer>()
            .WithMany()
            .HasForeignKey(project => project.DeveloperId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_Projects_Developers_DeveloperId");

        builder.Property(project => project.Code)
            .HasMaxLength(Project.CodeMaxLength)
            .IsRequired();
        builder.HasIndex(project => new { project.DeveloperId, project.Code })
            .IsUnique()
            .HasDatabaseName("UX_Projects_DeveloperId_Code");

        builder.Property(project => project.Name)
            .HasMaxLength(Project.NameMaxLength)
            .IsRequired();
        builder.Property(project => project.Area)
            .HasMaxLength(Project.AreaMaxLength)
            .IsRequired();
        builder.Property(project => project.Address)
            .HasMaxLength(Project.AddressMaxLength);
        builder.Property(project => project.TimeZoneId)
            .HasMaxLength(Project.TimeZoneIdMaxLength)
            .IsRequired();

        builder.Property(project => project.IsActive)
            .HasDefaultValue(true)
            .IsRequired();
        builder.HasIndex(project => new { project.DeveloperId, project.IsActive })
            .HasDatabaseName("IX_Projects_DeveloperId_IsActive");

        builder.Property(project => project.CreatedAtUtc)
            .HasPrecision(7)
            .IsRequired();
        builder.Property(project => project.CreatedByAccountId)
            .IsRequired();
        builder.Property(project => project.UpdatedAtUtc)
            .HasPrecision(7);

        builder.Property(project => project.Version)
            .HasDefaultValue(1L)
            .IsConcurrencyToken()
            .IsRequired();
    }
}
