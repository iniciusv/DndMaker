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

/// <summary>Bruxo puro de Eldritch Blast: dano à distância gratuito e espaços que voltam em descanso curto.</summary>
public sealed class EldritchWarlock() : BuildArchetype(Definition)
{
    private static readonly ArchetypeProfile Definition = new()
    {
        Id = new("lock"),
        Name = "Bruxo (Eldritch Blast)",
        Epithet = "O Ocultista",
        Summary = "Dano à distância gratuito e a classe mais personalizável, com espaços que voltam em descanso curto.",
        Status = ArchetypeStatus.UnderReview,
        BaseScores = AbilityMap.Of(8, 13, 14, 10, 12, 15),
        Priority = [Charisma, Constitution, Dexterity, Wisdom, Intelligence, Strength],
        SpellcastingAbility = Charisma,
        SuggestedBackground = new("charlatao"),
        Progression = ClassProgression.Of(ClassCatalog.Warlock),
        UsesConcentration = true,
        Milestones =
        [
            new(1, "Eldritch Blast, Armor of Shadows, magias de pacto"),
            new(2, "Agonizing Blast e Devil's Sight; Magical Cunning"),
            new(3, "Patrono (Celestial, Undead, Great Old One); espaços de 2º círculo"),
            new(4, "Talento ou aumento de atributo"),
            new(5, "Eldritch Blast com 2 raios; espaços de 3º círculo; 5 invocações"),
            new(6, "Característica do patrono"),
            new(7, "Espaços de 4º círculo; Whispers of the Grave"),
            new(8, "Talento ou aumento de atributo"),
            new(9, "Espaços de 5º círculo; Contact Patron"),
            new(10, "Característica do patrono"),
        ],
        Pros = "Dano constante sem gastar nada. Proficiência em Sabedoria. Gancho forte com Barovia.",
        Cons = "CA baixa e d8 de vida: não serve para o corpo a corpo. Mais um conjurador na party.",
    };

    public override IEnumerable<Effect> FeaturesAt(BuildContext context)
    {
        if (context.Level >= 2)
        {
            yield return new DarkvisionGrant(Meters.Of(36), DarkvisionKind.Minimum, "Devil's Sight: enxerga também em escuridão mágica");
        }
    }

    public override ArmorSetup ArmorAt(BuildContext context) =>
        ArchetypeRules.MageArmor(context, "Armor of Shadows (Mage Armor à vontade)");

    public override IEnumerable<AttackRoutine> RoutinesAt(BuildContext context)
    {
        var hit = context.AttackBonus(Charisma);
        var agonizing = context.Level >= 2 ? context.Mod(Charisma) : 0;
        var beam = Dice.D10 + agonizing;

        yield return AttackRoutine.AtWill("Eldritch Blast", new AttackStep(context.CantripDice, hit, beam))
            .WithNote(context.Level >= 2 ? "Com Repelling Blast, cada raio empurra 3 m." : string.Empty);
        yield return AttackRoutine.Concentration("Eldritch Blast + Hex", new AttackStep(context.CantripDice, hit, beam + Dice.D6));
    }
}
