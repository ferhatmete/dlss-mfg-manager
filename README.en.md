# DLSS MFG Manager

> [Türkçe belge](README.md) · English documentation

DLSS MFG Manager is a source-available .NET 8 WinForms utility for managing per-game `dlssg_for_sm86` installations on Windows 10/11. It targets RTX 20-series (SM75) and RTX 30-series (SM86) GPUs.

> [!CAUTION]
> **Use only with offline / single-player games.** Loading a proxy DLL in a game with anti-cheat or online services can lead to account penalties or a permanent ban. An entry in the built-in game catalog is not a compatibility guarantee. Check current game-specific information before every installation.

## What does it do?

- Scans all Steam libraries and Epic Games manifests.
- Lets you manually select the game's real rendering executable.
- Installs your selected `version.dll` and `dlssg_sm86.ini` beside the game executable.
- Backs up files with the same names before replacing them.
- Verifies the DLL with SHA-256 and validates the INI setting.
- Configures `MaxGeneratedFrames` for 2X, 3X, or 4X.
- Optionally checks and repairs missing or modified files before launch.
- Removes managed files and restores the first original backup when available.
- Persists the game list and user preferences.
- Can download the selected game's Steam artwork and display it as a dimmed hero background.
- Includes Turkish and English UI, a modern dark theme, a supported-GPU window, and a searchable game catalog.

## What it does not do

- It does not bundle or download `dlssg_for_sm86` DLL/INI files.
- It does not guarantee compatibility with any game.
- It does not disable or bypass anti-cheat.
- It does not modify save games, drivers, or Windows system files.
- It is not affiliated with NVIDIA, Microsoft, game developers, or any upstream project listed below.

## Requirements

- Windows 10 or Windows 11, x64
- NVIDIA RTX 20-series (SM75) or RTX 30-series (SM86) GPU
- A current NVIDIA driver
- A compatible DirectX 12 game with a DLSS Frame Generation integration
- Write permission for the game directory

The release EXE is self-contained; the target PC does not need a separate .NET installation.

## Download and integrity verification

1. Open the repository's **Releases** section.
2. Download `DlssMfgManager.exe` from the latest release.
3. Compare its digest with `SHA256SUMS.txt` from the same release.

PowerShell verification:

```powershell
Get-FileHash .\DlssMfgManager.exe -Algorithm SHA256
```

SHA-256 for the v1.3.0 Windows x64 EXE:

```text
3A5992DF1E99248F0E1E1870A24F474C13FB45453EC34F44E2942617C1DE67D2
```

The application is not digitally signed. Windows SmartScreen may therefore show an “Unknown publisher” warning on first launch. Download only from this repository's Releases section and verify the hash.

## Obtaining the required Frame Generation files

This application does not redistribute third-party runtime files. Obtain current files from the primary upstream project:

