namespace BrokerHub.Infrastructure.Operations;

internal static class PrivateDirectoryProbe
{
    internal static bool ExistsWithoutLinks(string path)
    {
        if(!Path.IsPathFullyQualified(path)||!Directory.Exists(path))return false;
        var full=Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
        if(full==Path.TrimEndingDirectorySeparator(Path.GetPathRoot(full)!))return false;
        for(var current=full;!string.IsNullOrEmpty(current);current=Path.GetDirectoryName(current))
            if((Directory.Exists(current)||File.Exists(current))&&(File.GetAttributes(current)&FileAttributes.ReparsePoint)!=0)return false;
        return true;
    }
    internal static async Task<bool> WritableAsync(string path,CancellationToken ct)
    {
        if(!ExistsWithoutLinks(path))return false;
        var root=Path.GetFullPath(path);var name=".readiness-"+Guid.NewGuid().ToString("N")+".probe";
        var target=Path.GetFullPath(Path.Combine(root,name));
        if(Path.GetDirectoryName(target)!=Path.TrimEndingDirectorySeparator(root))return false;
        // CreateNew plus DeleteOnClose owns only this one generated probe; never an image/key.
        await using var file=new FileStream(target,FileMode.CreateNew,FileAccess.Write,FileShare.None,1,FileOptions.Asynchronous|FileOptions.DeleteOnClose);
        await file.WriteAsync(new byte[]{0},ct);await file.FlushAsync(ct);return true;
    }
}
