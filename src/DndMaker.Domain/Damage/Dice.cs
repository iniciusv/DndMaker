using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace DndMaker.Domain.Damage;

/// <summary>Um grupo de dados iguais, como <c>2d8</c>.</summary>
public readonly record struct Dice
{
    public Dice(int count, int sides)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(sides, 2);
        Count = count;
        Sides = sides;
    }

    public static Dice D4 { get; } = new(1, 4);

    public static Dice D6 { get; } = new(1, 6);

    public static Dice D8 { get; } = new(1, 8);

    public static Dice D10 { get; } = new(1, 10);

    public static Dice D12 { get; } = new(1, 12);

    public static Dice D20 { get; } = new(1, 20);

    public int Count { get; }

    public int Sides { get; }

    /// <summary>Média: quantidade × (lados + 1) / 2.</summary>
    public double Average => Count * (Sides + 1) / 2.0;

    public static Dice Parse(string text) =>
        TryParse(text, out var dice) ? dice : throw new FormatException($"Dados inválidos: '{text}'. Use o formato 2d8.");

    public static bool TryParse([NotNullWhen(true)] string? text, out Dice dice)
    {
        dice = default;
        var parts = text?.Trim().ToUpperInvariant().Split('D');
        if (parts is not [var count, var sides]
            || !int.TryParse(count, NumberStyles.None, CultureInfo.InvariantCulture, out var n)
            || !int.TryParse(sides, NumberStyles.None, CultureInfo.InvariantCulture, out var d)
            || n < 1 || d < 2)
        {
            return false;
        }

        dice = new Dice(n, d);
        return true;
    }

    /// <summary>Multiplica a quantidade: <c>3 * Dice.D8</c> é <c>3d8</c>.</summary>
    public static Dice operator *(int times, Dice dice) => new(times * dice.Count, dice.Sides);

    public static Dice Multiply(int times, Dice dice) => times * dice;

    public static DamageFormula operator +(Dice dice, int flat) => dice.ToFormula().Plus(flat);

    public static DamageFormula operator +(Dice left, Dice right) => left.ToFormula().Plus(right);

    public static DamageFormula Add(Dice dice, int flat) => dice + flat;

    public static implicit operator DamageFormula(Dice dice) => dice.ToFormula();

    public DamageFormula ToFormula() => DamageFormula.Of(this);

    public override string ToString() => $"{Count}d{Sides}";
}
