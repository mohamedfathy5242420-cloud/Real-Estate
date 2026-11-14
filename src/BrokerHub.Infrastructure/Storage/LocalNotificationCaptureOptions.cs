namespace BrokerHub.Infrastructure.Storage;

public sealed class LocalNotificationCaptureOptions
{
    public const string SectionName="LocalNotificationCapture";
    public bool Enabled { get; set; }
    public string RootPath { get; set; }="";
    public Guid[] AllowedAccountIds { get; set; }=[];
    public bool IsValid=>!Enabled||!string.IsNullOrWhiteSpace(RootPath)&&RootPath.Length<=1024&&Path.IsPathFullyQualified(RootPath)&&
        !RootPath.StartsWith("\\\\",StringComparison.Ordinal)&&AllowedAccountIds is not null&&AllowedAccountIds.Length<=128&&
        AllowedAccountIds.All(x=>x!=Guid.Empty)&&AllowedAccountIds.Distinct().Count()==AllowedAccountIds.Length;
}
