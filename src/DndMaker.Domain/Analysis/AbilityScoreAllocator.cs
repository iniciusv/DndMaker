using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using DndMaker.Domain.Abilities;
using DndMaker.Domain.Builds;
using DndMaker.Domain.Catalog;
using DndMaker.Domain.Common;
using DndMaker.Domain.Progression;

namespace DndMaker.Domain.Analysis;

/// <summary>Bônus do antecedente: +2 em <see cref="Major"/> e +1 em <see cref="Minor"/>.</summary>
public sealed record BackgroundBonus(Ability Major, Ability Minor)
{
    /// <summary>Texto da ficha: <c>CAR +2, CON +1</c>.</summary>
    public override string ToString() => $"{Major.Abbreviation()} +2, {Minor.Abbreviation()} +1";
}

public sealed record ScoreChange(Ability Ability, int From, int To)
{
    /// <summary>Texto da ficha: <c>CAR 17 → 18</c>.</summary>
    public override string ToString() => $"{Ability.Abbreviation()} {From} → {To}";
}

/// <summary>O que aconteceu num nível de talento.</summary>
public sealed record LevelPick(int Level, LevelChoice Choice, ImmutableArray<ScoreChange> Changes)
{
    /// <summary>As mudanças de atributo separadas por vírgula; vazio se o talento não aumenta atributo.</summary>
    public string ChangesText => string.Join(", ", Changes);
}

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

/// <summary>Como dividir o +2/+1 do antecedente entre os atributos que ele permite.</summary>
public interface IBackgroundBonusPolicy
{
    /// <param name="candidates">As divisões possíveis; a primeira é a do logic.js (+2 no primeiro da prioridade, +1 no segundo).</param>
    /// <param name="modifiersAt">Os seis modificadores, na ordem de prioridade da build, com uma divisão e num nível.</param>
    BackgroundBonus Choose(IReadOnlyList<BackgroundBonus> candidates, Func<BackgroundBonus, Level, int[]> modifiersAt);
}

/// <summary>Como o logic.js: +2 no primeiro da prioridade e +1 no segundo, sem olhar se o valor fica par.</summary>
public sealed class PriorityBackgroundPolicy : IBackgroundBonusPolicy
{
    public static PriorityBackgroundPolicy Instance { get; } = new();

    public BackgroundBonus Choose(IReadOnlyList<BackgroundBonus> candidates, Func<BackgroundBonus, Level, int[]> modifiersAt) => candidates[0];
}

/// <summary>
/// Problema 8 do plano: a divisão que deixa a build com os maiores modificadores no último nível planejado,
/// comparados na ordem de prioridade; no empate, a que dá mais no nível 1; se ainda empatar, a do logic.js.
/// Com CAR 15 e DES 14, CAR +1 e DES +2 dão 16 e 16 em vez de 17 e 15. A divisão vale para a build inteira,
/// por isso olha o plano todo: com Fey Touched no 5, CAR 17 → 18 ganha de CAR 16 → 17.
/// </summary>
public sealed class BestModifiersBackgroundPolicy(Level planningLevel) : IBackgroundBonusPolicy
{
    public BackgroundBonus Choose(IReadOnlyList<BackgroundBonus> candidates, Func<BackgroundBonus, Level, int[]> modifiersAt)
    {
        int[] Key(BackgroundBonus bonus) => [.. modifiersAt(bonus, planningLevel), .. modifiersAt(bonus, Level.Of(Level.Min))];

        var best = candidates[0];
        var bestKey = Key(best);
        foreach (var candidate in candidates.Skip(1))
        {
            var key = Key(candidate);
            if (IsBetter(key, bestKey))
            {
                best = candidate;
                bestKey = key;
            }
        }

        return best;
    }

    private static bool IsBetter(int[] candidate, int[] current)
    {
        for (var i = 0; i < candidate.Length; i++)
        {
            if (candidate[i] != current[i])
            {
                return candidate[i] > current[i];
            }
        }

        return false;
    }
}

/// <summary>Atributos finais: conjunto base da build, antecedente e escolhas dos níveis de talento.</summary>
public sealed class AbilityScoreAllocator(IAsiPolicy policy, IBackgroundBonusPolicy backgroundPolicy)
{
    /// <summary>A divisão do antecedente só depende da build, não do nível: calcula uma vez por build.</summary>
    private readonly ConditionalWeakTable<CharacterBuild, BackgroundBonus> _bonusByBuild = [];

    /// <summary>Exatamente como o logic.js, para a paridade com o golden.</summary>
    public static AbilityScoreAllocator LogicJs { get; } = new(RoundUpPriorityPolicy.Instance, PriorityBackgroundPolicy.Instance);

    /// <summary>
    /// O que o programa usa: aumentos arredondados e a divisão do antecedente que planeja a build até
    /// <paramref name="planningLevel"/> (o último nível da campanha).
    /// </summary>
    public static AbilityScoreAllocator PlanningUpTo(Level planningLevel) =>
        new(RoundUpPriorityPolicy.Instance, new BestModifiersBackgroundPolicy(planningLevel));

    public AbilityAllocation Allocate(CharacterBuild build, Level level) =>
        AllocateWith(build, _bonusByBuild.GetValue(build, ChooseBackgroundBonus), level);

    private BackgroundBonus ChooseBackgroundBonus(CharacterBuild build)
    {
        var archetype = build.Archetype;
        var allowed = archetype.Priority.Where(a => build.Background.Abilities.Contains(a)).ToList();
        if (allowed.Count < 2)
        {
            throw new DomainException($"O antecedente {build.Background.Id} não tem dois atributos para distribuir.");
        }

        List<BackgroundBonus> candidates =
        [
            .. from major in allowed
               from minor in allowed
               where major != minor
               select new BackgroundBonus(major, minor),
        ];

        return backgroundPolicy.Choose(
            candidates,
            (candidate, at) => [.. archetype.Priority.Select(a => AllocateWith(build, candidate, at).Scores[a].Modifier)]);
    }

    private AbilityAllocation AllocateWith(CharacterBuild build, BackgroundBonus bonus, Level level)
    {
        var archetype = build.Archetype;
        var scores = archetype.Profile.BaseScores;
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
