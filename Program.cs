using System.Runtime.InteropServices;

namespace SparklesReborn;

internal static class Program
{
    [DllImport("user32.dll")]
    private static extern bool SetProcessDPIAware();

    [STAThread]
    private static void Main()
    {
        try { SetProcessDPIAware(); } catch { }
        ApplicationConfiguration.Initialize();
        Application.Run(new GameForm());
    }
}
