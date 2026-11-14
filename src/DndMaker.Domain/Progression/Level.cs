using System.Globalization;

namespace DndMaker.Domain.Progression;

/// <summary>Nível do personagem (1 a 20).</summary>
public readonly record struct Level : IComparable<Level>
{
    public const int Min = 1;
    public const int Max = 20;

    public Level(int value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, Min);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value, Max);
        Value = value;
    }

    public int Value { get; }

    /// <summary>+2 nos níveis 1–4, +3 nos 5–8, até +6 nos 17–20.</summary>
    public int ProficiencyBonus => 2 + ((Value - 1) / 4);

    public static Level Of(int value) => new(value);

    /// <summary>Os níveis de <paramref name="from"/> até <paramref name="to"/>, inclusive.</summary>
    public static IEnumerable<Level> Range(int from, int to) =>
        Enumerable.Range(from, to - from + 1).Select(Of);

    public int CompareTo(Level other) => Value.CompareTo(other.Value);

    public static bool operator <(Level left, Level right) => left.Value < right.Value;

    public static bool operator >(Level left, Level right) => left.Value > right.Value;

    public static bool operator <=(Level left, Level right) => left.Value <= right.Value;

    public static bool operator >=(Level left, Level right) => left.Value >= right.Value;

    // Comparações com int deixam as regras legíveis: "if (c.Level >= 5)".
    public static bool operator <(Level left, int right) => left.Value < right;

    public static bool operator >(Level left, int right) => left.Value > right;

    public static bool operator <=(Level left, int right) => left.Value <= right;

    public static bool operator >=(Level left, int right) => left.Value >= right;

    public static bool operator ==(Level left, int right) => left.Value == right;

    public static bool operator !=(Level left, int right) => left.Value != right;

    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}
