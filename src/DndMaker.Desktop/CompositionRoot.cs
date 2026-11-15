using DndMaker.Application;
using DndMaker.Content;
using DndMaker.Desktop.ViewModels;
using DndMaker.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace DndMaker.Desktop;

/// <summary>Único ponto onde as camadas são ligadas entre si.</summary>
internal static class CompositionRoot
{
    public static ServiceProvider Build(string? comparisonFilePath = null)
    {
        var services = new ServiceCollection();

        services.AddCurseOfStrahdContent();
        services.AddApplication();
        services.AddInfrastructure(comparisonFilePath);
        services.AddSingleton<MainViewModel>();

        return services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });
    }
}
