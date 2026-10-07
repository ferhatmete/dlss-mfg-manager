namespace DlssMfgManager.Models;

public sealed class GameEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string ExecutablePath { get; set; } = string.Empty;
    public int Multiplier { get; set; } = 4;
    public bool AutoRepairBeforeLaunch { get; set; } = true;
    public int? SteamAppId { get; set; }
}
