using System.Threading.Channels;
using BrokerHub.Application.DTOs.Catalog;
using BrokerHub.Application.Interfaces.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace BrokerHub.Infrastructure.Workers;

public sealed record PublicImageWarmupSnapshot(bool Enabled,bool Accepting,int Capacity,int Queued,int Processing,int WaitingOffers,
    long Accepted,long Completed,long Ineligible,long Failures,long Canceled,long Abandoned,long Busy);

public sealed class PublicImageWarmupWorker:BackgroundService,IPublicImageWarmupQueue
{
    private readonly record struct Job(Guid UnitId,Guid ImageId);
    private readonly PublicImageWarmupOptions settings;
    private readonly IServiceScopeFactory scopes;
    private readonly Channel<Job> channel;
    private readonly SemaphoreSlim offers;
    private readonly CancellationTokenSource closed=new();
    private int state,processing,waitingOffers;
    private long accepted,completed,ineligible,failures,canceled,abandoned,busy;

    public PublicImageWarmupWorker(IOptions<PublicImageWarmupOptions> options,IServiceScopeFactory scopes)
    {
        this.scopes=scopes;var configured=options.Value;
        settings=new(){Enabled=configured.Enabled,Capacity=configured.Capacity,MaximumWaitingOffers=configured.MaximumWaitingOffers,
            MaximumImagesPerOffer=configured.MaximumImagesPerOffer,ProducerWaitMilliseconds=configured.ProducerWaitMilliseconds,ItemTimeoutSeconds=configured.ItemTimeoutSeconds};
        if(!settings.IsValid)throw new ArgumentException("Public image warmup bounds are invalid.");
        channel=Channel.CreateBounded<Job>(new BoundedChannelOptions(settings.Capacity){FullMode=BoundedChannelFullMode.Wait,SingleReader=true,SingleWriter=false,AllowSynchronousContinuations=false});
        offers=new(settings.MaximumWaitingOffers,settings.MaximumWaitingOffers);
    }
    public PublicImageWarmupSnapshot Snapshot()=>new(settings.Enabled,settings.Enabled&&Volatile.Read(ref state)==1,settings.Capacity,
        channel.Reader.Count,Volatile.Read(ref processing),Volatile.Read(ref waitingOffers),Interlocked.Read(ref accepted),Interlocked.Read(ref completed),
        Interlocked.Read(ref ineligible),Interlocked.Read(ref failures),Interlocked.Read(ref canceled),Interlocked.Read(ref abandoned),Interlocked.Read(ref busy));
    public override Task StartAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();if(Interlocked.CompareExchange(ref state,1,0)!=0)throw new InvalidOperationException("Start a new warmup host after stop.");
        return base.StartAsync(ct);
    }
    public async ValueTask<PublicImageWarmupOffer> OfferAsync(Guid unitId,IReadOnlyList<Guid> imageIds,CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if(Volatile.Read(ref state)!=1)return new(PublicImageWarmupOfferStatus.Stopped,0,imageIds.Count);
        if(!settings.Enabled)return new(PublicImageWarmupOfferStatus.Disabled,0,imageIds.Count);
        if(unitId==Guid.Empty||imageIds.Count>100||imageIds.Any(id=>id==Guid.Empty)||imageIds.Distinct().Count()!=imageIds.Count)return new(PublicImageWarmupOfferStatus.Invalid,0,imageIds.Count);
        if(imageIds.Count==0)return new(PublicImageWarmupOfferStatus.Accepted,0,0);
        if(!await offers.WaitAsync(0,ct)){Interlocked.Increment(ref busy);return new(PublicImageWarmupOfferStatus.Busy,0,imageIds.Count);}
        Interlocked.Increment(ref waitingOffers);var count=0;
        try
        {
            using var deadline=CancellationTokenSource.CreateLinkedTokenSource(ct,closed.Token);deadline.CancelAfter(settings.ProducerWaitMilliseconds);
            try
            {
                for(var n=0;n<Math.Min(imageIds.Count,settings.MaximumImagesPerOffer);n++)
                {await channel.Writer.WriteAsync(new(unitId,imageIds[n]),deadline.Token);count++;Interlocked.Increment(ref accepted);}
                return new(PublicImageWarmupOfferStatus.Accepted,count,imageIds.Count-count);
            }
            catch(OperationCanceledException)when(!ct.IsCancellationRequested)
            {
                if(closed.IsCancellationRequested)return new(PublicImageWarmupOfferStatus.Stopped,count,imageIds.Count-count);
                Interlocked.Increment(ref busy);return new(PublicImageWarmupOfferStatus.Busy,count,imageIds.Count-count);
            }
            catch(ChannelClosedException){return new(PublicImageWarmupOfferStatus.Stopped,count,imageIds.Count-count);}
        }
        finally{Interlocked.Decrement(ref waitingOffers);offers.Release();}
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if(!settings.Enabled)return;
        try
        {
            await foreach(var job in channel.Reader.ReadAllAsync(stoppingToken))
            {
                Interlocked.Exchange(ref processing,1);
                try
                {
                    using var deadline=CancellationTokenSource.CreateLinkedTokenSource(stoppingToken,closed.Token);deadline.CancelAfter(TimeSpan.FromSeconds(settings.ItemTimeoutSeconds));
                    await using var scope=scopes.CreateAsyncScope();
                    // Reuse the original scoped catalog service as processor; it rechecks SQL before every cache read.
                    var result=await scope.ServiceProvider.GetRequiredService<IPublicCatalogService>().ImageAsync(job.UnitId,job.ImageId,deadline.Token);
                    if(result.Value is not null)await result.Value.Stream.DisposeAsync();
                    if(result.Status==PublicCatalogStatus.Success&&result.Value is not null)Interlocked.Increment(ref completed);
                    else if(result.Status is PublicCatalogStatus.NotFound or PublicCatalogStatus.Invalid)Interlocked.Increment(ref ineligible);
                    else Interlocked.Increment(ref failures);
                }
                catch(OperationCanceledException)when(stoppingToken.IsCancellationRequested||closed.IsCancellationRequested){Interlocked.Increment(ref canceled);break;}
                catch(Exception){Interlocked.Increment(ref failures);}
                finally{Interlocked.Exchange(ref processing,0);}
            }
        }
        catch(OperationCanceledException)when(stoppingToken.IsCancellationRequested||closed.IsCancellationRequested){ }
        finally{Close();while(channel.Reader.TryRead(out _))Interlocked.Increment(ref abandoned);}
    }
    private void Close(){Interlocked.Exchange(ref state,2);closed.Cancel();channel.Writer.TryComplete();}
    public override Task StopAsync(CancellationToken ct){Close();return base.StopAsync(ct);}
    public override void Dispose(){Close();base.Dispose();}
}
