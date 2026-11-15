using DndMaker.Content.Campaigns;
using DndMaker.Content.Tests.Golden;

namespace DndMaker.Content.Tests.Parity;

/// <summary>
/// Paridade com o motor original: cada caso de tools/golden/golden.json, calculado pelo logic.js,
/// tem de sair igual no C#, menos as correções da fase 6, descritas em <see cref="KnownDivergences"/>.
/// Um teste por build, que lista todos os casos e campos diferentes.
/// </summary>
public sealed class GoldenParityTests
{
    /// <summary>Quantas diferenças mostrar na mensagem de falha.</summary>
    private const int MaxReported = 25;

    public static TheoryData<string> Builds => [.. GoldenFile.Instance.Meta.Builds];

    /// <summary>Builds acrescentadas depois do logic.js: não têm golden e são testadas à parte.</summary>
    public static readonly IReadOnlySet<string> BuildsWithoutGolden = new HashSet<string>(StringComparer.Ordinal) { "ancients" };

    [Fact]
    public void Pacote_de_regras_tem_as_builds_do_golden_na_mesma_ordem()
    {
        CurseOfStrahd.Ruleset.Archetypes.Select(a => a.Id.Value).Where(id => !BuildsWithoutGolden.Contains(id))
            .Should().Equal(GoldenFile.Instance.Meta.Builds);
    }

    [Fact]
    public void Builds_sem_golden_existem_no_pacote_de_regras()
    {
        CurseOfStrahd.Ruleset.Archetypes.Select(a => a.Id.Value).Should().Contain(BuildsWithoutGolden);
    }

    [Theory]
    [MemberData(nameof(Builds))]
    public void Build_bate_com_o_logic_js(string build)
    {
        var cases = GoldenFile.Instance.Cases.Where(c => c.Input.B == build).ToList();
        cases.Should().NotBeEmpty();

        var failures = cases
            .SelectMany(c => Compare(c).Select(diff => $"{c.Input}: {diff}"))
            .ToList();

        failures.Should().BeEmpty(
            "{0} de {1} casos de {2} divergem do logic.js. Primeiras diferenças:\n{3}",
            failures.Select(f => f[..f.IndexOf(": ", StringComparison.Ordinal)]).Distinct().Count(),
            cases.Count,
            build,
            string.Join("\n", failures.Take(MaxReported)));
    }

    private static IEnumerable<string> Compare(GoldenCase golden)
    {
        try
        {
            var sheet = GoldenAdapter.Analyze(golden.Input);
            var (expected, actual) = KnownDivergences.Apply(golden, sheet, GoldenAdapter.Project(sheet));
            return GoldenComparer.Differences(expected, actual);
        }
#pragma warning disable CA1031 // Uma exceção num caso vira uma diferença relatada, sem esconder os outros casos.
        catch (Exception ex)
#pragma warning restore CA1031
        {
            return [$"exceção {ex.GetType().Name}: {ex.Message}"];
        }
    }
}
