using DndMaker.Domain.Abilities;
using DndMaker.Domain.Analysis;
using DndMaker.Domain.Builds;
using DndMaker.Domain.Catalog;
using DndMaker.Domain.Classes;
using DndMaker.Domain.Combat;
using DndMaker.Domain.Common;
using DndMaker.Domain.Damage;
using DndMaker.Domain.Effects;
using DndMaker.Domain.Equipment;
using DndMaker.Domain.Routines;

namespace DndMaker.Domain.Tests.Fixtures;

/// <summary>
/// Um pedaço mínimo de conteúdo para testar o domínio. As duas builds copiam regra por regra
/// o paladino e o Guerreiro 1 → Bruxo do Artefato Base/logic.js, para comparar com o golden.
/// </summary>
internal static class TestRules
{
    public static readonly CharacterClass Fighter = new(new("fighter"), "Guerreiro", 10, AbilitySet.Strength | AbilitySet.Constitution)
    {
        FeatLevels = [4, 6, 8, 12, 14, 16, 19],
    };

    public static readonly CharacterClass Warlock = new(new("warlock"), "Bruxo", 8, AbilitySet.Wisdom | AbilitySet.Charisma);
    public static readonly CharacterClass Paladin = new(new("paladin"), "Paladino", 10, AbilitySet.Wisdom | AbilitySet.Charisma);

    public static readonly Feat Alert = new(new("alert"), "Alert", FeatCategory.Origin, "")
    {
        Effects = [new InitiativeBonus(InitiativeAmount.ProficiencyBonus)],
    };

    public static readonly Feat Skilled = new(new("skilled"), "Skilled", FeatCategory.Origin, "");
    public static readonly Feat Survivor = new(new("survivor"), "Survivor", FeatCategory.Origin, "");
    public static readonly Feat Tough = new(new("tough"), "Tough", FeatCategory.Origin, "") { Effects = [new HitPointsPerLevel(2)] };
    public static readonly Feat MistWalker = new(new("dg_mist"), "Dark Gift: Mist Walker", FeatCategory.DarkGift, "");
    public static readonly Feat FeyTouched = new(new("fey"), "Fey Touched", FeatCategory.General, "") { Increase = AbilityIncreaseRule.BestOddMental };

    public static readonly Feat WarCaster = new(new("warcaster"), "War Caster", FeatCategory.General, "")
    {
        Increase = AbilityIncreaseRule.BestOddMental,
        Effects = [new ConcentrationAdvantage()],
    };

    public static readonly Feat ResilientWisdom = new(new("res_wis"), "Resilient (Sabedoria)", FeatCategory.General, "")
    {
        Increase = AbilityIncreaseRule.Fixed(Ability.Wisdom),
        Effects = [new SaveProficiency(Ability.Wisdom)],
    };

    public static readonly Species Human = new(new("humano"), "Humano", "PHB 2024", "") { GrantsExtraOriginFeat = true };

    public static readonly Species Gnome = new(new("gnomo"), "Gnomo", "PHB 2024", "")
    {
        Darkvision = Meters.Of(18),
        Effects = [new SaveAdvantage(AbilitySet.Mental)],
    };

    public static readonly Species Elf = new(new("elfo"), "Elfo", "PHB 2024", "")
    {
        Darkvision = Meters.Of(18),
        Effects = [new ConditionAdvantage(Condition.Charmed)],
    };

    public static readonly Species Dwarf = new(new("anao"), "Anão", "PHB 2024", "")
    {
        Darkvision = Meters.Of(36),
        Effects = [new HitPointsPerLevel(1)],
    };

    public static readonly Background Noble = new(new("nobre"), "Nobre", AbilitySet.Strength | AbilitySet.Intelligence | AbilitySet.Charisma, Skilled, "");
    public static readonly Background Haunted = new(new("haunted"), "Haunted One", AbilitySet.Constitution | AbilitySet.Wisdom | AbilitySet.Charisma, Survivor, "");
    public static readonly Background Farmer = new(new("fazendeiro"), "Fazendeiro", AbilitySet.Strength | AbilitySet.Constitution | AbilitySet.Wisdom, Tough, "");

    public static readonly ConditionThreat StrahdCharm = new("Charme de Strahd", Ability.Wisdom, new DifficultyClass(17), Condition.Charmed);

    public static readonly CharacterAnalyzer Analyzer = new(StrahdCharm);

    public static readonly TestPaladin PaladinBuild = new();
    public static readonly TestFighterWarlock FighterWarlockBuild = new();

    public static CharacterBuild Paladin6(Species? species = null, Feat? extra = null) =>
        CharacterBuild.Create(PaladinBuild, species ?? Human, Noble, extra ?? Alert);
}

internal sealed class TestPaladin() : BuildArchetype(Definition)
{
    public static readonly VariantId Dueling = new("dueling");
    public static readonly VariantId Defense = new("defense");

    private static readonly ArchetypeProfile Definition = new()
    {
        Id = new("pal"),
        Name = "Paladino (Watchers)",
        Epithet = "O Vigia",
        Summary = "",
        Status = ArchetypeStatus.Finalist,
        BaseScores = AbilityMap.Of(15, 10, 13, 8, 12, 14),
        Priority = [Ability.Strength, Ability.Charisma, Ability.Constitution, Ability.Wisdom, Ability.Dexterity, Ability.Intelligence],
        SpellcastingAbility = Ability.Charisma,
        SuggestedBackground = new("nobre"),
        Progression = ClassProgression.Of(TestRules.Paladin),
        Variants = [new(Dueling, "Estilo Dueling (+2 dano)"), new(Defense, "Estilo Defense (+1 CA)")],
        UsesConcentration = true,
    };

