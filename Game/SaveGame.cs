using System.Text.Json;

namespace SparklesReborn;

public sealed class SaveData
{
    public int GeneratorVersion { get; set; } = StableHash.GeneratorVersion;
    public bool[] Completed { get; set; } = new bool[28];
    public bool[] Unlocked { get; set; } = InitUnlocked();
    public int CurrentLevel { get; set; }
    public long HighScore { get; set; }
    public int ArmorCapacity { get; set; }
    public HashSet<string> DiscoveredCodes { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> SeenStoryIds { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public static bool[] InitUnlocked() { var a = new bool[28]; a[0] = true; return a; }
}

public sealed class SaveManager
{
    private readonly string _path;
    public SaveData Data { get; private set; }

    public SaveManager()
    {
        string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GnomeSnacks", "Sparkles");
        Directory.CreateDirectory(dir);
        _path = Path.Combine(dir, "save.json");
        Data = Load();
    }

    private SaveData Load()
    {
        try
        {
            if (!File.Exists(_path)) return new SaveData();
            SaveData? data = JsonSerializer.Deserialize<SaveData>(File.ReadAllText(_path));
            if (data is null) return new SaveData();
            if (data.Completed.Length != 28) data.Completed = Resize(data.Completed, 28);
            if (data.Unlocked.Length != 28) data.Unlocked = Resize(data.Unlocked, 28);
            data.Unlocked[0] = true;
            data.DiscoveredCodes ??= new(StringComparer.OrdinalIgnoreCase);
            data.SeenStoryIds ??= new(StringComparer.OrdinalIgnoreCase);
            // Version 1 saves were development checkpoints. Preserve progression, but all newly generated
            // campaign stages from this build are explicitly stamped with the v2 generation contract.
            if (data.GeneratorVersion <= 0) data.GeneratorVersion = StableHash.GeneratorVersion;
            return data;
        }
        catch { return new SaveData(); }
    }

    private static bool[] Resize(bool[] old, int n)
    {
        bool[] x = new bool[n]; Array.Copy(old, x, Math.Min(n, old.Length)); return x;
    }

    public void Save()
    {
        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            string temp = _path + ".tmp";
            File.WriteAllText(temp, JsonSerializer.Serialize(Data, options));
            File.Move(temp, _path, true);
        }
        catch { }
    }

    public void NewGame()
    {
        Data = new SaveData();
        Save();
    }

    public void CompleteLevel(int index, long score)
    {
        if (index >= 0 && index < 28)
        {
            Data.Completed[index] = true;
            foreach (int n in Campaign.AllNeighbors(index)) Data.Unlocked[n] = true;
            Data.CurrentLevel = index;
        }
        Data.HighScore = Math.Max(Data.HighScore, score);
        Save();
    }
}
