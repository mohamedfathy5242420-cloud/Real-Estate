using BrokerHub.Application.Abstractions.Identity;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BrokerHub.Infrastructure.Identity;

internal sealed class DevelopmentAccountEmailSender(
    ILogger<DevelopmentAccountEmailSender> logger,IOptions<DevelopmentIdentityCaptureOptions> options,IHostEnvironment environment) : IAccountEmailSender
{
    private readonly SemaphoreSlim gate=new(1,1);
    public Task SendConfirmationLinkAsync(
        string email,
        string confirmationLink,
        CancellationToken cancellationToken)
    {
        return Capture("confirmation",email,confirmationLink,cancellationToken);
    }

    public Task SendPasswordResetLinkAsync(
        string email,
        string resetLink,
        CancellationToken cancellationToken)
    {
        return Capture("password-reset",email,resetLink,cancellationToken);
    }

    private async Task Capture(string kind,string recipient,string link,CancellationToken ct)
    {
        var setting=options.Value;
        if(!environment.IsDevelopment()||!setting.Enabled||!setting.IsValid||
            !setting.AllowedRecipients.Contains(recipient,StringComparer.OrdinalIgnoreCase)||link.Length>8192)
            throw new InvalidOperationException("Owned Development identity capture is not configured for this request.");
        var root=Path.GetFullPath(setting.RootPath);var content=Path.GetFullPath(environment.ContentRootPath).TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar;
        if(root.StartsWith(content,StringComparison.OrdinalIgnoreCase)||root.Equals(content.TrimEnd(Path.DirectorySeparatorChar),StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Identity capture requires a private directory outside API content.");
        await gate.WaitAsync(ct);
        try
        {
            Directory.CreateDirectory(root);
            if((File.GetAttributes(root)&FileAttributes.ReparsePoint)!=0||Directory.EnumerateFileSystemEntries(root).Take(setting.MaximumRecords).Count()>=setting.MaximumRecords)
                throw new IOException("Owned identity capture capacity or directory is unavailable.");
            var path=Path.Combine(root,Guid.NewGuid().ToString("N")+".json");var created=false;
            try
            {
                await using(var file=new FileStream(path,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.Asynchronous|FileOptions.WriteThrough))
                {created=true;await JsonSerializer.SerializeAsync(file,new{Kind=kind,Recipient=recipient,Link=link},cancellationToken:ct);await file.FlushAsync(ct);}
            }
            catch{if(created)File.Delete(path);throw;}
            logger.LogInformation("An owned local identity link was captured; no email was sent.");
        }
        finally{gate.Release();}
    }
}
