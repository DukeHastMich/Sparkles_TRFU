using System.Text.Json;

namespace SparklesReborn;

public enum ControlAction
{
    MoveLeft, MoveRight, MoveUp, MoveDown,
    Jump, Dash, Joust, Stomp, Belch, Pause
}

public sealed class GameSettings
{
    public bool MusicEnabled { get; set; } = true;
    public float MasterVolume { get; set; } = .78f;
    public bool ScreenShake { get; set; } = true;
    public bool ReduceFlashes { get; set; }
    public bool ShowSeed { get; set; } = true;
    public bool ShowFps { get; set; }
    public Dictionary<ControlAction, Keys> KeyBindings { get; set; } = Defaults();

    public static Dictionary<ControlAction, Keys> Defaults() => new()
    {
        [ControlAction.MoveLeft] = Keys.A,
        [ControlAction.MoveRight] = Keys.D,
        [ControlAction.MoveUp] = Keys.W,
        [ControlAction.MoveDown] = Keys.S,
        [ControlAction.Jump] = Keys.Space,
        [ControlAction.Dash] = Keys.ShiftKey,
        [ControlAction.Joust] = Keys.X,
        [ControlAction.Stomp] = Keys.C,
        [ControlAction.Belch] = Keys.F,
        [ControlAction.Pause] = Keys.Escape
    };

    public void EnsureBindings()
    {
        KeyBindings ??= Defaults();
        foreach ((ControlAction action, Keys key) in Defaults())
            if (!KeyBindings.ContainsKey(action)) KeyBindings[action] = key;
        MasterVolume = Math.Clamp(MasterVolume, 0f, 1f);
    }
}

public sealed class SettingsManager
{
    private readonly string _path;
    public GameSettings Data { get; private set; }

    public SettingsManager()
    {
        string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GnomeSnacks", "Sparkles");
        Directory.CreateDirectory(dir);
        _path = Path.Combine(dir, "settings.json");
        Data = Load();
    }

    private GameSettings Load()
    {
        try
        {
            if (!File.Exists(_path)) return new GameSettings();
            GameSettings? data = JsonSerializer.Deserialize<GameSettings>(File.ReadAllText(_path));
            data ??= new GameSettings();
            data.EnsureBindings();
            return data;
        }
        catch { return new GameSettings(); }
    }

    public void Save()
    {
        try
        {
            Data.EnsureBindings();
            JsonSerializerOptions options = new() { WriteIndented = true };
            string temp = _path + ".tmp";
            File.WriteAllText(temp, JsonSerializer.Serialize(Data, options));
            File.Move(temp, _path, true);
        }
        catch { }
    }

    public void ResetControls()
    {
        Data.KeyBindings = GameSettings.Defaults();
        Save();
    }
}
