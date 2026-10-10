using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using DlssMfgManager.Models;
using Microsoft.Win32;

namespace DlssMfgManager.Services;

public sealed partial class GameDiscoveryService
{
    private static readonly string[] ExcludedDirectoryNames =
    [
        "_CommonRedist", "CommonRedist", "Redist", "Redistributables", "Prereq", "Prerequisites",
        "Installer", "Support", "EasyAntiCheat", "EasyAntiCheat_EOS", "BattlEye", "CrashReportClient",
        "Engine\\Extras", "Engine\\Binaries", "Engine\\Plugins", "Engine\\Source", "__Installer"
    ];

    private static readonly string[] ExcludedFileTerms =
    [
        "unins", "uninstall", "crashreport", "crashhandler", "reportclient", "easyanticheat", "eac_",
        "battleye", "beservice", "setup", "installer", "redist", "dxsetup", "vc_redist", "dotnet",
        "helper", "webhelper", "bootstrap", "launcher", "diagnostic", "benchmark", "dedicatedserver", "unrealeditor",
        "unityhub", "epicgameslauncher", "goggalaxy", "galaxyclient", "eadesktop", "ealauncher",
        "ubisoftconnect", "upc", "rockstarservice", "socialclubhelper", "battle.net", "crashpad"
    ];

    private static readonly string[] LauncherProductTerms =
    [
        "steam", "epic games launcher", "gog galaxy", "ea app", "origin", "ubisoft connect",
        "battle.net", "rockstar games launcher", "amazon games", "xbox", "gaming services", "riot client"
    ];

    private static readonly string[] NonGameProductTerms =
    [
        "unreal engine", "quixel bridge", "ubisoft anti-cheat", "riot vanguard", "epic online services",
        "easy anti-cheat", "battleye", "steamworks common redistributables", "dedicated server",
        "modding tools", "development kit", "fab ue plugin"
    ];

    public IReadOnlyList<DiscoveredGame> Scan(
        IEnumerable<string>? additionalFolders = null,
        bool includeKnownPlatforms = true)
    {
        var games = new List<DiscoveredGame>();
        if (includeKnownPlatforms)
        {
            ScanSteam(games);
            ScanEpic(games);
            ScanXboxGames(games);
            ScanGogRegistry(games);
            ScanInstalledGameRegistry(games);
            ScanKnownLibraryFolders(games);
        }

        foreach (var folder in (additionalFolders ?? []).Distinct(StringComparer.OrdinalIgnoreCase))
            ScanLibraryFolder(folder, "Local / Other", games, includeRootCandidate: true);

        return games
            .Where(game => File.Exists(game.ExecutablePath))
            .GroupBy(game => Path.GetFullPath(game.ExecutablePath), StringComparer.OrdinalIgnoreCase)
            .Select(group => group
                .OrderByDescending(game => DiscoveryPriority(game.Platform))
                .ThenByDescending(game => game.Name.Length)
                .First())
            .OrderBy(game => game.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    public IReadOnlyList<DiscoveredGame> Scan() => Scan([], includeKnownPlatforms: true);

    private static int DiscoveryPriority(string platform) => platform switch
    {
        "Steam" => 100,
        "Epic Games" => 95,
        "Xbox / Microsoft Store" => 90,
        "GOG" => 85,
        "EA app" or "Ubisoft Connect" or "Battle.net" or "Rockstar Games" or "Amazon Games" => 80,
        "Riot Games" or "itch.io" => 75,
        "Steam / Local" => 30,
        _ => 10
    };

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
                        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(installDirectoryName) ||
                            IsNonGameProduct(name))
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

                // Also inspect orphaned/copy-installed folders under Steam's
                // common directory. They may have no current appmanifest but
                // can still contain a valid local game executable.
                ScanLibraryFolder(Path.Combine(steamAppsDirectory, "common"),
                    "Steam / Local", games, includeRootCandidate: false);
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
                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(installLocation) ||
                    IsNonGameProduct(name))
                    continue;