- [`sdli1995/dlssg_for_sm86`](https://github.com/sdli1995/dlssg_for_sm86)
- [Upstream English README](https://github.com/sdli1995/dlssg_for_sm86/blob/main/README.en.md)
- [Upstream installation guide](https://github.com/sdli1995/dlssg_for_sm86/blob/main/docs/INSTALL.en.md)
- [Upstream releases](https://github.com/sdli1995/dlssg_for_sm86/releases)

The manager needs these two files:

```text
version.dll
dlssg_sm86.ini
```

Read the upstream release notes, signature/hash information, and `THIRD_PARTY_NOTICES.txt`. Do not download repackaged DLLs from unrelated websites.

## Step-by-step usage

### 1. Prepare the application

1. Download `DlssMfgManager.exe` from a release.
2. Move it to a permanent folder where you have write access.
3. Verify the EXE hash.
4. Completely exit the game and its launcher.

### 2. Select Frame Gen files

Click **Select Frame Gen Files** and select both `version.dll` and `dlssg_sm86.ini` from the upstream package.

Imported files are copied to:

```text
%LOCALAPPDATA%\DlssMfgManager\Package
```

Use **Use Folder** if you want to reference the files in their current directory without copying them. Moving those files later will invalidate the saved path.

### 3. Add games

There are two methods:

- **Auto-Scan Games:** Scans every Steam library and Epic Games manifests. Candidates are shown for confirmation before anything is changed.
- **Add Game:** Lets you manually select the game's real rendering executable.

The real executable is often under a folder such as `Binaries\Win64`, `bin\x64`, or a game-specific equivalent. Do not select a launcher unless it is also the rendering process. Auto-detection is a best-effort guess; inspect the path before installation.

### 4. Check current compatibility online

The **Frame Gen Game List** is an offline reference catalog. It can be incomplete or outdated. A game missing from the catalog may work, and a listed game may stop working after an update.

Before installation:

1. Confirm that the game can be played locally/offline.
2. Check for anti-cheat or online components.
3. Research current `dlssg_for_sm86` issues and game-specific installation notes.
4. If helpful, use **Check Selected Game Online** in the catalog window.

### 5. Use game artwork (optional)

When **Use online game artwork** is enabled, the application uses the Steam AppID captured during automatic discovery. If no AppID is known, it searches the Steam store by game name. The result is shown as a darkened background in the selected-game area. Use **Refresh Artwork** to replace the cached copy. Disabling the option uses the local gradient background only.

### 6. Select the MFG level

| UI option | INI value | Meaning |
|---|---:|---|
| 2X | `MaxGeneratedFrames=1` | At most 1 generated frame per real frame |
| 3X | `MaxGeneratedFrames=2` | At most 2 generated frames per real frame |
| 4X | `MaxGeneratedFrames=3` | At most 3 generated frames per real frame |

This is a ceiling. The actual level still depends on the game's integration and the upstream runtime.

### 7. Install and launch

1. Click **Install / Update**.
2. Read and accept the online/anti-cheat warning.
3. When the installation status is **Ready**, click **Launch Game**.
4. Enable DLSS Frame Generation in the game's graphics settings.

When **Automatically check and repair before launch** is enabled, the manager verifies file presence, the DLL hash, and the INI value before every launch. Missing or changed managed files are reinstalled from your selected package, so you do not have to copy them again for every session.

### 8. Uninstall

The red **Remove Frame Gen Files** button:

- Removes the managed `version.dll` and `dlssg_sm86.ini`.
- Restores files that existed before the first managed installation when a backup is available.
- Refuses to automatically delete a file that appears to have been modified by the user after installation.

Backups are stored under the game directory:

```text
.dlss-mfg-manager-backups\yyyyMMdd_HHmmss_fff\
```

## Buttons and states

- **Blue — Install / Update:** Installs Frame Gen files and updates the INI level.
- **Red — Remove Frame Gen Files:** Removes the managed installation and restores originals when possible.
- **Green — Launch Game:** Repairs first when enabled, then starts the game with its directory as the working directory.
- **Ready:** Files and settings match the selected package.
- **Not installed:** One or more managed files are missing.
- **Repair required:** The DLL hash or INI setting does not match.
- **Could not verify:** The game executable or package source is inaccessible.

Action buttons resize proportionally and retain their full labels when the window becomes narrow.

## Important safety notes

- Do not use this in anti-cheat or online games.
- Keep the game and launcher closed while installing, updating, or removing files.
- Recheck compatibility after every game update.
- Obtain third-party DLLs only from trusted upstream sources and verify signatures/hashes.
- Do not follow guides that ask you to completely disable antivirus protection.
- If the game directory already contains `version.dll`, identify its owner before replacing it. The manager creates a backup, but multiple mods can still conflict.
- Follow upstream game-specific instructions when a different proxy name such as `winmm.dll`, `dxgi.dll`, or `dbghelp.dll` is required. This manager version directly manages the `version.dll` method.
- A low base frame rate can increase latency and visual artifacts at higher MFG levels.
- Use is at your own risk. No guarantee is made regarding accounts, game files, stability, performance, or compatibility.

Read [SECURITY.md](SECURITY.md) for more safety information.

## Troubleshooting

### My game is not in the catalog

Use **Add Game** and select the real rendering executable. The catalog is not an authoritative compatibility list. Search the game's name, current version, and `dlssg_for_sm86` compatibility online.

### The Frame Generation option is missing

- Confirm that the game has a DLSS Frame Generation integration.
- Make sure the game is running in DX12 mode.
- Select the real rendering EXE directory, not a launcher.
- Review upstream logs and the current installation guide.
- Check the NVIDIA driver and upstream runtime versions.

### The game does not start or crashes

1. Exit the game.
2. Use **Remove Frame Gen Files**.
3. Verify game files through the launcher.
4. Search upstream issues for the specific game and version.

### Windows blocks the EXE

Confirm that the download came from this repository's Releases section and that SHA-256 matches. The EXE is not code-signed. A SmartScreen warning alone is not proof of malware, but never run the file when its hash does not match.

## Stored data and privacy

Game list and preferences:

```text
%LOCALAPPDATA%\DlssMfgManager\settings.json
```

Imported source package:

```text
%LOCALAPPDATA%\DlssMfgManager\Package
```

Downloaded game artwork:

```text
%LOCALAPPDATA%\DlssMfgManager\ArtworkCache
```

The application sends no telemetry, requests no account credentials, and does not automatically download DLLs. When online artwork is enabled, records without a known Steam AppID send the game name to Steam's store-search service. Artwork is accepted only from Steam HTTPS image domains and cached locally. You can disable this feature on the main screen. **Check Online** only opens a prepared search in the default browser.

## Building from source

Requirements:

- Windows 10/11 x64
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

```powershell
dotnet build .\DlssMfgManager.sln --configuration Release
```

To publish a single self-contained Windows EXE:

```powershell
.\publish.ps1
```

Output:

```text
outputs\win-x64-v1.3\DlssMfgManager.exe
```

## Upstream projects and acknowledgements

- [`sdli1995/dlssg_for_sm86`](https://github.com/sdli1995/dlssg_for_sm86) — primary upstream source for the `version.dll` and `dlssg_sm86.ini` format managed by this application.
- [`Coldwood1026/dlssg_for_sm75`](https://github.com/Coldwood1026/dlssg_for_sm75) — RTX 20 / SM75 adaptation source credited by the primary upstream project.
- [`Nukem9/dlssg-to-fsr3`](https://github.com/Nukem9/dlssg-to-fsr3) — GPLv3 project/loader lineage identified by the primary upstream project's third-party notice.

See [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md) for detailed attribution and licensing boundaries.

## Independence notice

DLSS MFG Manager is an independent community utility. It is not created, approved, or supported by NVIDIA, Microsoft, Valve, Epic Games, game developers, or the upstream maintainers named above. Product and game names are trademarks of their respective owners.
