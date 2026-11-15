using DndMaker.Content.Campaigns;
using DndMaker.Domain.Analysis;
using DndMaker.Domain.Builds;
using DndMaker.Domain.Catalog;
using DndMaker.Domain.Combat;
using DndMaker.Domain.Progression;
using DndMaker.Domain.Routines;

namespace DndMaker.Content.Tests.Archetypes;

/// <summary>
/// Paladino (Ancients), acrescentado depois do logic.js: sem golden, os números são conferidos à mão.
/// Humano, Soldado (FOR 17, CON 14, CAR 14 no 1), Alert como talento extra, contra CA 15.
/// </summary>
public sealed class AncientsPaladinTests
{
    private const double Tolerance = 1e-9;

    private static readonly CharacterAnalyzer Analyzer = CurseOfStrahd.Ruleset.CreateAnalyzer();

    private static CharacterSheet Sheet(int level)
    {
        var rules = CurseOfStrahd.Ruleset;
        var build = CharacterBuild.Create(
            rules.GetArchetype(new ArchetypeId("ancients")),
            rules.GetSpecies(new SpeciesId("humano")),
            rules.GetBackground(new BackgroundId("soldado")),
            rules.GetFeat(new FeatId("alert")));
        return Analyzer.Analyze(build, Level.Of(level), new CombatAssumptions(new ArmorClass(15)));
    }

    private static RoutineEstimate Routine(CharacterSheet sheet, string name) => sheet.Routines.Single(r => r.Name == name);

    [Fact]
    public void Nivel_1_espadao_sem_estilo_de_luta()
    {
        var sheet = Sheet(1);
        var swing = Routine(sheet, "Espadão");

        sheet.Routines.Should().ContainSingle();
        swing.Formula.Should().Be("(2d6+3)");
        swing.Raw.Should().BeApproximately(10, Tolerance, "2d6 tem média 7, mais FOR +3");
        swing.Expected.Should().BeApproximately(0.55 * 10, Tolerance, "+5 contra CA 15 acerta com 10 ou mais");
        sheet.Armor.Value.Value.Should().Be(16, "cota de malha, sem escudo");
    }

    [Fact]
    public void Nivel_2_great_weapon_fighting_sobe_a_media_do_d6_para_4()
    {
        var sheet = Sheet(2);

        Routine(sheet, "Espadão").Raw.Should().BeApproximately((2 * 4) + 3, Tolerance);
        Routine(sheet, "Espadão + Divine Smite").Raw.Should().BeApproximately((2 * 4) + 3 + 9, Tolerance, "o GWF não vale no 2d8 do smite");
        Routine(sheet, "… contra morto-vivo").Routine.Situation.Should().Be(Situation.VsUndead);
        sheet.Best(ResourceTier.Peak)!.Name.Should().Be("Espadão + Divine Smite");
    }

    [Fact]
    public void Nivel_5_extra_attack_cota_de_talas_e_moonbeam()
    {
        var sheet = Sheet(5);
        var moonbeam = Routine(sheet, "Espadão + Moonbeam");

        sheet.Armor.Value.Value.Should().Be(17);
        Routine(sheet, "Espadão").Expected.Should().BeApproximately(2 * 0.65 * 12, Tolerance, "+7 acerta com 8 ou mais");
        moonbeam.Tier.Should().Be(ResourceTier.Concentration);
        moonbeam.PrimaryDc!.Value.Value.Should().Be(13, "8 + PB 3 + CAR 2");

        // O monstro (+2) falha com 10 ou menos: 50%, metade do dano no sucesso, 2d10 = 11.
        moonbeam.Expected.Should().BeApproximately((2 * 0.65 * 12) + (11 * 0.75), Tolerance);
        sheet.Best(ResourceTier.Concentration).Should().Be(moonbeam);
    }

    [Fact]
    public void Nivel_6_aura_of_protection_soma_o_carisma_nos_testes()
    {
        Sheet(5).SaveBonus.Should().Be(0);

        var sheet = Sheet(6);

        sheet.SaveBonus.Should().Be(2);
        sheet.SaveBonusSources.Should().Equal("Aura of Protection");
        sheet.Charm.Passive.Value.Should().BeApproximately(0.5, Tolerance, "SAB +1, proficiente +3, aura +2 contra CD 17");
    }

    [Fact]
    public void Nivel_7_aura_of_warding_aparece_na_ficha()
    {
        Sheet(6).Armor.Extra.Should().BeNull();
        Sheet(7).Armor.Extra.Should().Contain("Aura of Warding").And.Contain("necrótico");
    }
}
