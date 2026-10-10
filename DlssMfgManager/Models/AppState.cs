namespace DlssMfgManager.Models;

public sealed class AppState
{
    public string Language { get; set; } = "tr";
    public string PackageSourceFolder { get; set; } = string.Empty;
    public bool OnlineArtworkEnabled { get; set; } = true;
    public List<string> AdditionalScanFolders { get; set; } = [];
    public List<GameEntry> Games { get; set; } = [];
}
