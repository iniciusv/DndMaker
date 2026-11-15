using DndMaker.Application.Session;
using DndMaker.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace DndMaker.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    /// <summary>
    /// Registra a gravação em JSON. Sem caminho, usa <see cref="JsonComparisonRepository.DefaultFilePath"/>.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string? comparisonFilePath = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<IComparisonRepository>(
            new JsonComparisonRepository(comparisonFilePath ?? JsonComparisonRepository.DefaultFilePath));
        return services;
    }
}
