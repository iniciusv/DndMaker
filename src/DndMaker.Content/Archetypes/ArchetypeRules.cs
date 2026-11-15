using DndMaker.Domain.Builds;
using DndMaker.Domain.Checks;
using DndMaker.Domain.Damage;
using DndMaker.Domain.Effects;
using DndMaker.Domain.Equipment;
using static DndMaker.Domain.Abilities.Ability;

namespace DndMaker.Content.Archetypes;

/// <summary>Regras repetidas entre builds: escala de truques, armaduras da campanha e Countercharm.</summary>
internal static class ArchetypeRules
{
    /// <summary>CA base da Mage Armor (13 + DES).</summary>
    private const int MageArmorBase = 13;

    /// <summary>Dado da arma com Shillelagh: d8, d10 no nível 5, d12 no 11.</summary>
    public static Dice ShillelaghDie(BuildContext context) => context.CantripDice switch
    {
        >= 3 => Dice.D12,
        2 => Dice.D10,
        _ => Dice.D8,
    };

    /// <summary>Dano radiante extra do True Strike: nenhum até o nível 4, 1d6 no 5, 2d6 no 11.</summary>
    public static DamageFormula TrueStrikeExtra(BuildContext context) =>
        context.CantripDice > 1 ? new Dice(context.CantripDice - 1, 6) : DamageFormula.Empty;

    /// <summary>
    /// Armadura média da campanha: cota de escamas até o nível 4 e meia armadura a partir do 5,
    /// contra couro batido; fica com a melhor (em empate, o couro, que não atrapalha a Furtividade).
    /// </summary>
    public static ArmorSetup MediumOrLight(BuildContext context, bool shield) =>
        ArmorTable.Best(
            [ArmorTable.StuddedLeather, context.Level >= 5 ? ArmorTable.HalfPlate : ArmorTable.ScaleMail],
            context.Mod(Dexterity),
            shield);

    /// <summary>Couro batido, a armadura de quem ainda não tem proficiência em armadura média.</summary>
    public static ArmorSetup StuddedLeather(BuildContext context) =>
        ArmorTable.Wear(ArmorTable.StuddedLeather, context.Mod(Dexterity), shield: false);

    /// <summary>Mage Armor (13 + DES, sem armadura de verdade), com o nome que a build dá a ela.</summary>
    public static ArmorSetup MageArmor(BuildContext context, string description) =>
        ArmorTable.Wear(
            new Armor(description, MageArmorBase, ArmorCategory.None, MaxDexterity: null, StealthDisadvantage: false),
            context.Mod(Dexterity),
            shield: false);

    /// <summary>Countercharm do bardo: reação para rerrolar com vantagem um teste falho contra Enfeitiçado ou Amedrontado.</summary>
    public static IEnumerable<Effect> Countercharm()
    {
        var reroll = new RerollOnFailure(RollMode.Advantage, "Countercharm");
        yield return new ConditionRecovery(Condition.Charmed, reroll);
        yield return new ConditionRecovery(Condition.Frightened, reroll);
    }
}
