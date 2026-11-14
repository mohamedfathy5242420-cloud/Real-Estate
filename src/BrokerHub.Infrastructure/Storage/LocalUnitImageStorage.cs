using System.Buffers.Binary;
using System.Text.RegularExpressions;
using BrokerHub.Application.DTOs.UnitImages;
using BrokerHub.Application.Exceptions;
using BrokerHub.Application.Interfaces.Storage;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SkiaSharp;

namespace BrokerHub.Infrastructure.Storage;

public sealed class LocalUnitImageStorage : IUnitImageStorage
{
    private static readonly SemaphoreSlim DecodeSlots = new(2, 2);
    private static readonly Regex KeyPattern = new("\\A[a-f0-9]{32}\\.(png|jpg)\\z", RegexOptions.CultureInvariant);
    private readonly string root;
    private readonly ILogger<LocalUnitImageStorage> logger;

    internal Task<bool> ProbeReadinessAsync(CancellationToken ct)=>Operations.PrivateDirectoryProbe.WritableAsync(root,ct);

    public LocalUnitImageStorage(IOptions<UnitImageStorageOptions> options, IHostEnvironment environment,
        ILogger<LocalUnitImageStorage> logger)
    {
        if (!environment.IsDevelopment()) throw new InvalidOperationException("Local image storage is development-only.");
        this.logger = logger;
        var configured = options.Value.RootPath;
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(local)) local = Path.GetTempPath();
        root = Path.TrimEndingDirectorySeparator(string.IsNullOrWhiteSpace(configured)
            ? Path.GetFullPath(Path.Combine(local, "brokerhub-unit-images"))
            : Path.GetFullPath(configured, environment.ContentRootPath));
        var webRoot = (environment as IWebHostEnvironment)?.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRoot)) webRoot = Path.Combine(environment.ContentRootPath, "wwwroot");
        if (root == Path.TrimEndingDirectorySeparator(Path.GetPathRoot(root)!) ||
            root.Equals(Path.TrimEndingDirectorySeparator(environment.ContentRootPath), StringComparison.OrdinalIgnoreCase) ||
            IsWithin(root, Path.GetFullPath(webRoot)))
            throw new InvalidOperationException("Image storage must be a dedicated private directory outside the web root.");
        CheckNoLinks(root);
    }

    public async Task<StoredUnitImage> StoreValidatedAsync(Stream content, CancellationToken cancellationToken)
    {
        await DecodeSlots.WaitAsync(cancellationToken);
        string? inputPath = null, outputPath = null;
        var inputOwned = false;
        var outputOwned = false;
        try
        {
            CheckNoLinks(root);
            Directory.CreateDirectory(root);
            CheckNoLinks(root);
            var token = Guid.NewGuid().ToString("N");
            inputPath = Path.Combine(root, token + ".upload");
            outputPath = Path.Combine(root, token + ".normalized");
            await using (var file = new FileStream(inputPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 65536, FileOptions.Asynchronous))
            {
                inputOwned = true;
                var buffer = new byte[65536];
                long count = 0;
                int read;
                while ((read = await content.ReadAsync(buffer, cancellationToken)) > 0)
                {
                    count += read;
                    if (count > UnitImageLimits.MaximumInputBytes) throw new InvalidImageException("File exceeds the 5 MiB input limit.");
                    await file.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                }
                if (count == 0) throw new InvalidImageException("Image file is empty.");
            }
            // ReadAllBytes is safe here only AFTER the bounded streaming copy above.
            var bytes = await File.ReadAllBytesAsync(inputPath, cancellationToken);
            var format = ValidateContainer(bytes);
            using var data = SKData.CreateCopy(bytes);
            using var codec = SKCodec.Create(data);
            if (codec is null || codec.EncodedFormat != format) throw new InvalidImageException("Invalid JPEG/PNG image.");
            var width = codec.Info.Width; var height = codec.Info.Height;
            if (width <= 0 || height <= 0 || width > UnitImageLimits.MaximumDimension || height > UnitImageLimits.MaximumDimension ||
                (long)width * height > UnitImageLimits.MaximumPixels)
                throw new InvalidImageException("Image exceeds 4096 per dimension or 16 million pixels.");
            cancellationToken.ThrowIfCancellationRequested();
            var info = new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
            using var bitmap = new SKBitmap(info);
            if (codec.GetPixels(info, bitmap.GetPixels()) != SKCodecResult.Success)
                throw new InvalidImageException("Image data is corrupt or incomplete.");
            using var image = SKImage.FromBitmap(bitmap);
            using var normalized = image.Encode(format, 90);
            if (normalized is null || normalized.Size <= 0 || normalized.Size > UnitImageLimits.MaximumStoredBytes)
                throw new InvalidImageException("Normalized image exceeds the 20 MiB storage limit.");
            using (var output = new FileStream(outputPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                outputOwned = true;
                normalized.SaveTo(output);
            }
            cancellationToken.ThrowIfCancellationRequested();
            var key = token + (format == SKEncodedImageFormat.Png ? ".png" : ".jpg");
            var final = SafePath(key);
            File.Move(outputPath, final, overwrite: false);
            outputOwned = false;
            outputPath = null;
            return new StoredUnitImage(key);
        }
        finally
        {
            // A failed CreateNew must never authorize deletion of a pre-existing object.
            if (inputOwned) RemoveTemporary(inputPath);
            if (outputOwned) RemoveTemporary(outputPath);
            DecodeSlots.Release();
        }
    }

    public Task<UnitImageContent?> OpenReadAsync(string key, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = SafePath(key);
        if (!File.Exists(path)) return Task.FromResult<UnitImageContent?>(null);
        CheckNoLinks(path);
        var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 65536, FileOptions.Asynchronous | FileOptions.SequentialScan);
        return Task.FromResult<UnitImageContent?>(new(stream, key.EndsWith(".png", StringComparison.Ordinal) ? "image/png" : "image/jpeg"));
    }

    public Task<bool> DiscardCreatedAsync(StoredUnitImage image, CancellationToken cancellationToken)
    {
        try { cancellationToken.ThrowIfCancellationRequested(); File.Delete(SafePath(image.Key)); return Task.FromResult(true); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or OperationCanceledException)
        {
            logger.LogWarning(e, "Compensation failed for generated image {Key}; retain for reconciliation.", image.Key);
            return Task.FromResult(false);
        }
    }

    private string SafePath(string key)
    {
        if (!KeyPattern.IsMatch(key)) throw new IOException("Invalid generated image storage key.");
        CheckNoLinks(root);
        var path = Path.Combine(root, key);
        CheckNoLinks(path);
        return path;
    }
    private static bool IsWithin(string path, string parent) => path.Equals(Path.TrimEndingDirectorySeparator(parent), StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith(Path.TrimEndingDirectorySeparator(parent) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    private static void CheckNoLinks(string path)
    {
        for (var current = path; !string.IsNullOrEmpty(current); current = Path.GetDirectoryName(current))
            if ((File.Exists(current) || Directory.Exists(current)) && (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                throw new IOException("Linked/reparse storage paths are not allowed.");
    }
    private void RemoveTemporary(string? path)
    {
        if (path is null) return;
        try { CheckNoLinks(root); CheckNoLinks(path); File.Delete(path); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        { logger.LogWarning(e, "Owned image staging cleanup failed; retain for operator reconciliation."); }
    }

    private static SKEncodedImageFormat ValidateContainer(byte[] bytes)
    {
        if (bytes.Length >= 4 && bytes[0] == 0xff && bytes[1] == 0xd8 && bytes[^2] == 0xff && bytes[^1] == 0xd9)
        {
            ValidateJpegStructure(bytes);
            return SKEncodedImageFormat.Jpeg;
        }
        ReadOnlySpan<byte> signature = [137, 80, 78, 71, 13, 10, 26, 10];
        if (bytes.Length < 33 || !bytes.AsSpan(0, 8).SequenceEqual(signature)) throw new InvalidImageException("Only complete JPEG/PNG files are supported.");
        var offset = 8; var hasData = false; var endedData = false; var chunks = 0;
        while (offset <= bytes.Length - 12)
        {
            var length = BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(offset, 4));
            if (length > bytes.Length - offset - 12) throw new InvalidImageException("Truncated PNG chunk.");
            var type = System.Text.Encoding.ASCII.GetString(bytes, offset + 4, 4);
            if (type.Any(c => c is not (>= 'A' and <= 'Z' or >= 'a' and <= 'z')) || type[2] is not (>= 'A' and <= 'Z'))
                throw new InvalidImageException("Invalid PNG chunk type.");
            if ((chunks == 0 && (type != "IHDR" || length != 13)) || (chunks > 0 && type == "IHDR") ||
                type is "acTL" or "fcTL" or "fdAT") throw new InvalidImageException("Invalid or animated PNG.");
            if (type[0] is >= 'A' and <= 'Z' && type is not ("IHDR" or "PLTE" or "IDAT" or "IEND"))
                throw new InvalidImageException("Unsupported critical PNG chunk.");
            uint crc = uint.MaxValue;
            foreach (var value in bytes.AsSpan(offset + 4, checked((int)length + 4)))
            {
                crc ^= value;
                for (var bit = 0; bit < 8; bit++) crc = (crc >> 1) ^ ((crc & 1) != 0 ? 0xedb88320u : 0);
            }
            if (~crc != BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(offset + 8 + (int)length, 4)))
                throw new InvalidImageException("PNG checksum is invalid.");
            offset += checked((int)length + 12); chunks++;
            if (type == "IDAT")
            {
                if (endedData) throw new InvalidImageException("PNG data chunks must be contiguous.");
                hasData = true;
            }
            else if (hasData) endedData = true;
            if (type == "IEND")
            {
                if (!hasData || length != 0 || offset != bytes.Length) throw new InvalidImageException("Invalid PNG end or trailing content.");
                return SKEncodedImageFormat.Png;
            }
        }
        throw new InvalidImageException("PNG end chunk is missing.");
    }

    private static void ValidateJpegStructure(byte[] bytes)
    {
        var offset = 2;
        var hasScan = false;
        while (offset < bytes.Length)
        {
            if (bytes[offset++] != 0xff) throw new InvalidImageException("Invalid JPEG marker.");
            while (offset < bytes.Length && bytes[offset] == 0xff) offset++;
            if (offset >= bytes.Length) break;
            var marker = bytes[offset++];
            if (marker == 0xd9)
            {
                if (!hasScan || offset != bytes.Length) throw new InvalidImageException("Invalid JPEG end or trailing content.");
                return;
            }
            if (marker is 0x00 or 0xd8 or >= 0xd0 and <= 0xd7)
                throw new InvalidImageException("Unexpected JPEG marker.");
            if (marker == 0x01) continue;
            if (offset > bytes.Length - 2) break;
            var length = BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(offset, 2));
            if (length < 2 || length > bytes.Length - offset) throw new InvalidImageException("Truncated JPEG segment.");
            offset += length;
            if (marker != 0xda) continue;
            hasScan = true;
            // Scan entropy until an unescaped non-restart marker; progressive JPEG may have more scans.
            while (offset < bytes.Length)
            {
                if (bytes[offset] != 0xff) { offset++; continue; }
                var start = offset++;
                while (offset < bytes.Length && bytes[offset] == 0xff) offset++;
                if (offset >= bytes.Length) break;
                var next = bytes[offset];
                if (next == 0x00 || next is >= 0xd0 and <= 0xd7) { offset++; continue; }
                offset = start;
                break;
            }
        }
        throw new InvalidImageException("JPEG end marker is missing.");
    }
}
