using BrokerHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BrokerHub.Infrastructure.Persistence.Configurations;

internal sealed class UnitConfiguration : IEntityTypeConfiguration<Unit>
{
    public void Configure(EntityTypeBuilder<Unit> builder)
    {
        builder.ToTable("Units", "Catalog", table =>
        {
            table.HasCheckConstraint("CK_Units_Code_NotBlank", "LEN(LTRIM(RTRIM([Code]))) > 0");
            table.HasCheckConstraint("CK_Units_Title_NotBlank", "LEN(LTRIM(RTRIM([Title]))) > 0");
            table.HasCheckConstraint("CK_Units_UnitType_NotBlank", "LEN(LTRIM(RTRIM([UnitType]))) > 0");
            table.HasCheckConstraint("CK_Units_AreaSqm_Positive", "[AreaSqm] > 0");
            table.HasCheckConstraint("CK_Units_ListPrice_Nonnegative", "[ListPrice] >= 0");
            table.HasCheckConstraint("CK_Units_Bedrooms_Nonnegative", "[Bedrooms] IS NULL OR [Bedrooms] >= 0");
            table.HasCheckConstraint("CK_Units_Bathrooms_Nonnegative", "[Bathrooms] IS NULL OR [Bathrooms] >= 0");
            table.HasCheckConstraint("CK_Units_Currency_Alphabetic", "LEN([Currency]) = 3 AND [Currency] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^A-Z]%'");
            table.HasCheckConstraint("CK_Units_PublicationStatus_NotBlank", "LEN(LTRIM(RTRIM([PublicationStatus]))) > 0");
            table.HasCheckConstraint("CK_Units_PublicationStatus", "[PublicationStatus] IN ('Unpublished','Published')");
            table.HasCheckConstraint("CK_Units_AvailabilityStatus_NotBlank", "LEN(LTRIM(RTRIM([AvailabilityStatus]))) > 0");
            table.HasCheckConstraint("CK_Units_Version_Positive", "[Version] > 0");
            table.HasCheckConstraint("CK_Units_Availability_Observation", "[AvailabilityStatus] IN ('Unconfirmed','Available','Unavailable')");
            table.HasCheckConstraint("CK_Units_ConfirmedPrice_Projection", "([LatestPriceConfirmationId] IS NULL AND [ConfirmedPrice] IS NULL AND [ConfirmedCurrency] IS NULL AND [LastPriceConfirmedAtUtc] IS NULL AND [LastPriceSource] IS NULL) OR ([LatestPriceConfirmationId] IS NOT NULL AND [ConfirmedPrice] IS NOT NULL AND [ConfirmedPrice] >= 0 AND [ConfirmedCurrency] IS NOT NULL AND LEN([ConfirmedCurrency]) = 3 AND [ConfirmedCurrency] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^A-Z]%' AND [LastPriceConfirmedAtUtc] IS NOT NULL AND [LastPriceSource] IS NOT NULL)");
            table.HasCheckConstraint("CK_Units_Availability_Projection", "([LatestAvailabilityConfirmationId] IS NULL AND [AvailabilityStatus] = 'Unconfirmed' AND [LastAvailabilityConfirmedAtUtc] IS NULL AND [LastAvailabilitySource] IS NULL) OR ([LatestAvailabilityConfirmationId] IS NOT NULL AND [AvailabilityStatus] IN ('Available','Unavailable') AND [LastAvailabilityConfirmedAtUtc] IS NOT NULL AND [LastAvailabilitySource] IS NOT NULL)");
        });
        builder.HasKey(unit => unit.Id);
        builder.HasOne<Project>().WithMany().HasForeignKey(unit => unit.ProjectId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_Units_Projects_ProjectId");
        builder.Property(unit => unit.Code).HasMaxLength(Unit.CodeMaxLength).IsRequired();
        builder.HasIndex(unit => new { unit.ProjectId, unit.Code }).IsUnique().HasDatabaseName("UX_Units_ProjectId_Code");
        builder.Property(unit => unit.Title).HasMaxLength(Unit.TitleMaxLength).IsRequired();
        builder.Property(unit => unit.Description).HasMaxLength(Unit.DescriptionMaxLength);
        builder.Property(unit => unit.UnitType).HasMaxLength(Unit.UnitTypeMaxLength).IsRequired();
        builder.Property(unit => unit.AreaSqm).HasPrecision(18, 2);
        builder.Property(unit => unit.ListPrice).HasPrecision(18, 2);
        builder.Property(unit => unit.Currency).HasMaxLength(3).IsRequired();
        builder.Property(unit => unit.PublicationStatus).HasMaxLength(32).IsRequired();
        builder.Property(unit => unit.AvailabilityStatus).HasMaxLength(32).IsRequired();
        builder.Property(unit => unit.LastPriceSource).HasMaxLength(500);
        builder.Property(unit => unit.LastAvailabilitySource).HasMaxLength(500);
        builder.Property(unit => unit.LastPriceConfirmedAtUtc).HasPrecision(7);
        builder.Property(unit => unit.LastAvailabilityConfirmedAtUtc).HasPrecision(7);
        builder.Property(unit => unit.CreatedAtUtc).HasPrecision(7);
        builder.Property(unit => unit.UpdatedAtUtc).HasPrecision(7);
        builder.Property(unit => unit.Version).HasDefaultValue(1L).IsConcurrencyToken();
        builder.Property(unit => unit.ConfirmedPrice).HasPrecision(18, 2);
        builder.Property(unit => unit.ConfirmedCurrency).HasMaxLength(3);
        builder.HasOne<UnitMarketConfirmation>().WithMany()
            .HasForeignKey(unit => new { unit.Id, unit.LatestPriceConfirmationId })
            .HasPrincipalKey(c => new { c.UnitId, c.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UnitMarketConfirmation>().WithMany()
            .HasForeignKey(unit => new { unit.Id, unit.LatestAvailabilityConfirmationId })
            .HasPrincipalKey(c => new { c.UnitId, c.Id }).OnDelete(DeleteBehavior.Restrict);
    }
}