                string? executable = null;
                if (!string.IsNullOrWhiteSpace(launchExecutable))
                {
                    var normalizedRelativePath = launchExecutable
                        .Replace('/', Path.DirectorySeparatorChar)
                        .TrimStart(Path.DirectorySeparatorChar);
                    var manifestExecutable = Path.GetFullPath(Path.Combine(installLocation, normalizedRelativePath));
                    if (File.Exists(manifestExecutable) && IsLikelyGameExecutable(manifestExecutable))
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

    private static void ScanXboxGames(List<DiscoveredGame> games)
    {
        foreach (var drive in ReadyFixedDrives())
        {
            var xboxRoot = Path.Combine(drive.RootDirectory.FullName, "XboxGames");
            if (Directory.Exists(xboxRoot))
                ScanLibraryFolder(xboxRoot, "Xbox / Microsoft Store", games, includeRootCandidate: false);
        }
    }

    private static void ScanGogRegistry(List<DiscoveredGame> games)
    {
        var locations = new[]
        {
            (RegistryHive.LocalMachine, RegistryView.Registry64, @"SOFTWARE\GOG.com\Games"),
            (RegistryHive.LocalMachine, RegistryView.Registry32, @"SOFTWARE\GOG.com\Games"),
            (RegistryHive.CurrentUser, RegistryView.Default, @"Software\GOG.com\Games")
        };

        foreach (var (hive, view, keyPath) in locations)
        {
            try
            {
                using var baseKey = RegistryKey.OpenBaseKey(hive, view);
                using var gamesKey = baseKey.OpenSubKey(keyPath);
                if (gamesKey is null)
                    continue;

                foreach (var subKeyName in gamesKey.GetSubKeyNames())
                {
                    using var gameKey = gamesKey.OpenSubKey(subKeyName);
                    var name = RegistryString(gameKey, "gameName");
                    var installDirectory = RegistryString(gameKey, "path");
                    var executableValue = RegistryString(gameKey, "exe");
                    if (string.IsNullOrWhiteSpace(installDirectory) || !Directory.Exists(installDirectory))
                        continue;

                    name = string.IsNullOrWhiteSpace(name) ? Path.GetFileName(installDirectory) : name;
                    var executable = ResolveExecutableValue(installDirectory, executableValue);
                    if (executable is null || !IsLikelyGameExecutable(executable))
                        executable = FindBestExecutable(installDirectory, name);
                    if (executable is not null)
                        games.Add(new DiscoveredGame(name, executable, "GOG"));
                }
            }
            catch
            {
                // Registry data varies across GOG Galaxy and offline installers.
            }
        }
    }

    private static void ScanInstalledGameRegistry(List<DiscoveredGame> games)
    {
        var locations = new[]
        {
            (RegistryHive.LocalMachine, RegistryView.Registry64,
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"),
            (RegistryHive.LocalMachine, RegistryView.Registry32,
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"),
            (RegistryHive.CurrentUser, RegistryView.Default,
                @"Software\Microsoft\Windows\CurrentVersion\Uninstall")
        };

        foreach (var (hive, view, keyPath) in locations)
        {
            try
            {
                using var baseKey = RegistryKey.OpenBaseKey(hive, view);
                using var uninstallKey = baseKey.OpenSubKey(keyPath);
                if (uninstallKey is null)
                    continue;

                foreach (var subKeyName in uninstallKey.GetSubKeyNames())
                {
                    using var productKey = uninstallKey.OpenSubKey(subKeyName);
                    var name = RegistryString(productKey, "DisplayName");
                    var publisher = RegistryString(productKey, "Publisher");
                    var installDirectory = NormalizeInstallDirectory(RegistryString(productKey, "InstallLocation"));
                    if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(installDirectory) ||
                        !Directory.Exists(installDirectory) || IsLauncherProduct(name) || IsNonGameProduct(name))
                        continue;

                    var platform = DetectPlatform(name, publisher, installDirectory);
                    if (platform is null)
                        continue;

                    var displayIcon = RegistryString(productKey, "DisplayIcon");
                    var executable = ResolveExecutableValue(installDirectory, displayIcon);
                    if (executable is null || !IsLikelyGameExecutable(executable))
                        executable = FindBestExecutable(installDirectory, name);
                    if (executable is not null)
                        games.Add(new DiscoveredGame(name, executable, platform));
                }
            }
            catch
            {
                // One inaccessible uninstall hive must not cancel other sources.
            }
        }
    }

    private static void ScanKnownLibraryFolders(List<DiscoveredGame> games)
    {
        var roots = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var drive in ReadyFixedDrives())
        {
            AddKnownRoot(roots, drive, "Games", "Local / Other");
            AddKnownRoot(roots, drive, "PC Games", "Local / Other");
            AddKnownRoot(roots, drive, "PCGames", "Local / Other");
            AddKnownRoot(roots, drive, "Portable Games", "Local / Other");
            AddKnownRoot(roots, drive, "Oyunlar", "Local / Other");
            AddKnownRoot(roots, drive, "GOG Games", "GOG");
            AddKnownRoot(roots, drive, "EA Games", "EA app");
            AddKnownRoot(roots, drive, "Ubisoft Games", "Ubisoft Connect");
            AddKnownRoot(roots, drive, "Rockstar Games", "Rockstar Games");
            AddKnownRoot(roots, drive, "Amazon Games", "Amazon Games");
            AddKnownRoot(roots, drive, "Riot Games", "Riot Games");

            AddKnownRoot(roots, drive, @"Program Files\EA Games", "EA app");
            AddKnownRoot(roots, drive, @"Program Files\Rockstar Games", "Rockstar Games");
            AddKnownRoot(roots, drive, @"Program Files\Riot Games", "Riot Games");
            AddKnownRoot(roots, drive, @"Program Files (x86)\Ubisoft\Ubisoft Game Launcher\games",
                "Ubisoft Connect");
            AddKnownRoot(roots, drive, @"Program Files (x86)\GOG Galaxy\Games", "GOG");
        }

        var roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        if (!string.IsNullOrWhiteSpace(roaming))
        {
            var itchRoot = Path.Combine(roaming, "itch", "apps");
            if (Directory.Exists(itchRoot))
                roots[itchRoot] = "itch.io";
        }

        foreach (var (root, platform) in roots)
            ScanLibraryFolder(root, platform, games, includeRootCandidate: false);
    }

    private static void AddKnownRoot(
        IDictionary<string, string> roots,
        DriveInfo drive,
        string relativePath,
        string platform)
    {
        var path = Path.Combine(drive.RootDirectory.FullName, relativePath);
        if (Directory.Exists(path))
            roots[path] = platform;
    }

    private static void ScanLibraryFolder(
        string root,
        string platform,
        List<DiscoveredGame> games,
        bool includeRootCandidate)
    {
        if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
            return;

        var normalizedRoot = Path.GetFullPath(root);
        if (includeRootCandidate && HasExecutableInTopLevel(normalizedRoot))
            AddFolderCandidate(normalizedRoot, platform, games);

        foreach (var directory in EnumerateDirectoriesSafe(normalizedRoot))
        {
            if (IsLauncherProduct(Path.GetFileName(directory)) || IsNonGameProduct(Path.GetFileName(directory)))
                continue;

            var contentDirectory = Directory.Exists(Path.Combine(directory, "Content"))
                ? Path.Combine(directory, "Content")
                : directory;
            AddFolderCandidate(contentDirectory, platform, games, Path.GetFileName(directory));
        }
    }

    private static void AddFolderCandidate(
        string directory,
        string platform,
        List<DiscoveredGame> games,
        string? displayName = null)
    {
        displayName = string.IsNullOrWhiteSpace(displayName) ? Path.GetFileName(directory) : displayName;
        if (string.IsNullOrWhiteSpace(displayName))
            displayName = "Local game";

        var xboxCandidate = TryReadMicrosoftGameConfig(directory, displayName);
        if (xboxCandidate is not null)
        {
            games.Add(xboxCandidate with { Platform = platform.Contains("Xbox", StringComparison.OrdinalIgnoreCase)
                ? platform
                : "Xbox / Microsoft Store" });
            return;
        }

        var executable = FindBestExecutable(directory, displayName);
        if (executable is not null)
            games.Add(new DiscoveredGame(displayName, executable, platform));
    }

    private static DiscoveredGame? TryReadMicrosoftGameConfig(string directory, string fallbackName)
    {
        var configPath = FindFileSafely(directory, "MicrosoftGame.config", maximumDepth: 3);
        if (configPath is null)
            return null;

        try
        {
            var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null };
            using var reader = XmlReader.Create(configPath, settings);
            var document = XDocument.Load(reader, LoadOptions.None);
            var displayName = document.Descendants()
                .FirstOrDefault(element => element.Name.LocalName == "ShellVisuals")?
                .Attribute("DefaultDisplayName")?.Value;
            if (string.IsNullOrWhiteSpace(displayName) || displayName.StartsWith("ms-resource", StringComparison.OrdinalIgnoreCase))
                displayName = fallbackName;

            var executableDirectory = Path.GetDirectoryName(configPath)!;
            foreach (var element in document.Descendants().Where(element => element.Name.LocalName == "Executable"))
            {
                if (bool.TryParse(element.Attribute("IsDevOnly")?.Value, out var developmentOnly) && developmentOnly)
                    continue;
                var executableName = element.Attribute("Name")?.Value;
                var executable = ResolveExecutableValue(executableDirectory, executableName);
                if (executable is not null && IsLikelyGameExecutable(executable))
                    return new DiscoveredGame(displayName, executable, "Xbox / Microsoft Store");
            }

            var fallbackExecutable = FindBestExecutable(executableDirectory, displayName);
            return fallbackExecutable is null
                ? null
                : new DiscoveredGame(displayName, fallbackExecutable, "Xbox / Microsoft Store");
        }
        catch
        {
            return null;
        }
    }

