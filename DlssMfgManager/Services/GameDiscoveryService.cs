using System.Text.Json;
using System.Text.RegularExpressions;
using DlssMfgManager.Models;
using Microsoft.Win32;

namespace DlssMfgManager.Services;

public sealed partial class GameDiscoveryService
{
    private static readonly string[] ExcludedDirectoryNames =
    [
        "_CommonRedist", "CommonRedist", "Redist", "Redistributables", "Prereq", "Prerequisites",
        "Installer", "Support", "EasyAntiCheat", "EasyAntiCheat_EOS", "BattlEye", "CrashReportClient",
        "Engine\\Extras", "Engine\\Binaries\\ThirdParty"
    ];

    private static readonly string[] ExcludedFileTerms =
    [
        "unins", "uninstall", "crashreport", "crashhandler", "reportclient", "easyanticheat", "eac_",
        "battleye", "beservice", "setup", "installer", "redist", "dxsetup", "vc_redist", "dotnet",
        "helper", "webhelper", "bootstrap", "diagnostic", "benchmark", "dedicatedserver"
    ];

    public IReadOnlyList<DiscoveredGame> Scan()
    {
        var games = new List<DiscoveredGame>();
        ScanSteam(games);
        ScanEpic(games);

        return games
            .Where(game => File.Exists(game.ExecutablePath))
            .GroupBy(game => Path.GetFullPath(game.ExecutablePath), StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderBy(game => game.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    private static void ScanSteam(List<DiscoveredGame> games)
    {
        foreach (var steamRoot in FindSteamRoots())
        {
            foreach (var steamAppsDirectory in FindSteamLibraries(steamRoot))
            {
                IEnumerable<string> manifests;
                try
                {
                    manifests = Directory.EnumerateFiles(steamAppsDirectory, "appmanifest_*.acf").ToArray();
                }
                catch
                {
                    continue;
                }

                foreach (var manifestPath in manifests)
                {
                    try
                    {
                        var contents = File.ReadAllText(manifestPath);
                        var name = ReadVdfValue(contents, "name");
                        var appIdText = ReadVdfValue(contents, "appid");
                        var installDirectoryName = ReadVdfValue(contents, "installdir");
                        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(installDirectoryName))
                            continue;

                        var installDirectory = Path.Combine(steamAppsDirectory, "common", installDirectoryName);
                        var executable = FindBestExecutable(installDirectory, name);
                        if (executable is not null)
                            games.Add(new DiscoveredGame(name, executable, "Steam",
                                int.TryParse(appIdText, out var appId) ? appId : null));
                    }
                    catch
                    {
                        // A single broken manifest must not stop the rest of the scan.
                    }
                }
            }
        }
    }

    private static IEnumerable<string> FindSteamRoots()
    {
        var candidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        AddRegistryValue(candidates, RegistryHive.CurrentUser, RegistryView.Default,
            @"Software\Valve\Steam", "SteamPath");
        AddRegistryValue(candidates, RegistryHive.LocalMachine, RegistryView.Registry32,
            @"SOFTWARE\Valve\Steam", "InstallPath");
        AddRegistryValue(candidates, RegistryHive.LocalMachine, RegistryView.Registry64,
            @"SOFTWARE\Valve\Steam", "InstallPath");

        var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        if (!string.IsNullOrWhiteSpace(programFilesX86))
            candidates.Add(Path.Combine(programFilesX86, "Steam"));

        return candidates.Where(Directory.Exists);
    }

    private static IEnumerable<string> FindSteamLibraries(string steamRoot)
    {
        var libraries = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var defaultSteamApps = Path.Combine(steamRoot, "steamapps");
        if (Directory.Exists(defaultSteamApps))
            libraries.Add(defaultSteamApps);

        var libraryFile = Path.Combine(defaultSteamApps, "libraryfolders.vdf");
        if (!File.Exists(libraryFile))
            return libraries;

        try
        {
            var contents = File.ReadAllText(libraryFile);
            foreach (Match match in SteamLibraryPathRegex().Matches(contents))
            {
                var libraryRoot = match.Groups[1].Value.Replace("\\\\", "\\");
                var steamApps = Path.Combine(libraryRoot, "steamapps");
                if (Directory.Exists(steamApps))
                    libraries.Add(steamApps);
            }
        }
        catch
        {
            // The default library can still be scanned.
        }

        return libraries;
    }

    private static void ScanEpic(List<DiscoveredGame> games)
    {
        var programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        var manifestsDirectory = Path.Combine(programData, "Epic", "EpicGamesLauncher", "Data", "Manifests");
        if (!Directory.Exists(manifestsDirectory))
            return;

        IEnumerable<string> manifests;
        try
        {
            manifests = Directory.EnumerateFiles(manifestsDirectory, "*.item").ToArray();
        }
        catch
        {
            return;
        }

        foreach (var manifestPath in manifests)
        {
            try
            {
                using var document = JsonDocument.Parse(File.ReadAllText(manifestPath));
                var root = document.RootElement;
                var name = ReadJsonString(root, "DisplayName");
                var installLocation = ReadJsonString(root, "InstallLocation");
                var launchExecutable = ReadJsonString(root, "LaunchExecutable");
                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(installLocation))
                    continue;

                string? executable = null;
                if (!string.IsNullOrWhiteSpace(launchExecutable))
                {
                    var normalizedRelativePath = launchExecutable
                        .Replace('/', Path.DirectorySeparatorChar)
                        .TrimStart(Path.DirectorySeparatorChar);
                    var manifestExecutable = Path.GetFullPath(Path.Combine(installLocation, normalizedRelativePath));
                    if (File.Exists(manifestExecutable) && Is64BitPortableExecutable(manifestExecutable))
                        executable = manifestExecutable;
                }

                executable ??= FindBestExecutable(installLocation, name);
                if (executable is not null)
                    games.Add(new DiscoveredGame(name, executable, "Epic Games"));
            }
            catch
            {
                // Ignore incomplete or malformed launcher manifests.
            }
        }
    }

    private static string? FindBestExecutable(string installDirectory, string gameName)
    {
        if (!Directory.Exists(installDirectory))
            return null;

        var normalizedGameName = NormalizeName(gameName);
        var candidates = EnumerateExecutablesSafely(installDirectory)
            .Where(IsLikelyGameExecutable)
            .Select(path => new { Path = path, Score = ScoreExecutable(path, installDirectory, normalizedGameName) })
            .Where(candidate => candidate.Score > 0)
            .OrderByDescending(candidate => candidate.Score)
            .ThenBy(candidate => candidate.Path.Length)
            .ToList();

        return candidates.FirstOrDefault()?.Path;
    }

    private static IEnumerable<string> EnumerateExecutablesSafely(string root)
    {
        var pending = new Stack<(string Directory, int Depth)>();
        pending.Push((root, 0));

        while (pending.Count > 0)
        {
            var (directory, depth) = pending.Pop();
            IEnumerable<string> files;
            try
            {
                files = Directory.EnumerateFiles(directory, "*.exe").ToArray();
            }
            catch
            {
                files = [];
            }

            foreach (var file in files)
                yield return file;

            if (depth >= 7)
                continue;

            IEnumerable<string> subdirectories;
            try
            {
                subdirectories = Directory.EnumerateDirectories(directory).ToArray();
            }
            catch
            {
                continue;
            }

            foreach (var subdirectory in subdirectories)
            {
                var relative = Path.GetRelativePath(root, subdirectory);
                if (IsExcludedDirectory(relative))
                    continue;
                pending.Push((subdirectory, depth + 1));
            }
        }
    }

    private static bool IsLikelyGameExecutable(string path)
    {
        var fileName = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
        if (ExcludedFileTerms.Any(fileName.Contains))
            return false;

        return Is64BitPortableExecutable(path);
    }

    private static int ScoreExecutable(string path, string installDirectory, string normalizedGameName)
    {
        var fileName = Path.GetFileNameWithoutExtension(path);
        var normalizedFileName = NormalizeName(fileName);
        var relative = Path.GetRelativePath(installDirectory, path).Replace('/', '\\').ToLowerInvariant();
        var score = 30; // It is already a valid x64 PE file.

        if (relative.Contains("binaries\\win64")) score += 55;
        else if (relative.Contains("win64") || relative.Contains("x64")) score += 30;
        if (relative.Contains("shipping")) score += 35;
        if (relative.Contains("binaries")) score += 20;
        if (relative.Contains("launcher")) score -= 35;
        if (fileName.Contains("launcher", StringComparison.OrdinalIgnoreCase)) score -= 45;
        if (fileName.Contains("shipping", StringComparison.OrdinalIgnoreCase)) score += 15;

        if (normalizedFileName.Equals(normalizedGameName, StringComparison.OrdinalIgnoreCase)) score += 70;
        else if (normalizedGameName.Length >= 4 &&
                 (normalizedFileName.Contains(normalizedGameName, StringComparison.OrdinalIgnoreCase) ||
                  normalizedGameName.Contains(normalizedFileName, StringComparison.OrdinalIgnoreCase))) score += 35;

        try
        {
            score += (int)Math.Min(new FileInfo(path).Length / (10 * 1024 * 1024), 25);
        }
        catch
        {
            // Size is only a tie-breaker.
        }

        return score;
    }

    private static bool Is64BitPortableExecutable(string path)
    {
        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new BinaryReader(stream);
            if (reader.ReadUInt16() != 0x5A4D)
                return false;
            stream.Position = 0x3C;
            var peOffset = reader.ReadInt32();
            if (peOffset <= 0 || peOffset > stream.Length - 6)
                return false;
            stream.Position = peOffset;
            if (reader.ReadUInt32() != 0x00004550)
                return false;
            return reader.ReadUInt16() == 0x8664;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsExcludedDirectory(string relativePath)
    {
        var normalized = relativePath.Replace('/', '\\');
        return ExcludedDirectoryNames.Any(excluded =>
            normalized.Equals(excluded, StringComparison.OrdinalIgnoreCase) ||
            normalized.StartsWith(excluded + "\\", StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains("\\" + excluded + "\\", StringComparison.OrdinalIgnoreCase));
    }

    private static void AddRegistryValue(
        ISet<string> values,
        RegistryHive hive,
        RegistryView view,
        string keyPath,
        string valueName)
    {
        try
        {
            using var baseKey = RegistryKey.OpenBaseKey(hive, view);
            using var key = baseKey.OpenSubKey(keyPath);
            if (key?.GetValue(valueName) is string value && !string.IsNullOrWhiteSpace(value))
                values.Add(value.Replace('/', Path.DirectorySeparatorChar));
        }
        catch
        {
            // Registry access can be restricted; other discovery paths remain available.
        }
    }

    private static string ReadVdfValue(string contents, string key)
    {
        var match = Regex.Match(contents, $"\\\"{Regex.Escape(key)}\\\"\\s+\\\"(?<value>[^\\\"]*)\\\"",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        return match.Success ? match.Groups["value"].Value : string.Empty;
    }

    private static string ReadJsonString(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString() ?? string.Empty
            : string.Empty;

    private static string NormalizeName(string value) =>
        new(value.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());

    [GeneratedRegex("\\\"path\\\"\\s+\\\"([^\\\"]+)\\\"", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SteamLibraryPathRegex();
}
