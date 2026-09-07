using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Media;
using ZeroSuite.Core;

namespace ZeroSuite.App;

public sealed class HealthRow
{
    public required string Title { get; init; }
    public required string Detail { get; init; }
    public string Remedy { get; init; } = "";
    public required Brush Accent { get; init; }
    public Visibility RemedyVisibility =>
        string.IsNullOrEmpty(Remedy) ? Visibility.Collapsed : Visibility.Visible;
}

public sealed class ModRow : INotifyPropertyChanged
{
    public required string Name { get; init; }
    public required bool Editable { get; init; }
    /// <summary>The value read from disk, for change detection at Apply.</summary>
    public bool Loaded { get; init; }
    private bool _enabled;
    public bool Enabled
    {
        get => _enabled;
        set { _enabled = value; PropertyChanged?.Invoke(this, new(nameof(Enabled))); }
    }
    public event PropertyChangedEventHandler? PropertyChanged;
}

public sealed class DisplayRow : INotifyPropertyChanged
{
    public required string Key { get; init; }
    public required string Description { get; init; }
    public required string Detail { get; init; }
    private bool _value;
    public bool Value
    {
        get => _value;
        set { _value = value; PropertyChanged?.Invoke(this, new(nameof(Value))); }
    }
    public event PropertyChangedEventHandler? PropertyChanged;
}

public sealed class FixRow : INotifyPropertyChanged
{
    public required string Key { get; init; }
    public required string Description { get; init; }
    public bool Loaded { get; init; }
    private bool _value;
    public bool Value
    {
        get => _value;
        set { _value = value; PropertyChanged?.Invoke(this, new(nameof(Value))); }
    }
    public event PropertyChangedEventHandler? PropertyChanged;
}

public partial class MainWindow : Window
{
    private GameInstall? _install;
    private readonly ObservableCollection<HealthRow> _health = new();
    private readonly ObservableCollection<ModRow> _mods = new();
    private readonly ObservableCollection<FixRow> _fixes = new();
    private readonly ObservableCollection<DisplayRow> _display = new();

    public MainWindow()
    {
        InitializeComponent();
        HealthList.ItemsSource = _health;
        ModList.ItemsSource = _mods;
        FixList.ItemsSource = _fixes;
        DisplayList.ItemsSource = _display;
        Load();
    }

    private string ZeroCamConfig =>
        Path.Combine(_install!.ModsDir, "ZeroCam", "Scripts", "config.lua");

    private void Load()
    {
        _health.Clear(); _mods.Clear(); _fixes.Clear(); _display.Clear();

        _install = GameLocator.Locate();
        if (_install is null)
        {
            PathText.Text = "Zero Company was not found in any Steam library.";
            ApplyButton.IsEnabled = false;
            return;
        }

        PathText.Text = _install.Root +
            (_install.LegacyLayout ? "   (legacy UE4SS layout)" : "");

        foreach (var f in HealthCheck.Inspect(_install))
        {
            _health.Add(new HealthRow
            {
                Title = f.Title,
                Detail = f.Detail,
                Remedy = f.Remedy ?? "",
                Accent = f.Level switch
                {
                    Health.Ok => (Brush)FindResource("Ok"),
                    Health.Warning => (Brush)FindResource("Warn"),
                    Health.Failed => (Brush)FindResource("Fail"),
                    _ => (Brush)FindResource("Muted"),
                },
            });
        }

        foreach (var m in ModsTxt.Read(Path.Combine(_install.ModsDir, "mods.txt")))
        {
            // Built-ins are shown for completeness but not offered as toggles:
            // Keybinds in particular must stay enabled and last, and mods.txt
            // says so in its own comment.
            _mods.Add(new ModRow { Name = m.Name, Enabled = m.Enabled, Loaded = m.Enabled, Editable = !m.IsBuiltIn });
        }

        foreach (var f in LuaConfig.Read(ZeroCamConfig))
            _fixes.Add(new FixRow { Key = f.Key, Description = f.Description, Value = f.Value, Loaded = f.Value });

        foreach (var d in GameSettings.Read())
        {
            _display.Add(new DisplayRow
            {
                Key = d.Title, Description = d.Description, Detail = d.Detail, Value = d.Enabled,
            });
        }

        // The game rewrites GameUserSettings.ini on exit, so anything written
        // while it is open is discarded without warning. Say so up front
        // rather than letting a change look like it silently failed.
        DisplayNote.Text = GameSettings.IsGameRunning()
            ? "The game is running. Close it before applying display changes, or it will overwrite them on exit."
            : "Reversible: the previous value of each setting is recorded before it is changed.";

        StatusText.Text = $"{_mods.Count} mods, {_fixes.Count} fixes";
        RefreshBridge();
    }