    private static string? FindFileSafely(string root, string fileName, int maximumDepth)
    {
        var pending = new Stack<(string Directory, int Depth)>();
        pending.Push((root, 0));
        while (pending.Count > 0)
        {
            var (directory, depth) = pending.Pop();
            try
            {
                var candidate = Path.Combine(directory, fileName);
                if (File.Exists(candidate))
                    return candidate;
            }
            catch
            {
                // Keep searching accessible siblings.
            }

            if (depth >= maximumDepth)
                continue;
            foreach (var child in EnumerateDirectoriesSafe(directory))
                pending.Push((child, depth + 1));
        }

        return null;
    }

    private static IEnumerable<DriveInfo> ReadyFixedDrives()
    {
        try
        {
            return DriveInfo.GetDrives()
                .Where(drive => drive.IsReady && drive.DriveType == DriveType.Fixed)
                .ToArray();
        }
        catch
        {
            return [];
        }
    }

    private static IEnumerable<string> EnumerateDirectoriesSafe(string directory)
    {
        try
        {
            return Directory.EnumerateDirectories(directory).ToArray();
        }
        catch
        {
            return [];
        }
    }

    private static bool HasExecutableInTopLevel(string directory)
    {
        try
        {
            return Directory.EnumerateFiles(directory, "*.exe", SearchOption.TopDirectoryOnly).Any();
        }
        catch
        {
            return false;
        }
    }

