using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DlssMfgManager.Models;

namespace DlssMfgManager.Services;

public sealed record GameArtworkResult(string LocalPath, int? SteamAppId, string SourceUrl);

public sealed class GameArtworkService : IDisposable
{
    public const string LocalCoverSource = "Local generated cover";
    private const int MaximumDownloadBytes = 10 * 1024 * 1024;
    private readonly HttpClient _httpClient;
    private readonly string _cacheDirectory;

    public GameArtworkService(string dataDirectory, HttpMessageHandler? messageHandler = null)
    {
        _cacheDirectory = Path.Combine(dataDirectory, "ArtworkCache");
        _httpClient = messageHandler is null ? new HttpClient() : new HttpClient(messageHandler);
        _httpClient.Timeout = TimeSpan.FromSeconds(12);
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("DlssMfgManager/1.9.2 (+https://github.com/ferhatmete/dlss-mfg-manager)");
    }

    public async Task<GameArtworkResult?> GetArtworkAsync(
        GameEntry game,
        bool forceRefresh,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_cacheDirectory);
        var steamAppId = game.SteamAppId ?? await FindSteamAppIdAsync(game, cancellationToken);
        if (steamAppId is null)
            return CreateLocalCover(game, forceRefresh);

        var cachePath = Path.Combine(_cacheDirectory, $"steam_{steamAppId.Value}_hero.jpg");
        var legacyCachePath = Path.Combine(_cacheDirectory, $"steam_{steamAppId.Value}.jpg");
        if (!forceRefresh && IsValidCachedImage(cachePath))
            return new GameArtworkResult(cachePath, steamAppId.Value, "Steam cache");

