using System.Diagnostics;
using System.Text.Json;
using DlssMfgManager.Models;

namespace DlssMfgManager.Services;

public enum InstallState
{
    NotInstalled,
    Installed,
    NeedsRepair,
    SourceUnavailable,
    Error
}

public sealed record InspectionResult(InstallState State, string Summary, string Details = "");

public sealed class InstallationService
{
    public const string ProxyFileName = "version.dll";
    public const string IniFileName = "dlssg_sm86.ini";
    private const string ManifestFileName = ".dlss-mfg-manager.json";
    private const string BackupDirectoryName = ".dlss-mfg-manager-backups";

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public InspectionResult Inspect(GameEntry game, string sourceFolder)
    {
        try
        {
            var gameDirectory = GetGameDirectory(game);
            var destinationDll = Path.Combine(gameDirectory, ProxyFileName);
            var destinationIni = Path.Combine(gameDirectory, IniFileName);

            if (!File.Exists(destinationDll) || !File.Exists(destinationIni))
                return new InspectionResult(InstallState.NotInstalled,
                    L("Kurulu değil", "Not installed"),
                    L("Gerekli dosyalardan biri eksik.", "One or more required files are missing."));

            var desiredValue = MultiplierToIniValue(game.Multiplier);
            if (IniEditor.ReadMaxGeneratedFrames(destinationIni) != desiredValue)
                return new InspectionResult(InstallState.NeedsRepair,
                    L("Ayar güncellenmeli", "Setting must be updated"),
                    L($"Beklenen MFG ayarı: {game.Multiplier}X.", $"Expected MFG setting: {game.Multiplier}X."));

            if (!TryValidateSource(sourceFolder, out var sourceError))
                return new InspectionResult(InstallState.SourceUnavailable,
                    L("Kurulu · dosyalar bulunamadı", "Installed · source files unavailable"), sourceError);

            var sourceDll = Path.Combine(sourceFolder, ProxyFileName);
            if (!HashUtility.Sha256(sourceDll).Equals(HashUtility.Sha256(destinationDll), StringComparison.OrdinalIgnoreCase))
                return new InspectionResult(InstallState.NeedsRepair,
                    L("Onarım gerekli", "Repair required"),
                    L("version.dll seçilen Frame Gen dosyasıyla eşleşmiyor.",
                        "version.dll does not match the selected Frame Gen file."));

            return new InspectionResult(InstallState.Installed,
                L("Hazır", "Ready"),
                L("Dosyalar ve MFG ayarı doğrulandı.", "The files and MFG setting were verified."));
        }
        catch (Exception ex)
        {
            return new InspectionResult(InstallState.Error, L("Kontrol edilemedi", "Could not verify"), ex.Message);
        }
    }

    public string Install(GameEntry game, string sourceFolder)
    {
        ValidateSourceOrThrow(sourceFolder);
        var gameDirectory = GetGameDirectory(game);
        EnsureSourceIsSeparate(gameDirectory, sourceFolder);
        var manifest = LoadManifest(gameDirectory);
        var backupDirectory = CreateBackupDirectory(gameDirectory);

        CopyManagedFile(Path.Combine(sourceFolder, ProxyFileName), gameDirectory, manifest, backupDirectory, true);
        CopyManagedFile(Path.Combine(sourceFolder, IniFileName), gameDirectory, manifest, backupDirectory, true);
        ApplyIniSetting(game, gameDirectory, manifest, backupDirectory);
        SaveManifest(gameDirectory, manifest);

        return L($"Kurulum tamamlandı. MFG üst sınırı {game.Multiplier}X olarak ayarlandı.",
            $"Installation complete. The MFG maximum was set to {game.Multiplier}X.");
    }

