using System.Collections.Immutable;

namespace DndMaker.Domain.Abilities;

/// <summary>Os seis atributos. A ordem é a da ficha e também o índice no <see cref="AbilityMap{T}"/>.</summary>
public enum Ability
{
    Strength = 0,
    Dexterity = 1,
    Constitution = 2,
    Intelligence = 3,
    Wisdom = 4,
    Charisma = 5,
}

public static class AbilityList
{
    public const int Count = 6;

    /// <summary>Todos os atributos, na ordem da ficha.</summary>
    public static ImmutableArray<Ability> All { get; } =
    [
        Ability.Strength, Ability.Dexterity, Ability.Constitution,
        Ability.Intelligence, Ability.Wisdom, Ability.Charisma,
    ];

    public static bool IsMental(this Ability ability) => AbilitySet.Mental.Contains(ability);

    /// <summary>Sigla em português, como na ficha: FOR, DES, CON, INT, SAB, CAR.</summary>
    public static string Abbreviation(this Ability ability) => ability switch
    {
        Ability.Strength => "FOR",
        Ability.Dexterity => "DES",
        Ability.Constitution => "CON",
        Ability.Intelligence => "INT",
        Ability.Wisdom => "SAB",
        Ability.Charisma => "CAR",
        _ => throw new ArgumentOutOfRangeException(nameof(ability), ability, "Atributo inexistente."),
    };

    internal static int Index(this Ability ability)
    {
        var index = (int)ability;
        if ((uint)index >= Count)
        {
            throw new ArgumentOutOfRangeException(nameof(ability), ability, "Atributo inexistente.");
        }

        return index;
    }
}
