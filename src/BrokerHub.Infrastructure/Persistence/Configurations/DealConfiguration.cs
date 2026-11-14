using BrokerHub.Domain.Entities;
using BrokerHub.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BrokerHub.Infrastructure.Persistence.Configurations;

internal sealed class DealConfiguration:IEntityTypeConfiguration<Deal>
{
    public void Configure(EntityTypeBuilder<Deal> b)
    {
        b.ToTable("Deals","Sales",t=>
        {
            t.HasCheckConstraint("CK_Deals_Versions","[Version]=1 AND [LeadVersion]>0 AND [AgreementVersion]>0 AND [RuleVersion]>0");
            t.HasCheckConstraint("CK_Deals_Clock","[SaleOccurredAtUtc]>'00010101' AND [AdoptedAtUtc]>=[SaleOccurredAtUtc]");
            t.HasCheckConstraint("CK_Deals_OriginDates","[AgreementStartsOn]>'00010101' AND [AgreementEndsOn]>=[AgreementStartsOn] AND [LeadOpenedOnLocalDate]>=[AgreementStartsOn] AND [LeadOpenedOnLocalDate]<=[AgreementEndsOn] AND [ProtectionPeriodDays]>=0 AND [ProtectionEndsOn]>=[AgreementEndsOn] AND DATEDIFF(day,[AgreementEndsOn],[ProtectionEndsOn])=[ProtectionPeriodDays] AND [SaleLocalDate]>=[LeadOpenedOnLocalDate] AND [SaleLocalDate]<=[ProtectionEndsOn]");
            t.HasCheckConstraint("CK_Deals_SnapshotScope","[AgreementScope] IN (N'AllDeveloperProjects',N'SelectedProjects') AND ([RuleProjectId] IS NULL OR [RuleProjectId]=[ProjectId]) AND LEN(LTRIM(RTRIM([AgreementNumber])))>0 AND LEN(LTRIM(RTRIM([OpeningTimeZoneId])))>0");
            t.HasCheckConstraint("CK_Deals_Money","[FinalUnitPrice]>0 AND [Amount]>=0 AND LEN([CurrencyCode])=3 AND [CurrencyCode] COLLATE Latin1_General_100_BIN2 NOT LIKE N'%[^A-Z]%' AND (([RuleType]=N'Percentage' AND [Percentage] IS NOT NULL AND [Percentage]>0 AND [Percentage]<=100 AND [FixedAmount] IS NULL AND [Amount]=ROUND([FinalUnitPrice]*[Percentage]/CONVERT(decimal(3,0),100),2)) OR ([RuleType]=N'FixedAmount' AND [FixedAmount] IS NOT NULL AND [FixedAmount]>=0 AND [Percentage] IS NULL AND [Amount]=[FixedAmount]))");
            t.HasCheckConstraint("CK_Deals_Evidence","LEN(LTRIM(RTRIM([Source])))>0 AND LEN(LTRIM(RTRIM([Reference])))>0 AND ([SelectionReason] IS NULL OR LEN(LTRIM(RTRIM([SelectionReason])))>0)");
        });
        b.HasKey(x=>x.Id);b.Property(x=>x.Id).ValueGeneratedNever();
        b.HasAlternateKey(x=>new{x.Id,x.CustomerId,x.UnitId});
        b.HasAlternateKey(x=>new{x.Id,x.CustomerId,x.UnitId,x.Version,x.CurrencyCode,x.SaleOccurredAtUtc,x.Amount});
        b.HasIndex(x=>x.UnitId).IsUnique().HasDatabaseName("UX_Deals_Unit");
        b.HasIndex(x=>new{x.CustomerId,x.AdoptedAtUtc,x.Id});
        b.HasOne<Unit>().WithMany().HasForeignKey(x=>new{x.UnitId,x.ProjectId})
            .HasPrincipalKey(x=>new{x.Id,x.ProjectId}).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Lead>().WithMany().HasForeignKey(x=>new{x.LeadId,x.CustomerId,x.ProjectId,x.AgreementId})
            .HasPrincipalKey(x=>new{x.Id,x.CustomerId,x.ProjectId,x.MarketingAgreementId}).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<MarketingAgreement>().WithMany().HasForeignKey(x=>new{x.AgreementId,x.DeveloperId})
            .HasPrincipalKey(x=>new{x.Id,x.DeveloperId}).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<CommissionRule>().WithMany().HasForeignKey(x=>new{x.RuleId,x.AgreementId,x.DeveloperId})
            .HasPrincipalKey(x=>new{x.Id,x.MarketingAgreementId,x.DeveloperId}).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ApplicationUser>().WithMany().HasForeignKey(x=>x.AdoptedByAccountId).OnDelete(DeleteBehavior.Restrict);
        b.Property(x=>x.Version).IsConcurrencyToken();
        b.Property(x=>x.AgreementNumber).HasMaxLength(MarketingAgreement.NumberMaxLength).IsRequired();
        b.Property(x=>x.AgreementScope).HasConversion<string>().HasMaxLength(32).IsRequired();
        b.Property(x=>x.OpeningTimeZoneId).HasMaxLength(100).IsRequired();
        b.Property(x=>x.RuleType).HasConversion<string>().HasMaxLength(16).IsRequired();
        b.Property(x=>x.Percentage).HasPrecision(7,4);b.Property(x=>x.FixedAmount).HasPrecision(18,2);
        b.Property(x=>x.FinalUnitPrice).HasPrecision(18,2);b.Property(x=>x.Amount).HasPrecision(18,2);
        b.Property(x=>x.CurrencyCode).HasMaxLength(3).IsRequired();
        b.Property(x=>x.Source).HasMaxLength(Deal.SourceMaxLength).IsRequired();b.Property(x=>x.Reference).HasMaxLength(Deal.ReferenceMaxLength).IsRequired();
        b.Property(x=>x.SelectionReason).HasMaxLength(Deal.SelectionReasonMaxLength);
        b.Property(x=>x.SaleOccurredAtUtc).HasPrecision(7);b.Property(x=>x.AdoptedAtUtc).HasPrecision(7);
    }
}