    public string Repair(GameEntry game, string sourceFolder)
    {
        ValidateSourceOrThrow(sourceFolder);
        var gameDirectory = GetGameDirectory(game);
        EnsureSourceIsSeparate(gameDirectory, sourceFolder);
        var manifest = LoadManifest(gameDirectory);
        var backupDirectory = CreateBackupDirectory(gameDirectory);
        var changed = false;

        var sourceDll = Path.Combine(sourceFolder, ProxyFileName);
        var destinationDll = Path.Combine(gameDirectory, ProxyFileName);
        if (!File.Exists(destinationDll) ||
            !HashUtility.Sha256(sourceDll).Equals(HashUtility.Sha256(destinationDll), StringComparison.OrdinalIgnoreCase))
        {
            CopyManagedFile(sourceDll, gameDirectory, manifest, backupDirectory, true);
            changed = true;
        }

        var destinationIni = Path.Combine(gameDirectory, IniFileName);
        if (!File.Exists(destinationIni))
        {
            CopyManagedFile(Path.Combine(sourceFolder, IniFileName), gameDirectory, manifest, backupDirectory, true);
            changed = true;
        }

        if (IniEditor.ReadMaxGeneratedFrames(destinationIni) != MultiplierToIniValue(game.Multiplier))
        {
            ApplyIniSetting(game, gameDirectory, manifest, backupDirectory);
            changed = true;
        }

        RefreshManifestHashes(gameDirectory, manifest);
        SaveManifest(gameDirectory, manifest);
        return changed
            ? L("Eksik veya farklı dosyalar onarıldı.", "Missing or mismatched files were repaired.")
            : L("Kurulum zaten güncel.", "The installation is already up to date.");
    }

    public string RestoreOrRemove(GameEntry game)
    {
        var gameDirectory = GetGameDirectory(game);
        var manifest = LoadManifest(gameDirectory);
        var restored = new List<string>();
        var removed = new List<string>();
        var preserved = new List<string>();

        foreach (var fileName in new[] { ProxyFileName, IniFileName })
        {
            var destination = Path.Combine(gameDirectory, fileName);
            if (manifest.OriginalBackups.TryGetValue(fileName, out var relativeBackup))
            {
                var backup = Path.Combine(gameDirectory, relativeBackup);
                if (File.Exists(backup))
                {
                    File.Copy(backup, destination, true);
                    restored.Add(fileName);
                    continue;
                }
            }

            if (!File.Exists(destination))
                continue;

            if (manifest.InstalledHashes.TryGetValue(fileName, out var installedHash) &&
                HashUtility.Sha256(destination).Equals(installedHash, StringComparison.OrdinalIgnoreCase))
            {
                File.Delete(destination);
                removed.Add(fileName);
            }
            else
            {
                preserved.Add(fileName);
            }
        }

        var manifestPath = Path.Combine(gameDirectory, ManifestFileName);
        if (File.Exists(manifestPath))
            File.Delete(manifestPath);

        var parts = new List<string>();
        if (restored.Count > 0) parts.Add(L($"Geri yüklendi: {string.Join(", ", restored)}", $"Restored: {string.Join(", ", restored)}"));
        if (removed.Count > 0) parts.Add(L($"Kaldırıldı: {string.Join(", ", removed)}", $"Removed: {string.Join(", ", removed)}"));
        if (preserved.Count > 0) parts.Add(L(
            $"Kullanıcı tarafından değiştirilmiş olabileceği için korundu: {string.Join(", ", preserved)}",
            $"Preserved because the files may have been modified by the user: {string.Join(", ", preserved)}"));
        return parts.Count == 0
            ? L("Kaldırılacak yönetilen dosya bulunamadı.", "No managed files were found to remove.")
            : string.Join(Environment.NewLine, parts);
    }

    public void Launch(GameEntry game)
    {
        if (!File.Exists(game.ExecutablePath))
            throw new FileNotFoundException(L("Oyun çalıştırma dosyası bulunamadı.", "The game executable was not found."), game.ExecutablePath);

        Process.Start(new ProcessStartInfo
        {
            FileName = game.ExecutablePath,
            WorkingDirectory = Path.GetDirectoryName(game.ExecutablePath)!,
            UseShellExecute = true
        });
    }

    public static bool TryValidateSource(string sourceFolder, out string error)
    {
        if (string.IsNullOrWhiteSpace(sourceFolder) || !Directory.Exists(sourceFolder))
        {
            error = L("Geçerli Frame Gen dosyalarını seçin.", "Select valid Frame Gen files.");
            return false;
        }

        foreach (var fileName in new[] { ProxyFileName, IniFileName })
        {
            if (!File.Exists(Path.Combine(sourceFolder, fileName)))
            {
                error = L($"Seçilen konumda {fileName} bulunamadı.", $"{fileName} was not found in the selected location.");
                return false;
            }
        }

        error = string.Empty;
        return true;
    }

    private static void ValidateSourceOrThrow(string sourceFolder)
    {
        if (!TryValidateSource(sourceFolder, out var error))
            throw new InvalidOperationException(error);
    }

    private static string GetGameDirectory(GameEntry game)
    {
        if (string.IsNullOrWhiteSpace(game.ExecutablePath) || !File.Exists(game.ExecutablePath))
            throw new FileNotFoundException(L("Oyun .exe dosyası bulunamadı.", "The game .exe was not found."), game.ExecutablePath);
        return Path.GetDirectoryName(Path.GetFullPath(game.ExecutablePath))!;
    }

