namespace ZeroSuite.Core;

public sealed class ModEntry
{
    public required string Name { get; init; }
    public required bool Enabled { get; set; }
    /// <summary>Built-in UE4SS mods are shown but not offered as toggles.</summary>
    public bool IsBuiltIn => Name is "Keybinds" or "BPModLoaderMod" or "BPML_GenericFunctions";
}

/// <summary>
/// Reads and writes UE4SS's mods.txt without disturbing it. The file is
/// order-sensitive -- "Keybinds : 1" must stay last, and its own comment says
/// so -- and it carries blank lines and comments that mean something to the
/// person who wrote them. So this rewrites values in place, line by line,
/// rather than regenerating the file from a model.
/// </summary>
public static class ModsTxt
{
    public static IReadOnlyList<ModEntry> Read(string path)
    {
        var entries = new List<ModEntry>();
        if (!File.Exists(path)) return entries;

        foreach (var raw in File.ReadAllLines(path))
        {
            if (!TryParse(raw, out var name, out var enabled)) continue;
            entries.Add(new ModEntry { Name = name, Enabled = enabled });
        }
        return entries;
    }

    public static void Write(string path, IReadOnlyDictionary<string, bool> desired)
    {
        var lines = File.ReadAllLines(path);
        for (var i = 0; i < lines.Length; i++)
        {
            if (!TryParse(lines[i], out var name, out _)) continue;
            if (!desired.TryGetValue(name, out var enabled)) continue;

            // Preserve the author's spacing; only the flag changes.
            var colon = lines[i].IndexOf(':');
            lines[i] = string.Concat(lines[i].AsSpan(0, colon + 1), " ", enabled ? "1" : "0");
        }
        Backup.Once(path);
        File.WriteAllLines(path, lines);
    }

    private static bool TryParse(string raw, out string name, out bool enabled)
    {
        name = ""; enabled = false;
        var line = raw.Trim();
        if (line.Length == 0 || line.StartsWith(';')) return false;

        var colon = line.IndexOf(':');
        if (colon <= 0) return false;

        name = line[..colon].Trim();
        var value = line[(colon + 1)..].Trim();
        if (name.Length == 0) return false;

        enabled = value.StartsWith('1');
        return true;
    }
}

/// <summary>
/// A one-time .zsbak beside any file before its first modification. One-time
/// on purpose: a rolling backup would eventually hold a copy of our own bad
/// write, which is the opposite of what a backup is for.
/// </summary>
public static class Backup
{
    public static void Once(string path)
    {
        var backup = path + ".zsbak";
        if (!File.Exists(backup) && File.Exists(path))
            File.Copy(path, backup);
    }
}
