using DndMaker.Domain.Abilities;
using DndMaker.Domain.Analysis;
using DndMaker.Domain.Builds;
using DndMaker.Domain.Combat;
using DndMaker.Domain.Common;
using DndMaker.Domain.Effects;
using DndMaker.Domain.Progression;
using DndMaker.Domain.Routines;
using static DndMaker.Domain.Tests.Fixtures.TestRules;

namespace DndMaker.Domain.Tests.Analysis;

/// <summary>
/// As fichas abaixo têm de bater com os casos de tools/golden/golden.json
/// (mesma build, espécie, antecedente, talento, nível e CA 15).
/// </summary>
public sealed class CharacterAnalyzerTests
{
    private const double Tolerance = 1e-9;
    private static readonly CombatAssumptions Ac15 = CombatAssumptions.Default;

    private static int[] Scores(CharacterSheet s) => [.. AbilityList.All.Select(a => s.Scores[a].Value)];

    private static int[] Saves(CharacterSheet s) => [.. AbilityList.All.Select(a => s.SavingThrows[a].Modifier)];

    [Fact]
    public void Paladino_nivel_6_humano_nobre_Alert()
    {
        var sheet = Analyzer.Analyze(Paladin6(), Level.Of(6), Ac15);

        sheet.Allocation.BackgroundBonus.Should().Be(new BackgroundBonus(Ability.Strength, Ability.Charisma));
        Scores(sheet).Should().Equal(18, 10, 13, 8, 12, 16);
        sheet.HitPoints.Should().Be(46);
        sheet.Armor.Value.Should().Be(new ArmorClass(19));
        sheet.Armor.Description.Should().Be("cota de talas + escudo");
        sheet.Armor.StealthDisadvantage.Should().BeTrue();
        sheet.Initiative.Should().Be(3);
        sheet.Darkvision.Range.Value.Should().Be(0);
        sheet.SaveBonus.Should().Be(3);
        Saves(sheet).Should().Equal(7, 3, 4, 2, 7, 9);
        sheet.SavingThrows[Ability.Wisdom].Proficient.Should().BeTrue();
        sheet.SavingThrows[Ability.Strength].Proficient.Should().BeFalse();
        sheet.Charm.WithResources.Value.Should().BeApproximately(0.55, Tolerance);
        sheet.Charm.Advantage.Should().BeFalse();
        sheet.Concentration!.Success.Value.Should().BeApproximately(0.75, Tolerance);
        sheet.Feats.Select(f => f.Id.Value).Should().BeEquivalentTo("skilled", "alert");
        sheet.Allocation.Picks.Should().ContainSingle().Which.Changes.Should().Equal(
            new ScoreChange(Ability.Strength, 17, 18), new ScoreChange(Ability.Charisma, 15, 16));

        sheet.Routines.Select(r => r.Formula).Should().Equal("2 × (1d8+6)", "2 × (1d8+6) +2d8", "2 × (1d8+6) +3d8");
        sheet.Routines.Select(r => r.Raw).Should().Equal(21, 30, 34.5);
        sheet.Routines.Select(r => r.Expected).Should().SatisfyRespectively(
            e => e.Should().BeApproximately(13.65, Tolerance),
            e => e.Should().BeApproximately(21.5475, Tolerance),
            e => e.Should().BeApproximately(25.49625, Tolerance));
        sheet.Routines[0].PrimaryAttackBonus.Should().Be(new AttackBonus(7));
        sheet.Best(ResourceTier.AtWill)!.Name.Should().Be("Espada longa");
        sheet.Best(ResourceTier.Peak)!.Name.Should().Be("… contra morto-vivo");
    }

    [Fact]
    public void Paladino_nivel_5_gnomo_tem_vantagem_mental_e_visao_no_escuro()
    {
        var sheet = Analyzer.Analyze(Paladin6(Gnome), Level.Of(5), Ac15);

        sheet.HitPoints.Should().Be(39);
        sheet.Initiative.Should().Be(0); // o gnomo não tem talento extra, então o Alert guardado não vale
        sheet.Darkvision.Range.Value.Should().Be(18);
        sheet.SaveBonus.Should().Be(0);
        Saves(sheet).Should().Equal(4, 0, 1, -1, 4, 6);
        AbilityList.All.Select(a => sheet.SavingThrows[a].Advantage).Should().Equal(false, false, false, true, true, true);
        sheet.Charm.WithResources.Value.Should().BeApproximately(0.64, Tolerance);
        sheet.Charm.Advantage.Should().BeTrue();
        sheet.Concentration!.Success.Value.Should().BeApproximately(0.6, Tolerance);
        sheet.Feats.Select(f => f.Id.Value).Should().Equal("skilled");
    }

