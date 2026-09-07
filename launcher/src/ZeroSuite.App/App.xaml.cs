using System.IO;
using System.Windows;
using System.Windows.Threading;
using ZeroSuite.Core;

namespace ZeroSuite.App;

public partial class App : Application
{
    // A WPF app has no console, so an unhandled startup exception closes the
    // window with nothing to look at. Write it down instead.
    private static readonly string CrashLog =
        Path.Combine(Path.GetTempPath(), "zerosuite-crash.txt");

    protected override void OnStartup(StartupEventArgs e)
    {
        DispatcherUnhandledException += OnDispatcherException;
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            Record(args.ExceptionObject as Exception);
        base.OnStartup(e);

        // Headless mode. WPF cannot initialise in a non-interactive session --
        // its font cache throws before any window exists -- so a GUI-only
        // build is one that automation can never exercise. This path touches
        // no WPF types and exits without a window.
        //
        //   ZeroSuite.exe --json <out.json> [game root]
        //   ZeroSuite.exe --set <key>=<true|false> ... [--root <path>]
        if (TryHeadless(e.Args)) { Shutdown(0); return; }

        // Shown explicitly rather than via StartupUri: with StartupUri the
        // process started and exited 0 without ever presenting a window.
        var window = new MainWindow();
        MainWindow = window;
        window.Show();
    }

    private static bool TryHeadless(string[] args)
    {
        if (args.Length == 0) return false;

        var root = ValueAfter(args, "--root");

        if (args[0] == "--json")
        {
            var outPath = args.Length > 1 ? args[1] : "zerosuite.json";
            File.WriteAllText(outPath, Reporter.ToJson(Reporter.Build(root)));
            return true;
        }

        if (args[0] == "--set")
        {
            var install = GameLocator.Locate(root);
            if (install is null) return true;

            var mods = new Dictionary<string, bool>();
            var fixes = new Dictionary<string, bool>();
            var display = new Dictionary<string, bool>();
            var knownDisplay = GameSettings.Read().Select(d => d.Key).ToHashSet();
            var knownFixes = LuaConfig
                .Read(Path.Combine(install.ModsDir, "ZeroCam", "Scripts", "config.lua"))
                .Select(f => f.Key).ToHashSet();

            foreach (var pair in args.Skip(1).TakeWhile(a => !a.StartsWith("--")))
            {
                var split = pair.Split('=', 2);
                if (split.Length != 2) continue;
                var on = split[1].Equals("true", StringComparison.OrdinalIgnoreCase);
                if (knownFixes.Contains(split[0])) fixes[split[0]] = on;
                else if (knownDisplay.Contains(split[0])) display[split[0]] = on;
                else mods[split[0]] = on;
            }

            if (mods.Count > 0)
                ModsTxt.Write(Path.Combine(install.ModsDir, "mods.txt"), mods);
            if (fixes.Count > 0)
                LuaConfig.Write(
                    Path.Combine(install.ModsDir, "ZeroCam", "Scripts", "config.lua"), fixes);
            if (display.Count > 0)
                GameSettings.Apply(display);
            return true;
        }

        return false;
    }

    private static string? ValueAfter(string[] args, string flag)
    {
        var at = Array.IndexOf(args, flag);
        return at >= 0 && at + 1 < args.Length ? args[at + 1] : null;
    }

    private void OnDispatcherException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Record(e.Exception);
        MessageBox.Show($"ZeroSuite hit an error:\n\n{e.Exception.Message}\n\nDetails: {CrashLog}",
            "ZeroSuite", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }

    private static void Record(Exception? ex)
    {
        if (ex is null) return;
        try { File.WriteAllText(CrashLog, $"{DateTime.Now:u}\n{ex}"); } catch { }
    }
}
