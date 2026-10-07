using System.Drawing.Imaging;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DlssMfgManager.Models;

namespace DlssMfgManager.Services;

public sealed record GameArtworkResult(string LocalPath, int SteamAppId, string SourceUrl);

public sealed class GameArtworkService : IDisposable
{
    private const int MaximumDownloadBytes = 10 * 1024 * 1024;
    private readonly HttpClient _httpClient;
    private readonly string _cacheDirectory;

    public GameArtworkService(string dataDirectory, HttpMessageHandler? messageHandler = null)
    {
        _cacheDirectory = Path.Combine(dataDirectory, "ArtworkCache");
        _httpClient = messageHandler is null ? new HttpClient() : new HttpClient(messageHandler);
        _httpClient.Timeout = TimeSpan.FromSeconds(12);
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("DlssMfgManager/1.3 (+https://github.com/ferhatmete/dlss-mfg-manager)");
    }

    public async Task<GameArtworkResult?> GetArtworkAsync(
        GameEntry game,
        bool forceRefresh,
        CancellationToken cancellationToken)
    {
        var steamAppId = game.SteamAppId ?? await FindSteamAppIdAsync(game.Name, cancellationToken);
        if (steamAppId is null)
            return null;

        Directory.CreateDirectory(_cacheDirectory);
        var cachePath = Path.Combine(_cacheDirectory, $"steam_{steamAppId.Value}.jpg");
        if (!forceRefresh && IsValidCachedImage(cachePath))
            return new GameArtworkResult(cachePath, steamAppId.Value, "Steam cache");

        var imageUrl = await FindHeaderImageUrlAsync(steamAppId.Value, cancellationToken);
        if (imageUrl is null)
            return null;

        using var response = await _httpClient.GetAsync(imageUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        if (response.Content.Headers.ContentLength is > MaximumDownloadBytes)
            return null;
        if (response.Content.Headers.ContentType?.MediaType?.StartsWith("image/", StringComparison.OrdinalIgnoreCase) != true)
            return null;

        var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        if (bytes.Length is < 1024 or > MaximumDownloadBytes)
            return null;

        using var memory = new MemoryStream(bytes);
        using var source = Image.FromStream(memory, useEmbeddedColorManagement: false, validateImageData: true);
        if (source.Width < 300 || source.Height < 120)
            return null;

        using var bitmap = new Bitmap(source);
        var temporaryPath = cachePath + ".tmp";
        SaveJpeg(bitmap, temporaryPath);
        File.Move(temporaryPath, cachePath, true);
        return new GameArtworkResult(cachePath, steamAppId.Value, imageUrl);
    }

    public void Dispose() => _httpClient.Dispose();

    private async Task<int?> FindSteamAppIdAsync(string gameName, CancellationToken cancellationToken)
    {
        var query = Uri.EscapeDataString(gameName.Trim());
        if (query.Length == 0)
            return null;

        var url = $"https://store.steampowered.com/api/storesearch/?term={query}&l=english&cc=US";
        using var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        if (!document.RootElement.TryGetProperty("items", out var items) || items.ValueKind != JsonValueKind.Array)
            return null;

        var normalizedTarget = NormalizeName(gameName);
        return items.EnumerateArray()
            .Select(item => new
            {
                Id = item.TryGetProperty("id", out var id) && id.TryGetInt32(out var parsedId) ? parsedId : 0,
                Name = item.TryGetProperty("name", out var name) ? name.GetString() ?? string.Empty : string.Empty
            })
            .Where(item => item.Id > 0)
            .Select(item => new { item.Id, Score = MatchScore(normalizedTarget, NormalizeName(item.Name)) })
            .Where(item => item.Score >= 60)
            .OrderByDescending(item => item.Score)
            .Select(item => (int?)item.Id)
            .FirstOrDefault();
    }

    private async Task<string?> FindHeaderImageUrlAsync(int steamAppId, CancellationToken cancellationToken)
    {
        var url = $"https://store.steampowered.com/api/appdetails?appids={steamAppId}&l=english";
        using var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        if (!document.RootElement.TryGetProperty(steamAppId.ToString(), out var app) ||
            !app.TryGetProperty("success", out var success) || !success.GetBoolean() ||
            !app.TryGetProperty("data", out var data) ||
            !data.TryGetProperty("header_image", out var headerImage))
            return null;

        var imageUrl = headerImage.GetString();
        return IsAllowedSteamImageUrl(imageUrl) ? imageUrl : null;
    }

    private static bool IsAllowedSteamImageUrl(string? value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
        uri.Scheme == Uri.UriSchemeHttps &&
        (uri.Host.Equals("steamstatic.com", StringComparison.OrdinalIgnoreCase) ||
         uri.Host.EndsWith(".steamstatic.com", StringComparison.OrdinalIgnoreCase));

    private static bool IsValidCachedImage(string path)
    {
        if (!File.Exists(path))
            return false;
        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var image = Image.FromStream(stream);
            return image.Width >= 300 && image.Height >= 120;
        }
        catch
        {
            return false;
        }
    }

    private static int MatchScore(string target, string candidate)
    {
        if (target == candidate) return 100;
        if (target.Length >= 5 && (target.Contains(candidate, StringComparison.OrdinalIgnoreCase) ||
                                  candidate.Contains(target, StringComparison.OrdinalIgnoreCase))) return 78;
        return 0;
    }

    private static string NormalizeName(string value) =>
        new(value.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());

    private static void SaveJpeg(Image image, string path)
    {
        var encoder = ImageCodecInfo.GetImageEncoders().First(codec => codec.FormatID == ImageFormat.Jpeg.Guid);
        using var parameters = new EncoderParameters(1);
        parameters.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 88L);
        image.Save(path, encoder, parameters);
    }
}
