using DndMaker.Content.Catalog;
using DndMaker.Domain.Abilities;
using DndMaker.Domain.Builds;
using DndMaker.Domain.Classes;
using DndMaker.Domain.Damage;
using DndMaker.Domain.Effects;
using DndMaker.Domain.Equipment;
using DndMaker.Domain.Routines;
using static DndMaker.Domain.Abilities.Ability;

namespace DndMaker.Content.Archetypes;

/// <summary>
/// Paladino do Juramento dos Anciões com espadão: Great Weapon Fighting no 2, Moonbeam no 5,
/// Aura of Protection no 6 e Aura of Warding no 7. Não existia no logic.js, por isso não tem golden.
/// </summary>
public sealed class AncientsPaladin() : BuildArchetype(Definition)
{
    /// <summary>Nível em que o paladino escolhe o estilo de luta (Great Weapon Fighting).</summary>
    private const int FightingStyleLevel = 2;

    /// <summary>Great Weapon Fighting: 1 e 2 nos dados da arma de duas mãos contam como 3.</summary>
    private const int GreatWeaponFightingMinimum = 3;

    private static readonly Dice Greatsword = 2 * Dice.D6;

    private static readonly ArchetypeProfile Definition = new()
    {
        Id = new("ancients"),
        Name = "Paladino (Ancients)",
        Epithet = "O Guardião Antigo",
        Summary = "Espadão com Great Weapon Fighting, Moonbeam no 5 e, no 7, Aura of Warding contra dano necrótico e radiante.",
        Status = ArchetypeStatus.UnderReview,
        BaseScores = AbilityMap.Of(15, 10, 13, 8, 12, 14),
        Priority = [Strength, Charisma, Constitution, Wisdom, Dexterity, Intelligence],
        SpellcastingAbility = Charisma,
        SuggestedBackground = new("soldado"),
        Progression = ClassProgression.Of(ClassCatalog.Paladin),
        UsesConcentration = true,
        Milestones =
        [
            new(1, "Armadura pesada, Lay on Hands, Weapon Mastery (Graze no espadão), magias de paladino"),
            new(2, "Estilo Great Weapon Fighting (1 e 2 nos dados do espadão contam como 3) e Divine Smite"),
            new(3, "Juramento dos Anciões: Nature's Wrath (Canalizar Divindade, Contido com teste de FOR) e Ensnaring Strike sempre preparada"),
            new(4, "Talento ou aumento de atributo"),
            new(5, "Extra Attack; magias de 2º círculo, com Misty Step e Moonbeam sempre preparadas"),
            new(6, "Aura of Protection: + Carisma em todos os testes de resistência, seus e de aliados a até 3 m"),
            new(7, "Aura of Warding: resistência a dano necrótico, psíquico e radiante, sua e de aliados na aura"),
            new(8, "Talento ou aumento de atributo"),
            new(9, "Abjure Foes; magias de 3º círculo, com Plant Growth e Protection from Energy"),
            new(10, "Aura of Courage: imune a Amedrontado"),
        ],
        Pros = "Mais dano por golpe que o Watchers. Aura of Warding corta pela metade o dano necrótico dos mortos-vivos de Barovia. Moonbeam dá dano radiante contínuo.",
        Cons = "Sem escudo: 2 de CA a menos que o Watchers. Contra o charme, só a Aura of Protection. A party já tem um paladino.",
    };

    public override IEnumerable<Effect> FeaturesAt(BuildContext context)
    {
        if (context.Level >= 6)
        {
            yield return new SaveAura(Charisma, 1, "Aura of Protection");
        }
    }

    public override ArmorSetup ArmorAt(BuildContext context)
    {
        var armor = ArmorTable.Wear(
            context.Level >= 5 ? ArmorTable.Splint : ArmorTable.ChainMail,
            context.Mod(Dexterity),
            shield: false);

        return context.Level >= 7
            ? armor with { Extra = "Aura of Warding: resistência a dano necrótico, psíquico e radiante na aura (3 m)" }
            : armor;
    }

    public override IEnumerable<AttackRoutine> RoutinesAt(BuildContext context)
    {
        var greatWeaponFighting = context.Level >= FightingStyleLevel;
        var dice = greatWeaponFighting ? Greatsword.WithMinimumFace(GreatWeaponFightingMinimum) : Greatsword;
        var swing = new AttackStep(context.Level >= 5 ? 2 : 1, context.AttackBonus(Strength), dice + context.Mod(Strength));
        var style = greatWeaponFighting ? "Great Weapon Fighting: 1 e 2 nos d6 do espadão contam como 3. " : string.Empty;

        yield return AttackRoutine.AtWill("Espadão", swing)
            .WithNote(style + "Graze (dano no erro) não entra na conta.");
        if (context.Level < FightingStyleLevel)
        {
            yield break;
        }

        // O GWF vale só para os dados da arma; o Divine Smite fica com a média normal.
        yield return AttackRoutine.Peak("Espadão + Divine Smite", swing, new RiderStep(swing, 2 * Dice.D8))
            .WithNote("Espaço de 1º círculo; dano radiante.");
        yield return AttackRoutine.Peak("… contra morto-vivo", swing, new RiderStep(swing, 3 * Dice.D8)).When(Situation.VsUndead);

        if (context.Level >= 5)
        {
            yield return AttackRoutine.Concentration(
                    "Espadão + Moonbeam",
                    swing,
                    new SaveStep(context.SpellSaveDc, 2 * Dice.D10, halfOnSuccess: true))
                .WithNote("Espaço de 2º círculo; radiante, metade se o inimigo passar (CON). Vale nos turnos seguintes, com o inimigo no cilindro.");
        }
    }
}
