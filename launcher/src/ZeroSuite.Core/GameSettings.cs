using System.Globalization;
using System.Text.Json;

namespace ZeroSuite.Core;

public sealed record DisplayTweak(
    string Key,
    string Title,
    string Description,
    bool Enabled,
    string Detail);

/// <summary>
/// Reversible fixes to GameUserSettings.ini.
///
/// Each tweak records the value it replaced in a sidecar file before writing,
/// so turning it off restores exactly what was there rather than a guess at a
/// default. Without that, "revert" is just another edit.
///
/// The game rewrites this file when it exits, so it must be closed while
/// these are applied. IsGameRunning() exists to say so plainly.
/// </summary>
public static class GameSettings
{
    private const string Section = "/Script/BitReactorCore.BitReactorUserSettingsLocal";

    public static string? IniPath()
    {
        var path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SWZeroCompany", "Saved", "Config", "Windows", "GameUserSettings.ini");
        return File.Exists(path) ? path : null;
    }

    public static bool IsGameRunning() =>
        System.Diagnostics.Process.GetProcessesByName("SWZeroCompany").Length > 0;

    private static string SidecarPath(string ini) => ini + ".zerosuite.json";

    private static Dictionary<string, string> Originals(string ini)
    {
        var path = SidecarPath(ini);
        if (!File.Exists(path)) return new();
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(path))
                   ?? new();
        }
        catch { return new(); }
    }

    private static void SaveOriginals(string ini, Dictionary<string, string> originals) =>
        File.WriteAllText(SidecarPath(ini),
            JsonSerializer.Serialize(originals, new JsonSerializerOptions { WriteIndented = true }));

    // ── reading ────────────────────────────────────────────────────────────
    public static string? Value(string ini, string key)
    {
        foreach (var line in File.ReadAllLines(ini))
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith(';')) continue;
            var eq = trimmed.IndexOf('=');
            if (eq <= 0) continue;
            if (trimmed[..eq].Trim().Equals(key, StringComparison.OrdinalIgnoreCase))
                return trimmed[(eq + 1)..].Trim();
        }
        return null;
    }

    private static double Number(string ini, string key, double fallback) =>
        double.TryParse(Value(ini, key), NumberStyles.Float, CultureInfo.InvariantCulture, out var v)
            ? v : fallback;

    private static bool Flag(string ini, string key) =>
        string.Equals(Value(ini, key), "True", StringComparison.OrdinalIgnoreCase);

    public static IReadOnlyList<DisplayTweak> Read()
    {
        var ini = IniPath();
        if (ini is null) return [];

        // No resolution-scale tweak here. It was tried and removed: the
        // game recomputes ResolutionPercentage at runtime and rewrites it on
        // exit, so the setting reverted every session. A toggle that cannot
        // hold its value is worse than no toggle, because it looks like it
        // worked.

        return
        [
            new DisplayTweak("vsync", "VSync",
                "The in-game menu reports VSync as on while the engine flag that actually controls it is off, which is what produces screen tearing in exclusive fullscreen.",
                Flag(ini, "bUseVSync"),
                $"bUseVSync={Value(ini, "bUseVSync")}, menu shows VSyncRate={Value(ini, "VSyncRate")}"),

            new DisplayTweak("redo_autodetect", "Re-run quality auto-detect",
                "Auto-detect is marked complete but both benchmark results are -1, so every quality group was pinned to Epic without measuring this machine. Enabling this makes the game benchmark again on next launch.",
                !Flag(ini, "bHasDoneAutoDetect"),
                $"bHasDoneAutoDetect={Value(ini, "bHasDoneAutoDetect")}, GPU={Value(ini, "LastGPUBenchmarkResult")}, CPU={Value(ini, "LastCPUBenchmarkResult")}"),
        ];
    }

    // ── writing ────────────────────────────────────────────────────────────
    public static void Apply(IReadOnlyDictionary<string, bool> desired)
    {
        var ini = IniPath();
        if (ini is null) return;

        var originals = Originals(ini);
        var writes = new Dictionary<string, string>();

        foreach (var (key, on) in desired)
        {
            switch (key)
            {
                case "vsync":
                    Stage(ini, originals, writes, "bUseVSync", on ? "True" : "False");
                    break;

                case "redo_autodetect":
                    // Enabled means "benchmark again next launch", so the flag
                    // that says it is already done must be cleared.
                    Stage(ini, originals, writes, "bHasDoneAutoDetect", on ? "False" : "True");
                    break;
            }
        }

        if (writes.Count == 0) return;

        Backup.Once(ini);
        var lines = File.ReadAllLines(ini);
        for (var i = 0; i < lines.Length; i++)
        {
            var trimmed = lines[i].Trim();
            var eq = trimmed.IndexOf('=');
            if (eq <= 0 || trimmed.StartsWith(';')) continue;
            var name = trimmed[..eq].Trim();
            if (writes.TryGetValue(name, out var value))
                lines[i] = name + "=" + value;
        }
        File.WriteAllLines(ini, lines);
        SaveOriginals(ini, originals);
    }

    /// <summary>Remember what was there before the first time we change it.</summary>
    private static void Stage(string ini, Dictionary<string, string> originals,
        Dictionary<string, string> writes, string key, string value)
    {
        if (!originals.ContainsKey(key))
        {
            var current = Value(ini, key);
            if (current is not null) originals[key] = current;
        }
        writes[key] = value;
    }
}