    public override IEnumerable<Effect> FeaturesAt(BuildContext c)
    {
        if (c.Level >= 6)
        {
            yield return new SaveAura(Ability.Charisma, 1, "Aura of Protection");
        }
    }

    public override ArmorSetup ArmorAt(BuildContext c)
    {
        var defense = c.IsVariant(Defense);
        return ArmorTable.Wear(
            c.Level >= 5 ? ArmorTable.Splint : ArmorTable.ChainMail,
            c.Mod(Ability.Dexterity),
            shield: true,
            bonus: defense ? 1 : 0,
            suffix: defense ? " + Defense" : null);
    }

    public override IEnumerable<AttackRoutine> RoutinesAt(BuildContext c)
    {
        var dueling = c.IsVariant(Dueling) ? 2 : 0;
        var sword = new AttackStep(c.Level >= 5 ? 2 : 1, c.AttackBonus(Ability.Strength), Dice.D8 + (c.Mod(Ability.Strength) + dueling));
        yield return AttackRoutine.AtWill("Espada longa", sword);
        if (c.Level < 2)
        {
            yield break;
        }

        yield return AttackRoutine.Peak("Espada longa + Divine Smite", sword, new RiderStep(sword, 2 * Dice.D8))
            .WithNote("Espaço de 1º círculo; dano radiante.");
        yield return AttackRoutine.Peak("… contra morto-vivo", sword, new RiderStep(sword, 3 * Dice.D8));
    }
}

internal sealed class TestFighterWarlock() : BuildArchetype(Definition)
{
    public static readonly VariantId Defense = new("defense");
    public static readonly VariantId Dueling = new("dueling");

    private static readonly ArchetypeProfile Definition = new()
    {
        Id = new("fwl"),
        Name = "Guerreiro 1 → Bruxo",
        Epithet = "O Cavaleiro do Pacto",
        Summary = "",
        Status = ArchetypeStatus.Finalist,
        BaseScores = AbilityMap.Of(13, 12, 14, 8, 10, 15),
        Priority = [Ability.Charisma, Ability.Constitution, Ability.Strength, Ability.Dexterity, Ability.Wisdom, Ability.Intelligence],
        SpellcastingAbility = Ability.Charisma,
        SuggestedBackground = new("haunted"),
        Progression = ClassProgression.Dip(TestRules.Fighter, 1, TestRules.Warlock),
        Variants = [new(Defense, "Estilo Defense (+1 CA)"), new(Dueling, "Estilo Dueling (+2 dano)")],
        UsesConcentration = true,
    };

    private static int PactSlotLevel(BuildContext c) => Math.Min(5, (c.LevelIn(TestRules.Warlock) + 1) / 2);

    public override IEnumerable<Effect> FeaturesAt(BuildContext c)
    {
        if (c.LevelIn(TestRules.Warlock) >= 2)
        {
            yield return new DarkvisionGrant(Meters.Of(36), DarkvisionKind.Minimum, "Devil's Sight: enxerga também em escuridão mágica");
        }
    }

    public override ArmorSetup ArmorAt(BuildContext c)
    {
        var defense = c.IsVariant(Defense);
        var armor = ArmorTable.Wear(ArmorTable.ChainMail, c.Mod(Ability.Dexterity), shield: true, bonus: defense ? 1 : 0, suffix: defense ? " + Defense" : null);
        return c.LevelIn(TestRules.Warlock) >= 1
            ? armor with { Extra = $"Armor of Agathys: +{5 * PactSlotLevel(c)} PV temporários, sem concentração" }
            : armor;
    }

    public override IEnumerable<AttackRoutine> RoutinesAt(BuildContext c)
    {
        var dueling = c.IsVariant(Dueling) ? 2 : 0;
        var warlock = c.LevelIn(TestRules.Warlock);
        if (warlock == 0)
        {
            yield return AttackRoutine.AtWill("Espada longa (ainda com Força)",
                new AttackStep(1, c.AttackBonus(Ability.Strength), Dice.D8 + (c.Mod(Ability.Strength) + dueling)));
            yield break;
        }

        var hit = c.AttackBonus(Ability.Charisma);
        var attacks = warlock >= 5 ? 2 : 1;
        var weapon = new AttackStep(attacks, hit, Dice.D8 + (c.Mod(Ability.Charisma) + dueling));
        yield return AttackRoutine.AtWill("Arma do pacto (espada longa)", weapon);
        if (c.Level == 5)
        {
            yield return AttackRoutine.AtWill("True Strike com a arma do pacto",
                new AttackStep(1, hit, Dice.D8 + (c.Mod(Ability.Charisma) + dueling) + Dice.D6))
                .WithNote("Vale até o Extra Attack chegar no nível 6.");
        }

        yield return AttackRoutine.Concentration("Arma do pacto + Hex",
            new AttackStep(attacks, hit, Dice.D8 + (c.Mod(Ability.Charisma) + dueling) + Dice.D6));
        if (warlock >= 5)
        {
            yield return AttackRoutine.Peak("Arma do pacto + Eldritch Smite", weapon, new RiderStep(weapon, (PactSlotLevel(c) + 1) * Dice.D8))
                .WithNote("Gasta um espaço de pacto e derruba o alvo.");
        }

        var agonizing = warlock >= 2 ? c.Mod(Ability.Charisma) : 0;
        yield return AttackRoutine.AtWill("Eldritch Blast (à distância)", new AttackStep(c.CantripDice, hit, Dice.D10 + agonizing));
    }
}