    private static void EnsureSourceIsSeparate(string gameDirectory, string sourceFolder)
    {
        if (Path.GetFullPath(gameDirectory).TrimEnd(Path.DirectorySeparatorChar)
            .Equals(Path.GetFullPath(sourceFolder).TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(L(
                "Frame Gen kaynak klasörü oyun klasörüyle aynı olamaz.",
                "The Frame Gen source folder cannot be the same as the game folder."));
    }

    private static int MultiplierToIniValue(int multiplier) => multiplier switch
    {
        2 => 1,
        3 => 2,
        4 => 3,
        _ => throw new ArgumentOutOfRangeException(nameof(multiplier),
            L("MFG seviyesi 2X, 3X veya 4X olmalıdır.", "The MFG level must be 2X, 3X or 4X."))
    };

    private static string CreateBackupDirectory(string gameDirectory)
    {
        var directory = Path.Combine(gameDirectory, BackupDirectoryName,
            DateTime.Now.ToString("yyyyMMdd_HHmmss_fff"));
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static void CopyManagedFile(
        string source,
        string gameDirectory,
        InstallManifest manifest,
        string backupDirectory,
        bool overwrite)
    {
        var fileName = Path.GetFileName(source);
        var destination = Path.Combine(gameDirectory, fileName);
        BackupIfNeeded(destination, gameDirectory, manifest, backupDirectory);

        var temporaryPath = destination + ".mfgmanager.tmp";
        try
        {
            File.Copy(source, temporaryPath, true);
            File.Move(temporaryPath, destination, overwrite);
        }
        finally
        {
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);
        }

        manifest.InstalledHashes[fileName] = HashUtility.Sha256(destination);
    }

    private static void ApplyIniSetting(
        GameEntry game,
        string gameDirectory,
        InstallManifest manifest,
        string backupDirectory)
    {
        var iniPath = Path.Combine(gameDirectory, IniFileName);
        BackupIfNeeded(iniPath, gameDirectory, manifest, backupDirectory);
        IniEditor.WriteMaxGeneratedFrames(iniPath, MultiplierToIniValue(game.Multiplier));
        manifest.InstalledHashes[IniFileName] = HashUtility.Sha256(iniPath);
    }

    private static void BackupIfNeeded(
        string destination,
        string gameDirectory,
        InstallManifest manifest,
        string backupDirectory)
    {
        if (!File.Exists(destination))
            return;

        var fileName = Path.GetFileName(destination);
        var currentHash = HashUtility.Sha256(destination);
        if (manifest.InstalledHashes.TryGetValue(fileName, out var managedHash) &&
            currentHash.Equals(managedHash, StringComparison.OrdinalIgnoreCase))
            return;

        var backupPath = Path.Combine(backupDirectory, fileName);
        File.Copy(destination, backupPath, false);
        if (!manifest.OriginalBackups.ContainsKey(fileName))
            manifest.OriginalBackups[fileName] = Path.GetRelativePath(gameDirectory, backupPath);
    }

    private static void RefreshManifestHashes(string gameDirectory, InstallManifest manifest)
    {
        foreach (var fileName in new[] { ProxyFileName, IniFileName })
        {
            var path = Path.Combine(gameDirectory, fileName);
            if (File.Exists(path))
                manifest.InstalledHashes[fileName] = HashUtility.Sha256(path);
        }
    }

    private static InstallManifest LoadManifest(string gameDirectory)
    {
        var path = Path.Combine(gameDirectory, ManifestFileName);
        if (!File.Exists(path))
            return new InstallManifest();

        try
        {
            return JsonSerializer.Deserialize<InstallManifest>(File.ReadAllText(path), JsonOptions)
                   ?? new InstallManifest();
        }
        catch
        {
            return new InstallManifest();
        }
    }

    private static void SaveManifest(string gameDirectory, InstallManifest manifest)
    {
        manifest.LastUpdatedUtc = DateTimeOffset.UtcNow;
        var path = Path.Combine(gameDirectory, ManifestFileName);
        var temporaryPath = path + ".tmp";
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(manifest, JsonOptions));
        File.Move(temporaryPath, path, true);
        File.SetAttributes(path, File.GetAttributes(path) | FileAttributes.Hidden);
    }

    private static string L(string turkish, string english) => Localization.Text(turkish, english);
}
