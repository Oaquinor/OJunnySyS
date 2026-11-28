using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components.Forms;
using OJunnySyS.Application.Abstractions;
using OJunnySyS.Domain.Common;

namespace OJunnySyS.Web.Identity;

public sealed class ProductImageStorage(IWebHostEnvironment environment, ITenantContext tenantContext)
{
    public const long MaxImageBytes = 5 * 1024 * 1024;
    private static readonly Regex FileNamePattern = new("^[a-fA-F0-9]{32}\\.(?:jpg|png|webp)$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public async Task<string> SaveAsync(IBrowserFile upload, CancellationToken cancellationToken = default)
    {
        if (upload.Size is <= 0 or > MaxImageBytes)
            throw new DomainException("La imagen debe tener un tamaño máximo de 5 MB.");

        await using var source = upload.OpenReadStream(MaxImageBytes, cancellationToken);
        using var buffer = new MemoryStream((int)upload.Size);
        await source.CopyToAsync(buffer, cancellationToken);
        var bytes = buffer.ToArray();
        var extension = DetectExtension(bytes);
        if (extension is null)
            throw new DomainException("El archivo debe ser una imagen PNG, JPEG o WebP válida.");

        var fileName = Guid.NewGuid().ToString("N") + extension;
        var directory = GetTenantDirectory();
        Directory.CreateDirectory(directory);
        await File.WriteAllBytesAsync(Path.Combine(directory, fileName), bytes, cancellationToken);
        return $"/media/products/{fileName}";
    }

    public bool TryResolve(string fileName, out string path, out string contentType)
    {
        path = string.Empty;
        contentType = string.Empty;
        if (!FileNamePattern.IsMatch(fileName)) return false;
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        contentType = extension switch { ".jpg" => "image/jpeg", ".png" => "image/png", ".webp" => "image/webp", _ => string.Empty };
        path = Path.Combine(GetTenantDirectory(), fileName);
        return File.Exists(path);
    }

    public Task DeleteAsync(string? imageUrl, CancellationToken cancellationToken = default)
    {
        const string prefix = "/media/products/";
        if (imageUrl is not null && imageUrl.StartsWith(prefix, StringComparison.Ordinal) &&
            TryResolve(imageUrl[prefix.Length..], out var path, out _))
        {
            try { File.Delete(path); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
        return Task.CompletedTask;
    }

    private string GetTenantDirectory()
    {
        var webRoot = environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot");
        return Path.Combine(webRoot, "uploads", "products", tenantContext.GetRequiredTenantId().ToString("N"));
    }

    private static string? DetectExtension(ReadOnlySpan<byte> bytes)
    {
        ReadOnlySpan<byte> pngSignature = [137, 80, 78, 71, 13, 10, 26, 10];
        if (bytes.Length >= pngSignature.Length && bytes[..pngSignature.Length].SequenceEqual(pngSignature)) return ".png";
        if (bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF) return ".jpg";
        if (bytes.Length >= 12 && Encoding.ASCII.GetString(bytes[..4]) == "RIFF" && Encoding.ASCII.GetString(bytes.Slice(8, 4)) == "WEBP") return ".webp";
        return null;
    }
}
