using System.Reflection;

namespace SparklesReborn;

public sealed class AssetStore : IDisposable
{
    private readonly Dictionary<string, Bitmap> _bitmaps = new(StringComparer.OrdinalIgnoreCase);
    private readonly Assembly _assembly = Assembly.GetExecutingAssembly();

    public Bitmap Bitmap(string name)
    {
        if (_bitmaps.TryGetValue(name, out var found)) return found;
        string suffix = ".Assets." + name.Replace('/', '.').Replace('\\', '.');
        string? resource = _assembly.GetManifestResourceNames().FirstOrDefault(n => n.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));
        if (resource is null) throw new FileNotFoundException($"Embedded asset not found: {name}");
        using Stream stream = _assembly.GetManifestResourceStream(resource)!;
        using Image temp = Image.FromStream(stream);
        var bmp = new Bitmap(temp.Width, temp.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using (Graphics g = Graphics.FromImage(bmp)) g.DrawImageUnscaled(temp, 0, 0);
        _bitmaps[name] = bmp;
        return bmp;
    }

    public void Dispose()
    {
        foreach (var b in _bitmaps.Values) b.Dispose();
        _bitmaps.Clear();
    }
}
