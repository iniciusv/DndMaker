using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using DndMaker.Application.Session;
using DndMaker.Desktop.ViewModels;
using DndMaker.Desktop.Views;
using Microsoft.Extensions.DependencyInjection;

namespace DndMaker.Desktop.Tests.Headless;

/// <summary>
/// A janela completa renderizada sem tela: nenhuma ligação de dados pode falhar, o teclado funciona
/// e as capturas ficam em <c>bin/.../screenshots</c> para conferência visual.
/// </summary>
[Collection(AvaloniaTestGroup.Name)]
public sealed class MainWindowTests(AvaloniaFixture avalonia)
{
    private static readonly string ScreenshotDirectory = Path.Combine(AppContext.BaseDirectory, "screenshots");

    [Theory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public Task Janela_renderiza_sem_erros_de_ligacao(string theme) =>
        avalonia.Session.Dispatch(
            async () =>
            {
                avalonia.Log.Clear();
                Avalonia.Application.Current!.RequestedThemeVariant = theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;
                await using var services = CompositionRoot.Build(TemporaryFile());
                var window = new MainWindow { DataContext = services.GetRequiredService<MainViewModel>(), Width = 1400, Height = 4400 };

                window.Show();
                Dispatcher.UIThread.RunJobs();
                Save(window, $"janela-{theme.ToLowerInvariant()}.png");

                // As outras duas abas, para a conferência visual.
                var tabs = window.GetVisualDescendants().OfType<TabControl>().Single();
                for (var tab = 1; tab < tabs.ItemCount; tab++)
                {
                    tabs.SelectedIndex = tab;
                    Dispatcher.UIThread.RunJobs();
                    Save(window, $"aba-{tab}-{theme.ToLowerInvariant()}.png");
                }

                tabs.SelectedIndex = 0;

                // Troca a build de uma carta e anda de nível para exercitar as ligações que mudam.
                var main = (MainViewModel)window.DataContext!;
                main.Cards[0].SelectedArchetype = main.Cards[0].Archetypes.Single(a => a.Id == "twi");
                main.NextLevelCommand.Execute(null);
                Dispatcher.UIThread.RunJobs();

                avalonia.Log.Messages.Should().BeEmpty();
                window.Close();
            },
            CancellationToken.None);

    [Fact]
    public Task Ctrl_seta_anda_pela_estrada_de_niveis() =>
        avalonia.Session.Dispatch(
            async () =>
            {
                await using var services = CompositionRoot.Build(TemporaryFile());
                var main = services.GetRequiredService<MainViewModel>();
                var window = new MainWindow { DataContext = main, Width = 1400, Height = 900 };
                window.Show();
                Dispatcher.UIThread.RunJobs();

                window.KeyPress(Key.Right, RawInputModifiers.Control, PhysicalKey.ArrowRight, null);
                window.KeyRelease(Key.Right, RawInputModifiers.Control, PhysicalKey.ArrowRight, null);
                window.KeyPress(Key.Right, RawInputModifiers.Control, PhysicalKey.ArrowRight, null);
                window.KeyRelease(Key.Right, RawInputModifiers.Control, PhysicalKey.ArrowRight, null);
                window.KeyPress(Key.Left, RawInputModifiers.Control, PhysicalKey.ArrowLeft, null);
                window.KeyRelease(Key.Left, RawInputModifiers.Control, PhysicalKey.ArrowLeft, null);
                Dispatcher.UIThread.RunJobs();

                services.GetRequiredService<ComparisonSession>().Current.Level.Value.Should().Be(6);
                main.LevelText.Should().Be("6");
                window.Close();
            },
            CancellationToken.None);

    private static string TemporaryFile() =>
        Path.Combine(Path.GetTempPath(), "DndMaker.Tests", Guid.NewGuid().ToString("N"), "comparison.json");

    private static void Save(MainWindow window, string name)
    {
        Directory.CreateDirectory(ScreenshotDirectory);
        using var frame = window.CaptureRenderedFrame();
        frame.Should().NotBeNull();
        frame!.Save(Path.Combine(ScreenshotDirectory, name), new PngBitmapEncoderOptions());
    }
}
