using DndMaker.Application.Session;
using DndMaker.Application.Tests.Fakes;
using DndMaker.Content;
using DndMaker.Content.Campaigns;
using Microsoft.Extensions.DependencyInjection;

namespace DndMaker.Application.Tests;

public sealed class PersistenceTests
{
    private static readonly TimeSpan JustBefore = ComparisonPersistence.SaveDelay - TimeSpan.FromMilliseconds(1);

    private readonly ManualTimeProvider _clock = new();
    private readonly InMemoryComparisonRepository _repository = new();
    private readonly ComparisonSession _session = new(CurseOfStrahd.Ruleset);

    private ComparisonPersistence NewPersistence() => new(_session, _repository, _clock);

    [Fact]
    public async Task Grava_meio_segundo_depois_da_ultima_mudanca()
    {
        await using var persistence = NewPersistence();

        _session.SetLevel(6);
        _clock.Advance(JustBefore);
        _session.SetLevel(7); // reinicia a contagem
        _clock.Advance(JustBefore);
        _repository.SaveCount.Should().Be(0);
        persistence.HasPendingChanges.Should().BeTrue();

        _clock.Advance(TimeSpan.FromMilliseconds(1));

        _repository.SaveCount.Should().Be(1);
        _repository.Stored!.Level.Should().Be(7);
        persistence.HasPendingChanges.Should().BeFalse();
    }

    [Fact]
    public async Task Flush_grava_na_hora_e_so_se_houver_mudanca()
    {
        await using var persistence = NewPersistence();

        await persistence.FlushAsync();
        _repository.SaveCount.Should().Be(0);

        _session.SetEnemyArmorClass(18);
        await persistence.FlushAsync();
        _repository.SaveCount.Should().Be(1);

        _clock.Advance(ComparisonPersistence.SaveDelay);
        _repository.SaveCount.Should().Be(1, "o timer foi cancelado pelo flush");
    }

    [Fact]
    public async Task Descartar_grava_o_que_estiver_pendente()
    {
        var persistence = NewPersistence();
        _session.SetLevel(3);

        await persistence.DisposeAsync();

        _repository.Stored!.Level.Should().Be(3);
        _session.SetLevel(4);
        _clock.Advance(ComparisonPersistence.SaveDelay);
        _repository.SaveCount.Should().Be(1, "depois de descartada ela não escuta mais a sessão");
    }

    [Fact]
    public async Task Restaurar_carrega_sem_marcar_mudanca()
    {
        var saved = _session.ToSnapshot() with { Level = 9 };
        var repository = new InMemoryComparisonRepository(saved);
        await using var persistence = new ComparisonPersistence(_session, repository, _clock);

        await persistence.RestoreAsync();

        _session.Current.Level.Value.Should().Be(9);
        persistence.HasPendingChanges.Should().BeFalse();
        _session.SetLevel(2);
        persistence.HasPendingChanges.Should().BeTrue("a sessão continua sendo observada depois de restaurar");
    }

    [Fact]
    public async Task Falha_ao_gravar_avisa_e_tenta_de_novo_depois()
    {
        await using var persistence = NewPersistence();
        var failures = new List<Exception>();
        persistence.SaveFailed += (_, ex) => failures.Add(ex);

        _repository.FailNextSave = true;
        _session.SetLevel(6);
        _clock.Advance(ComparisonPersistence.SaveDelay);

        failures.Should().ContainSingle().Which.Should().BeOfType<IOException>();
        persistence.HasPendingChanges.Should().BeTrue();

        await persistence.FlushAsync();
        _repository.Stored!.Level.Should().Be(6);
    }

    [Fact]
    public async Task Registro_completo_resolve_todos_os_servicos()
    {
        var services = new ServiceCollection()
            .AddCurseOfStrahdContent()
            .AddApplication()
            .AddSingleton<IComparisonRepository>(_repository);

        await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

        provider.GetRequiredService<ComparisonSession>().Current.Slots.Should().HaveCount(3);
        provider.GetRequiredService<ComparisonPersistence>().Should().NotBeNull();
        provider.GetRequiredService<TimeProvider>().Should().BeSameAs(TimeProvider.System);
    }
}
