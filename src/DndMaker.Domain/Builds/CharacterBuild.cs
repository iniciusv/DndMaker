using System.Collections.Immutable;
using DndMaker.Domain.Catalog;
using DndMaker.Domain.Common;
using DndMaker.Domain.Progression;

namespace DndMaker.Domain.Builds;

/// <summary>O que escolher num nível de talento.</summary>
public abstract record LevelChoice
{
    public static LevelChoice AbilityScoreImprovement { get; } = new AbilityScoreImprovementChoice();

    public static LevelChoice ForFeat(Feat feat) => new FeatChoice(feat);
}

/// <summary>Aumento de atributo (+2 distribuído pela política da build).</summary>
public sealed record AbilityScoreImprovementChoice : LevelChoice;

public sealed record FeatChoice(Feat Feat) : LevelChoice;

/// <summary>
/// Um personagem montado: build, variante, espécie, antecedente, talento extra e escolhas de nível.
/// Imutável; cada mudança devolve um novo personagem e as regras são conferidas na hora.
/// </summary>
public sealed class CharacterBuild
{
    private CharacterBuild(
        BuildArchetype archetype,
        BuildVariant? variant,
        Species species,
        Background background,
        Feat? extraOriginFeat,
        ImmutableSortedDictionary<int, LevelChoice> levelChoices)
    {
        Archetype = archetype;
        Variant = variant;
        Species = species;
        Background = background;
        ExtraOriginFeat = extraOriginFeat;
        LevelChoices = levelChoices;
    }

    public BuildArchetype Archetype { get; }

    public BuildVariant? Variant { get; }

    public Species Species { get; }

    public Background Background { get; }

    /// <summary>O talento extra guardado, mesmo que a espécie atual não o conceda (volta ao trocar para humano).</summary>
    public Feat? ExtraOriginFeat { get; }

    /// <summary>O talento extra que vale de fato: só se a espécie concede um.</summary>
    public Feat? EffectiveExtraOriginFeat => Species.GrantsExtraOriginFeat ? ExtraOriginFeat : null;

    /// <summary>Escolhas feitas por nível. Níveis sem escolha usam aumento de atributo.</summary>
    public ImmutableSortedDictionary<int, LevelChoice> LevelChoices { get; }

    public static CharacterBuild Create(
        BuildArchetype archetype,
        Species species,
        Background background,
        Feat? extraOriginFeat = null,
        BuildVariant? variant = null)
    {
        EnsureValidExtraFeat(extraOriginFeat);
        return new CharacterBuild(
            archetype,
            ResolveVariant(archetype, variant),
            species,
            background,
            extraOriginFeat,
            ImmutableSortedDictionary<int, LevelChoice>.Empty);
    }

    public LevelChoice ChoiceAt(int level) =>
        LevelChoices.TryGetValue(level, out var choice) ? choice : LevelChoice.AbilityScoreImprovement;

    /// <summary>Troca a build mantendo espécie e talento extra; volta à variante padrão, ao antecedente sugerido e sem escolhas.</summary>
    public CharacterBuild WithArchetype(BuildArchetype archetype, Background suggestedBackground)
    {
        if (suggestedBackground.Id != archetype.Profile.SuggestedBackground)
        {
            throw new DomainException($"{suggestedBackground.Id} não é o antecedente sugerido de {archetype.Id}.");
        }

        return new CharacterBuild(
            archetype,
            archetype.DefaultVariant,
            Species,
            suggestedBackground,
            ExtraOriginFeat,
            ImmutableSortedDictionary<int, LevelChoice>.Empty);
    }

    public CharacterBuild WithVariant(BuildVariant variant) =>
        new(Archetype, ResolveVariant(Archetype, variant), Species, Background, ExtraOriginFeat, LevelChoices);

    public CharacterBuild WithSpecies(Species species) =>
        new(Archetype, Variant, species, Background, ExtraOriginFeat, LevelChoices);

    public CharacterBuild WithBackground(Background background) =>
        new(Archetype, Variant, Species, background, ExtraOriginFeat, LevelChoices);

    public CharacterBuild WithExtraOriginFeat(Feat? feat)
    {
        EnsureValidExtraFeat(feat);
        return new CharacterBuild(Archetype, Variant, Species, Background, feat, LevelChoices);
    }

    public CharacterBuild WithLevelChoice(int level, LevelChoice choice)
    {
        if (!Archetype.FeatLevels(Level.Max).Contains(level))
        {
            throw new DomainException($"{Archetype.Name} não escolhe talento no nível {level}.");
        }

        if (choice is FeatChoice { Feat.Category: FeatCategory.DarkGift } dark)
        {
            throw new DomainException($"{dark.Feat.Name} é um Dark Gift e não pode ser escolhido num nível de talento.");
        }

        var choices = choice is AbilityScoreImprovementChoice ? LevelChoices.Remove(level) : LevelChoices.SetItem(level, choice);
        return new CharacterBuild(Archetype, Variant, Species, Background, ExtraOriginFeat, choices);
    }

    private static BuildVariant? ResolveVariant(BuildArchetype archetype, BuildVariant? variant)
    {
        if (variant is null)
        {
            return archetype.DefaultVariant;
        }

        return archetype.FindVariant(variant.Id) is not null
            ? variant
            : throw new DomainException($"A variante {variant.Id} não pertence a {archetype.Id}.");
    }

    private static void EnsureValidExtraFeat(Feat? feat)
    {
        if (feat is not null && feat.Category != FeatCategory.Origin)
        {
            throw new DomainException($"{feat.Name} não é um talento de origem; o talento extra do humano precisa ser.");
        }
    }
}
