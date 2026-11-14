using System.Globalization;

namespace DndMaker.Domain.Common;

/// <summary>Probabilidade entre 0 e 1.</summary>
public readonly record struct Probability : IComparable<Probability>
{
    public Probability(double value)
    {
        if (double.IsNaN(value) || value < 0 || value > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "Probabilidade precisa estar entre 0 e 1.");
        }

        Value = value;
    }

    public static Probability Zero { get; } = new(0);

    public static Probability One { get; } = new(1);

    public double Value { get; }

    public Probability Complement => new(1 - Value);

    /// <summary>Limita a [0, 1]. Para resultados de fórmulas que podem sair do intervalo.</summary>
    public static Probability Clamp(double value) =>
        double.IsNaN(value) ? throw new ArgumentOutOfRangeException(nameof(value), "NaN não é probabilidade.")
            : new Probability(Math.Clamp(value, 0, 1));

    public static Probability Clamp(double value, double floor, double ceiling) =>
        Clamp(Math.Clamp(value, floor, ceiling));

    /// <summary>Chance de pelo menos um sucesso em <paramref name="trials"/> tentativas independentes.</summary>
    public Probability AtLeastOnceIn(int trials)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(trials);
        return Clamp(1 - Math.Pow(1 - Value, trials));
    }

    /// <summary>Chance de este evento acontecer ou, se não acontecer, o outro (independente) acontecer.</summary>
    public Probability OrElse(Probability other) => Clamp(Value + ((1 - Value) * other.Value));

    /// <summary>Chance de este evento e o outro (independente) acontecerem.</summary>
    public Probability And(Probability other) => new(Value * other.Value);

    public int CompareTo(Probability other) => Value.CompareTo(other.Value);

    public static bool operator <(Probability left, Probability right) => left.Value < right.Value;

    public static bool operator >(Probability left, Probability right) => left.Value > right.Value;

    public static bool operator <=(Probability left, Probability right) => left.Value <= right.Value;

    public static bool operator >=(Probability left, Probability right) => left.Value >= right.Value;

    public override string ToString() => Value.ToString("P0", CultureInfo.InvariantCulture);
}
