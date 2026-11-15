using DndMaker.Domain.Abilities;
using DndMaker.Domain.Analysis;
using DndMaker.Domain.Checks;
using DndMaker.Domain.Classes;
using DndMaker.Domain.Common;
using DndMaker.Domain.Effects;
using DndMaker.Domain.Equipment;
using DndMaker.Domain.Progression;
using static DndMaker.Domain.Tests.Fixtures.TestRules;

namespace DndMaker.Domain.Tests.Classes;

public sealed class ClassProgressionTests
{
    private static readonly ClassProgression FighterWarlock = ClassProgression.Dip(Fighter, 1, Warlock);

    [Fact]
    public void Niveis_em_cada_classe()
    {
        FighterWarlock.LevelIn(Fighter, Level.Of(6)).Should().Be(1);
        FighterWarlock.LevelIn(Warlock, Level.Of(6)).Should().Be(5);
        FighterWarlock.LevelIn(Warlock, Level.Of(1)).Should().Be(0);
        FighterWarlock.FirstClass.Should().Be(Fighter);
    }

    [Fact]
    public void Dados_de_vida_por_nivel()
    {
        FighterWarlock.HitDice(Level.Of(4)).Should().Equal(10, 8, 8, 8);
    }

    [Fact]
    public void Niveis_de_talento_seguem_o_nivel_de_classe()
    {
        FighterWarlock.FeatLevels(10).Should().Equal(5, 9);
        ClassProgression.Of(Fighter).FeatLevels(10).Should().Equal(4, 6, 8);
        ClassProgression.Of(Paladin).FeatLevels(20).Should().Equal(4, 8, 12, 16, 19);
    }

    [Fact]
    public void So_o_ultimo_trecho_pode_ser_aberto()
    {
        var act = () => new ClassProgression([new ClassSegment(Fighter, null), new ClassSegment(Warlock, null)]);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Media_de_PV_por_nivel()
    {
        Fighter.AverageHitPointsPerLevel.Should().Be(6);
        Warlock.AverageHitPointsPerLevel.Should().Be(5);
    }
}

public sealed class ArmorTableTests
{
    [Theory]
    [InlineData(4, 4, "couro batido", 16, false)] // 16 contra 16: empate fica com a mais leve
    [InlineData(1, 5, "meia armadura", 16, true)]
    [InlineData(2, 4, "cota de escamas", 16, true)]
    [InlineData(5, 5, "couro batido", 17, false)]
    public void Melhor_armadura_media_ou_leve(int dex, int level, string name, int armorClass, bool stealth)
    {
        var options = new[] { ArmorTable.StuddedLeather, level >= 5 ? ArmorTable.HalfPlate : ArmorTable.ScaleMail };

        var setup = ArmorTable.Best(options, dex, shield: false);

        setup.Description.Should().Be(name);
        setup.Value.Value.Should().Be(armorClass);
        setup.StealthDisadvantage.Should().Be(stealth);
    }

    [Fact]
    public void Escudo_e_bonus()
    {
        var setup = ArmorTable.Wear(ArmorTable.ChainMail, dexterityModifier: 3, shield: true, bonus: 1, suffix: " + Defense");

        setup.Value.Value.Should().Be(19); // pesada ignora a Destreza
        setup.Description.Should().Be("cota de malha + escudo + Defense");
        setup.WithBonus(1).Value.Value.Should().Be(20);
    }
}

public sealed class CharacterTraitsTests
{
    [Fact]
    public void Visao_no_escuro_minima_e_extensao()
    {
        var traits = CharacterTraits.From(
        [
            new DarkvisionGrant(Meters.Of(18), DarkvisionKind.Extension, "Umbral Sight"),
            new DarkvisionGrant(Meters.Of(36), DarkvisionKind.Minimum),
        ]);

        traits.Darkvision(Meters.Zero).Should().Be(new DarkvisionResult(Meters.Of(36), "Umbral Sight"));
        traits.Darkvision(Meters.Of(36)).Range.Value.Should().Be(54);
    }

    [Fact]
    public void Iniciativa_soma_todas_as_fontes()
    {
        var traits = CharacterTraits.From(
        [
            new InitiativeBonus(InitiativeAmount.ProficiencyBonus),
            new InitiativeBonus(InitiativeAmount.HalfProficiencyBonus),
            new InitiativeAbilityBonus(Ability.Wisdom),
        ]);

        traits.InitiativeModifier(3, AbilityMap.Of(0, 0, 0, 0, 2, 0)).Should().Be(3 + 1 + 2);
    }

    [Fact]
    public void Aura_tem_piso()
    {
        var traits = CharacterTraits.From([new SaveAura(Ability.Charisma, 1, "Aura of Protection")]);

        traits.SaveBonus(AbilityMap.Of(0, 0, 0, 0, 0, -1)).Should().Be(1);
        traits.SaveBonus(AbilityMap.Of(0, 0, 0, 0, 0, 4)).Should().Be(4);
    }

    [Fact]
    public void Countercharm_e_Survivor_entram_so_no_numero_com_recursos()
    {
        var traits = CharacterTraits.From(
        [
            new ConditionRecovery(Condition.Charmed, new RerollOnFailure(RollMode.Advantage, "Countercharm")),
            new ConditionRecovery(Condition.Charmed, new BonusOnFailure(3, "Survivor")),
            new ConditionRecovery(Condition.Frightened, new BonusOnFailure(3, "outra coisa")),
        ]);
        var saves = AbilityMap.Uniform(new SavingThrow(1, false, false));

        var resistance = DefenseCalculator.Resist(StrahdCharm, saves, traits, proficiencyBonus: 3);

        resistance.Passive.Value.Should().BeApproximately(0.25, 1e-12);
        resistance.WithResources.Should().BeGreaterThan(resistance.Passive);
        resistance.Resources.Should().Equal("Countercharm", "Survivor");
    }
}
