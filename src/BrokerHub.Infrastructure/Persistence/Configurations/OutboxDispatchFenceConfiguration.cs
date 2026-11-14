using BrokerHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BrokerHub.Infrastructure.Persistence.Configurations;

internal sealed class OutboxDispatchFenceConfiguration:IEntityTypeConfiguration<OutboxDispatchFence>
{
    public void Configure(EntityTypeBuilder<OutboxDispatchFence> b)
    {
        b.ToTable("OutboxDispatchFences","Reliability",t=>t.HasCheckConstraint("CK_OutboxDispatchFences_Singleton","[Id]=1"));
        b.HasKey(x=>x.Id);b.Property(x=>x.Id).ValueGeneratedNever();b.HasData(OutboxDispatchFence.Create());
    }
}
