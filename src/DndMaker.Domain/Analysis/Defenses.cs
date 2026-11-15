using System.Collections.Immutable;
using DndMaker.Domain.Abilities;
using DndMaker.Domain.Checks;
using DndMaker.Domain.Classes;
using DndMaker.Domain.Combat;
using DndMaker.Domain.Common;
using DndMaker.Domain.Effects;
using DndMaker.Domain.Progression;

namespace DndMaker.Domain.Analysis;

public sealed record SavingThrow(int Modifier, bool Proficient, bool Advantage)
{
    public RollMode Mode => Advantage ? RollMode.Advantage : RollMode.Normal;
}

/// <summary>Um efeito da campanha que exige teste de resistência, como o charme de Strahd (SAB, CD 17).</summary>
public sealed record ConditionThreat(string Name, Ability Ability, DifficultyClass Dc, Condition Condition);

/// <summary>
/// Chance de resistir a uma ameaça: <see cref="Passive"/> sem gastar nada e <see cref="WithResources"/>
/// gastando o que a ficha tem (vantagem de Lucky ou Watcher's Will, bônus do Survivor, rerrolagem do Countercharm).
/// <see cref="Resources"/> lista o que entrou na conta; <see cref="Remedies"/>, o que encerra a condição depois
/// sem mudar a chance (Twilight Sanctuary).
/// </summary>
public sealed record ThreatResistance(
    ConditionThreat Threat,
    Probability Passive,
    Probability WithResources,
    bool Advantage,
    ImmutableArray<string> Resources)
{
    public ImmutableArray<string> Remedies { get; init; } = [];
}

public sealed record ConcentrationResult(Probability Success, bool Advantage);

public static class HitPointCalculator
{
    /// <summary>Dado máximo no 1º nível, média arredondada para cima nos demais, Constituição e bônus por nível.</summary>
    public static int Calculate(ClassProgression progression, Level level, int constitutionModifier, int bonusPerLevel)
    {
        var dice = progression.HitDice(level).ToList();
        var total = dice[0] + constitutionModifier;
        total += dice.Skip(1).Sum(die => (die / 2) + 1 + constitutionModifier);
        return total + (bonusPerLevel * level.Value);
    }
}

public static class DefenseCalculator
{
    /// <summary>CD mínima de um teste de concentração ao sofrer dano.</summary>
    public const int ConcentrationDc = 10;

    public static AbilityMap<SavingThrow> SavingThrows(
        AbilityMap<int> modifiers, int proficiencyBonus, AbilitySet proficiencies, AbilitySet advantages, int bonus) =>
        AbilityMap.Create(ability => new SavingThrow(
            modifiers[ability] + (proficiencies.Contains(ability) ? proficiencyBonus : 0) + bonus,
            proficiencies.Contains(ability),
            advantages.Contains(ability)));

    /// <summary>
    /// Sem gastar nada vale só a vantagem permanente (espécie). Gastando recursos, uma fonte de vantagem
    /// (Lucky, Watcher's Will) entra se ainda não houver vantagem; depois, bônus e rerrolagens sobre a falha.
    /// </summary>
    public static ThreatResistance Resist(ConditionThreat threat, AbilityMap<SavingThrow> saves, CharacterTraits traits, int proficiencyBonus)
    {
        var save = saves[threat.Ability];
        var advantage = save.Advantage || traits.ConditionAdvantages.Contains(threat.Condition);
        var check = D20Check.Save(save.Modifier, threat.Dc, advantage ? RollMode.Advantage : RollMode.Normal);

        var advantageSources = advantage ? [] : traits.AdvantageResourcesFor(threat.Ability);
        var spending = advantageSources.Count > 0 ? check.WithAdvantage() : check;
        var recoveries = traits.RecoveriesAgainst(threat.Condition, proficiencyBonus);

        return new ThreatResistance(
            threat,
            check.Success(),
            spending.SuccessWith(recoveries),
            advantage,
            [.. advantageSources, .. recoveries.Select(r => r.Source)])
        {
            Remedies = [.. traits.RemediesAgainst(threat.Condition)],
        };
    }

    public static ConcentrationResult Concentration(SavingThrow constitution, CharacterTraits traits)
    {
        var mode = traits.HasConcentrationAdvantage ? RollMode.Advantage : constitution.Mode;
        var check = D20Check.Save(constitution.Modifier, new DifficultyClass(ConcentrationDc), mode);
        return new ConcentrationResult(check.Success(), traits.HasConcentrationAdvantage);
    }
}
