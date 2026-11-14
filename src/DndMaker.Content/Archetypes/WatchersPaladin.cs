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

/// <summary>Paladino do Juramento dos Vigias: Extra Attack no 5 e Aura of Protection no 6.</summary>
public sealed class WatchersPaladin() : BuildArchetype(Definition)
{
    public static readonly VariantId Dueling = new("dueling");
    public static readonly VariantId Defense = new("defense");

    private static readonly ArchetypeProfile Definition = new()
    {
        Id = new("pal"),
        Name = "Paladino (Watchers)",
        Epithet = "O Vigia",
        Summary = "Extra Attack no 5 e Aura of Protection no 6, com Divine Smite radiante contra mortos-vivos.",
        Status = ArchetypeStatus.Finalist,
        BaseScores = AbilityMap.Of(15, 10, 13, 8, 12, 14),
        Priority = [Strength, Charisma, Constitution, Wisdom, Dexterity, Intelligence],
        SpellcastingAbility = Charisma,
        SuggestedBackground = new("nobre"),
        Progression = ClassProgression.Of(ClassCatalog.Paladin),
        Variants = [new(Dueling, "Estilo Dueling (+2 dano)"), new(Defense, "Estilo Defense (+1 CA)")],
        UsesConcentration = true,
        Milestones =
        [
            new(1, "Armadura pesada, escudo, Lay on Hands, Weapon Mastery, magias de paladino"),
            new(2, "Estilo de luta e Divine Smite"),
            new(3, "Juramento Watchers: Watcher's Will dá vantagem em testes de INT, SAB e CAR a você e aliados por 1 minuto"),
            new(4, "Talento ou aumento de atributo"),
            new(5, "Extra Attack; magias de 2º círculo"),
            new(6, "Aura of Protection: + Carisma em todos os testes de resistência, seus e de aliados a até 3 m"),
            new(7, "Aura of the Sentinel: + PB na iniciativa de quem está na aura"),
            new(8, "Talento ou aumento de atributo"),
            new(9, "Abjure Foes; magias de 3º círculo"),
            new(10, "Aura of Courage: imune a Amedrontado"),
        ],
        Pros = "O mais forte nos níveis 5 e 6. Melhores testes de resistência do jogo a partir do 6. Dano radiante.",
        Cons = "A party já tem um paladino. Poucos espaços de magia. Quase sem ataque à distância.",
    };

    public override IEnumerable<Effect> FeaturesAt(BuildContext c)
    {
        if (c.Level >= 6)
        {
            yield return new SaveAura(Charisma, 1, "Aura of Protection");
        }
    }

    public override ArmorSetup ArmorAt(BuildContext c)
    {
        var defense = c.IsVariant(Defense);
        return ArmorTable.Wear(
            c.Level >= 5 ? ArmorTable.Splint : ArmorTable.ChainMail,
            c.Mod(Dexterity),
            shield: true,
            bonus: defense ? 1 : 0,
            suffix: defense ? " + Defense" : null);
    }

    public override IEnumerable<AttackRoutine> RoutinesAt(BuildContext c)
    {
        var dueling = c.IsVariant(Dueling) ? 2 : 0;
        var sword = new AttackStep(c.Level >= 5 ? 2 : 1, c.AttackBonus(Strength), Dice.D8 + (c.Mod(Strength) + dueling));
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
