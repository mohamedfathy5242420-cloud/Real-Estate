using System.Security.Cryptography;
using BrokerHub.Application.DTOs.Notifications;
using BrokerHub.Application.Exceptions;
using BrokerHub.Application.Interfaces.Storage;
using BrokerHub.Application.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace BrokerHub.Infrastructure.Storage;

internal sealed class LocalNotificationArtifactStore(IOptions<LocalNotificationCaptureOptions> options,IHostEnvironment environment):ILocalNotificationArtifactStore
{
    private string Root()
    {
        var o=options.Value;if(!environment.IsDevelopment()||!o.Enabled||!o.IsValid)throw new InvalidOperationException("Owned Development capture is not configured.");
        var root=Path.TrimEndingDirectorySeparator(Path.GetFullPath(o.RootPath));
        var content=Path.TrimEndingDirectorySeparator(Path.GetFullPath(environment.ContentRootPath));
        var web=(environment as IWebHostEnvironment)?.WebRootPath;if(string.IsNullOrWhiteSpace(web))web=Path.Combine(content,"wwwroot");
        var webRoot=Path.TrimEndingDirectorySeparator(Path.GetFullPath(web));
        if(root==Path.TrimEndingDirectorySeparator(Path.GetPathRoot(root)!)||root.Equals(content,StringComparison.OrdinalIgnoreCase)||
            root.Equals(webRoot,StringComparison.OrdinalIgnoreCase)||root.StartsWith(webRoot+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))
            throw new LocalNotificationArtifactConflictException();
        NoLinks(root);return root;
    }
    private static void NoLinks(string path)
    {
        for(string? at=Path.GetFullPath(path);at is not null;at=Path.GetDirectoryName(at))
        {
            try{if((File.GetAttributes(at)&FileAttributes.ReparsePoint)!=0)throw new LocalNotificationArtifactConflictException();}
            catch(FileNotFoundException){}catch(DirectoryNotFoundException){}
        }
    }
    private static void Validate(LocalNotificationArtifact artifact)
    {
        if(artifact.NotificationId==Guid.Empty||artifact.Bytes.Length is <1 or >LocalNotificationCaptureContent.MaxBytes||
            Convert.ToHexString(SHA256.HashData(artifact.Bytes))!=artifact.ContentHash)throw new ArgumentException("Exact bounded owned artifact required.");
    }
    private static string Target(string root,Guid id)
    {
        var path=Path.GetFullPath(Path.Combine(root,id.ToString("N")+".eml"));
        if(!path.StartsWith(root+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new LocalNotificationArtifactConflictException();
        NoLinks(path);return path;
    }
    private static async Task<bool> Existing(string path,LocalNotificationArtifact artifact,CancellationToken ct)
    {
        NoLinks(path);if(Directory.Exists(path))throw new LocalNotificationArtifactConflictException();if(!File.Exists(path))return false;
        await using var stream=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read,4096,FileOptions.Asynchronous);
        if(stream.Length!=artifact.Bytes.Length||stream.Length>LocalNotificationCaptureContent.MaxBytes)throw new LocalNotificationArtifactConflictException();
        var bytes=new byte[artifact.Bytes.Length];await stream.ReadExactlyAsync(bytes,ct);
        if(!bytes.AsSpan().SequenceEqual(artifact.Bytes)||Convert.ToHexString(SHA256.HashData(bytes))!=artifact.ContentHash)throw new LocalNotificationArtifactConflictException();return true;
    }
    public async Task<bool> VerifyAsync(LocalNotificationArtifact artifact,CancellationToken ct)
    {Validate(artifact);var root=Root();return await Existing(Target(root,artifact.NotificationId),artifact,ct);}
    public async Task EnsureAsync(LocalNotificationArtifact artifact,CancellationToken ct)
    {
        Validate(artifact);ct.ThrowIfCancellationRequested();var root=Root();Directory.CreateDirectory(root);NoLinks(root);
        var target=Target(root,artifact.NotificationId);if(await Existing(target,artifact,ct))return;
        var temporary=Path.Combine(root,"."+artifact.NotificationId.ToString("N")+"-"+Guid.NewGuid().ToString("N")+".tmp");var owned=false;
        try
        {
            await using(var stream=new FileStream(temporary,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.Asynchronous|FileOptions.WriteThrough))
            {owned=true;await stream.WriteAsync(artifact.Bytes,ct);await stream.FlushAsync(ct);stream.Flush(flushToDisk:true);}
            ct.ThrowIfCancellationRequested();NoLinks(root);NoLinks(target);
            try{File.Move(temporary,target,overwrite:false);owned=false;}
            catch(IOException)when(File.Exists(target)){if(!await Existing(target,artifact,ct))throw;}
            if(!await Existing(target,artifact,ct))throw new IOException("Owned capture was not durably published.");
        }
        finally{if(owned&&File.Exists(temporary)){NoLinks(temporary);File.Delete(temporary);}}
    }
}
