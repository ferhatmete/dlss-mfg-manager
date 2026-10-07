using System.Text;
using System.Text.RegularExpressions;

namespace DlssMfgManager.Services;

internal static partial class IniEditor
{
    private const string SectionName = "FrameGeneration";
    private const string KeyName = "MaxGeneratedFrames";

    public static int? ReadMaxGeneratedFrames(string path)
    {
        if (!File.Exists(path))
            return null;

        var inSection = false;
        foreach (var line in File.ReadLines(path))
        {
            var section = SectionRegex().Match(line);
            if (section.Success)
            {
                inSection = section.Groups[1].Value.Equals(SectionName, StringComparison.OrdinalIgnoreCase);
                continue;
            }

            if (!inSection)
                continue;

            var key = KeyRegex().Match(line);
            if (key.Success && int.TryParse(key.Groups[2].Value, out var value))
                return value;
        }

        return null;
    }

    public static void WriteMaxGeneratedFrames(string path, int value)
    {
        if (value is < 1 or > 3)
            throw new ArgumentOutOfRangeException(nameof(value),
                Localization.Text("Değer 1 ile 3 arasında olmalıdır.", "The value must be between 1 and 3."));

        var lines = File.Exists(path) ? File.ReadAllLines(path).ToList() : [];
        var sectionStart = -1;
        var sectionEnd = lines.Count;

        for (var i = 0; i < lines.Count; i++)
        {
            var section = SectionRegex().Match(lines[i]);
            if (!section.Success)
                continue;

            if (sectionStart >= 0)
            {
                sectionEnd = i;
                break;
            }

            if (section.Groups[1].Value.Equals(SectionName, StringComparison.OrdinalIgnoreCase))
                sectionStart = i;
        }

        if (sectionStart < 0)
        {
            if (lines.Count > 0 && !string.IsNullOrWhiteSpace(lines[^1]))
                lines.Add(string.Empty);
            lines.Add($"[{SectionName}]");
            lines.Add($"{KeyName}={value}");
        }
        else
        {
            var replaced = false;
            for (var i = sectionStart + 1; i < sectionEnd; i++)
            {
                var key = KeyRegex().Match(lines[i]);
                if (!key.Success)
                    continue;

                var comment = key.Groups[3].Value;
                lines[i] = $"{key.Groups[1].Value}{value}{comment}";
                replaced = true;
                break;
            }

            if (!replaced)
                lines.Insert(sectionEnd, $"{KeyName}={value}");
        }

        var temporaryPath = path + ".mfgmanager.tmp";
        File.WriteAllLines(temporaryPath, lines, new UTF8Encoding(false));
        File.Move(temporaryPath, path, true);
    }

    [GeneratedRegex(@"^\s*\[\s*([^\]]+)\s*\]\s*(?:;.*)?$", RegexOptions.CultureInvariant)]
    private static partial Regex SectionRegex();

    [GeneratedRegex(@"^(\s*MaxGeneratedFrames\s*=\s*)([-+]?\d+)(\s*(?:;.*)?)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex KeyRegex();
}
