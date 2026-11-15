using DndMaker.Application.Session;

namespace DndMaker.Application.Tests.Fakes;

/// <summary>Repositório em memória que conta as gravações e pode ser mandado falhar.</summary>
internal sealed class InMemoryComparisonRepository(ComparisonSnapshot? stored = null) : IComparisonRepository
{
    public ComparisonSnapshot? Stored { get; private set; } = stored;

    public int SaveCount { get; private set; }

    public bool FailNextSave { get; set; }

    public Task<ComparisonSnapshot?> LoadAsync(CancellationToken cancellationToken = default) => Task.FromResult(Stored);

    public Task SaveAsync(ComparisonSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        if (FailNextSave)
        {
            FailNextSave = false;
            throw new IOException("Disco cheio.");
        }

        Stored = snapshot;
        SaveCount++;
        return Task.CompletedTask;
    }
}
