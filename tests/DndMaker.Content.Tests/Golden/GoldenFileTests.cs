namespace DndMaker.Content.Tests.Golden;

/// <summary>
/// Garante que o golden existe e cobre o que os testes de paridade da fase 3 vão precisar.
/// Se falhar, rode: node tools/golden/export-golden.js
/// </summary>
public sealed class GoldenFileTests
{
    private static readonly string[] ExpectedBuilds =
        ["fwl", "valor", "pal", "twi", "gloom", "lock", "swords", "fval", "shadow", "bm"];

    private readonly GoldenFile _golden = GoldenFile.Instance;

    [Fact]
    public void Contagem_declarada_bate_com_os_casos()
    {
        _golden.Cases.Should().HaveCount(_golden.Meta.Count);
    }

    [Fact]
    public void Cobre_todas_as_builds_em_todos_os_niveis()
    {
        _golden.Meta.Builds.Should().Equal(ExpectedBuilds);

        foreach (var build in ExpectedBuilds)
        {
            _golden.Cases.Where(c => c.Input.B == build)
                .Select(c => c.Input.L).Distinct()
                .Should().BeEquivalentTo(Enumerable.Range(1, 10), $"a build {build} precisa de todos os níveis");
        }
    }

    [Fact]
    public void Cada_caso_tem_seis_atributos_e_ao_menos_uma_rotina()
    {
        _golden.Cases.Should().AllSatisfy(c =>
        {
            c.Output.Sc.Should().HaveCount(6);
            c.Output.Saves.Should().HaveCount(6);
            c.Output.Routines.Should().NotBeEmpty();
            c.Output.Best.Should().HaveCount(3);
        });
    }

    [Fact]
    public void Probabilidades_ficam_entre_zero_e_um()
    {
        _golden.Cases.Should().AllSatisfy(c =>
        {
            c.Output.Strahd.P.Should().BeInRange(0, 1);
            c.Output.Conc?.P.Should().BeInRange(0, 1);
        });
    }
}
