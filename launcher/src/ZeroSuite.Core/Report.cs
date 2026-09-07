using System.Text.Json;
using System.Text.Json.Serialization;

namespace ZeroSuite.Core;

/// <summary>
/// The whole of what ZeroSuite knows, as data. The GUI renders this; a
/// headless caller can assert against it. Having one source for both is what
/// makes the tool testable without a desktop session -- WPF cannot start in a
/// non-interactive session at all, so a GUI-only design is a design that
/// cannot be verified by automation.
/// </summary>
public sealed record Report(
    [property: JsonPropertyName("found")] bool Found,
    [property: JsonPropertyName("root")] string? Root,
    [property: JsonPropertyName("legacyLayout")] bool LegacyLayout,
    [property: JsonPropertyName("findings")] IReadOnlyList<ReportFinding> Findings,
    [property: JsonPropertyName("mods")] IReadOnlyList<ReportToggle> Mods,
    [property: JsonPropertyName("fixes")] IReadOnlyList<ReportToggle> Fixes,
    [property: JsonPropertyName("display")] IReadOnlyList<ReportToggle> Display,
    [property: JsonPropertyName("gameRunning")] bool GameRunning);

public sealed record ReportFinding(
    [property: JsonPropertyName("level")] string Level,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("detail")] string Detail,
    [property: JsonPropertyName("remedy")] string? Remedy);

public sealed record ReportToggle(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("enabled")] bool Enabled,
    [property: JsonPropertyName("editable")] bool Editable);

public static class Reporter
{
    private static readonly JsonSerializerOptions Pretty = new() { WriteIndented = true };

    public static Report Build(string? manualRoot = null)
    {
        var install = GameLocator.Locate(manualRoot);
        if (install is null)
            return new Report(false, null, false, [], [], [], [], false);

        var modsPath = Path.Combine(install.ModsDir, "mods.txt");
        var configPath = Path.Combine(install.ModsDir, "ZeroCam", "Scripts", "config.lua");

        return new Report(
            Found: true,
            Root: install.Root,
            LegacyLayout: install.LegacyLayout,
            Findings: HealthCheck.Inspect(install)
                .Select(f => new ReportFinding(f.Level.ToString(), f.Title, f.Detail, f.Remedy))
                .ToList(),
            Mods: ModsTxt.Read(modsPath)
                .Select(m => new ReportToggle(m.Name, m.Enabled, !m.IsBuiltIn))
                .ToList(),
            Fixes: LuaConfig.Read(configPath)
                .Select(f => new ReportToggle(f.Key, f.Value, true))
                .ToList(),
            Display: GameSettings.Read()
                .Select(d => new ReportToggle(d.Key, d.Enabled, true))
                .ToList(),
            GameRunning: GameSettings.IsGameRunning());
    }

    public static string ToJson(Report report) => JsonSerializer.Serialize(report, Pretty);
}
