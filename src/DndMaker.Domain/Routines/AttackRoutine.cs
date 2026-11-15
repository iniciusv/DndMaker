using System.Collections.Immutable;
using DndMaker.Domain.Combat;
using DndMaker.Domain.Damage;

namespace DndMaker.Domain.Routines;

/// <summary>Quanto a rotina gasta.</summary>
public enum ResourceTier
{
    /// <summary>À vontade: truques e ataques.</summary>
    AtWill = 0,

    /// <summary>Exige manter uma magia de concentração.</summary>
    Concentration = 1,

    /// <summary>Pico: gasta espaço de magia ou outro recurso limitado.</summary>
    Peak = 2,
}

/// <summary>Situação específica em que a rotina vale. Rotinas situacionais não entram no "melhor turno" geral.</summary>
public enum Situation
{
    /// <summary>Só contra mortos-vivos (Divine Smite radiante a mais).</summary>
    VsUndead,

    /// <summary>Dano extra opcional da espécie; aparece na lista, mas não no melhor turno.</summary>
    SpeciesOption,
}

public abstract record RoutineStep;

/// <summary><see cref="Count"/> ataques com o mesmo bônus e o mesmo dano.</summary>
public sealed record AttackStep : RoutineStep
{
    public AttackStep(int count, AttackBonus bonus, DamageFormula damage)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 1);
        Count = count;
        Bonus = bonus;
        Damage = damage;
    }

    public int Count { get; }

    public AttackBonus Bonus { get; }

    public DamageFormula Damage { get; }
}

/// <summary>Dano extra uma vez por turno, se algum ataque de <see cref="Trigger"/> acertar (Divine Smite, Hunter's Mark não).</summary>
public sealed record RiderStep(AttackStep Trigger, DamageFormula Damage) : RoutineStep;

/// <summary>Efeito com teste de resistência do alvo; metade do dano no sucesso se <see cref="HalfOnSuccess"/>.</summary>
public sealed record SaveStep : RoutineStep
{
    public SaveStep(DifficultyClass dc, DamageFormula damage, bool halfOnSuccess = false, int targets = 1)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(targets, 1);
        Dc = dc;
        Damage = damage;
        HalfOnSuccess = halfOnSuccess;
        Targets = targets;
    }

    public DifficultyClass Dc { get; }

    public DamageFormula Damage { get; }

    public bool HalfOnSuccess { get; }

    public int Targets { get; }
}

/// <summary>Um turno de dano, do jeito que a build faria.</summary>
public sealed record AttackRoutine(string Name, ResourceTier Tier, ImmutableArray<RoutineStep> Steps)
{
    public string Note { get; init; } = string.Empty;

    public Situation? Situation { get; init; }

    /// <summary>Atinge mais de um alvo; não entra na comparação de um alvo só.</summary>
    public bool MultipleTargets => Steps.OfType<SaveStep>().Any(s => s.Targets > 1);

    public static AttackRoutine AtWill(string name, params RoutineStep[] steps) => new(name, ResourceTier.AtWill, [.. steps]);

    public static AttackRoutine Concentration(string name, params RoutineStep[] steps) => new(name, ResourceTier.Concentration, [.. steps]);

    public static AttackRoutine Peak(string name, params RoutineStep[] steps) => new(name, ResourceTier.Peak, [.. steps]);

    public AttackRoutine WithNote(string note) => this with { Note = note };

    public AttackRoutine When(Situation situation) => this with { Situation = situation };
}
