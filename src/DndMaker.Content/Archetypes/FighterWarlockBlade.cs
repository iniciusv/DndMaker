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

/// <summary>Guerreiro 1 → Bruxo com Pact of the Blade: armadura pesada e arma do pacto com Carisma.</summary>
public sealed class FighterWarlockBlade() : BuildArchetype(Definition)
{
    public static readonly VariantId Defense = new("defense");
    public static readonly VariantId Dueling = new("dueling");

    private static readonly ArchetypeProfile Definition = new()
    {
        Id = new("fwl"),
        Name = "Guerreiro 1 → Bruxo",
        Epithet = "O Cavaleiro do Pacto",
        Summary = "Tanque arcano: armadura pesada, arma do pacto com Carisma e Eldritch Blast de reserva.",
        Status = ArchetypeStatus.Finalist,
        BaseScores = AbilityMap.Of(13, 12, 14, 8, 10, 15),
        Priority = [Charisma, Constitution, Strength, Dexterity, Wisdom, Intelligence],
        SpellcastingAbility = Charisma,
        SuggestedBackground = new("haunted"),
        Progression = ClassProgression.Dip(ClassCatalog.Fighter, 1, ClassCatalog.Warlock),
        Variants = [new(Defense, "Estilo Defense (+1 CA)"), new(Dueling, "Estilo Dueling (+2 dano)")],
        UsesConcentration = true,
        Milestones =
        [
            new(1, "Guerreiro: armadura pesada, escudo, estilo de luta, Weapon Mastery, Second Wind, testes de FOR e CON"),
            new(2, "Bruxo 1: Pact of the Blade (ataque com Carisma), Eldritch Blast, Armor of Agathys"),
            new(3, "Agonizing Blast e Devil's Sight; Magical Cunning; 2 espaços de pacto"),
            new(4, "Patrono (Celestial ou Undead); espaços de 2º círculo"),
            new(5, "Talento ou aumento de atributo; Eldritch Blast com 2 raios"),
            new(6, "Thirsting Blade (Extra Attack) e Eldritch Smite; espaços de 3º círculo"),
            new(7, "Característica do patrono"),
            new(8, "Espaços de 4º círculo"),
            new(9, "Talento ou aumento de atributo"),
            new(10, "Lifedrinker; espaços de 5º círculo"),
        ],
        Pros = "Melhor CA e concentração entre quem ataca com arma. Luta de perto e de longe. Espaços voltam em descanso curto.",
        Cons = "Sem proficiência em Sabedoria. Nível 1 fraco. Só 2 espaços de magia por descanso curto.",
    };

    private static int WarlockLevel(BuildContext context) => context.LevelIn(ClassCatalog.Warlock);

    /// <summary>Círculo dos espaços de pacto: metade do nível de bruxo, arredondada para cima, até 5.</summary>
    private static int PactSlotLevel(BuildContext context) => Math.Min(5, (WarlockLevel(context) + 1) / 2);

    public override IEnumerable<Effect> FeaturesAt(BuildContext context)
    {
        if (WarlockLevel(context) >= 2)
        {
            yield return new DarkvisionGrant(Meters.Of(36), DarkvisionKind.Minimum, "Devil's Sight: enxerga também em escuridão mágica");
        }
    }

    public override ArmorSetup ArmorAt(BuildContext context)
    {
        var defense = context.IsVariant(Defense);
        var armor = ArmorTable.Wear(ArmorTable.ChainMail, context.Mod(Dexterity), shield: true, bonus: defense ? 1 : 0, suffix: defense ? " + Defense" : null);
        return WarlockLevel(context) >= 1
            ? armor with { Extra = $"Armor of Agathys: +{5 * PactSlotLevel(context)} PV temporários, sem concentração" }
            : armor;
    }

    public override IEnumerable<AttackRoutine> RoutinesAt(BuildContext context)
    {
        var dueling = context.IsVariant(Dueling) ? 2 : 0;
        if (WarlockLevel(context) == 0)
        {
            yield return AttackRoutine.AtWill("Espada longa (ainda com Força)",
                new AttackStep(1, context.AttackBonus(Strength), Dice.D8 + (context.Mod(Strength) + dueling)));
            yield break;
        }

        var hit = context.AttackBonus(Charisma);
        var attacks = WarlockLevel(context) >= 5 ? 2 : 1; // Thirsting Blade
        var sword = Dice.D8 + (context.Mod(Charisma) + dueling);
        var weapon = new AttackStep(attacks, hit, sword);

        yield return AttackRoutine.AtWill("Arma do pacto (espada longa)", weapon);
        if (context.Level == 5)
        {
            yield return AttackRoutine.AtWill("True Strike com a arma do pacto", new AttackStep(1, hit, sword + Dice.D6))
                .WithNote("Vale até o Extra Attack chegar no nível 6.");
        }

        yield return AttackRoutine.Concentration("Arma do pacto + Hex", new AttackStep(attacks, hit, sword + Dice.D6));
        if (WarlockLevel(context) >= 5)
        {
            yield return AttackRoutine.Peak("Arma do pacto + Eldritch Smite", weapon, new RiderStep(weapon, (PactSlotLevel(context) + 1) * Dice.D8))
                .WithNote("Gasta um espaço de pacto e derruba o alvo.");
        }

        var agonizing = WarlockLevel(context) >= 2 ? context.Mod(Charisma) : 0;
        yield return AttackRoutine.AtWill("Eldritch Blast (à distância)", new AttackStep(context.CantripDice, hit, Dice.D10 + agonizing));
    }
}
