using DndMaker.Content.Catalog;
using DndMaker.Domain.Abilities;
using DndMaker.Domain.Builds;
using DndMaker.Domain.Classes;
using DndMaker.Domain.Common;
using DndMaker.Domain.Damage;
using DndMaker.Domain.Effects;
using DndMaker.Domain.Equipment;
using DndMaker.Domain.Routines;
using static DndMaker.Domain.Abilities.Ability;

namespace DndMaker.Content.Archetypes;

/// <summary>Feiticeiro das Sombras: conjurador completo com proficiência em Constituição e visão no escuro.</summary>
public sealed class ShadowSorcerer() : BuildArchetype(Definition)
{
    private const int FireballTargets = 3;

    private static readonly Dice FireballDamage = 8 * Dice.D6;

    private static readonly ArchetypeProfile Definition = new()
    {
        Id = new("shadow"),
        Name = "Feiticeiro das Sombras",
        Epithet = "A Sombra",
        Summary = "Conjurador completo com proficiência em Constituição, que enxerga no escuro e resiste a cair.",
        Status = ArchetypeStatus.LittleExplored,
        BaseScores = AbilityMap.Of(8, 13, 14, 10, 12, 15),
        Priority = [Charisma, Constitution, Dexterity, Wisdom, Intelligence, Strength],
        SpellcastingAbility = Charisma,
        SuggestedBackground = new("mercador"),
        Progression = ClassProgression.Of(ClassCatalog.Sorcerer),
        UsesConcentration = true,
        Milestones =
        [
            new(1, "Innate Sorcery (vantagem nos ataques de magia por 1 minuto, 2x por dia)"),
            new(2, "Font of Magic e Metamagic"),
            new(3, "Sombras: visão no escuro de 36 m, Darkness que você enxerga, Strength of the Grave"),
            new(4, "Talento ou aumento de atributo"),
            new(5, "Magias de 3º círculo (Fireball); Sorcerous Restoration"),
            new(6, "Invoca uma besta das sombras sem espaço e sem concentração"),
            new(7, "Sorcery Incarnate; magias de 4º círculo"),
            new(8, "Talento ou aumento de atributo"),
            new(9, "Magias de 5º círculo"),
            new(10, "Mais uma opção de Metamagic"),
        ],
        Pros = "Maior dano em área. Concentração firme. Difícil de derrubar de vez.",
        Cons = "Frágil: d6 de vida e sem armadura. Mais um conjurador puro. Pouco analisada até aqui.",
    };

    public override IEnumerable<Effect> FeaturesAt(BuildContext context)
    {
        if (context.Level >= 3)
        {
            yield return new DarkvisionGrant(Meters.Of(36), DarkvisionKind.Minimum, "Eyes of the Dark");
        }
    }

    public override ArmorSetup ArmorAt(BuildContext context) =>
        ArchetypeRules.MageArmor(context, "Mage Armor (gasta um espaço de 1º círculo)");

    public override IEnumerable<AttackRoutine> RoutinesAt(BuildContext context)
    {
        var hit = context.SpellAttackBonus;
        yield return AttackRoutine.AtWill("Fire Bolt", new AttackStep(1, hit, new Dice(context.CantripDice, 10)));

        if (context.Level >= 3)
        {
            yield return AttackRoutine.Peak("Scorching Ray", new AttackStep(3, hit, 2 * Dice.D6))
                .WithNote("Espaço de 2º círculo.");
        }

        if (context.Level >= 5)
        {
            yield return AttackRoutine.Peak("Fireball (1 alvo)", new SaveStep(context.SpellSaveDc, FireballDamage, halfOnSuccess: true))
                .WithNote("Espaço de 3º círculo; metade se o alvo passar.");
            yield return AttackRoutine.Peak(
                "Fireball (3 alvos)",
                new SaveStep(context.SpellSaveDc, FireballDamage, halfOnSuccess: true, targets: FireballTargets));
        }
    }
}
