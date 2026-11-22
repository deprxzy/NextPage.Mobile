using Microsoft.Maui.Storage;
namespace NextPage.Mobile.Diagnostics;

public static class AppDiagnostics
{
    private static readonly object Gate = new();
    private static bool installed;
    public static void Install()
    {
        if (installed) return;
        installed = true;
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            if (e.ExceptionObject is Exception ex) Log(ex);
        };
        TaskScheduler.UnobservedTaskException += (_, e) => Log(e.Exception);
#if ANDROID
        Android.Runtime.AndroidEnvironment.UnhandledExceptionRaiser += (_, e) => Log(e.Exception);
#endif
    }
    public static void Log(Exception exception)
    {
        System.Diagnostics.Debug.WriteLine(exception);
        try
        {
            lock (Gate)
            {
                var path = Path.Combine(FileSystem.AppDataDirectory, "nextpage-diagnostics.log");
                if (File.Exists(path) && new FileInfo(path).Length > 200_000)
                    File.Move(path, path + ".previous", true);
                File.AppendAllText(path, $"{DateTimeOffset.UtcNow:O}\n{exception}\n\n");
            }
        }
        catch { }
    }
}
