using DndMaker.Content.Catalog;
using DndMaker.Domain.Abilities;
using DndMaker.Domain.Builds;
using DndMaker.Domain.Catalog;
using DndMaker.Domain.Classes;
using DndMaker.Domain.Common;
using DndMaker.Domain.Damage;
using DndMaker.Domain.Effects;
using DndMaker.Domain.Equipment;
using DndMaker.Domain.Routines;
using static DndMaker.Domain.Abilities.Ability;

namespace DndMaker.Content.Archetypes;

/// <summary>Clérigo do Crepúsculo da ordem Protector: armadura pesada, Twilight Sanctuary e Spirit Guardians.</summary>
public sealed class TwilightCleric() : BuildArchetype(Definition)
{
    public static readonly VariantId SacredFlame = new("flame");
    public static readonly VariantId TollTheDead = new("toll");

    private static readonly ArchetypeProfile Definition = new()
    {
        Id = new("twi"),
        Name = "Clérigo do Crepúsculo",
        Epithet = "O Guardião do Crepúsculo",
        Summary = "Conjurador completo de armadura pesada, com Twilight Sanctuary e Spirit Guardians.",
        Status = ArchetypeStatus.UnderReview,
        BaseScores = AbilityMap.Of(13, 12, 14, 8, 15, 10),
        Priority = [Wisdom, Constitution, Strength, Dexterity, Charisma, Intelligence],
        SpellcastingAbility = Wisdom,
        SuggestedBackground = new("eremita"),
        Progression = ClassProgression.Of(ClassCatalog.Cleric),
        Variants = [new(SacredFlame, "Sacred Flame (radiante)"), new(TollTheDead, "Toll the Dead (necrótico)")],
        UsesConcentration = true,
        Milestones =
        [
            new(1, "Ordem Protector: armadura pesada e armas marciais; magias de clérigo"),
            new(2, "Canalizar Divindade: Divine Spark e Turn Undead"),
            new(3, "Crepúsculo: visão no escuro de 90 m, Vigilant Blessing e Twilight Sanctuary (PV temporários ou fim de charme e medo)"),
            new(4, "Talento ou aumento de atributo"),
            new(5, "Spirit Guardians e Revivify; Sear Undead"),
            new(6, "Steps of Night: voo no escuro; 3 usos de Canalizar Divindade"),
            new(7, "Blessed Strikes: + Sabedoria no dano dos truques"),
            new(8, "Talento ou aumento de atributo"),
            new(9, "Magias de 5º círculo"),
            new(10, "Divine Intervention"),
        ],
        Pros = "CA 18 e Sabedoria alta desde cedo. Domina os níveis 3 a 6. Dano radiante em área.",
        Cons = "Segundo clérigo da party e mais suporte. Muitos Mestres vetam. Luta com magias, não com arma.",
    };

    /// <summary>Spirit Guardians no 3º círculo: 3d8 radiante, metade no sucesso.</summary>
    private static readonly Dice SpiritGuardiansDamage = 3 * Dice.D8;

    private const int SpiritGuardiansTargets = 3;

    public override IEnumerable<Effect> FeaturesAt(BuildContext context)
    {
        if (context.Level >= 3)
        {
            yield return new DarkvisionGrant(Meters.Of(90), DarkvisionKind.Minimum, "Eyes of Night");

            // Problema 1 do plano: o Twilight Sanctuary encerra o charme no fim do turno; aparece ao lado da chance.
            yield return new ConditionRemedy(Condition.Charmed, "Twilight Sanctuary");
            yield return new ConditionRemedy(Condition.Frightened, "Twilight Sanctuary");
        }
    }

    public override ArmorSetup ArmorAt(BuildContext context)
    {
        var armor = ArmorTable.Wear(ArmorTable.ChainMail, context.Mod(Dexterity), shield: true, suffix: " (ordem Protector)");

        // Twilight Sanctuary: 1d6 + nível de clérigo, com a média escrita como na página (ex.: "6,5").
        return context.Level >= 3
            ? armor with { Extra = $"Twilight Sanctuary: {context.Level.Value + 3},5 PV temporários por turno para quem está na esfera" }
            : armor;
    }

    public override IEnumerable<AttackRoutine> RoutinesAt(BuildContext context)
    {
        var toll = context.IsVariant(TollTheDead);
        var cantripName = toll ? "Toll the Dead" : "Sacred Flame";
        var blessedStrikes = context.Level >= 7 ? context.Mod(Wisdom) : 0;
        var cantrip = new SaveStep(
            context.SpellSaveDc,
            new Dice(context.CantripDice, toll ? 12 : 8) + blessedStrikes);

        yield return AttackRoutine.AtWill(cantripName, cantrip)
            .WithNote(toll ? "Dano necrótico: muitos mortos-vivos resistem." : string.Empty);

        yield return AttackRoutine.Peak("Guiding Bolt", new AttackStep(1, context.SpellAttackBonus, 4 * Dice.D6))
            .WithNote("Espaço de 1º círculo; radiante.");

        if (context.Level >= 3 && context.Level < 5)
        {
            yield return AttackRoutine.Concentration(
                cantripName + " + Spiritual Weapon",
                cantrip,
                new AttackStep(1, context.SpellAttackBonus, Dice.D8 + context.Mod(Wisdom)));
        }

        if (context.Level >= 5)
        {
            yield return AttackRoutine.Concentration(
                    "Spirit Guardians (1 inimigo) + " + cantripName,
                    new SaveStep(context.SpellSaveDc, SpiritGuardiansDamage, halfOnSuccess: true),
                    cantrip)
                .WithNote("Radiante; metade do dano se o inimigo passar.");

            yield return AttackRoutine.Concentration(
                "Spirit Guardians (3 inimigos) + " + cantripName,
                new SaveStep(context.SpellSaveDc, SpiritGuardiansDamage, halfOnSuccess: true, targets: SpiritGuardiansTargets),
                cantrip);
        }
    }
}
