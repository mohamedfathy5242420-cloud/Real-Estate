using BrokerHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace BrokerHub.Infrastructure.Persistence.Configurations;
internal sealed class UnitImageConfiguration : IEntityTypeConfiguration<UnitImage>
{
    public void Configure(EntityTypeBuilder<UnitImage> builder)
    {
        builder.ToTable("UnitImages", "Catalog", table =>
        {
            table.HasCheckConstraint("CK_UnitImages_StorageKey_NotBlank", "LEN(LTRIM(RTRIM([StorageKey]))) > 0");
            table.HasCheckConstraint("CK_UnitImages_SortOrder_Nonnegative", "[SortOrder] >= 0");
        });
        builder.HasKey(image => image.Id);
        builder.HasOne<Unit>().WithMany().HasForeignKey(image => image.UnitId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(image => image.StorageKey).HasMaxLength(UnitImage.StorageKeyMaxLength).IsRequired();
        builder.Property(image => image.AltText).HasMaxLength(UnitImage.AltTextMaxLength);
        builder.Property(image => image.CreatedAtUtc).HasPrecision(7);
        builder.Property(image => image.UpdatedAtUtc).HasPrecision(7);
        builder.HasIndex(image => image.StorageKey).IsUnique().HasDatabaseName("UX_UnitImages_StorageKey");
        builder.HasIndex(image => new { image.UnitId, image.SortOrder, image.Id }).HasDatabaseName("IX_UnitImages_UnitId_SortOrder_Id");
        // IsPrimary is retained only. No proposed one-primary constraint or selection workflow.
    }
}
