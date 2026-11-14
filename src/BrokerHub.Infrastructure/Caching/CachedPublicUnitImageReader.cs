using System.Text.RegularExpressions;
using BrokerHub.Application.DTOs.UnitImages;
using BrokerHub.Application.Interfaces.Storage;
using Microsoft.Extensions.Options;

namespace BrokerHub.Infrastructure.Caching;

public sealed record PublicImageCacheSnapshot(long Hits,long Misses,long SourceOpens,int Entries,long StoredBytes,long Evictions,long Bypasses);

public sealed class CachedPublicUnitImageReader:IPublicUnitImageReader
{
    private sealed record Entry(byte[] Bytes,string ContentType,long LoadedAt,long Access);
    private static readonly Regex KeyPattern=new("\\A[a-f0-9]{32}\\.(png|jpg)\\z",RegexOptions.CultureInvariant);
    private readonly IUnitImageStorage storage;
    private readonly PublicImageCacheOptions settings;
    private readonly TimeProvider clock;
    private readonly object sync=new();
    private readonly SemaphoreSlim fill=new(1,1);
    private readonly Dictionary<string,Entry> entries=new(StringComparer.Ordinal);
    private long storedBytes,sequence,hits,misses,sourceOpens,evictions,bypasses;

    public CachedPublicUnitImageReader(IUnitImageStorage storage,IOptions<PublicImageCacheOptions> options,TimeProvider clock)
    {
        this.storage=storage;this.clock=clock;var configured=options.Value;
        settings=new(){Enabled=configured.Enabled,MaximumBytes=configured.MaximumBytes,MaximumEntries=configured.MaximumEntries,MaximumEntryBytes=configured.MaximumEntryBytes,LifetimeSeconds=configured.LifetimeSeconds};
        if(!settings.IsValid)throw new ArgumentException("Public image cache bounds are invalid.");
    }
    private static UnitImageContent Content(Entry entry)=>new(new MemoryStream(entry.Bytes,0,entry.Bytes.Length,writable:false,publiclyVisible:false),entry.ContentType);
    private bool Expired(Entry entry)=>clock.GetElapsedTime(entry.LoadedAt,clock.GetTimestamp())>=TimeSpan.FromSeconds(settings.LifetimeSeconds);
    private void Remove(string key){storedBytes-=entries[key].Bytes.Length;entries.Remove(key);evictions++;}
    private void Expire()
    {foreach(var key in entries.Where(p=>Expired(p.Value)).Select(p=>p.Key).ToArray())Remove(key);}
    private UnitImageContent? Hit(string key)
    {
        lock(sync)
        {
            if(!entries.TryGetValue(key,out var entry))return null;
            if(Expired(entry)){Remove(key);return null;}
            entries[key]=entry with{Access=++sequence};hits++;return Content(entry);
        }
    }
    private Task<UnitImageContent?> Source(string key,CancellationToken ct)
    {Interlocked.Increment(ref sourceOpens);return storage.OpenReadAsync(key,ct);}
    public PublicImageCacheSnapshot Snapshot()
    {lock(sync){Expire();return new(hits,misses,Interlocked.Read(ref sourceOpens),entries.Count,storedBytes,evictions,Interlocked.Read(ref bypasses));}}
    public async Task<UnitImageContent?> OpenReadAsync(string key,CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();if(!KeyPattern.IsMatch(key))throw new IOException("Invalid generated image storage key.");
        if(!settings.Enabled){Interlocked.Increment(ref bypasses);return await Source(key,ct);}
        var cached=Hit(key);if(cached is not null)return cached;
        lock(sync)misses++;
        await fill.WaitAsync(ct);
        try
        {
            ct.ThrowIfCancellationRequested();cached=Hit(key);if(cached is not null)return cached;
            var source=await Source(key,ct);if(source is null)return null;
            var stream=source.Stream;long length;
            try
            {
                length=stream.CanSeek?stream.Length:-1;
                if(length<=0||length>settings.MaximumEntryBytes||stream.Position!=0||source.ContentType is not ("image/png" or "image/jpeg"))
                {Interlocked.Increment(ref bypasses);return source;}
            }
            catch{await stream.DisposeAsync();throw;}
            byte[] bytes;
            await using(stream)
            {
                bytes=new byte[checked((int)length)];await stream.ReadExactlyAsync(bytes,ct);ct.ThrowIfCancellationRequested();
            }
            ct.ThrowIfCancellationRequested();
            var entry=new Entry(bytes,source.ContentType,clock.GetTimestamp(),0);
            lock(sync)
            {
                Expire();
                while(entries.Count>=settings.MaximumEntries||storedBytes+bytes.Length>settings.MaximumBytes)
                    Remove(entries.MinBy(p=>p.Value.Access).Key);
                entry=entry with{Access=++sequence};entries.Add(key,entry);storedBytes+=bytes.Length;
            }
            return Content(entry);
        }
        finally{fill.Release();}
    }
}
