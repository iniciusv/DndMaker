using DndMaker.Content.Campaigns;
using DndMaker.Content.Tests.Golden;
using DndMaker.Domain.Analysis;
using DndMaker.Domain.Builds;
using DndMaker.Domain.Combat;
using DndMaker.Domain.Progression;
using DndMaker.Domain.Routines;

namespace DndMaker.Content.Tests.Parity;

/// <summary>
/// As correções da fase 6 (seção 12 do plano), cada uma comparada com o que o logic.js fazia.
/// A parte que não muda continua conferida pela paridade (<see cref="KnownDivergences"/>).
/// O problema 5 (legenda das tabelas) é testado na Application e no Desktop, e o 6 (melhor turno sem
/// conferir faixa e alvos) no Domain, em RoutineTests.
/// </summary>
public sealed class DivergenceTests
{
    private const double Tolerance = 1e-9;

    private static GoldenInput Input(string build, string? variant, string species, string background, string extra, int level,
        Dictionary<int, string>? choices = null) =>
        new(build, variant, species, background, extra, choices ?? [], level, 15);

    /// <summary>A ficha como o programa calcula, com o alocador padrão (a paridade usa o do logic.js).</summary>
    private static class Program
    {
        private static readonly CharacterAnalyzer Analyzer = CurseOfStrahd.Ruleset.CreateAnalyzer();

        public static CharacterSheet Analyze(CharacterBuild build, int level) =>
            Analyzer.Analyze(build, Level.Of(level), new CombatAssumptions(new ArmorClass(15)));
    }

    private static GoldenCase Golden(GoldenInput input) =>
        GoldenFile.Instance.Cases.Single(c => c.Input.ToString() == input.ToString() && c.Input.G.Count == input.G.Count);

    [Fact]
    public void Diverge_1_Survivor_soma_o_bonus_de_proficiencia_contra_o_charme()
    {
        // Guerreiro 1 → Bruxo, Haunted One (Survivor), nível 5: SAB +0 contra CD 17.
        var input = Input("fwl", "defense", "humano", "haunted", "alert", 5);
        var sheet = GoldenAdapter.Analyze(input);

        Golden(input).Output.Strahd.P.Should().BeApproximately(0.20, Tolerance, "o logic.js ignorava o Survivor");
        sheet.Charm.Passive.Value.Should().BeApproximately(0.20, Tolerance);
        sheet.Charm.WithResources.Value.Should().BeApproximately(0.35, Tolerance, "+3 depois da falha: passa com 14 ou mais");
        sheet.Charm.Resources.Should().Equal("Survivor");
    }

    [Fact]
    public void Diverge_1_Survivor_que_nao_salvou_vale_na_rerrolagem_do_Countercharm()
    {
        // Guerreiro 1 → Bardo do Valor, Haunted One, nível 8: SAB +0 contra CD 17, PB +3.
        // Passa de primeira com o Survivor (35%); senão o Survivor fica guardado para a rerrolagem com vantagem.
        var sheet = GoldenAdapter.Analyze(Input("fval", "defense", "humano", "haunted", "alert", 8));

        sheet.Charm.Resources.Should().Equal("Countercharm", "Survivor");
        sheet.Charm.WithResources.Value.Should().BeApproximately(0.35 + (0.65 * (1 - (0.65 * 0.65))), Tolerance);
    }

    [Fact]
    public void Diverge_1_Lucky_e_Watchers_Will_dao_vantagem_gastando_recurso()
    {
        // Paladino, nível 3, Lucky como talento extra: SAB +3 contra CD 17 (35%); com vantagem, 1 − 0,65².
        var input = Input("pal", "dueling", "humano", "nobre", "lucky", 3);
        var sheet = GoldenAdapter.Analyze(input);

        sheet.Charm.Passive.Value.Should().BeApproximately(0.35, Tolerance);
        sheet.Charm.WithResources.Value.Should().BeApproximately(1 - (0.65 * 0.65), Tolerance);
        sheet.Charm.Resources.Should().Equal("Lucky", "Watcher's Will");
    }

