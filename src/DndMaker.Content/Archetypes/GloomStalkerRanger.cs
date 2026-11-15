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

/// <summary>Patrulheiro Gloom Stalker com arco longo: age primeiro e some no escuro.</summary>
public sealed class GloomStalkerRanger() : BuildArchetype(Definition)
{
    /// <summary>Estilo de luta Archery, no nível 2.</summary>
    private const int ArcheryBonus = 2;

    private static readonly ArchetypeProfile Definition = new()
    {
        Id = new("gloom"),
        Name = "Patrulheiro Gloom Stalker",
        Epithet = "O Caçador nas Brumas",
        Summary = "Arqueiro que age primeiro, some no escuro e não precisa de talento nenhum.",
        Status = ArchetypeStatus.UnderReview,
        BaseScores = AbilityMap.Of(8, 15, 13, 10, 14, 12),
        Priority = [Dexterity, Wisdom, Constitution, Charisma, Intelligence, Strength],
        SpellcastingAbility = Wisdom,
        SuggestedBackground = new("andarilho"),
        Progression = ClassProgression.Of(ClassCatalog.Ranger),
        UsesConcentration = true,
        Milestones =
        [
            new(1, "Hunter's Mark grátis 2x por dia, Weapon Mastery, magias de patrulheiro"),
            new(2, "Estilo Archery (+2), Expertise em 1 perícia"),
            new(3, "Gloom Stalker: Dreadful Strike (+2d6 psíquico), + Sabedoria na iniciativa, Umbral Sight"),
            new(4, "Talento ou aumento de atributo"),
            new(5, "Extra Attack; magias de 2º círculo (Pass without Trace)"),
            new(6, "Roving: +3 m de deslocamento, escalada e natação"),
            new(7, "Iron Mind: proficiência em testes de Sabedoria"),
            new(8, "Talento ou aumento de atributo"),
            new(9, "Expertise em mais 2 perícias; magias de 3º círculo"),
            new(10, "Tireless"),
        ],
        Pros = "Melhor dano à distância com arma. Cobre exploração e furtividade. Nenhum talento obrigatório.",
        Cons = "Hunter's Mark prende a concentração. Poucos espaços de magia. Sabedoria só melhora no 7.",
    };

    public override IEnumerable<Effect> FeaturesAt(BuildContext context)
    {
        if (context.Level >= 3)
        {
            yield return new InitiativeAbilityBonus(Wisdom); // Dread Ambusher
            yield return new DarkvisionGrant(Meters.Of(18), DarkvisionKind.Extension, "Umbral Sight: invisível para quem depende de visão no escuro");
        }

        if (context.Level >= 7)
        {
            yield return new SaveProficiency(Wisdom); // Iron Mind
        }
    }

    public override ArmorSetup ArmorAt(BuildContext context) => ArchetypeRules.MediumOrLight(context, shield: false);

    public override IEnumerable<AttackRoutine> RoutinesAt(BuildContext context)
    {
        var hit = context.AttackBonus(Dexterity) + (context.Level >= 2 ? ArcheryBonus : 0);
        var attacks = context.Level >= 5 ? 2 : 1; // Extra Attack
        var bow = Dice.D8 + context.Mod(Dexterity);
        var marked = new AttackStep(attacks, hit, bow + Dice.D6);

        yield return AttackRoutine.AtWill("Arco longo", new AttackStep(attacks, hit, bow));
        yield return AttackRoutine.Concentration("Arco longo + Hunter's Mark", marked);
        if (context.Level >= 3)
        {
            var uses = Math.Max(1, context.Mod(Wisdom));
            yield return AttackRoutine.Peak("… + Dreadful Strike", marked, new RiderStep(marked, 2 * Dice.D6))
                .WithNote($"{uses} usos por descanso longo.");
        }
    }
}
