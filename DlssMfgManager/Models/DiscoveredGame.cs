namespace DlssMfgManager.Models;

public sealed record DiscoveredGame(string Name, string ExecutablePath, string Platform, int? SteamAppId = null)
{
    public override string ToString() => $"{Name}  ·  {Platform}  —  {ExecutablePath}";
}
