namespace BrokerHub.Api.Configuration;

public sealed class ApiSecurityOptions
{
    public const string SectionName="ApiSecurity";
    public string[] FrontendOrigins { get; init; }=[];
    public const long MaximumJsonBytes=262144;
    public const long MaximumApiBytes=6*1024*1024;
    public bool IsValid(bool development)=>FrontendOrigins is {Length:>0 and <=16}&&
        FrontendOrigins.Distinct(StringComparer.Ordinal).Count()==FrontendOrigins.Length&&
        FrontendOrigins.All(value=>value.Length<=512&&Uri.TryCreate(value,UriKind.Absolute,out var origin)&&
            (origin.Scheme==Uri.UriSchemeHttps||development&&origin.Scheme==Uri.UriSchemeHttp&&origin.IsLoopback)&&
            origin.UserInfo.Length==0&&origin.Query.Length==0&&origin.Fragment.Length==0&&
            value==origin.GetLeftPart(UriPartial.Authority)&&!value.Contains('*'));
}
