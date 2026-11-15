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
        // Os serviços só sobem no programa de verdade; nos testes sem janela o App serve apenas de tema.
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var services = CompositionRoot.Build();
            _services = services;

            // A comparação gravada volta antes da primeira tela; o arquivo é pequeno e a leitura não usa a thread da interface.
            var persistence = services.GetRequiredService<ComparisonPersistence>();
            Task.Run(() => persistence.RestoreAsync()).GetAwaiter().GetResult();

            desktop.MainWindow = new MainWindow
            {
                DataContext = services.GetRequiredService<MainViewModel>(),
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
