using DndMaker.Application.Catalog;
using DndMaker.Application.Comparison;
using DndMaker.Application.Overview;
using DndMaker.Application.Session;
using DndMaker.Domain.Analysis;
using DndMaker.Domain.Rulesets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DndMaker.Application;

public static class ApplicationServiceCollectionExtensions
{
    /// <summary>
    /// Registra consultas, sessão e gravação automática. Precisa de um <see cref="Ruleset"/> (o conteúdo)
    /// e de um <see cref="IComparisonRepository"/> (a infraestrutura) registrados.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton(sp => new CharacterAnalyzer(sp.GetRequiredService<Ruleset>().Campaign.CharmThreat));

        services.AddSingleton<GetCatalogQuery>();
        services.AddSingleton<AnalyzeComparisonQuery>();
        services.AddSingleton<BuildsOverviewQuery>();
        services.AddSingleton<DamageProgressionQuery>();

        services.AddSingleton<ComparisonSession>();
        services.AddSingleton<ComparisonPersistence>();
        return services;
    }
}
