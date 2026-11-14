using DndMaker.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace DndMaker.Desktop;

/// <summary>Único ponto onde as camadas são ligadas entre si.</summary>
internal static class CompositionRoot
{
    public static ServiceProvider Build()
    {
        var services = new ServiceCollection();

        // Fase 4: AddCurseOfStrahdContent(), AddApplication(), AddInfrastructure().
        services.AddSingleton<MainViewModel>();

        return services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });
    }
}
