using DndMaker.Content.Catalog;
using DndMaker.Domain.Abilities;
using DndMaker.Domain.Builds;
using DndMaker.Domain.Catalog;
using DndMaker.Domain.Classes;
using DndMaker.Domain.Damage;
using DndMaker.Domain.Effects;
using DndMaker.Domain.Equipment;
using DndMaker.Domain.Routines;
using static DndMaker.Domain.Abilities.Ability;

namespace DndMaker.Content.Archetypes;

/// <summary>Bardo do Colégio das Espadas: gasta a Inspiração em floreios de dano, defesa e mobilidade.</summary>
public sealed class SwordsBard() : BuildArchetype(Definition)
{
    public static readonly VariantId Melee = new("melee");
    public static readonly VariantId Staff = new("staff");
    public static readonly VariantId Bow = new("arco");

    /// <summary>Estilo de luta Dueling, que o colégio concede no nível 3.</summary>
    private const int DuelingBonus = 2;

    private static readonly ArchetypeProfile Definition = new()
    {
        Id = new("swords"),
        Name = "Bardo das Espadas",
        Epithet = "O Duelista",
        Summary = "Bardo que gasta a Inspiração em si mesmo, com floreios de dano, defesa e mobilidade.",
        Status = ArchetypeStatus.UnderReview,
        BaseScores = AbilityMap.Of(8, 14, 13, 10, 12, 15),
        Priority = [Charisma, Dexterity, Constitution, Wisdom, Intelligence, Strength],
        SpellcastingAbility = Charisma,
        SuggestedBackground = new("artista"),
        Progression = ClassProgression.Of(ClassCatalog.Bard),
        Variants =
        [
            new(Melee, "Cimitarra (Dueling)"),
            new(Staff, "Bordão com Shillelagh (Dueling)") { RequiredFeat = FeatCatalog.MagicInitiateDruid.Id },
            new(Bow, "Arco curto"),
        ],
        UsesConcentration = true,
        Milestones =
        [
            new(1, "Inspiração Bárdica (d6), magias de bardo, True Strike"),
            new(2, "Expertise em 2 perícias; Jack of All Trades"),
            new(3, "Espadas: armadura média, cimitarra, estilo Dueling e Blade Flourish; magias de 2º círculo"),
            new(4, "Talento ou aumento de atributo"),
            new(5, "Magias de 3º círculo; Inspiração volta em descanso curto e vira d8"),
            new(6, "Extra Attack"),
            new(7, "Countercharm (reação: rerrola com vantagem um teste falho contra Enfeitiçado ou Amedrontado, seu ou de um aliado a 9 m); magias de 4º círculo"),
            new(8, "Talento ou aumento de atributo"),
            new(9, "Expertise em mais 2 perícias"),
            new(10, "Magical Secrets; Inspiração vira d10"),
        ],
        Pros = "Conjurador completo que luta de perto ou com arco. Bom dano no nível 6 com floreios.",
        Cons = "Sem escudo. Depende de Destreza e Carisma, a menos que use Shillelagh. Floreios gastam Inspiração.",
    };

    /// <summary>Dado de Inspiração Bárdica: d6, d8 no nível 5, d10 no 10.</summary>
    private static Dice InspirationDie(BuildContext context) => context.Level.Value switch
    {
        >= 10 => Dice.D10,
        >= 5 => Dice.D8,
        _ => Dice.D6,
    };

    public override IEnumerable<Effect> FeaturesAt(BuildContext context)
    {
        if (context.Level >= 2)
        {
            yield return new InitiativeBonus(InitiativeAmount.HalfProficiencyBonus); // Jack of All Trades
        }

        if (context.Level >= 7)
        {
            foreach (var effect in ArchetypeRules.Countercharm())
            {
                yield return effect;
            }
        }
    }

    public override ArmorSetup ArmorAt(BuildContext context) =>
        context.Level < 3
            ? ArchetypeRules.StuddedLeather(context)
            : ArchetypeRules.MediumOrLight(context, shield: false) with
            {
                Extra = "Defensive Flourish soma o dado de Inspiração à CA por 1 turno",
            };

    public override IEnumerable<AttackRoutine> RoutinesAt(BuildContext context)
    {
        var melee = !context.IsVariant(Bow);
        var shillelagh = context.Has(FeatCatalog.MagicInitiateDruid.Id) && context.IsVariant(Staff);
        var trueStrikeBonus = context.AttackBonus(Charisma);
        var trueStrikeExtra = ArchetypeRules.TrueStrikeExtra(context);

        if (context.Level < 3)
        {
            var (name, die) = shillelagh
                ? ("bordão com Shillelagh", ArchetypeRules.ShillelaghDie(context))
                : melee ? ("maça", Dice.D6) : ("besta leve", Dice.D8);
            yield return AttackRoutine.AtWill(
                $"True Strike ({name})",
                new AttackStep(1, trueStrikeBonus, die + context.Mod(Charisma) + trueStrikeExtra));
            yield break;
        }

        var attacks = context.Level >= 6 ? 2 : 1; // Extra Attack
        var dueling = melee ? DuelingBonus : 0;
        var weaponName = shillelagh ? "Bordão com Shillelagh" : melee ? "Cimitarra" : "Arco curto";
        var weaponDie = shillelagh ? ArchetypeRules.ShillelaghDie(context) : Dice.D6;
        var ability = shillelagh ? Charisma : Dexterity;
        var weapon = new AttackStep(attacks, context.AttackBonus(ability), weaponDie + (context.Mod(ability) + dueling));

        yield return AttackRoutine.AtWill(weaponName, weapon);
        yield return AttackRoutine.Peak(weaponName + " + Blade Flourish", weapon, new RiderStep(weapon, InspirationDie(context)))
            .WithNote("Gasta uma Inspiração Bárdica.");
        yield return AttackRoutine.AtWill(
                "True Strike",
                new AttackStep(1, trueStrikeBonus, weaponDie + (context.Mod(Charisma) + dueling) + trueStrikeExtra))
            .WithNote("É uma magia: não ativa o floreio.");
    }
}
