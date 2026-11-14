using System.Collections.Immutable;
using DndMaker.Domain.Abilities;
using DndMaker.Domain.Catalog;
using DndMaker.Domain.Classes;
using DndMaker.Domain.Common;
using DndMaker.Domain.Effects;
using DndMaker.Domain.Equipment;
using DndMaker.Domain.Routines;

namespace DndMaker.Domain.Builds;

public enum ArchetypeStatus
{
    Finalist,
    UnderReview,
    LittleExplored,
    Discarded,
}

/// <summary>Variante de uma build. Se <see cref="RequiredFeat"/> existir, a variante só funciona com esse talento.</summary>
public sealed record BuildVariant(VariantId Id, string Name)
{
    public FeatId? RequiredFeat { get; init; }
}

/// <summary>O que a build ganha num nível, para a linha do tempo da ficha.</summary>
public sealed record LevelMilestone(int Level, string Text);

/// <summary>Dados fixos de uma build. As regras que dependem do nível ficam na subclasse de <see cref="BuildArchetype"/>.</summary>
public sealed record ArchetypeProfile
{
    public required ArchetypeId Id { get; init; }

    public required string Name { get; init; }

    public required string Epithet { get; init; }

    public required string Summary { get; init; }

    public required ArchetypeStatus Status { get; init; }

    /// <summary>Conjunto padrão (15, 14, 13, 12, 10, 8) já distribuído.</summary>
    public required AbilityMap<int> BaseScores { get; init; }

    /// <summary>Os seis atributos, do mais para o menos importante.</summary>
    public required ImmutableArray<Ability> Priority { get; init; }

    public required Ability SpellcastingAbility { get; init; }

    public required BackgroundId SuggestedBackground { get; init; }

    public required ClassProgression Progression { get; init; }

    public ImmutableArray<BuildVariant> Variants { get; init; } = [];

    public bool UsesConcentration { get; init; }

    public ImmutableArray<LevelMilestone> Milestones { get; init; } = [];

    public string Pros { get; init; } = string.Empty;

    public string Cons { get; init; } = string.Empty;
}

/// <summary>
/// Uma build: dados fixos (<see cref="ArchetypeProfile"/>) e as regras que mudam com o nível.
/// Cada build do conteúdo é uma subclasse.
/// </summary>
public abstract class BuildArchetype
{
    protected BuildArchetype(ArchetypeProfile profile)
    {
        if (profile.Priority.Length != AbilityList.Count || profile.Priority.Distinct().Count() != AbilityList.Count)
        {
            throw new DomainException($"A prioridade de {profile.Id} precisa listar os seis atributos uma vez cada.");
        }

        if (profile.Variants.Select(v => v.Id).Distinct().Count() != profile.Variants.Length)
        {
            throw new DomainException($"A build {profile.Id} tem variantes com o mesmo id.");
        }

        Profile = profile;
    }

    public ArchetypeProfile Profile { get; }

    public ArchetypeId Id => Profile.Id;

    public string Name => Profile.Name;

    public ClassProgression Progression => Profile.Progression;

    public ImmutableArray<Ability> Priority => Profile.Priority;

    public ImmutableArray<BuildVariant> Variants => Profile.Variants;

    /// <summary>Testes de resistência da primeira classe.</summary>
    public AbilitySet SavingThrows => Progression.FirstClass.SavingThrows;

    public BuildVariant? DefaultVariant => Variants.IsEmpty ? null : Variants[0];

    public BuildVariant? FindVariant(VariantId id) => Variants.FirstOrDefault(v => v.Id == id);

    public ImmutableArray<int> FeatLevels(int maxLevel) => Progression.FeatLevels(maxLevel);

    /// <summary>Efeitos das características de classe até o nível do contexto.</summary>
    public virtual IEnumerable<Effect> FeaturesAt(BuildContext context) => [];

    public abstract ArmorSetup ArmorAt(BuildContext context);

    public abstract IEnumerable<AttackRoutine> RoutinesAt(BuildContext context);

    public override string ToString() => Name;
}
