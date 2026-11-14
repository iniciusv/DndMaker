using System.Runtime.CompilerServices;

namespace DndMaker.Domain.Abilities;

/// <summary>
/// Um valor por atributo, guardado em seis posições contíguas sem alocação no heap.
/// Imutável: <see cref="With"/> devolve uma cópia.
/// </summary>
public readonly struct AbilityMap<T> : IEquatable<AbilityMap<T>>
{
    private readonly AbilityBuffer<T> _values;

    internal AbilityMap(AbilityBuffer<T> values) => _values = values;

    public T this[Ability ability] => _values[ability.Index()];

    public AbilityMap<T> With(Ability ability, T value)
    {
        var copy = _values;
        copy[ability.Index()] = value;
        return new AbilityMap<T>(copy);
    }

    public AbilityMap<TResult> Select<TResult>(Func<Ability, T, TResult> selector)
    {
        var self = this;
        return AbilityMap.Create(ability => selector(ability, self[ability]));
    }

    /// <summary>Os pares atributo/valor, na ordem da ficha.</summary>
    public IReadOnlyList<KeyValuePair<Ability, T>> ToList()
    {
        var self = this;
        return [.. AbilityList.All.Select(ability => KeyValuePair.Create(ability, self[ability]))];
    }

    public bool Equals(AbilityMap<T> other)
    {
        var comparer = EqualityComparer<T>.Default;
        foreach (var ability in AbilityList.All)
        {
            if (!comparer.Equals(this[ability], other[ability]))
            {
                return false;
            }
        }

        return true;
    }

    public override bool Equals(object? obj) => obj is AbilityMap<T> other && Equals(other);

    public override int GetHashCode()
    {
        var hash = default(HashCode);
        foreach (var ability in AbilityList.All)
        {
            hash.Add(this[ability]);
        }

        return hash.ToHashCode();
    }

    public static bool operator ==(AbilityMap<T> left, AbilityMap<T> right) => left.Equals(right);

    public static bool operator !=(AbilityMap<T> left, AbilityMap<T> right) => !left.Equals(right);

    public override string ToString()
    {
        var self = this;
        return string.Join(", ", AbilityList.All.Select(ability => $"{ability}: {self[ability]}"));
    }
}

/// <summary>Fábricas de <see cref="AbilityMap{T}"/>.</summary>
public static class AbilityMap
{
    public static AbilityMap<T> Create<T>(Func<Ability, T> valueOf)
    {
        var buffer = default(AbilityBuffer<T>);
        foreach (var ability in AbilityList.All)
        {
            buffer[ability.Index()] = valueOf(ability);
        }

        return new AbilityMap<T>(buffer);
    }

    public static AbilityMap<T> Uniform<T>(T value) => Create(_ => value);

    /// <summary>Atalho para a ordem da ficha: FOR, DES, CON, INT, SAB, CAR.</summary>
    public static AbilityMap<T> Of<T>(T strength, T dexterity, T constitution, T intelligence, T wisdom, T charisma) =>
        Create(ability => ability switch
        {
            Ability.Strength => strength,
            Ability.Dexterity => dexterity,
            Ability.Constitution => constitution,
            Ability.Intelligence => intelligence,
            Ability.Wisdom => wisdom,
            _ => charisma,
        });
}

[InlineArray(AbilityList.Count)]
internal struct AbilityBuffer<T>
{
#pragma warning disable IDE0044, IDE0051 // O campo é o primeiro elemento do inline array; o compilador gera os demais.
    private T _element0;
#pragma warning restore IDE0044, IDE0051
}
