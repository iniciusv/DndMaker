using DndMaker.Content.Catalog;
using DndMaker.Domain.Abilities;
using DndMaker.Domain.Builds;
using DndMaker.Domain.Catalog;
using DndMaker.Domain.Classes;
using DndMaker.Domain.Damage;
using DndMaker.Domain.Equipment;
using DndMaker.Domain.Routines;
using static DndMaker.Domain.Abilities.Ability;

namespace DndMaker.Content.Archetypes;

/// <summary>Guerreiro Battle Master atirador: sem magias, com manobras e Action Surge.</summary>
public sealed class BattleMasterFighter() : BuildArchetype(Definition)
{
    public static readonly VariantId HeavyCrossbow = new("besta");
    public static readonly VariantId RepeatingShot = new("rep");
    public static readonly VariantId Longbow = new("arco");

    /// <summary>Estilo de luta Archery.</summary>
    private const int ArcheryBonus = 2;

    /// <summary>Besta com a infusão Repeating Shot do artífice: arma +1 que ignora a recarga.</summary>
    private const int RepeatingShotBonus = 1;

    private static readonly ArchetypeProfile Definition = new()
    {
        Id = new("bm"),
        Name = "Guerreiro Battle Master",
        Epithet = "O Besteiro",
        Summary = "Atirador tático sem magias, com manobras que derrubam, empurram e amedrontam.",
        Status = ArchetypeStatus.Discarded,
        BaseScores = AbilityMap.Of(8, 15, 14, 10, 13, 12),
        Priority = [Dexterity, Constitution, Wisdom, Charisma, Intelligence, Strength],
        SpellcastingAbility = Wisdom,
        SuggestedBackground = new("soldado"),
        Progression = ClassProgression.Of(ClassCatalog.Fighter),
        Variants =
        [
            new(HeavyCrossbow, "Besta pesada"),
            new(RepeatingShot, "Besta Repeating Shot (do artífice)"),
            new(Longbow, "Arco longo"),
        ],
        Milestones =
        [
            new(1, "Estilo Archery (+2), Second Wind, Weapon Mastery (Push na besta pesada)"),
            new(2, "Action Surge e Tactical Mind"),
            new(3, "Battle Master: 4 dados de superioridade (d8) e 3 manobras"),
            new(4, "Talento ou aumento de atributo"),
            new(5, "Extra Attack (a besta exige Crossbow Expert ou Repeating Shot)"),
            new(6, "Talento extra do guerreiro"),
            new(7, "Know Your Enemy; 5 dados e 5 manobras"),
            new(8, "Talento ou aumento de atributo"),
            new(9, "Indomitable (rerrola um teste de resistência)"),
            new(10, "Dados viram d10; 7 manobras"),
        ],
        Pros = "Dano constante, recursos em descanso curto e a melhor sinergia com o ladino.",
        Cons = "Sem magias. A besta cobra um talento ou depende do artífice. Pouca utilidade fora de combate.",
    };

    public override ArmorSetup ArmorAt(BuildContext context) => ArchetypeRules.MediumOrLight(context, shield: false);

    public override IEnumerable<AttackRoutine> RoutinesAt(BuildContext context)
    {
        var repeating = context.IsVariant(RepeatingShot) ? RepeatingShotBonus : 0;
        var bow = context.IsVariant(Longbow);

        // A besta pesada só dispara mais de uma vez por turno sem recarga (Crossbow Expert ou Repeating Shot).
        var canAttackTwice = bow || repeating > 0 || context.Has(FeatCatalog.CrossbowExpert.Id);
        var attacks = context.Level >= 5 && canAttackTwice ? 2 : 1;
        var hit = context.AttackBonus(Dexterity) + ArcheryBonus + repeating;
        var damage = (bow ? Dice.D8 : Dice.D10) + (context.Mod(Dexterity) + repeating);
        var weaponName = bow ? "Arco longo" : "Besta pesada";
        var weapon = new AttackStep(attacks, hit, damage);
        var surge = new AttackStep(2 * attacks, hit, damage);

        yield return AttackRoutine.AtWill(weaponName, weapon)
            .WithNote(context.Level >= 5 && !canAttackTwice ? "Sem Crossbow Expert, a recarga limita a 1 tiro por turno." : string.Empty);

        if (context.Level >= 3)
        {
            var superiority = context.Level >= 10 ? Dice.D10 : Dice.D8;
            yield return AttackRoutine.Peak(weaponName + " + manobra", weapon, new RiderStep(weapon, superiority))
                .WithNote("Gasta um dado de superioridade (volta em descanso curto).");
            yield return AttackRoutine.Peak("Action Surge + manobra", surge, new RiderStep(surge, superiority))
                .WithNote("1x por descanso.");
        }
        else if (context.Level >= 2)
        {
            yield return AttackRoutine.Peak("Action Surge", surge).WithNote("1x por descanso.");
        }
    }
}