    [Fact]
    public void Guerreiro_Bruxo_nivel_10_com_aumentos_de_atributo()
    {
        var build = CharacterBuild.Create(FighterWarlockBuild, Human, Haunted, Alert);
        var sheet = Analyzer.Analyze(build, Level.Of(10), Ac15);

        Scores(sheet).Should().Equal(13, 12, 16, 8, 10, 20);
        sheet.Allocation.Picks.Select(p => p.Level).Should().Equal(5, 9);
        sheet.Allocation.Picks[1].Changes.Should().Equal(new ScoreChange(Ability.Charisma, 18, 20));
        sheet.HitPoints.Should().Be(85);
        sheet.Armor.Value.Value.Should().Be(19);
        sheet.Armor.Description.Should().Be("cota de malha + escudo + Defense");
        sheet.Armor.Extra.Should().Be("Armor of Agathys: +25 PV temporários, sem concentração");
        sheet.Initiative.Should().Be(5);
        sheet.Darkvision.Should().Be(new DarkvisionResult(Meters.Of(36), "Devil's Sight: enxerga também em escuridão mágica"));
        Saves(sheet).Should().Equal(5, 1, 7, -1, 0, 5);
        sheet.Charm.WithResources.Value.Should().BeApproximately(0.2, Tolerance);
        sheet.Concentration!.Success.Value.Should().BeApproximately(0.9, Tolerance);

        sheet.Routines.Select(r => r.Name).Should().Equal(
            "Arma do pacto (espada longa)", "Arma do pacto + Hex", "Arma do pacto + Eldritch Smite", "Eldritch Blast (à distância)");
        sheet.Routines.Select(r => r.Formula).Should().Equal(
            "2 × (1d8+5)", "2 × (1d8+5+1d6)", "2 × (1d8+5) +6d8", "2 × (1d10+5)");
        sheet.Routines.Select(r => r.Expected).Should().SatisfyRespectively(
            e => e.Should().BeApproximately(14.25, Tolerance),
            e => e.Should().BeApproximately(19.5, Tolerance),
            e => e.Should().BeApproximately(39.5625, Tolerance),
            e => e.Should().BeApproximately(15.75, Tolerance));
        sheet.Best(ResourceTier.AtWill)!.Name.Should().Be("Eldritch Blast (à distância)");
        sheet.Best(ResourceTier.Concentration)!.Name.Should().Be("Arma do pacto + Hex");
        sheet.Best(ResourceTier.Peak)!.Name.Should().Be("Arma do pacto + Eldritch Smite");
    }

    [Fact]
    public void Fey_Touched_vai_para_o_mental_impar_ou_para_o_atributo_de_conjuracao()
    {
        var build = CharacterBuild.Create(FighterWarlockBuild, Human, Haunted, Alert)
            .WithLevelChoice(5, LevelChoice.ForFeat(FeyTouched))
            .WithLevelChoice(9, LevelChoice.ForFeat(FeyTouched));
        var sheet = Analyzer.Analyze(build, Level.Of(10), Ac15);

        Scores(sheet).Should().Equal(13, 12, 15, 8, 10, 19);
        sheet.Allocation.Picks.Select(p => p.Changes.Single()).Should().Equal(
            new ScoreChange(Ability.Charisma, 17, 18),  // 17 é o mental ímpar mais importante
            new ScoreChange(Ability.Charisma, 18, 19)); // nenhum mental ímpar: cai no Carisma, de conjuração
        sheet.HitPoints.Should().Be(75);
        sheet.Concentration!.Success.Value.Should().BeApproximately(0.85, Tolerance);
        sheet.Routines[0].Expected.Should().BeApproximately(11.9, Tolerance);
    }

    [Fact]
    public void Nivel_1_do_multiclasse_ainda_ataca_com_Forca()
    {
        var build = CharacterBuild.Create(FighterWarlockBuild, Human, Haunted, Alert);
        var sheet = Analyzer.Analyze(build, Level.Of(1), Ac15);

        sheet.HitPoints.Should().Be(12); // d10 do guerreiro + CON 15
        sheet.Routines.Should().ContainSingle().Which.Name.Should().Be("Espada longa (ainda com Força)");
        sheet.Armor.Extra.Should().BeNull();
    }

    [Fact]
    public void Efeitos_de_talento_e_especie_entram_na_ficha()
    {
        var tough = Analyzer.Analyze(Paladin6(extra: Tough), Level.Of(6), Ac15);
        var dwarf = Analyzer.Analyze(Paladin6(Dwarf), Level.Of(6), Ac15);
        var baseline = Analyzer.Analyze(Paladin6(), Level.Of(6), Ac15);

        (tough.HitPoints - baseline.HitPoints).Should().Be(12);
        (dwarf.HitPoints - baseline.HitPoints).Should().Be(6);
        dwarf.Darkvision.Range.Value.Should().Be(36);
    }

    [Fact]
    public void Elfo_tem_vantagem_so_contra_o_charme()
    {
        var sheet = Analyzer.Analyze(Paladin6(Elf), Level.Of(6), Ac15);

        sheet.SavingThrows[Ability.Wisdom].Advantage.Should().BeFalse();
        sheet.Charm.Advantage.Should().BeTrue();
        sheet.Charm.WithResources.Value.Should().BeApproximately(1 - (0.45 * 0.45), Tolerance);
    }

    [Fact]
    public void War_Caster_e_Resilient()
    {
        var build = Paladin6()
            .WithLevelChoice(4, LevelChoice.ForFeat(WarCaster))
            .WithLevelChoice(8, LevelChoice.ForFeat(ResilientWisdom));
        var sheet = Analyzer.Analyze(build, Level.Of(8), Ac15);

        sheet.Concentration!.Advantage.Should().BeTrue();
        sheet.Allocation.Picks[0].Changes.Should().Equal(new ScoreChange(Ability.Charisma, 15, 16));
        sheet.Allocation.Picks[1].Changes.Should().Equal(new ScoreChange(Ability.Wisdom, 12, 13));
        sheet.Feats.Select(f => f.Id.Value).Should().Contain(["warcaster", "res_wis"]);
    }
}
