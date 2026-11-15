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

/// <summary>Guerreiro 1 → Bardo do Valor: armadura pesada e proficiência em Constituição, um nível atrasado.</summary>
public sealed class FighterValorBard() : BuildArchetype(Definition)
{
    public static readonly VariantId Defense = new("defense");
    public static readonly VariantId Dueling = new("dueling");

    private const int DuelingBonus = 2;

    private static readonly ArchetypeProfile Definition = new()
    {
        Id = new("fval"),
        Name = "Guerreiro 1 → Bardo do Valor",
        Epithet = "O Bardo de Ferro",
        Summary = "Bardo do Valor com armadura pesada e proficiência em Constituição, um nível atrasado.",
        Status = ArchetypeStatus.UnderReview,
        BaseScores = AbilityMap.Of(13, 12, 14, 8, 10, 15),
        Priority = [Charisma, Constitution, Strength, Dexterity, Wisdom, Intelligence],
        SpellcastingAbility = Charisma,
        SuggestedBackground = new("haunted"),
        Progression = ClassProgression.Dip(ClassCatalog.Fighter, 1, ClassCatalog.Bard),
        Variants = [new(Defense, "Estilo Defense (+1 CA)"), new(Dueling, "Estilo Dueling (+2 dano)")],
        UsesConcentration = true,
        Milestones =
        [
            new(1, "Guerreiro: armadura pesada, escudo, estilo de luta, Weapon Mastery, Second Wind, testes de FOR e CON"),
            new(2, "Bardo 1: Inspiração Bárdica, magias de bardo, True Strike"),
            new(3, "Expertise em 2 perícias; Jack of All Trades"),
            new(4, "Valor: Combat Inspiration; magias de 2º círculo"),
            new(5, "Talento ou aumento de atributo; True Strike +1d6 radiante"),
            new(6, "Magias de 3º círculo (Hypnotic Pattern, Slow)"),
            new(7, "Extra Attack, podendo trocar um ataque por um truque"),
            new(8, "Countercharm (reação: rerrola com vantagem um teste falho contra Enfeitiçado ou Amedrontado); magias de 4º círculo"),
            new(9, "Talento ou aumento de atributo"),
            new(10, "Expertise em mais 2 perícias; magias de 5º círculo"),
        ],
        Pros = "CA 19 desde o nível 1 e concentração firme, com todas as magias do bardo.",
        Cons = "Extra Attack só no nível 7. Perde os testes de Destreza e Carisma. Sabedoria fraca.",
    };

    private static int BardLevel(BuildContext context) => context.LevelIn(ClassCatalog.Bard);

    public override IEnumerable<Effect> FeaturesAt(BuildContext context)
    {
        if (BardLevel(context) >= 2)
        {
            yield return new InitiativeBonus(InitiativeAmount.HalfProficiencyBonus); // Jack of All Trades
        }

        if (BardLevel(context) >= 7)
        {
            foreach (var effect in ArchetypeRules.Countercharm())
            {
                yield return effect;
            }
        }
    }

    public override ArmorSetup ArmorAt(BuildContext context)
    {
        var defense = context.IsVariant(Defense);
        return ArmorTable.Wear(ArmorTable.ChainMail, context.Mod(Dexterity), shield: true, bonus: defense ? 1 : 0, suffix: defense ? " + Defense" : null);
    }

    public override IEnumerable<AttackRoutine> RoutinesAt(BuildContext context)
    {
        var dueling = context.IsVariant(Dueling) ? DuelingBonus : 0;

        // Paridade com o logic.js: Shillelagh entra só por ter Magic Initiate (Druida), sem variante própria,
        // e soma Dueling ao bordão. A fase 6 revê isso (problema 4 do plano).
        var shillelagh = context.Has(FeatCatalog.MagicInitiateDruid.Id);
        var charismaHit = context.AttackBonus(Charisma);
        var longsword = new AttackStep(1, context.AttackBonus(Strength), Dice.D8 + (context.Mod(Strength) + dueling));

        if (BardLevel(context) == 0)
        {
            yield return shillelagh
                ? AttackRoutine.AtWill("Bordão com Shillelagh", new AttackStep(1, charismaHit, ArchetypeRules.ShillelaghDie(context) + (context.Mod(Charisma) + dueling)))
                : AttackRoutine.AtWill("Espada longa (ainda com Força)", longsword);
            yield break;
        }

        var (weaponName, weaponDie) = shillelagh ? ("bordão com Shillelagh", ArchetypeRules.ShillelaghDie(context)) : ("espada longa", Dice.D8);
        var charismaWeapon = weaponDie + (context.Mod(Charisma) + dueling);
        var trueStrike = new AttackStep(1, charismaHit, charismaWeapon + ArchetypeRules.TrueStrikeExtra(context));

        yield return AttackRoutine.AtWill($"True Strike ({weaponName})", trueStrike);
        if (BardLevel(context) >= 6)
        {
            var attack = shillelagh ? new AttackStep(1, charismaHit, charismaWeapon) : longsword;
            yield return AttackRoutine.AtWill($"Ataque + True Strike ({weaponName})", attack, trueStrike);
        }
    }
}
