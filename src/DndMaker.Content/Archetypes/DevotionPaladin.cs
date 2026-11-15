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
/// Paladino do Juramento da Devoção com Destreza: rapieira e escudo com Dueling, Sacred Weapon no 3 e,
/// no 7, Aura of Devotion (imune a Enfeitiçado). Não existia no logic.js, por isso não tem golden.
/// </summary>
public sealed class DevotionPaladin() : BuildArchetype(Definition)
{
    /// <summary>Nível em que o paladino escolhe o estilo de luta (Dueling).</summary>
    private const int FightingStyleLevel = 2;

    private const int DuelingBonus = 2;

    /// <summary>Sacred Weapon soma o Carisma ao ataque, no mínimo +1.</summary>
    private const int SacredWeaponMinimum = 1;

    private static readonly ArchetypeProfile Definition = new()
    {
        Id = new("devotion"),
        Name = "Paladino (Devotion)",
        Epithet = "A Lâmina Devota",
        Summary = "Paladino de Destreza com rapieira e escudo. Sacred Weapon soma Carisma ao ataque e, no 7, a Aura of Devotion o deixa imune ao charme de Strahd.",
        Status = ArchetypeStatus.UnderReview,
        BaseScores = AbilityMap.Of(8, 15, 13, 10, 12, 14),
        Priority = [Dexterity, Charisma, Constitution, Wisdom, Intelligence, Strength],
        SpellcastingAbility = Charisma,
        SuggestedBackground = new("andarilho"),
        Progression = ClassProgression.Of(ClassCatalog.Paladin),
        UsesConcentration = true,
        Milestones =
        [
            new(1, "Armadura média e escudo, Lay on Hands, Weapon Mastery (Vex na rapieira), magias de paladino"),
            new(2, "Estilo Dueling (+2 dano) e Divine Smite"),
            new(3, "Juramento da Devoção: Sacred Weapon (Canalizar Divindade, + Carisma no ataque por 10 minutos); Protection from Evil and Good e Shield of Faith sempre preparadas"),
            new(4, "Talento ou aumento de atributo"),
            new(5, "Extra Attack; magias de 2º círculo, com Aid e Zone of Truth sempre preparadas"),
            new(6, "Aura of Protection: + Carisma em todos os testes de resistência, seus e de aliados a até 3 m"),
            new(7, "Aura of Devotion: você e os aliados na aura são imunes a Enfeitiçado"),
            new(8, "Talento ou aumento de atributo"),
            new(9, "Abjure Foes; magias de 3º círculo, com Beacon of Hope e Dispel Magic"),
            new(10, "Aura of Courage: imune a Amedrontado"),
        ],
        Pros = "Imune ao charme de Strahd a partir do 7, e a aura protege os aliados por perto. Sacred Weapon acerta muito. Destreza dá iniciativa e testes de DES.",
        Cons = "Menos dano por golpe que um paladino de Força com arma pesada. Precisa de Destreza e Carisma ao mesmo tempo. A party já tem um paladino.",
    };

    public override IEnumerable<Effect> FeaturesAt(BuildContext context)
    {
        if (context.Level >= 6)
        {
            yield return new SaveAura(Charisma, 1, "Aura of Protection");
        }

        if (context.Level >= 7)
        {
            yield return new ConditionImmunity(Condition.Charmed, "Aura of Devotion");
        }
    }

    public override ArmorSetup ArmorAt(BuildContext context) => ArchetypeRules.MediumOrLight(context, shield: true);

    public override IEnumerable<AttackRoutine> RoutinesAt(BuildContext context)
    {
        var dueling = context.Level >= FightingStyleLevel ? DuelingBonus : 0;
        var attacks = context.Level >= 5 ? 2 : 1;
        var damage = Dice.D8 + (context.Mod(Dexterity) + dueling);
        var rapier = new AttackStep(attacks, context.AttackBonus(Dexterity), damage);

        yield return AttackRoutine.AtWill("Rapieira", rapier).WithNote("Vex (vantagem depois de acertar) não entra na conta.");
        if (context.Level < FightingStyleLevel)
        {
            yield break;
        }

        yield return AttackRoutine.Peak("Rapieira + Divine Smite", rapier, new RiderStep(rapier, 2 * Dice.D8))
            .WithNote("Espaço de 1º círculo; dano radiante.");
        yield return AttackRoutine.Peak("… contra morto-vivo", rapier, new RiderStep(rapier, 3 * Dice.D8)).When(Situation.VsUndead);

        if (context.Level >= 3)
        {
            var sacred = new AttackStep(attacks, context.AttackBonus(Dexterity) + Math.Max(SacredWeaponMinimum, context.Mod(Charisma)), damage);
            yield return AttackRoutine.Peak("Rapieira com Sacred Weapon", sacred)
                .WithNote("Canalizar Divindade: + Carisma no ataque por 10 minutos, o combate inteiro.");
            yield return AttackRoutine.Peak("Sacred Weapon + Divine Smite", sacred, new RiderStep(sacred, 2 * Dice.D8))
                .WithNote("Canalizar Divindade e espaço de 1º círculo.");
        }
    }
}
