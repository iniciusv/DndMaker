using System.Collections.Immutable;
using DndMaker.Domain.Abilities;
using DndMaker.Domain.Builds;
using DndMaker.Domain.Catalog;
using DndMaker.Domain.Common;
using DndMaker.Domain.Progression;

namespace DndMaker.Domain.Analysis;

/// <summary>Bônus do antecedente: +2 em <see cref="Major"/> e +1 em <see cref="Minor"/>.</summary>
public sealed record BackgroundBonus(Ability Major, Ability Minor);

public sealed record ScoreChange(Ability Ability, int From, int To);

/// <summary>O que aconteceu num nível de talento.</summary>
public sealed record LevelPick(int Level, LevelChoice Choice, ImmutableArray<ScoreChange> Changes);

public sealed record AbilityAllocation(
    AbilityMap<AbilityScore> Scores,
    BackgroundBonus BackgroundBonus,
    ImmutableArray<LevelPick> Picks);

/// <summary>Como distribuir um aumento de atributo (+2).</summary>
public interface IAsiPolicy
{
    IReadOnlyList<ScoreChange> Improve(AbilityMap<int> scores, ImmutableArray<Ability> priority);
}

/// <summary>
/// Arredonda o atributo principal: se ele for ímpar, ganha +1 e o ponto que sobra vai para o próximo atributo ímpar
/// da prioridade; se for par, ganha +2.
/// </summary>
public sealed class RoundUpPriorityPolicy : IAsiPolicy
{
    public static RoundUpPriorityPolicy Instance { get; } = new();

    public IReadOnlyList<ScoreChange> Improve(AbilityMap<int> scores, ImmutableArray<Ability> priority)
    {
        bool BelowMax(Ability a) => scores[a] < AbilityScore.CharacterMax;
        bool IsOdd(Ability a) => scores[a] % 2 != 0;

        var main = priority.Where(BelowMax).Cast<Ability?>().FirstOrDefault();
        if (main is not { } primary)
        {
            return [];
        }

        if (!IsOdd(primary))
        {
            return [new ScoreChange(primary, scores[primary], scores[primary] + 2)];
        }

        var others = priority.Where(a => a != primary && BelowMax(a)).ToList();
        var secondary = others.Where(IsOdd).Cast<Ability?>().FirstOrDefault() ?? others.Cast<Ability?>().FirstOrDefault();
        var changes = new List<ScoreChange> { new(primary, scores[primary], scores[primary] + 1) };
        if (secondary is { } second)
        {
            changes.Add(new ScoreChange(second, scores[second], scores[second] + 1));
        }

        return changes;
    }
}

/// <summary>Atributos finais: conjunto base da build, antecedente e escolhas dos níveis de talento.</summary>
public sealed class AbilityScoreAllocator(IAsiPolicy policy)
{
    public static AbilityScoreAllocator Default { get; } = new(RoundUpPriorityPolicy.Instance);

    public AbilityAllocation Allocate(CharacterBuild build, Level level)
    {
        var archetype = build.Archetype;
        var scores = archetype.Profile.BaseScores;

        var allowed = archetype.Priority.Where(a => build.Background.Abilities.Contains(a)).ToList();
        if (allowed.Count < 2)
        {
            throw new DomainException($"O antecedente {build.Background.Id} não tem dois atributos para distribuir.");
        }

        var bonus = new BackgroundBonus(allowed[0], allowed[1]);
        scores = scores.With(bonus.Major, scores[bonus.Major] + 2).With(bonus.Minor, scores[bonus.Minor] + 1);

        var picks = ImmutableArray.CreateBuilder<LevelPick>();
        foreach (var featLevel in archetype.FeatLevels(level.Value))
        {
            var choice = build.ChoiceAt(featLevel);
            var changes = choice switch
            {
                AbilityScoreImprovementChoice => policy.Improve(scores, archetype.Priority),
                FeatChoice feat => FeatIncrease(feat.Feat.Increase, scores, archetype),
                _ => throw new DomainException($"Escolha desconhecida: {choice.GetType().Name}."),
            };

            foreach (var change in changes)
            {
                scores = scores.With(change.Ability, change.To);
            }

            picks.Add(new LevelPick(featLevel, choice, [.. changes]));
        }

        return new AbilityAllocation(scores.Select((_, value) => new AbilityScore(value)), bonus, picks.ToImmutable());
    }

    private static IReadOnlyList<ScoreChange> FeatIncrease(AbilityIncreaseRule rule, AbilityMap<int> scores, BuildArchetype archetype)
    {
        Ability? target = rule switch
        {
            NoIncrease => null,
            FixedIncrease f => f.Ability,
            OddMentalIncrease => archetype.Priority
                .Where(a => a.IsMental() && scores[a] % 2 != 0 && scores[a] < AbilityScore.CharacterMax)
                .Cast<Ability?>()
                .FirstOrDefault() ?? archetype.Profile.SpellcastingAbility,
            _ => throw new DomainException($"Regra de aumento desconhecida: {rule.GetType().Name}."),
        };

        return target is { } ability && scores[ability] < AbilityScore.CharacterMax
            ? [new ScoreChange(ability, scores[ability], scores[ability] + 1)]
            : [];
    }
}
