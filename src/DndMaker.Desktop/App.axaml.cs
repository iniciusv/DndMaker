using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using DndMaker.Application.Session;
using DndMaker.Desktop.ViewModels;
using DndMaker.Desktop.Views;
using Microsoft.Extensions.DependencyInjection;

namespace DndMaker.Desktop;

public partial class App : Avalonia.Application
{
    private ServiceProvider? _services;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        _services = CompositionRoot.Build();

        // A comparação gravada volta antes da primeira tela; o arquivo é pequeno e a leitura não usa a thread da interface.
        var persistence = _services.GetRequiredService<ComparisonPersistence>();
        Task.Run(() => persistence.RestoreAsync()).GetAwaiter().GetResult();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = _services.GetRequiredService<MainViewModel>(),
            };
            desktop.Exit += (_, _) => Shutdown();
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>
    /// Libera os serviços. O container descarta a <see cref="ComparisonPersistence"/>, que grava o que estiver pendente.
    /// </summary>
    private void Shutdown()
    {
        if (_services is { } services)
        {
            Task.Run(async () => await services.DisposeAsync().ConfigureAwait(false)).GetAwaiter().GetResult();
        }
    }
}
