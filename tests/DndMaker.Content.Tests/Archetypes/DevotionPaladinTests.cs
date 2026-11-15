using DndMaker.Content.Campaigns;
using DndMaker.Domain.Analysis;
using DndMaker.Domain.Catalog;
using DndMaker.Domain.Combat;
using DndMaker.Domain.Progression;
using DndMaker.Domain.Routines;

namespace DndMaker.Content.Tests.Archetypes;

/// <summary>
/// Paladino (Devotion), acrescentado depois do logic.js: sem golden, os números são conferidos à mão.
/// A carta padrão: humano, Andarilho (DES 16 e CAR 16 no 1, com Lucky), Alert como talento extra, contra CA 15.
/// </summary>
public sealed class DevotionPaladinTests
{
    private const double Tolerance = 1e-9;

    private static readonly CharacterAnalyzer Analyzer = CurseOfStrahd.Ruleset.CreateAnalyzer();

    private static CharacterSheet Sheet(int level)
    {
        var rules = CurseOfStrahd.Ruleset;
        var build = rules.DefaultCharacter(rules.GetArchetype(new ArchetypeId("devotion")));
        return Analyzer.Analyze(build, Level.Of(level), new CombatAssumptions(new ArmorClass(15)));
    }

    private static RoutineEstimate Routine(CharacterSheet sheet, string name) => sheet.Routines.Single(r => r.Name == name);

    [Fact]
    public void Nivel_1_destreza_e_carisma_em_16_com_armadura_media_e_escudo()
    {
        var sheet = Sheet(1);

        sheet.Allocation.BackgroundBonus.ToString().Should().Be("CAR +2, DES +1", "DES 15 e CAR 14 viram 16 e 16 (problema 8)");
        sheet.Armor.Value.Value.Should().Be(18, "cota de escamas 14 + DES 2 + escudo 2");
        sheet.Initiative.Should().Be(5, "DES +3 e Alert +2");
        Routine(sheet, "Rapieira").Expected.Should().BeApproximately(0.55 * 7.5, Tolerance, "+5 acerta com 10 ou mais; 1d8+3");
    }

    [Fact]
    public void Lucky_do_Andarilho_entra_na_chance_contra_o_charme()
    {
        var charm = Sheet(1).Charm;

        charm.Passive.Value.Should().BeApproximately(0.35, Tolerance, "SAB +1 e proficiência +2 contra CD 17");
        charm.WithResources.Value.Should().BeApproximately(1 - (0.65 * 0.65), Tolerance);
        charm.Resources.Should().Equal("Lucky");
    }

    [Fact]
    public void Nivel_3_Sacred_Weapon_soma_o_carisma_ao_ataque()
    {
        var sheet = Sheet(3);
        var sacred = Routine(sheet, "Rapieira com Sacred Weapon");

        sacred.PrimaryAttackBonus!.Value.Value.Should().Be(8, "PB 2 + DES 3 + CAR 3");
        sacred.Tier.Should().Be(ResourceTier.Peak);
        sacred.Expected.Should().BeApproximately(0.70 * 9.5, Tolerance, "1d8 + DES 3 + Dueling 2");
        sheet.Best(ResourceTier.AtWill)!.Name.Should().Be("Rapieira", "Sacred Weapon gasta Canalizar Divindade");
        sheet.Best(ResourceTier.Peak)!.Name.Should().Be("Sacred Weapon + Divine Smite");
    }

    [Fact]
    public void Nivel_7_Aura_of_Devotion_deixa_imune_ao_charme()
    {
        Sheet(6).Charm.ImmunitySource.Should().BeNull();

        var charm = Sheet(7).Charm;

        charm.ImmunitySource.Should().Be("Aura of Devotion");
        charm.Passive.Value.Should().Be(1);
        charm.WithResources.Value.Should().Be(1);
    }
}
