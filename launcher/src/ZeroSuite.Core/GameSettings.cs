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
                "Auto-detect is marked complete but both benchmark results are -1, the never-ran sentinel, so the quality preset was applied without ever measuring this machine. Enabling this makes the game benchmark again on next launch.",
                !Flag(ini, "bHasDoneAutoDetect"),
                $"bHasDoneAutoDetect={Value(ini, "bHasDoneAutoDetect")}, GPU={Value(ini, "LastGPUBenchmarkResult")}, CPU={Value(ini, "LastCPUBenchmarkResult")}, preset={Value(ini, "AutoDetectPreset")}"),

            new DisplayTweak("render_resolution", "Render at the resolution you asked for",
                ResolutionSummary(ini),
                RenderMatchesDesired(ini),
                $"ResolutionSize={Value(ini, "ResolutionSizeX")}x{Value(ini, "ResolutionSizeY")}, "
                + $"Desired={Value(ini, "DesiredScreenWidth")}x{Value(ini, "DesiredScreenHeight")}, "
                + $"ResolutionPercentage={Value(ini, "ResolutionPercentage")} (file states minimum {Value(ini, "MinResolutionPercentage")})"),

            // REMOVED 2026-09-07: hdr_output and dynamic_resolution.
            //
            // Both flag pairs (bUseHDRDisplayOutput/bHDROutputEnabled and
            // bUseDynamicResolution/bUserDynResEnabled) really do hold
            // contradictory values, and the writes really did land -- the
            // sidecar recorded originals for all four keys. The game then
            // restored the contradiction on its next launch, twice.
            //
            // So the mismatch is not a stale artefact to be corrected once:
            // something in the game's settings code rewrites one half of each
            // pair and not the other, every launch. An ini edit cannot win
            // against a writer that runs after it. Fixing this means reaching
            // the code at runtime, which is mod territory, not launcher
            // territory.
            //
            // Shipping a toggle that silently loses its value every launch is
            // worse than shipping nothing, because it looks like it worked.
            // Same reason the resolution-scale toggle was removed above.
        ];
    }


    // ── resolution ─────────────────────────────────────────────────────────
    // The file records three different things and they do not agree:
    //   DesiredScreenWidth/Height  what the user asked for
    //   ResolutionSizeX/Y          what the game actually renders
    //   ResolutionPercentage       a further scale applied on top
    //
    // Observed in the wild: Desired 3840x2160, ResolutionSize 1920x1080,
    // ResolutionPercentage 66.7 -- an internal render of roughly 1280x720 on a
    // 4K panel, upscaled twice. The percentage was also BELOW the file's own
    // MinResolutionPercentage of 74.01, which makes it a bug rather than a
    // preference: a value is violating a floor the game itself wrote.
    private static bool RenderMatchesDesired(string ini)
    {
        var desiredW = Number(ini, "DesiredScreenWidth", 0);
        var actualW  = Number(ini, "ResolutionSizeX", 0);
        if (desiredW <= 0 || actualW <= 0) return true;   // nothing to say

        var pct = Number(ini, "ResolutionPercentage", 100);
        var min = Number(ini, "MinResolutionPercentage", 0);
        return Math.Abs(desiredW - actualW) < 1 && pct >= min;
    }

    private static string ResolutionSummary(string ini)
    {
        var desiredW = Number(ini, "DesiredScreenWidth", 0);
        var desiredH = Number(ini, "DesiredScreenHeight", 0);
        var actualW  = Number(ini, "ResolutionSizeX", 0);
        var actualH  = Number(ini, "ResolutionSizeY", 0);
        var pct      = Number(ini, "ResolutionPercentage", 100);
        var min      = Number(ini, "MinResolutionPercentage", 0);

        if (RenderMatchesDesired(ini))
            return "Render resolution matches the resolution you selected.";

        var parts = new List<string>();
        if (Math.Abs(desiredW - actualW) >= 1)
            parts.Add($"you selected {desiredW:0}x{desiredH:0} but the game renders {actualW:0}x{actualH:0}");
        if (pct < min)
            parts.Add($"and then scales that to {pct:0.#}%, below the file's own stated minimum of {min:0.#}%");

        return "Resolution mismatch: " + string.Join(", ", parts)
             + ". Enabling this sets the render resolution to the one you selected and lifts the scale back to the stated minimum.";
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

                case "render_resolution":
                    if (on)
                    {
                        var w = Value(ini, "DesiredScreenWidth");
                        var h = Value(ini, "DesiredScreenHeight");
                        if (w is not null && h is not null)
                        {
                            var wi = ((int)double.Parse(w, CultureInfo.InvariantCulture))
                                .ToString(CultureInfo.InvariantCulture);
                            var hi = ((int)double.Parse(h, CultureInfo.InvariantCulture))
                                .ToString(CultureInfo.InvariantCulture);
                            Stage(ini, originals, writes, "ResolutionSizeX", wi);
                            Stage(ini, originals, writes, "ResolutionSizeY", hi);
                            Stage(ini, originals, writes, "LastUserConfirmedResolutionSizeX", wi);
                            Stage(ini, originals, writes, "LastUserConfirmedResolutionSizeY", hi);
                        }

                        // Only ever raise the scale to the floor the game
                        // itself declared. Not to 100: that would be us
                        // choosing a performance level for the user, and the
                        // defensible claim here is only "stop breaking your
                        // own stated minimum".
                        var pct = Number(ini, "ResolutionPercentage", 100);
                        var min = Number(ini, "MinResolutionPercentage", 0);
                        if (pct < min)
                            Stage(ini, originals, writes, "ResolutionPercentage",
                                min.ToString("0.000000", CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        RestoreIfKnown(originals, writes, "ResolutionSizeX");
                        RestoreIfKnown(originals, writes, "ResolutionSizeY");
                        RestoreIfKnown(originals, writes, "LastUserConfirmedResolutionSizeX");
                        RestoreIfKnown(originals, writes, "LastUserConfirmedResolutionSizeY");
                        RestoreIfKnown(originals, writes, "ResolutionPercentage");
                    }
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

    /// <summary>
    /// Put back the value recorded before we first touched this key. If we
    /// never touched it there is nothing to restore, and inventing a
    /// "default" would be a fresh edit wearing the word revert.
    /// </summary>
    private static void RestoreIfKnown(Dictionary<string, string> originals,
        Dictionary<string, string> writes, string key)
    {
        if (originals.TryGetValue(key, out var was)) writes[key] = was;
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
