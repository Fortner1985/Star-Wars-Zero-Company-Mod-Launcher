namespace ZeroSuite.Core;

public enum Health { Ok, Warning, Failed, Unknown }

public sealed record Finding(Health Level, string Title, string Detail, string? Remedy = null);

/// <summary>
/// Reads UE4SS.log and answers the question no mod manager answers: did the
/// loader actually start, and did each mod actually run?
///
/// Every signal here was observed on a real install. The failures below all
/// present to the player identically -- "the mod did nothing" -- with no
/// mod-specific error anywhere, which is precisely why they need naming.
/// </summary>
public static class HealthCheck
{
    public static IReadOnlyList<Finding> Inspect(GameInstall install)
    {
        var findings = new List<Finding>();

        if (install.LegacyLayout)
        {
            findings.Add(new Finding(Health.Warning,
                "Old UE4SS layout",
                "UE4SS.dll sits directly in Win64. Current builds nest it under ue4ss/.",
                "Upgrade the loader; mods and mods.txt are preserved."));
        }

        if (!File.Exists(install.LogPath))
        {
            findings.Add(new Finding(Health.Unknown,
                "No loader log yet",
                "UE4SS.log does not exist. The game has not been launched since UE4SS was installed.",
                "Launch the game once, then re-check."));
            return findings;
        }

        var text = ReadShared(install.LogPath);

        // --- Loader ---
        if (text.Contains("Fatal Error: PS scan timed out", StringComparison.Ordinal))
        {
            findings.Add(new Finding(Health.Failed,
                "Loader failed to start",
                "UE4SS could not resolve the engine signatures and gave up. No Lua mod ran at all.",
                "Repair the runtime: install a UE5-capable UE4SS build."));
        }
        else if (text.Contains("PS scan successful", StringComparison.Ordinal))
        {
            findings.Add(new Finding(Health.Ok, "Loader healthy",
                "UE4SS resolved its signatures and started the Lua VM."));
        }

        if (text.Contains("Failed to find StaticConstructObject_Internal", StringComparison.Ordinal)
            && !text.Contains("StaticConstructObject_Internal address:", StringComparison.Ordinal))
        {
            findings.Add(new Finding(Health.Failed,
                "Missing signature: StaticConstructObject_Internal",
                "A known UE 5.6 defect. Without this the scan never succeeds and no mod loads.",
                "Generate UE4SS_Signatures/StaticConstructObject.lua."));
        }

        // --- Per-mod compile failures ---
        // A Lua chunk over 200 locals fails to compile, and the mod then looks
        // exactly like it was never installed. Cost hours to identify once.
        foreach (var line in text.Split('\n'))
        {
            if (line.Contains("Error loading script", StringComparison.Ordinal) ||
                line.Contains("Failed to execute main script", StringComparison.Ordinal))
            {
                findings.Add(new Finding(Health.Failed,
                    "A mod failed to compile",
                    line.Trim(),
                    "The mod is installed but its script did not load. Fix or revert the script."));
            }
        }

        // --- Which mods actually started ---
        var started = new List<string>();
        const string marker = "Starting Lua mod '";
        var at = 0;
        while ((at = text.IndexOf(marker, at, StringComparison.Ordinal)) >= 0)
        {
            at += marker.Length;
            var end = text.IndexOf('\'', at);
            if (end < 0) break;
            started.Add(text[at..end]);
        }
        if (started.Count > 0)
        {
            findings.Add(new Finding(Health.Ok, $"{started.Count} mod(s) started",
                string.Join(", ", started.Distinct())));
        }

        return findings;
    }

    /// <summary>The game may hold the log open; never fail on a sharing violation.</summary>
    private static string ReadShared(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete);
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
