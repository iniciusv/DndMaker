namespace DndMaker.Application.Session;

/// <summary>
/// Liga a sessão ao repositório: restaura a comparação ao abrir e grava sozinha
/// <see cref="SaveDelay"/> depois da última mudança. <see cref="FlushAsync"/> grava na hora o que estiver pendente
/// (ao fechar o programa). Falhas de gravação não derrubam o programa: viram o evento <see cref="SaveFailed"/>.
/// </summary>
public sealed class ComparisonPersistence : IAsyncDisposable
{
    public static readonly TimeSpan SaveDelay = TimeSpan.FromMilliseconds(500);

    private readonly ComparisonSession _session;
    private readonly IComparisonRepository _repository;
    private readonly ITimer _timer;
    private readonly SemaphoreSlim _saving = new(1, 1);
    private int _pending;
    private bool _disposed;

    public ComparisonPersistence(ComparisonSession session, IComparisonRepository repository, TimeProvider timeProvider)
    {
        _session = session;
        _repository = repository;
        _timer = timeProvider.CreateTimer(_ => _ = SaveIfPendingAsync(), null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        _session.Changed += OnChanged;
    }

    public event EventHandler<Exception>? SaveFailed;

    /// <summary>Há mudança ainda não gravada.</summary>
    public bool HasPendingChanges => Volatile.Read(ref _pending) != 0;

    /// <summary>Carrega a comparação gravada na sessão. Não conta como mudança a gravar.</summary>
    public async Task RestoreAsync(CancellationToken cancellationToken = default)
    {
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        _session.Changed -= OnChanged;
        try
        {
            _session.Restore(snapshot);
        }
        finally
        {
            _session.Changed += OnChanged;
        }
    }

    /// <summary>Grava agora, se houver mudança pendente.</summary>
    public Task FlushAsync()
    {
        _timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        return SaveIfPendingAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _session.Changed -= OnChanged;
        await _timer.DisposeAsync().ConfigureAwait(false);
        await SaveIfPendingAsync().ConfigureAwait(false);

        // O semáforo não é descartado: sem AvailableWaitHandle ele não segura recurso nativo,
        // e um callback do timer que já começou ainda pode estar esperando por ele.
    }

    private void OnChanged(object? sender, EventArgs e)
    {
        Volatile.Write(ref _pending, 1);
        _timer.Change(SaveDelay, Timeout.InfiniteTimeSpan);
    }

    private async Task SaveIfPendingAsync()
    {
        await _saving.WaitAsync().ConfigureAwait(false);
        try
        {
            if (Interlocked.Exchange(ref _pending, 0) == 0)
            {
                return;
            }

            await _repository.SaveAsync(_session.ToSnapshot()).ConfigureAwait(false);
        }
#pragma warning disable CA1031 // Gravar é um efeito colateral; a falha é relatada e a comparação segue na memória.
        catch (Exception ex)
#pragma warning restore CA1031
        {
            Volatile.Write(ref _pending, 1);
            SaveFailed?.Invoke(this, ex);
        }
        finally
        {
            _saving.Release();
        }
    }
}
