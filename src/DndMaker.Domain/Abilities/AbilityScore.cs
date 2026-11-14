using System.Globalization;

namespace DndMaker.Domain.Abilities;

/// <summary>Valor de atributo (1 a 30). Personagens param em 20 sem itens mágicos.</summary>
public readonly record struct AbilityScore : IComparable<AbilityScore>
{
    public const int Min = 1;
    public const int Max = 30;
    public const int CharacterMax = 20;

    public AbilityScore(int value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, Min);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value, Max);
        Value = value;
    }

    public int Value { get; }

    /// <summary>(valor − 10) / 2, arredondado para baixo.</summary>
    public int Modifier => (int)Math.Floor((Value - 10) / 2.0);

    public bool IsOdd => Value % 2 != 0;

    public bool IsAtCharacterMax => Value >= CharacterMax;

    /// <summary>Soma pontos sem passar do teto do personagem. Um valor já acima do teto (item mágico) fica como está.</summary>
    public AbilityScore Increase(int amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);
        return IsAtCharacterMax ? this : new AbilityScore(Math.Min(CharacterMax, Value + amount));
    }

    public int CompareTo(AbilityScore other) => Value.CompareTo(other.Value);

    public static bool operator <(AbilityScore left, AbilityScore right) => left.Value < right.Value;

    public static bool operator >(AbilityScore left, AbilityScore right) => left.Value > right.Value;

    public static bool operator <=(AbilityScore left, AbilityScore right) => left.Value <= right.Value;

    public static bool operator >=(AbilityScore left, AbilityScore right) => left.Value >= right.Value;

    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}
