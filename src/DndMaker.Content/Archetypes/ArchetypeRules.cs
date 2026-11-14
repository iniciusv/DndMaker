using DndMaker.Domain.Builds;
using DndMaker.Domain.Checks;
using DndMaker.Domain.Damage;
using DndMaker.Domain.Effects;
using DndMaker.Domain.Equipment;

namespace DndMaker.Content.Archetypes;

/// <summary>Regras repetidas entre builds: escala de truques, armadura média da campanha e Countercharm.</summary>
internal static class ArchetypeRules
{
    /// <summary>Dado da arma com Shillelagh: d8, d10 no nível 5, d12 no 11.</summary>
    public static Dice ShillelaghDie(BuildContext c) => c.CantripDice switch
    {
        >= 3 => Dice.D12,
        2 => Dice.D10,
        _ => Dice.D8,
    };

    /// <summary>Dano radiante extra do True Strike: nenhum até o nível 4, 1d6 no 5, 2d6 no 11.</summary>
    public static DamageFormula TrueStrikeExtra(BuildContext c) =>
        c.CantripDice > 1 ? new Dice(c.CantripDice - 1, 6) : DamageFormula.Empty;

    /// <summary>
    /// Armadura média da campanha: cota de escamas até o nível 4 e meia armadura a partir do 5,
    /// contra couro batido; fica com a melhor (em empate, o couro, que não atrapalha a Furtividade).
    /// </summary>
    public static ArmorSetup MediumOrLight(BuildContext c, bool shield) =>
        ArmorTable.Best(
            [ArmorTable.StuddedLeather, c.Level >= 5 ? ArmorTable.HalfPlate : ArmorTable.ScaleMail],
            c.Mod(Domain.Abilities.Ability.Dexterity),
            shield);

    /// <summary>Countercharm do bardo: reação para rerrolar com vantagem um teste falho contra Enfeitiçado ou Amedrontado.</summary>
    public static IEnumerable<Effect> Countercharm()
    {
        var reroll = new RerollOnFailure(RollMode.Advantage, "Countercharm");
        yield return new ConditionRecovery(Condition.Charmed, reroll);
        yield return new ConditionRecovery(Condition.Frightened, reroll);
    }
}
