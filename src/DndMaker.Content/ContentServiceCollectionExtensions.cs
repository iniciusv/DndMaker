using DndMaker.Content.Campaigns;
using DndMaker.Domain.Rulesets;
using Microsoft.Extensions.DependencyInjection;

namespace DndMaker.Content;

public static class ContentServiceCollectionExtensions
{
    /// <summary>Registra o pacote de regras do Curse of Strahd (<see cref="Ruleset"/> e <see cref="Campaign"/>).</summary>
    public static IServiceCollection AddCurseOfStrahdContent(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton(CurseOfStrahd.Ruleset);
        services.AddSingleton(CurseOfStrahd.Campaign);
        return services;
    }
}