    private static string? ResolveExecutableValue(string installDirectory, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var expanded = Environment.ExpandEnvironmentVariables(value.Trim());
        var commaIndex = expanded.LastIndexOf(',');
        if (commaIndex > 2 && int.TryParse(expanded[(commaIndex + 1)..].Trim(), out _))
            expanded = expanded[..commaIndex];
        expanded = expanded.Trim().Trim('"');

        try
        {
            var candidate = Path.IsPathRooted(expanded)
                ? expanded
                : Path.Combine(installDirectory, expanded.Replace('/', Path.DirectorySeparatorChar));
            return File.Exists(candidate) ? Path.GetFullPath(candidate) : null;
        }
        catch
        {
            return null;
        }
    }

    private static string NormalizeInstallDirectory(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;
        try
        {
            return Path.GetFullPath(Environment.ExpandEnvironmentVariables(value.Trim().Trim('"')));
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string? DetectPlatform(string name, string publisher, string installDirectory)
    {
        var value = $"{name} {publisher} {installDirectory}".ToLowerInvariant();
        if (value.Contains("gog") || value.Contains("cd projekt")) return "GOG";
        if (value.Contains("electronic arts") || value.Contains("ea games")) return "EA app";
        if (value.Contains("ubisoft")) return "Ubisoft Connect";
        if (value.Contains("blizzard") || value.Contains("battle.net") || value.Contains("activision")) return "Battle.net";
        if (value.Contains("rockstar")) return "Rockstar Games";
        if (value.Contains("amazon games")) return "Amazon Games";
        if (value.Contains("riot games")) return "Riot Games";
        if (value.Contains("itch.io") || value.Contains("\\itch\\apps")) return "itch.io";
        if (value.Contains("xbox game studios") || value.Contains("xboxgames") || value.Contains("microsoftgame.config"))
            return "Xbox / Microsoft Store";
        return null;
    }

    private static bool IsLauncherProduct(string value)
    {
        var normalized = value.Trim().ToLowerInvariant();
        return LauncherProductTerms.Any(term => normalized.Equals(term, StringComparison.OrdinalIgnoreCase) ||
                                                normalized.Contains(term, StringComparison.OrdinalIgnoreCase) &&
                                                (normalized.Contains("launcher") || normalized.Contains("client") ||
                                                 normalized.Contains("services") || normalized == term));
    }

    private static bool IsNonGameProduct(string value)
    {
        var normalized = value.Trim().ToLowerInvariant();
        return NonGameProductTerms.Any(normalized.Contains) ||
               normalized.EndsWith(" sdk", StringComparison.OrdinalIgnoreCase) ||
               normalized.EndsWith(" editor", StringComparison.OrdinalIgnoreCase);
    }

    private static string RegistryString(RegistryKey? key, string valueName) =>
        key?.GetValue(valueName) as string ?? string.Empty;

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
