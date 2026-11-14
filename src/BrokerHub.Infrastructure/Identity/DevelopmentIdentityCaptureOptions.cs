namespace BrokerHub.Infrastructure.Identity;

public sealed class DevelopmentIdentityCaptureOptions
{
    public const string SectionName="DevelopmentIdentityCapture";
    public bool Enabled { get; init; }
    public string RootPath { get; init; }="";
    public string[] AllowedRecipients { get; init; }=[];
    public int MaximumRecords { get; init; }=256;
    public bool IsValid=>!Enabled||Path.IsPathFullyQualified(RootPath)&&RootPath.Length<=1024&&
        MaximumRecords is >=1 and <=10000&&AllowedRecipients is {Length:>0 and <=16}&&
        AllowedRecipients.Distinct(StringComparer.OrdinalIgnoreCase).Count()==AllowedRecipients.Length&&
        AllowedRecipients.All(x=>x.Length<=254&&x.Contains('@')&&!x.Any(char.IsWhiteSpace)&&
            (x.EndsWith(".invalid",StringComparison.OrdinalIgnoreCase)||x.EndsWith(".test",StringComparison.OrdinalIgnoreCase)));
}
