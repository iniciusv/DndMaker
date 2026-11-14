using System.Collections.Immutable;
using System.Text;
using DndMaker.Domain.Checks;
using DndMaker.Domain.Combat;
using DndMaker.Domain.Common;

namespace DndMaker.Domain.Routines;

/// <summary>O que se assume sobre o inimigo ao estimar dano.</summary>
public sealed record CombatAssumptions(ArmorClass TargetArmorClass, int MonsterSaveBonus = CombatAssumptions.DefaultMonsterSaveBonus)
{
    public const int DefaultMonsterSaveBonus = 2;

    public static CombatAssumptions Default { get; } = new(new ArmorClass(15));
}

/// <summary>Estimativa de uma etapa: chance de dar certo, dano se tudo der certo e dano esperado.</summary>
public sealed record StepEstimate(RoutineStep Step, Probability Chance, double Raw, double Expected);

public sealed record RoutineEstimate(AttackRoutine Routine, ImmutableArray<StepEstimate> Steps)
{
    public string Name => Routine.Name;

    public ResourceTier Tier => Routine.Tier;

    /// <summary>Dano se todos os ataques acertarem e todos os alvos falharem.</summary>
    public double Raw => Steps.Sum(s => s.Raw);

    /// <summary>Dano médio considerando a chance de acertar, sem críticos.</summary>
    public double Expected => Steps.Sum(s => s.Expected);

    public string Formula => RoutineFormatter.Format(Routine);

    /// <summary>Bônus do primeiro ataque (o que a página original mostrava).</summary>
    public AttackBonus? PrimaryAttackBonus => Routine.Steps.OfType<AttackStep>().Select(a => (AttackBonus?)a.Bonus).FirstOrDefault();

    /// <summary>CD do primeiro efeito com teste.</summary>
    public DifficultyClass? PrimaryDc => Routine.Steps.OfType<SaveStep>().Select(s => (DifficultyClass?)s.Dc).FirstOrDefault();
}

/// <summary>Dano esperado = média dos dados × chance de dar certo, sem críticos.</summary>
public static class DamageEstimator
{
    public static RoutineEstimate Estimate(AttackRoutine routine, CombatAssumptions assumptions) =>
        new(routine, [.. routine.Steps.Select(step => EstimateStep(step, assumptions))]);

    private static StepEstimate EstimateStep(RoutineStep step, CombatAssumptions assumptions)
    {
        switch (step)
        {
            case AttackStep attack:
            {
                var hit = HitChance(attack, assumptions);
                var average = attack.Damage.Average;
                return new StepEstimate(step, hit, attack.Count * average, attack.Count * hit.Value * average);
            }

            case RiderStep rider:
            {
                var anyHit = HitChance(rider.Trigger, assumptions).AtLeastOnceIn(rider.Trigger.Count);
                var average = rider.Damage.Average;
                return new StepEstimate(step, anyHit, average, anyHit.Value * average);
            }

            case SaveStep save:
            {
                var fail = D20Check.Save(assumptions.MonsterSaveBonus, save.Dc).Success().Complement;
                var average = save.Damage.Average;
                var fraction = fail.Value + (save.HalfOnSuccess ? (1 - fail.Value) / 2 : 0);
                return new StepEstimate(step, fail, average * save.Targets, average * fraction * save.Targets);
            }

            default:
                throw new DomainException($"Etapa sem estimativa: {step.GetType().Name}.");
        }
    }

    private static Probability HitChance(AttackStep attack, CombatAssumptions assumptions) =>
        D20Check.Attack(attack.Bonus, assumptions.TargetArmorClass).Success();
}

/// <summary>Escolhe a rotina de maior dano esperado.</summary>
public static class RoutineSelector
{
    /// <summary>
    /// Melhor rotina até <paramref name="maxTier"/>, contando só um alvo e ignorando rotinas situacionais.
    /// Em empate fica a que veio primeiro. Nulo se nenhuma servir.
    /// </summary>
    public static RoutineEstimate? Best(IEnumerable<RoutineEstimate> estimates, ResourceTier maxTier)
    {
        RoutineEstimate? best = null;
        foreach (var estimate in estimates)
        {
            var eligible = estimate.Tier <= maxTier && !estimate.Routine.MultipleTargets && estimate.Routine.Situation is null;
            if (eligible && (best is null || estimate.Expected > best.Expected))
            {
                best = estimate;
            }
        }

        return best;
    }
}

/// <summary>Texto da rotina como na página original: <c>2 × (1d8+5) +2d8</c>.</summary>
public static class RoutineFormatter
{
    private const string Times = " × ";

    public static string Format(AttackRoutine routine)
    {
        var text = new StringBuilder();
        for (var i = 0; i < routine.Steps.Length; i++)
        {
            var step = routine.Steps[i];
            if (i > 0)
            {
                text.Append(step is RiderStep ? " " : " + ");
            }

            text.Append(Format(step));
        }

        return text.ToString();
    }

    public static string Format(RoutineStep step) => step switch
    {
        AttackStep a => Grouped(a.Count, a.Damage.ToString()),
        RiderStep r => "+" + r.Damage,
        SaveStep s => Grouped(s.Targets, s.Damage.ToString()),
        _ => throw new DomainException($"Etapa sem formato: {step.GetType().Name}."),
    };

    private static string Grouped(int count, string damage) =>
        (count > 1 ? count + Times : string.Empty) + "(" + damage + ")";
}
