using Avalonia;

namespace DndMaker.Desktop;

internal static class Program
{
    // Nada de Avalonia ou código que dependa de SynchronizationContext antes de AppMain.
    [STAThread]
    public static void Main(string[] args) =>
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

    // Usado também pelo designer visual.
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .LogToTrace();
}