    [Fact]
    public void Diverge_1_vantagem_da_especie_nao_e_gasta_de_novo()
    {
        // Gnomo já tem vantagem em SAB: Lucky não muda nada e nem aparece na conta.
        var sheet = GoldenAdapter.Analyze(Input("pal", "dueling", "gnomo", "nobre", "lucky", 3));

        sheet.Charm.WithResources.Should().Be(sheet.Charm.Passive);
        sheet.Charm.Resources.Should().BeEmpty();
    }

    [Fact]
    public void Diverge_1_Twilight_Sanctuary_aparece_fora_da_conta()
    {
        var sheet = GoldenAdapter.Analyze(Input("twi", "flame", "humano", "eremita", "alert", 3));

        sheet.Charm.Remedies.Should().Equal("Twilight Sanctuary");
        sheet.Charm.WithResources.Should().Be(sheet.Charm.Passive, "encerrar o charme depois não muda a chance do teste");
    }

    [Fact]
    public void Diverge_2_smite_contra_morto_vivo_nao_e_o_melhor_turno()
    {
        var input = Input("pal", "dueling", "humano", "nobre", "alert", 5);
        var sheet = GoldenAdapter.Analyze(input);

        Golden(input).Output.Best[2].Should().Be("… contra morto-vivo");
        sheet.Best(ResourceTier.Peak)!.Name.Should().Be("Espada longa + Divine Smite");
        sheet.Routines.Should().Contain(r => r.Name == "… contra morto-vivo" && r.Routine.Situation == Situation.VsUndead);
    }

    [Fact]
    public void Diverge_3_rotina_com_dois_ataques_mostra_os_dois_bonus()
    {
        // Bardo do Valor no 6: ataque com DES (+6) e True Strike com CAR (+7).
        var input = Input("valor", "arco", "humano", "artista", "alert", 6);
        var routine = GoldenAdapter.Analyze(input).Routines.Single(r => r.Name.StartsWith("Ataque + True Strike", StringComparison.Ordinal));

        Golden(input).Output.Routines.Single(r => r.N == routine.Name).Hit.Should().Be(6, "o logic.js mostrava só o primeiro");
        routine.AttackBonuses.Select(b => b.Value).Should().Equal(6, 7);
    }

    [Fact]
    public void Diverge_4_Shillelagh_do_Guerreiro_Bardo_exige_a_variante_do_bordao()
    {
        // Com Magic Initiate (Druida) e Dueling, o JS usava o bordão. Agora é a espada; o bordão tem variante própria.
        var input = Input("fval", "dueling", "humano", "haunted", "mi_druid", 6);
        var dueling = GoldenAdapter.Analyze(input);

        Golden(input).Output.Routines[0].N.Should().Be("True Strike (bordão com Shillelagh)");
        dueling.Routines[0].Name.Should().Be("True Strike (espada longa)");
        GoldenAdapter.Analyze(input with { V = "staff", Hf = "alert" }).VariantRequirementMissing.Should().BeTrue();
    }

    [Fact]
    public void Diverge_4_variante_do_bordao_reproduz_o_bordao_do_JS()
    {
        // Todo caso do JS com Dueling e Magic Initiate (Druida) tem de sair igual na variante do bordão.
        var cases = GoldenFile.Instance.Cases
            .Where(c => c.Input.B == "fval" && c.Input.V == "dueling" && c.Output.Feats.Contains("mi_druid"))
            .ToList();
        cases.Should().NotBeEmpty();

        var failures = cases.SelectMany(golden =>
        {
            var staff = GoldenAdapter.Project(GoldenAdapter.Analyze(golden.Input with { V = "staff" }));
            var routinesOnly = golden.Output with { Routines = staff.Routines, Best = staff.Best };
            return GoldenComparer.Differences(golden.Output, routinesOnly).Select(d => $"{golden.Input}: {d}");
        }).ToList();

        failures.Should().BeEmpty(string.Join("\n", failures.Take(10)));
    }

