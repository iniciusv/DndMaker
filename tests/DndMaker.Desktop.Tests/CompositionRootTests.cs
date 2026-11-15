using DndMaker.Application.Session;
using DndMaker.Desktop.ViewModels;
using DndMaker.Domain.Rulesets;
using Microsoft.Extensions.DependencyInjection;

namespace DndMaker.Desktop.Tests;

public sealed class CompositionRootTests
{
    [Fact]
    public async Task Monta_todas_as_camadas_e_resolve_a_tela()
    {
        var file = Path.Combine(Path.GetTempPath(), "DndMaker.Tests", Guid.NewGuid().ToString("N"), "comparison.json");

        await using var services = CompositionRoot.Build(file);

        services.GetRequiredService<Ruleset>().Archetypes.Should().HaveCount(10);
        services.GetRequiredService<MainViewModel>().Should().NotBeNull();
        await services.GetRequiredService<ComparisonPersistence>().RestoreAsync();
        services.GetRequiredService<ComparisonSession>().Current.Level.Value.Should().Be(5, "sem arquivo, vale a comparação inicial");
    }
}
