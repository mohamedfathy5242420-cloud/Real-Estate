namespace BrokerHub.Infrastructure.Caching;

public sealed class PublicImageCacheOptions
{
    public const string SectionName="PublicImageCache";
    public bool Enabled { get; set; }=true;
    public long MaximumBytes { get; set; }=8*1024*1024;
    public int MaximumEntries { get; set; }=32;
    public int MaximumEntryBytes { get; set; }=1024*1024;
    public int LifetimeSeconds { get; set; }=60;
    public bool IsValid=>MaximumBytes is >=64*1024 and <=32*1024*1024&&MaximumEntries is >=1 and <=128&&
        MaximumEntryBytes is >=1024 and <=1024*1024&&MaximumEntryBytes<=MaximumBytes&&LifetimeSeconds is >=1 and <=300;
}