    [Theory]
    [InlineData("aasimar", 3, "Celestial Revelation", "+2")]
    [InlineData("bugbear", 1, "Surprise Attack", "+2d6")]
    [InlineData("golias", 5, "Fire's Burn", "+1d10")]
    public void Diverge_7_dano_extra_da_especie_vira_rotina_opcional(string species, int level, string option, string rider)
    {
        var input = Input("pal", "dueling", species, "nobre", "alert", level);
        var sheet = GoldenAdapter.Analyze(input);

        var extra = sheet.Routines.Single(r => r.Routine.Situation == Situation.SpeciesOption);
        extra.Name.Should().Be($"Espada longa + {option}");
        extra.Formula.Should().EndWith(" " + rider);
        extra.Expected.Should().BeGreaterThan(sheet.Routines[0].Expected);
        Golden(input).Output.Routines.Should().NotContain(r => r.N == extra.Name, "o logic.js não calculava");
        Enum.GetValues<ResourceTier>().Should().OnlyContain(t => sheet.Best(t)!.Routine.Situation == null, "opcionais não entram no melhor turno");
    }

    [Fact]
    public void Diverge_8_antecedente_divide_o_bonus_para_nao_desperdicar_modificador()
    {
        // Bardo das Espadas com Artista: o logic.js dava CAR 17 e DES 15; CAR 16 e DES 16 dão +1 em DES.
        var input = Input("swords", "melee", "humano", "artista", "alert", 1);
        var golden = Golden(input).Output;
        var sheet = Program.Analyze(GoldenAdapter.ToBuild(input), 1);

        golden.Sc.Should().Equal(8, 15, 13, 10, 12, 17);
        golden.BgApplied.Should().Be("CAR +2, DES +1");
        sheet.Allocation.BackgroundBonus.ToString().Should().Be("DES +2, CAR +1");
        GoldenAdapter.Project(sheet).Sc.Should().Equal(8, 16, 13, 10, 12, 16);
        sheet.Armor.Value.Value.Should().Be(golden.Ac.V + 1, "couro batido soma DES");
        sheet.Initiative.Should().Be(golden.Init + 1);
    }

    [Fact]
    public void Diverge_8_o_aumento_do_nivel_4_chega_ao_mesmo_lugar()
    {
        var input = Input("swords", "melee", "humano", "artista", "alert", 4);

        GoldenAdapter.Project(Program.Analyze(GoldenAdapter.ToBuild(input), 4)).Sc.Should().Equal(Golden(input).Output.Sc);
    }

    [Fact]
    public void Diverge_8_a_divisao_olha_o_plano_inteiro()
    {
        // Guerreiro 1 → Bruxo com Haunted One. Só com aumentos de atributo, CAR 16 e CON 16 (melhor no começo);
        // com Fey Touched no 5, a divisão do logic.js (CAR 17 → 18) termina melhor que CAR 16 → 17.
        var plain = Input("fwl", "defense", "humano", "haunted", "alert", 5);
        var fey = Input("fwl", "defense", "humano", "haunted", "alert", 5, new() { [5] = "fey" });

        Program.Analyze(GoldenAdapter.ToBuild(plain), 1).Allocation.BackgroundBonus.ToString().Should().Be("CON +2, CAR +1");
        Program.Analyze(GoldenAdapter.ToBuild(fey), 1).Allocation.BackgroundBonus.ToString().Should().Be("CAR +2, CON +1");
        Program.Analyze(GoldenAdapter.ToBuild(fey), 5).Allocation.Picks.Single().ChangesText.Should().Be("CAR 17 → 18");
    }

    [Fact]
    public void Diverge_7_Celestial_Revelation_so_a_partir_do_nivel_3()
    {
        var sheet = GoldenAdapter.Analyze(Input("pal", "dueling", "aasimar", "nobre", "alert", 2));
        sheet.Routines.Should().NotContain(r => r.Routine.Situation == Situation.SpeciesOption);
    }
}
