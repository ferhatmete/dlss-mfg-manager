using System.Text.Json;
using DlssMfgManager.Models;

namespace DlssMfgManager.Services;

public sealed class AppStateStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public string DataDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "DlssMfgManager");

    public string ImportedPackageDirectory => Path.Combine(DataDirectory, "Package");

    private string StatePath => Path.Combine(DataDirectory, "settings.json");

    public AppState Load()
    {
        try
        {
            if (!File.Exists(StatePath))
                return new AppState();

            return JsonSerializer.Deserialize<AppState>(File.ReadAllText(StatePath), JsonOptions)
                   ?? new AppState();
        }
        catch
        {
            return new AppState();
        }
    }

    public void Save(AppState state)
    {
        Directory.CreateDirectory(DataDirectory);
        var temporaryPath = StatePath + ".tmp";
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(state, JsonOptions));
        File.Move(temporaryPath, StatePath, true);
    }
}
