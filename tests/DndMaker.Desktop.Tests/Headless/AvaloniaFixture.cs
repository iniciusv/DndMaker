using Avalonia;
using Avalonia.Headless;
using Avalonia.Logging;

namespace DndMaker.Desktop.Tests.Headless;

/// <summary>O App real (tema e estilos) numa plataforma sem tela, desenhando com Skia para permitir capturas.</summary>
public static class HeadlessEntryPoint
{
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UseSkia()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
}

/// <summary>Uma sessão do Avalonia para todos os testes de janela, com a thread da interface própria.</summary>
public sealed class AvaloniaFixture : IDisposable
{
    public AvaloniaFixture()
    {
        Logger.Sink = Log;
        Session = HeadlessUnitTestSession.StartNew(typeof(HeadlessEntryPoint));
    }

    public HeadlessUnitTestSession Session { get; }

    /// <summary>Avisos de ligação de dados e de layout registrados pelo Avalonia.</summary>
    public CollectingLogSink Log { get; } = new();

    public void Dispose() => Session.Dispose();
}

[CollectionDefinition(Name)]
public sealed class AvaloniaTestGroup : ICollectionFixture<AvaloniaFixture>
{
    public const string Name = "Avalonia";
}

/// <summary>Guarda os avisos de ligação de dados (binding) para o teste conferir.</summary>
public sealed class CollectingLogSink : ILogSink
{
    private readonly List<string> _messages = [];

    public IReadOnlyList<string> Messages
    {
        get
        {
            lock (_messages)
            {
                return [.. _messages];
            }
        }
    }

    public void Clear()
    {
        lock (_messages)
        {
            _messages.Clear();
        }
    }

    public bool IsEnabled(LogEventLevel level, string area) =>
        level >= LogEventLevel.Warning && area == LogArea.Binding;

    public void Log(LogEventLevel level, string area, object? source, string messageTemplate) =>
        Log(level, area, source, messageTemplate, []);

    public void Log(LogEventLevel level, string area, object? source, string messageTemplate, params object?[] propertyValues)
    {
        if (!IsEnabled(level, area))
        {
            return;
        }

        var text = $"{area} [{source?.GetType().Name}] {messageTemplate} :: {string.Join(" | ", propertyValues)}";
        lock (_messages)
        {
            _messages.Add(text);
        }
    }
}