    private void OnRefresh(object sender, RoutedEventArgs e) => Load();

    private void RefreshBridge()
    {
        if (_install is null) return;
        var installed = Bridge.IsInstalled(_install);
        DumpObjectsButton.IsEnabled = installed;
        DumpUsmapButton.IsEnabled = installed;

        if (!installed)
        {
            BridgeText.Text = "ZeroSuiteBridge mod is not installed.";
            return;
        }
        var newest = Bridge.Artefacts(_install).FirstOrDefault();
        var last = Bridge.LastResult(_install);
        BridgeText.Text = newest.Name is null
            ? $"bridge: {last ?? "no run yet"}"
            : $"bridge: {last ?? "-"}   |   {newest.Name} ({newest.Bytes / 1024 / 1024} MB)";
    }

    private void RequestDump(string command)
    {
        if (_install is null) return;
        if (!GameSettings.IsGameRunning())
        {
            MessageBox.Show(
                "Start the game first, and get into a combat mission.\n\n" +
                "The dumpers only record what is currently loaded, so a dump taken " +
                "at the main menu misses the combat classes entirely.",
                "ZeroSuite", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        Bridge.Request(_install, command);
        BridgeText.Text = $"requested '{command}' - the game will freeze while it writes";
    }

    private void OnDumpObjects(object sender, RoutedEventArgs e) => RequestDump("objects");
    private void OnDumpUsmap(object sender, RoutedEventArgs e) => RequestDump("usmap");

    /// <summary>True when a toggle's on-disk value no longer matches what was loaded.</summary>
    private bool ChangedOnDisk()
    {
        if (_install is null) return false;
        var mods = ModsTxt.Read(Path.Combine(_install.ModsDir, "mods.txt"));
        foreach (var row in _mods)
        {
            var live = mods.FirstOrDefault(m => m.Name == row.Name);
            if (live is not null && live.Enabled != row.Loaded) return true;
        }
        var fixes = LuaConfig.Read(ZeroCamConfig);
        foreach (var row in _fixes)
        {
            var live = fixes.FirstOrDefault(f => f.Key == row.Key);
            if (live is not null && live.Value != row.Loaded) return true;
        }
        return false;
    }

    private void OnApply(object sender, RoutedEventArgs e)
    {
        if (_install is null) return;

        // Warn if the files changed under us. The window holds the state it
        // read at load; applying a stale snapshot silently reverts whatever
        // changed since -- which happened during testing and cost a round of
        // confusion.
        if (ChangedOnDisk())
        {
            var answer = MessageBox.Show(
                "These settings changed on disk since this window was loaded.\n\n" +
                "Apply anyway and overwrite them, or reload first?\n\n" +
                "Yes = overwrite    No = reload and discard my changes",
                "ZeroSuite", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (answer == MessageBoxResult.No) { Load(); return; }
        }

        try
        {
            var modsPath = Path.Combine(_install.ModsDir, "mods.txt");
            if (File.Exists(modsPath))
                ModsTxt.Write(modsPath, _mods.ToDictionary(m => m.Name, m => m.Enabled));

            if (File.Exists(ZeroCamConfig))
                LuaConfig.Write(ZeroCamConfig, _fixes.ToDictionary(f => f.Key, f => f.Value));

            var tweaks = GameSettings.Read();
            var gameRunning = GameSettings.IsGameRunning();
            var displayApplied = false;

            if (tweaks.Count == _display.Count && !gameRunning)
            {
                GameSettings.Apply(
                    tweaks.Select((t, i) => (t.Key, _display[i].Value))
                          .ToDictionary(x => x.Key, x => x.Item2));
                displayApplied = true;
            }

            // Say what actually happened. Reporting "Saved" while the display
            // section was skipped is the same failure this tool exists to
            // catch -- a program giving a confident answer about a state it
            // did not achieve.
            StatusText.Text =
                gameRunning && _display.Count > 0
                    ? "Mods saved. Display changes SKIPPED - the game is running."
                    : displayApplied
                        ? "Saved. Restart the game to apply."
                        : "Mods saved.";
        }
        catch (Exception ex)
        {
            // Writing into Program Files needs elevation; say so rather than
            // failing silently with a stack trace the player cannot act on.
            MessageBox.Show(
                ex is UnauthorizedAccessException
                    ? "Could not write to the game folder. Run ZeroSuite as administrator."
                    : ex.Message,
                "ZeroSuite", MessageBoxButton.OK, MessageBoxImage.Warning);
            StatusText.Text = "Not saved.";
        }
    }
}
