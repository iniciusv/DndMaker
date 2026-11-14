using System.Collections.Immutable;
using DndMaker.Domain.Builds;
using DndMaker.Domain.Common;
using DndMaker.Domain.Progression;
using DndMaker.Domain.Routines;

namespace DndMaker.Domain.Comparison;

/// <summary>
/// A comparação aberta na tela: três personagens, o nível, o inimigo e até que faixa de recurso contar.
/// É o que se salva entre uma sessão e outra. Imutável.
/// </summary>
public sealed class BuildComparison
{
    public const int SlotCount = 3;

    private BuildComparison(Level level, CombatAssumptions assumptions, ResourceTier maxTier, ImmutableArray<CharacterBuild> slots)
    {
        if (slots.Length != SlotCount)
        {
            throw new DomainException($"A comparação tem exatamente {SlotCount} personagens.");
        }

        Level = level;
        Assumptions = assumptions;
        MaxTier = maxTier;
        Slots = slots;
    }

    public Level Level { get; }

    public CombatAssumptions Assumptions { get; }

    public ResourceTier MaxTier { get; }

    public ImmutableArray<CharacterBuild> Slots { get; }

    public static BuildComparison Create(
        IEnumerable<CharacterBuild> slots, Level level, CombatAssumptions assumptions, ResourceTier maxTier) =>
        new(level, assumptions, maxTier, [.. slots]);

    public BuildComparison WithLevel(Level level) => new(level, Assumptions, MaxTier, Slots);

    public BuildComparison WithAssumptions(CombatAssumptions assumptions) => new(Level, assumptions, MaxTier, Slots);

    public BuildComparison WithMaxTier(ResourceTier maxTier) => new(Level, Assumptions, maxTier, Slots);

    public BuildComparison WithSlot(int index, CharacterBuild build)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, SlotCount);
        return new BuildComparison(Level, Assumptions, MaxTier, Slots.SetItem(index, build));
    }

    public BuildComparison UpdateSlot(int index, Func<CharacterBuild, CharacterBuild> change) =>
        WithSlot(index, change(Slots[index]));
}
