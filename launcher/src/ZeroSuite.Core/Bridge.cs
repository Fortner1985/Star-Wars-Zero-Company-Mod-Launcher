namespace ZeroSuite.Core;

/// <summary>
/// Requests a UE4SS dump by writing a one-word file the ZeroSuiteBridge mod
/// polls for.
///
/// The keybind route (Ctrl+J) produced nothing on this install: UE4SS's
/// console and GUI are both disabled and the log recorded no ObjectDumper
/// activity, so the keystroke never arrived. A file is unambiguous, works
/// while the game is running, and reports back what happened.
/// </summary>
public static class Bridge
{
    public static readonly string[] Commands = ["objects", "usmap", "sdk", "actors"];

    private static string RequestPath(GameInstall i) => Path.Combine(i.Ue4ssDir, "zerosuite-request.txt");
    private static string ResultPath(GameInstall i) => Path.Combine(i.Ue4ssDir, "zerosuite-result.txt");

    public static bool IsInstalled(GameInstall install) =>
        File.Exists(Path.Combine(install.ModsDir, "ZeroSuiteBridge", "Scripts", "main.lua"));

    public static void Request(GameInstall install, string command)
    {
        if (!Commands.Contains(command)) return;
        File.WriteAllText(RequestPath(install), command);
    }

    /// <summary>The bridge's last message, or null if it has never run.</summary>
    public static string? LastResult(GameInstall install)
    {
        var path = ResultPath(install);
        if (!File.Exists(path)) return null;
        try { return File.ReadAllText(path).Trim(); } catch { return null; }
    }

    /// <summary>Dump artefacts UE4SS has produced, newest first.</summary>
    public static IReadOnlyList<(string Name, long Bytes, DateTime When)> Artefacts(GameInstall install)
    {
        var results = new List<(string, long, DateTime)>();
        foreach (var pattern in new[] { "*ObjectDump*", "*.usmap", "*Actors*" })
        {
            foreach (var file in Directory.EnumerateFiles(install.Ue4ssDir, pattern))
            {
                var info = new FileInfo(file);
                results.Add((info.Name, info.Length, info.LastWriteTime));
            }
        }
        return results.OrderByDescending(r => r.Item3).ToList();
    }
}
