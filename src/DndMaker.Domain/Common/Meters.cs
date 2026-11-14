using System.Globalization;

namespace DndMaker.Domain.Common;

/// <summary>Distância em metros (1,5 m por quadrado, como nos livros em português).</summary>
public readonly record struct Meters : IComparable<Meters>
{
    public Meters(int value)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(value);
        Value = value;
    }

    public static Meters Zero { get; } = new(0);

    public int Value { get; }

    public bool IsZero => Value == 0;

    public static Meters Of(int value) => new(value);

    public static Meters Max(Meters left, Meters right) => left >= right ? left : right;

    public static Meters operator +(Meters left, Meters right) => new(left.Value + right.Value);

    public static Meters Add(Meters left, Meters right) => left + right;

    public int CompareTo(Meters other) => Value.CompareTo(other.Value);

    public static bool operator <(Meters left, Meters right) => left.Value < right.Value;

    public static bool operator >(Meters left, Meters right) => left.Value > right.Value;

    public static bool operator <=(Meters left, Meters right) => left.Value <= right.Value;

    public static bool operator >=(Meters left, Meters right) => left.Value >= right.Value;

    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture) + " m";
}
