using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace DndMaker.Domain.Damage;

/// <summary>
/// Um grupo de dados iguais, como <c>2d8</c>. Pode ter uma face mínima: com Great Weapon Fighting,
/// 1 e 2 contam como 3 (<see cref="WithMinimumFace"/>). O texto não mostra o mínimo; a rotina explica.
/// </summary>
public readonly record struct Dice
{
    /// <summary>0 quando não há mínimo, para que <c>default</c> e <c>WithMinimumFace(1)</c> sejam iguais.</summary>
    private readonly int _minimumFace;

    public Dice(int count, int sides)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(sides, 2);
        Count = count;
        Sides = sides;
    }

    private Dice(int count, int sides, int minimumFace)
        : this(count, sides)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(minimumFace, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(minimumFace, sides);
        _minimumFace = minimumFace == 1 ? 0 : minimumFace;
    }

    public static Dice D4 { get; } = new(1, 4);

    public static Dice D6 { get; } = new(1, 6);

    public static Dice D8 { get; } = new(1, 8);

    public static Dice D10 { get; } = new(1, 10);

    public static Dice D12 { get; } = new(1, 12);

    public static Dice D20 { get; } = new(1, 20);

    public int Count { get; }

    public int Sides { get; }

    /// <summary>Faces abaixo deste valor contam como ele. 1 quando não há mínimo.</summary>
    public int MinimumFace => Math.Max(1, _minimumFace);

    /// <summary>
    /// Média: quantidade × (lados + 1) / 2. Com face mínima m, as faces de 1 a m valem m
    /// (um d6 com mínimo 3 tem média 4).
    /// </summary>
    public double Average
    {
        get
        {
            var min = MinimumFace;
            var faceTotal = (min * min) + (((Sides * (Sides + 1)) - (min * (min + 1))) / 2.0);
            return Count * faceTotal / Sides;
        }
    }

    /// <summary>O mesmo grupo de dados, com as faces abaixo de <paramref name="face"/> contando como ela.</summary>
    public Dice WithMinimumFace(int face) => new(Count, Sides, face);

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
    public static Dice operator *(int times, Dice dice) => new(times * dice.Count, dice.Sides, dice.MinimumFace);

    public static Dice Multiply(int times, Dice dice) => times * dice;

    public static DamageFormula operator +(Dice dice, int flat) => dice.ToFormula().Plus(flat);

    public static DamageFormula operator +(Dice left, Dice right) => left.ToFormula().Plus(right);

    public static DamageFormula Add(Dice dice, int flat) => dice + flat;

    public static implicit operator DamageFormula(Dice dice) => dice.ToFormula();

    public DamageFormula ToFormula() => DamageFormula.Of(this);

    public override string ToString() => $"{Count}d{Sides}";
}
