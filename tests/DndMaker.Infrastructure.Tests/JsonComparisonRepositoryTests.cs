using DndMaker.Application;
using DndMaker.Application.Session;
using DndMaker.Content;
using DndMaker.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace DndMaker.Infrastructure.Tests;

public sealed class JsonComparisonRepositoryTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "DndMaker.Tests", Guid.NewGuid().ToString("N"));

    private string FilePath => Path.Combine(_directory, "sub", "comparison.json");

    private static ComparisonSnapshot Sample => new(
        6,
        16,
        2,
        [
            new("pal", "defense", "gnomo", "haunted", "tough", new Dictionary<int, string> { [4] = "fey", [8] = "asi" }),
            new("valor", null, "humano", "artista", null, new Dictionary<int, string>()),
            new("bm", "rep", "anao", "soldado", "alert", new Dictionary<int, string> { [6] = "xbow" }),
        ]);

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    [Fact]
    public async Task Grava_e_le_de_volta_criando_a_pasta()
    {
        var repository = new JsonComparisonRepository(FilePath);

        await repository.SaveAsync(Sample);
        var loaded = await repository.LoadAsync();

        loaded.Should().BeEquivalentTo(Sample);
        Directory.GetFiles(Path.GetDirectoryName(FilePath)!).Should().ContainSingle("o arquivo temporário não pode sobrar");
    }

    [Fact]
    public async Task Arquivo_tem_versao_do_formato_e_nomes_em_camelCase()
    {
        await new JsonComparisonRepository(FilePath).SaveAsync(Sample);

        var json = await File.ReadAllTextAsync(FilePath);
        json.Should().Contain("\"schemaVersion\": 1").And.Contain("\"enemyArmorClass\": 16").And.Contain("\"levelChoices\"");
    }

    [Fact]
    public async Task Gravar_de_novo_substitui_o_arquivo()
    {
        var repository = new JsonComparisonRepository(FilePath);
        await repository.SaveAsync(Sample);

        await repository.SaveAsync(Sample with { Level = 2 });

        (await repository.LoadAsync())!.Level.Should().Be(2);
    }

    [Fact]
    public async Task Sem_arquivo_le_nulo()
    {
        (await new JsonComparisonRepository(FilePath).LoadAsync()).Should().BeNull();
    }

    [Theory]
    [InlineData("{ isto não é json")]
    [InlineData("{\"schemaVersion\": 99, \"comparison\": {\"level\": 3, \"enemyArmorClass\": 15, \"maxTier\": 0, \"slots\": []}}")]
    [InlineData("")]
    public async Task Arquivo_ilegivel_ou_de_outra_versao_le_nulo(string content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        await File.WriteAllTextAsync(FilePath, content);

        (await new JsonComparisonRepository(FilePath).LoadAsync()).Should().BeNull();
    }

    [Fact]
    public void Caminho_padrao_fica_no_AppData()
    {
        JsonComparisonRepository.DefaultFilePath.Should()
            .StartWith(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData))
            .And.EndWith(Path.Combine("DndMaker", "comparison.json"));
    }

    [Fact]
    public async Task Comparacao_sobrevive_a_fechar_e_abrir()
    {
        ComparisonSnapshot edited;
        await using (var first = BuildProvider())
        {
            var persistence = first.GetRequiredService<ComparisonPersistence>();
            await persistence.RestoreAsync();
            var session = first.GetRequiredService<ComparisonSession>();
            session.SetLevel(8);
            session.SetEnemyArmorClass(13);
            session.ChangeArchetype(0, "gloom");
            session.SetSpecies(0, "elfo");
            session.SetLevelChoice(0, 4, "res_wis");
            edited = session.ToSnapshot();
        } // fechar o programa: o container grava o que estiver pendente

        await using var second = BuildProvider();
        await second.GetRequiredService<ComparisonPersistence>().RestoreAsync();

        second.GetRequiredService<ComparisonSession>().ToSnapshot().Should().BeEquivalentTo(edited);
    }

    private ServiceProvider BuildProvider() =>
        new ServiceCollection()
            .AddCurseOfStrahdContent()
            .AddApplication()
            .AddInfrastructure(FilePath)
            .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
}
