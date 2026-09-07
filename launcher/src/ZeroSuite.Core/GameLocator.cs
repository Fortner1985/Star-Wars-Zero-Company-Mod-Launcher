namespace ZeroSuite.Core;

/// <summary>A located Zero Company installation.</summary>
public sealed record GameInstall(
    string Root,
    string Executable,
    string Ue4ssDir,
    string ModsDir,
    string LogPath,
    bool LegacyLayout);

/// <summary>
/// Finds Star Wars Zero Company (Steam App ID 2075800) by parsing Steam's
/// libraryfolders.vdf across every library root, rather than assuming the
/// default install path. Verified against a real install: the game lives at
/// &lt;root&gt;/SWZeroCompany/Binaries/Win64.
/// </summary>
public static class GameLocator
{
    public const string AppId = "2075800";
    private const string GameFolder = "Star Wars Zero Company";

    public static GameInstall? Locate(string? manualRoot = null)
    {
        if (!string.IsNullOrWhiteSpace(manualRoot))
            return Describe(manualRoot!);

        foreach (var library in SteamLibraries())
        {
            var candidate = Path.Combine(library, "steamapps", "common", GameFolder);
            if (Directory.Exists(candidate))
            {
                var install = Describe(candidate);
                if (install is not null) return install;
            }
        }
        return null;
    }

    /// <summary>Every Steam library root, starting with the Steam install itself.</summary>
    public static IEnumerable<string> SteamLibraries()
    {
        var steam = SteamRoot();
        if (steam is null) yield break;
        yield return steam;

        var vdf = Path.Combine(steam, "steamapps", "libraryfolders.vdf");
        if (!File.Exists(vdf)) yield break;

        // Minimal VDF read: we only need "path" values. A full parser would be
        // more code for no extra capability here.
        foreach (var line in File.ReadLines(vdf))
        {
            var trimmed = line.Trim();
            if (!trimmed.StartsWith("\"path\"", StringComparison.OrdinalIgnoreCase)) continue;
            var parts = trimmed.Split('"', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 3) continue;
            var path = parts[^1].Replace("\\\\", "\\");
            if (Directory.Exists(path) && !string.Equals(path, steam, StringComparison.OrdinalIgnoreCase))
                yield return path;
        }
    }

    /// <summary>
    /// Steam's install folder. Deliberately not reliant on a single
    /// environment variable: the first version used ProgramFiles(x86) alone
    /// and found nothing on a machine where the game was plainly installed.
    /// Tries the well-known folders, then every fixed drive.
    /// </summary>
    private static string? SteamRoot()
    {
        foreach (var candidate in Candidates())
        {
            if (!string.IsNullOrWhiteSpace(candidate) &&
                Directory.Exists(Path.Combine(candidate, "steamapps")))
            {
                return candidate;
            }
        }
        return null;

        static IEnumerable<string?> Candidates()
        {
            yield return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam");
            yield return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Steam");
            yield return Environment.GetEnvironmentVariable("ProgramFiles(x86)") is string p86
                ? Path.Combine(p86, "Steam") : null;

            foreach (var drive in DriveInfo.GetDrives())
            {
                if (drive.DriveType != DriveType.Fixed || !drive.IsReady) continue;
                yield return Path.Combine(drive.RootDirectory.FullName, "Steam");
                yield return Path.Combine(drive.RootDirectory.FullName, "SteamLibrary");
                yield return Path.Combine(drive.RootDirectory.FullName, "Games", "Steam");
            }
        }
    }

    private static GameInstall? Describe(string root)
    {
        var win64 = Path.Combine(root, "SWZeroCompany", "Binaries", "Win64");
        var exe = Path.Combine(win64, "SWZeroCompany.exe");
        if (!File.Exists(exe)) return null;

        // Current UE4SS builds nest everything under ue4ss/. Older ones put
        // UE4SS.dll and Mods/ directly in Win64. Both are recognised so the
        // bootstrap can report and migrate rather than fail.
        var nested = Path.Combine(win64, "ue4ss");
        var legacy = !Directory.Exists(nested);
        var ue4ss = legacy ? win64 : nested;

        return new GameInstall(
            Root: root,
            Executable: exe,
            Ue4ssDir: ue4ss,
            ModsDir: Path.Combine(ue4ss, "Mods"),
            LogPath: Path.Combine(ue4ss, "UE4SS.log"),
            LegacyLayout: legacy);
    }
}
