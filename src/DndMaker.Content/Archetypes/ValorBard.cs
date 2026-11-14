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

/// <summary>Bardo do Valor: conjurador completo com armadura média e, no 6, ataque mais truque.</summary>
public sealed class ValorBard() : BuildArchetype(Definition)
{
    public static readonly VariantId Melee = new("melee");
    public static readonly VariantId Staff = new("staff");
    public static readonly VariantId Bow = new("arco");

    private static readonly ArchetypeProfile Definition = new()
    {
        Id = new("valor"),
        Name = "Bardo do Valor",
        Epithet = "O Trovador de Guerra",
        Summary = "Conjurador completo com armadura média, perícias e, no nível 6, ataque mais truque no mesmo turno.",
        Status = ArchetypeStatus.Finalist,
        BaseScores = AbilityMap.Of(8, 14, 13, 10, 12, 15),
        Priority = [Charisma, Dexterity, Constitution, Wisdom, Intelligence, Strength],
        SpellcastingAbility = Charisma,
        SuggestedBackground = new("artista"),
        Progression = ClassProgression.Of(ClassCatalog.Bard),
        Variants =
        [
            new(Melee, "Rapieira + escudo"),
            new(Staff, "Bordão com Shillelagh + escudo") { RequiredFeat = FeatCatalog.MagicInitiateDruid.Id },
            new(Bow, "Arco longo"),
        ],
        UsesConcentration = true,
        Milestones =
        [
            new(1, "Inspiração Bárdica (d6), magias de bardo, True Strike"),
            new(2, "Expertise em 2 perícias; Jack of All Trades (vale para iniciativa)"),
            new(3, "Valor: armadura média, escudo, armas marciais; Combat Inspiration; magias de 2º círculo (Mirror Image)"),
            new(4, "Talento ou aumento de atributo"),
            new(5, "Magias de 3º círculo (Hypnotic Pattern, Slow); Inspiração volta em descanso curto e vira d8; True Strike +1d6 radiante"),
            new(6, "Extra Attack, podendo trocar um ataque por um truque"),
            new(7, "Countercharm (reação: rerrola com vantagem um teste falho contra Enfeitiçado ou Amedrontado, seu ou de um aliado a 9 m); magias de 4º círculo"),
            new(8, "Talento ou aumento de atributo"),
            new(9, "Expertise em mais 2 perícias; magias de 5º círculo"),
            new(10, "Magical Secrets; Inspiração vira d10"),
        ],
        Pros = "Mais magias e perícias de todas as opções. CA alta com escudo. Dano radiante com True Strike.",
        Cons = "Sabedoria e Constituição fracas nos testes. A party já tem um bardo. Dano modesto até o nível 6.",
    };

    public override IEnumerable<Effect> FeaturesAt(BuildContext c)
    {
        if (c.Level >= 2)
        {
            yield return new InitiativeBonus(InitiativeAmount.HalfProficiencyBonus); // Jack of All Trades
        }

        if (c.Level >= 7)
        {
            foreach (var effect in ArchetypeRules.Countercharm())
            {
                yield return effect;
            }
        }
    }

    public override ArmorSetup ArmorAt(BuildContext c) =>
        c.Level < 3
            ? ArmorTable.Wear(ArmorTable.StuddedLeather, c.Mod(Dexterity), shield: false)
            : ArchetypeRules.MediumOrLight(c, shield: !c.IsVariant(Bow));

    public override IEnumerable<AttackRoutine> RoutinesAt(BuildContext c)
    {
        var shillelagh = c.Has(FeatCatalog.MagicInitiateDruid.Id) && c.IsVariant(Staff);
        var melee = !c.IsVariant(Bow);

        var (weaponName, weaponDie) = (shillelagh, c.Level >= 3) switch
        {
            (true, _) => ("bordão com Shillelagh", ArchetypeRules.ShillelaghDie(c)),
            (false, false) => melee ? ("maça", Dice.D6) : ("besta leve", Dice.D8),
            (false, true) => melee ? ("rapieira", Dice.D8) : ("arco longo", Dice.D8),
        };

        var trueStrike = new AttackStep(1, c.AttackBonus(Charisma), weaponDie + c.Mod(Charisma) + ArchetypeRules.TrueStrikeExtra(c));
        yield return AttackRoutine.AtWill($"True Strike ({weaponName})", trueStrike).WithNote("Todo o dano pode ser radiante.");

        if (c.Level >= 6)
        {
            var attack = shillelagh
                ? new AttackStep(1, c.AttackBonus(Charisma), weaponDie + c.Mod(Charisma))
                : new AttackStep(1, c.AttackBonus(Dexterity), weaponDie + c.Mod(Dexterity));
            yield return AttackRoutine.AtWill($"Ataque + True Strike ({weaponName})", attack, trueStrike);
        }
    }
}
