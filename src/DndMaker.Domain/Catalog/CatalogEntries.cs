using System.Collections.Immutable;
using DndMaker.Domain.Abilities;
using DndMaker.Domain.Common;
using DndMaker.Domain.Effects;

namespace DndMaker.Domain.Catalog;

public enum FeatCategory
{
    Origin,
    General,
    DarkGift,
}

/// <summary>Como um talento aumenta atributo.</summary>
public abstract record AbilityIncreaseRule
{
    public static AbilityIncreaseRule None { get; } = new NoIncrease();

    /// <summary>+1 no atributo mental ímpar mais importante da build; se não houver, no de conjuração (Fey Touched, War Caster).</summary>
    public static AbilityIncreaseRule BestOddMental { get; } = new OddMentalIncrease();

    public static AbilityIncreaseRule Fixed(Ability ability) => new FixedIncrease(ability);
}

public sealed record NoIncrease : AbilityIncreaseRule;

public sealed record OddMentalIncrease : AbilityIncreaseRule;

public sealed record FixedIncrease(Ability Ability) : AbilityIncreaseRule;

/// <summary>Talento. Dois talentos são iguais se têm o mesmo id.</summary>
public sealed record Feat(FeatId Id, string Name, FeatCategory Category, string Description)
{
    public AbilityIncreaseRule Increase { get; init; } = AbilityIncreaseRule.None;

    public ImmutableArray<Effect> Effects { get; init; } = [];

    public bool Equals(Feat? other) => other is not null && Id == other.Id;

    public override int GetHashCode() => Id.GetHashCode();
}

/// <summary>Espécie. Duas espécies são iguais se têm o mesmo id.</summary>
public sealed record Species(SpeciesId Id, string Name, string Source, string Description)
{
    public Meters Darkvision { get; init; } = Meters.Zero;

    /// <summary>O alcance ainda precisa ser conferido no livro.</summary>
    public bool DarkvisionNeedsConfirmation { get; init; }

    /// <summary>Concede um talento de origem extra (humano).</summary>
    public bool GrantsExtraOriginFeat { get; init; }

    public ImmutableArray<Effect> Effects { get; init; } = [];

    public bool Equals(Species? other) => other is not null && Id == other.Id;

    public override int GetHashCode() => Id.GetHashCode();
}

/// <summary>Antecedente: três atributos permitidos (+2 e +1 em dois deles), um talento de origem e perícias.</summary>
public sealed record Background
{
    public const int AbilityCount = 3;

    public Background(BackgroundId id, string name, AbilitySet abilities, Feat originFeat, string skills)
    {
        if (abilities.Members().Count() != AbilityCount)
        {
            throw new DomainException($"O antecedente {id} precisa permitir exatamente {AbilityCount} atributos.");
        }

        if (originFeat.Category == FeatCategory.General)
        {
            throw new DomainException($"O talento de {id} precisa ser de origem, não geral.");
        }

        Id = id;
        Name = name;
        Abilities = abilities;
        OriginFeat = originFeat;
        Skills = skills;
    }

    public BackgroundId Id { get; }

    public string Name { get; }

    public AbilitySet Abilities { get; }

    public Feat OriginFeat { get; }

    public string Skills { get; }

    public bool Equals(Background? other) => other is not null && Id == other.Id;

    public override int GetHashCode() => Id.GetHashCode();
}
