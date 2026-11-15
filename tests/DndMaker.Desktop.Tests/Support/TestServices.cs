using DndMaker.Application;
using DndMaker.Application.Session;
using DndMaker.Content;
using DndMaker.Desktop.Services;
using DndMaker.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace DndMaker.Desktop.Tests.Support;

/// <summary>Despachante que roda a ação na hora, na thread do teste.</summary>
internal sealed class ImmediateDispatcher : IUiDispatcher
{
    public void Post(Action action) => action();
}

/// <summary>Repositório em memória que pode ser mandado falhar.</summary>
internal sealed class MemoryRepository : IComparisonRepository
{
    public ComparisonSnapshot? Stored { get; private set; }

    public bool Fail { get; set; }

    public Task<ComparisonSnapshot?> LoadAsync(CancellationToken cancellationToken = default) => Task.FromResult(Stored);

    public Task SaveAsync(ComparisonSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        if (Fail)
        {
            throw new IOException("Disco cheio.");
        }

        Stored = snapshot;
        return Task.CompletedTask;
    }
}

/// <summary>As camadas reais com repositório em memória e despachante imediato.</summary>
internal static class TestServices
{
    public static ServiceProvider Build(MemoryRepository? repository = null) =>
        new ServiceCollection()
            .AddCurseOfStrahdContent()
            .AddApplication()
            .AddSingleton<IComparisonRepository>(repository ?? new MemoryRepository())
            .AddSingleton<IUiDispatcher, ImmediateDispatcher>()
            .AddSingleton<MainViewModel>()
            .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });
}
