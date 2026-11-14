using System.Collections.Immutable;
using DndMaker.Domain.Abilities;
using DndMaker.Domain.Catalog;

namespace DndMaker.Domain.Classes;

/// <summary>Classe de personagem: dado de vida, testes de resistência e níveis de talento.</summary>
public sealed record CharacterClass(ClassId Id, string Name, int HitDie, AbilitySet SavingThrows)
{
    /// <summary>Níveis de classe com talento ou aumento de atributo na maioria das classes.</summary>
    public static ImmutableArray<int> StandardFeatLevels { get; } = [4, 8, 12, 16, 19];

    public ImmutableArray<int> FeatLevels { get; init; } = StandardFeatLevels;

    /// <summary>PV ganhos num nível depois do primeiro: média do dado arredondada para cima.</summary>
    public int AverageHitPointsPerLevel => (HitDie / 2) + 1;

    public bool Equals(CharacterClass? other) => other is not null && Id == other.Id;

    public override int GetHashCode() => Id.GetHashCode();
}
