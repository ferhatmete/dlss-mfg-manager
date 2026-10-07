namespace DlssMfgManager.Services;

public enum UiLanguage
{
    Turkish,
    English
}

public static class Localization
{
    public static UiLanguage Current { get; private set; } = UiLanguage.Turkish;

    public static void Use(string? languageCode)
    {
        Current = languageCode?.Equals("en", StringComparison.OrdinalIgnoreCase) == true
            ? UiLanguage.English
            : UiLanguage.Turkish;
    }

    public static string Text(string turkish, string english) =>
        Current == UiLanguage.Turkish ? turkish : english;
}
