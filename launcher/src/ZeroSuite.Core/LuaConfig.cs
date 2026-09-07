using System.Text.RegularExpressions;

namespace ZeroSuite.Core;

public sealed class LuaFlag
{
    public required string Key { get; init; }
    public required bool Value { get; set; }
    public required string Description { get; init; }
}

/// <summary>
/// Reads and writes the boolean flags in a mod's config.lua -- for ZeroCam,
/// discovery_mode plus the six per-fix toggles.
///
/// This edits assignments in place with a targeted expression rather than
/// parsing Lua. Rewriting the file from a model would discard the comments,
/// which in this project are load-bearing: they record why a setting exists.
/// </summary>
public static class LuaConfig
{
    private static readonly Dictionary<string, string> Described = new()
    {
        ["discovery_mode"]   = "Log the camera architecture instead of applying fixes",
        ["action_camera"]    = "Block the forced action camera when the setting is off",
        ["blind_spot"]       = "Fix the obstructed view during reinforcements",
        ["enemy_turn_lock"]  = "Free the camera during the enemy turn",
        ["clip_through"]     = "Prevent terrain clip-through",
        ["pitch_black"]      = "Recover from the pitch-black camera bug",
        ["floaty_controls"]  = "Reduce floaty third-person camera movement",
    };

    public static IReadOnlyList<LuaFlag> Read(string path)
    {
        var flags = new List<LuaFlag>();
        if (!File.Exists(path)) return flags;

        foreach (var line in File.ReadAllLines(path))
        {
            var code = CodePart(line);
            if (code.Length == 0) continue;

            foreach (var (key, description) in Described)
            {
                var match = Regex.Match(code, Pattern(key));
                if (!match.Success) continue;
                if (flags.Any(f => f.Key == key)) continue;
                flags.Add(new LuaFlag
                {
                    Key = key,
                    Value = match.Groups["v"].Value == "true",
                    Description = description,
                });
            }
        }
        return flags;
    }

    public static void Write(string path, IReadOnlyDictionary<string, bool> desired)
    {
        var lines = File.ReadAllLines(path);
        for (var i = 0; i < lines.Length; i++)
        {
            // Only the code before a "--" is eligible. The first version
            // regex-replaced the whole line and rewrote a comment that read
            // "(only apply when discovery_mode = false)" into "= true",
            // silently turning documentation into a lie.
            var line = lines[i];
            var split = CommentIndex(line);
            var code = split < 0 ? line : line[..split];
            var comment = split < 0 ? "" : line[split..];

            foreach (var (key, value) in desired)
            {
                code = Regex.Replace(code, Pattern(key),
                    m => m.Groups["p"].Value + (value ? "true" : "false"));
            }
            lines[i] = code + comment;
        }
        Backup.Once(path);
        File.WriteAllLines(path, lines);
    }

    /// <summary>The executable part of a line, with any trailing comment removed.</summary>
    private static string CodePart(string line)
    {
        var at = CommentIndex(line);
        return (at < 0 ? line : line[..at]).Trim();
    }

    /// <summary>
    /// Index of the "--" that starts a comment, or -1. Quotes are tracked so a
    /// "--" inside a string literal is not mistaken for one.
    /// </summary>
    private static int CommentIndex(string line)
    {
        var quote = '\0';
        for (var i = 0; i < line.Length - 1; i++)
        {
            var c = line[i];
            if (quote != '\0')
            {
                if (c == '\\') { i++; continue; }
                if (c == quote) quote = '\0';
                continue;
            }
            if (c is '"' or '\'') { quote = c; continue; }
            if (c == '-' && line[i + 1] == '-') return i;
        }
        return -1;
    }

    // Captures the assignment prefix so the replacement keeps the original
    // indentation and spacing exactly as the author wrote it.
    private static string Pattern(string key)
        => @"(?<p>\b" + Regex.Escape(key) + @"\s*=\s*)(?<v>true|false)";
}