        try
        {
            var imageUrls = await FindArtworkImageUrlsAsync(steamAppId.Value, cancellationToken);
            foreach (var imageUrl in imageUrls)
            {
                try
                {
                    if (await DownloadArtworkAsync(imageUrl, cachePath, cancellationToken))
                        return new GameArtworkResult(cachePath, steamAppId.Value, imageUrl);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch
                {
                    // Steam sometimes exposes an unavailable background URL.
                    // Continue with the next background/header candidate.
                }
            }

            return LegacyArtworkOrNull(legacyCachePath, steamAppId.Value) ?? CreateLocalCover(game, forceRefresh);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch when (IsValidCachedImage(legacyCachePath))
        {
            return new GameArtworkResult(legacyCachePath, steamAppId.Value, "Legacy Steam cache");
        }
        catch
        {
            return CreateLocalCover(game, forceRefresh);
        }
    }

    public void Dispose() => _httpClient.Dispose();

    public string? GetCachedArtworkPath(GameEntry game)
    {
        if (game.SteamAppId is { } appId)
        {
            var heroPath = Path.Combine(_cacheDirectory, $"steam_{appId}_hero.jpg");
            if (IsValidCachedImage(heroPath))
                return heroPath;

            var legacyPath = Path.Combine(_cacheDirectory, $"steam_{appId}.jpg");
            if (IsValidCachedImage(legacyPath))
                return legacyPath;
        }

        var localPath = GetLocalCoverPath(game);
        return IsValidCachedImage(localPath) ? localPath : null;
    }

    public bool NeedsOnlineArtwork(GameEntry game)
    {
        if (game.SteamAppId is not { } appId)
            return true;

        return !IsValidCachedImage(Path.Combine(_cacheDirectory, $"steam_{appId}_hero.jpg")) &&
               !IsValidCachedImage(Path.Combine(_cacheDirectory, $"steam_{appId}.jpg"));
    }

    private async Task<int?> FindSteamAppIdAsync(GameEntry game, CancellationToken cancellationToken)
    {
        foreach (var searchName in GetSearchNames(game))
        {
            try
            {
                var match = await FindSteamAppIdByNameAsync(searchName, cancellationToken);
                if (match is not null)
                    return match;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                // A connection/store failure affects every alias in this pass.
                // Stop immediately so the local cover can appear without delay.
                return null;
            }
        }

        return null;
    }

    private async Task<int?> FindSteamAppIdByNameAsync(string gameName, CancellationToken cancellationToken)
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

    private async Task<IReadOnlyList<string>> FindArtworkImageUrlsAsync(int steamAppId, CancellationToken cancellationToken)
    {
        var results = new List<string>();
        try
        {
            var url = $"https://store.steampowered.com/api/appdetails?appids={steamAppId}&l=english";
            using var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            if (document.RootElement.TryGetProperty(steamAppId.ToString(), out var app) &&
                app.TryGetProperty("success", out var success) && success.GetBoolean() &&
                app.TryGetProperty("data", out var data))
            {
                foreach (var propertyName in new[] { "background_raw", "background", "header_image" })
                {
                    if (!data.TryGetProperty(propertyName, out var imageProperty))
                        continue;
                    var imageUrl = imageProperty.GetString();
                    if (IsAllowedSteamImageUrl(imageUrl))
                        results.Add(imageUrl!);
                }
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            // The public store endpoint can occasionally reject or omit an app.
            // Continue with the same official Steam CDN asset paths SteamDB links to.
        }

        results.AddRange(SteamCdnFallbackUrls(steamAppId));

        return results.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private static IEnumerable<string> SteamCdnFallbackUrls(int steamAppId)
    {
        // SteamDB does not provide a public API and explicitly disallows crawling.
        // It states that its image assets are linked from Steam's CDN, so this
        // fallback probes those official CDN paths directly instead of scraping it.
        var roots = new[]
        {
            $"https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/{steamAppId}",
            $"https://shared.akamai.steamstatic.com/steam/apps/{steamAppId}",
            $"https://cdn.akamai.steamstatic.com/steam/apps/{steamAppId}"
        };
        foreach (var root in roots)
        {
            yield return $"{root}/page_bg_raw.jpg";
            yield return $"{root}/page_bg_generated_v6b.jpg";
            yield return $"{root}/library_hero.jpg";
            yield return $"{root}/capsule_616x353.jpg";
            yield return $"{root}/header.jpg";
        }
    }

    private static IEnumerable<string> GetSearchNames(GameEntry game)
    {
        var names = new List<string> { game.Name };
        if (File.Exists(game.ExecutablePath))
        {
            try
            {
                var version = FileVersionInfo.GetVersionInfo(game.ExecutablePath);
                names.Add(version.ProductName ?? string.Empty);
                names.Add(version.FileDescription ?? string.Empty);
            }
            catch
            {
                // Some protected executables do not expose version metadata.
            }
        }

        names.Add(Path.GetFileNameWithoutExtension(game.ExecutablePath));
        var directory = Path.GetDirectoryName(game.ExecutablePath);
        for (var level = 0; level < 4 && !string.IsNullOrWhiteSpace(directory); level++)
        {
            names.Add(Path.GetFileName(directory));
            directory = Path.GetDirectoryName(directory);
        }

        var ignored = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "bin", "binaries", "game", "games", "content", "x64", "x86", "win64", "win32"
        };
        return names
            .Select(CleanSearchName)
            .Where(name => name.Length >= 3 && !ignored.Contains(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(5);
    }

    private static string CleanSearchName(string value)
    {
        var cleaned = value
            .Replace("-Win64-Shipping", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("Win64-Shipping", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("_x64", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("-x64", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace(".exe", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace('_', ' ')
            .Trim(' ', '-', '.');
        return cleaned;
    }

    private GameArtworkResult CreateLocalCover(GameEntry game, bool forceRefresh)
    {
        var path = GetLocalCoverPath(game);
        if (!forceRefresh && IsValidCachedImage(path))
            return new GameArtworkResult(path, game.SteamAppId, LocalCoverSource);

        using var bitmap = new Bitmap(1280, 720);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;

        var seed = SHA256.HashData(Encoding.UTF8.GetBytes($"{game.Name}|{game.ExecutablePath}"));
        var first = Color.FromArgb(255, 48 + seed[0] % 62, 61 + seed[1] % 70, 132 + seed[2] % 90);
        var second = Color.FromArgb(255, 36 + seed[3] % 55, 132 + seed[4] % 78, 128 + seed[5] % 94);
        using (var gradient = new LinearGradientBrush(new Rectangle(0, 0, bitmap.Width, bitmap.Height), first, second, 24F))
            graphics.FillRectangle(gradient, 0, 0, bitmap.Width, bitmap.Height);

        using (var veil = new SolidBrush(Color.FromArgb(72, 5, 10, 34)))
            graphics.FillRectangle(veil, 0, 0, bitmap.Width, bitmap.Height);
        using (var glow = new SolidBrush(Color.FromArgb(42, 255, 255, 255)))
        {
            graphics.FillEllipse(glow, 840, -170, 600, 600);
            graphics.FillEllipse(glow, -220, 430, 520, 520);
        }

        DrawExecutableIcon(graphics, game.ExecutablePath);

        using var captionFont = new Font("Segoe UI Semibold", 17F, FontStyle.Bold, GraphicsUnit.Point);
        using var titleFont = new Font("Segoe UI Semibold", game.Name.Length > 32 ? 42F : 52F,
            FontStyle.Bold, GraphicsUnit.Point);
        using var captionBrush = new SolidBrush(Color.FromArgb(205, 224, 235, 255));
        using var titleBrush = new SolidBrush(Color.White);
        graphics.DrawString("LOCAL GAME", captionFont, captionBrush, new PointF(70, 445));
        using var format = new StringFormat { Trimming = StringTrimming.EllipsisWord };
        graphics.DrawString(game.Name, titleFont, titleBrush, new RectangleF(64, 485, 820, 170), format);

        var temporaryPath = path + ".tmp";
        SaveJpeg(bitmap, temporaryPath);
        File.Move(temporaryPath, path, true);
        return new GameArtworkResult(path, game.SteamAppId, LocalCoverSource);
    }

    private static void DrawExecutableIcon(Graphics graphics, string executablePath)
    {
        try
        {
            if (!File.Exists(executablePath))
                return;
            using var icon = Icon.ExtractAssociatedIcon(executablePath);
            if (icon is null)
                return;
            using var image = icon.ToBitmap();
            using var shadow = new SolidBrush(Color.FromArgb(55, 0, 0, 0));
            using var tile = new SolidBrush(Color.FromArgb(145, 255, 255, 255));
            graphics.FillEllipse(shadow, 922, 152, 246, 246);
            graphics.FillEllipse(tile, 910, 140, 246, 246);
            graphics.DrawImage(image, new Rectangle(951, 181, 164, 164));
        }
        catch
        {
            // A generated gradient and title still provide a useful game card.
        }
    }

    private string GetLocalCoverPath(GameEntry game)
    {
        var identity = $"{NormalizeName(game.Name)}|{game.ExecutablePath.Trim().ToLowerInvariant()}";
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identity)))[..16];
        return Path.Combine(_cacheDirectory, $"local_{hash}_hero.jpg");
    }

    private async Task<bool> DownloadArtworkAsync(string imageUrl, string cachePath, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(imageUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        if (response.Content.Headers.ContentLength is > MaximumDownloadBytes)
            return false;
        if (response.Content.Headers.ContentType?.MediaType?.StartsWith("image/", StringComparison.OrdinalIgnoreCase) != true)
            return false;

        var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        if (bytes.Length is < 1024 or > MaximumDownloadBytes)
            return false;

        using var memory = new MemoryStream(bytes);
        using var source = Image.FromStream(memory, useEmbeddedColorManagement: false, validateImageData: true);
        if (source.Width < 300 || source.Height < 120)
            return false;

        using var bitmap = new Bitmap(source);
        var temporaryPath = cachePath + ".tmp";
        SaveJpeg(bitmap, temporaryPath);
        File.Move(temporaryPath, cachePath, true);
        return true;
    }

    private static GameArtworkResult? LegacyArtworkOrNull(string path, int steamAppId) =>
        IsValidCachedImage(path) ? new GameArtworkResult(path, steamAppId, "Legacy Steam cache") : null;

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
