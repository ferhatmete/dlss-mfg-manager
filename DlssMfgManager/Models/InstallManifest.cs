namespace DlssMfgManager.Models;

public sealed class InstallManifest
{
    public int FormatVersion { get; set; } = 1;
    public DateTimeOffset LastUpdatedUtc { get; set; } = DateTimeOffset.UtcNow;
    public Dictionary<string, string> InstalledHashes { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, string> OriginalBackups { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);
}
