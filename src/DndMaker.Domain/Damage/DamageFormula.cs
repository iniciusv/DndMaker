using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using DndMaker.Domain.Combat;

namespace DndMaker.Domain.Damage;

/// <summary>Uma parcela de dano: um grupo de dados ou um valor fixo.</summary>
public readonly record struct DamageTerm
{
    private DamageTerm(Dice? dice, int flat)
    {
        Dice = dice;
        Flat = flat;
    }

    public Dice? Dice { get; }

    public int Flat { get; }

    public bool IsDice => Dice.HasValue;

    public double Average => Dice?.Average ?? Flat;

    public static DamageTerm Of(Dice dice) => new(dice, 0);

    public static DamageTerm Of(int flat) => new(null, flat);
}

/// <summary>
/// Soma de dados e valores fixos, na ordem em que foram escritos (<c>1d8+4+1d6</c>).
/// Valores fixos vizinhos são somados e zeros somem, como na página original.
/// </summary>
public sealed record DamageFormula
{
    private DamageFormula(ImmutableArray<DamageTerm> terms) => Terms = terms;

    public static DamageFormula Empty { get; } = new([]);

    public ImmutableArray<DamageTerm> Terms { get; }

    public double Average
    {
        get
        {
            var total = 0.0;
            foreach (var term in Terms)
            {
                total += term.Average;
            }

            return total;
        }
    }

    public static DamageFormula Of(Dice dice) => Empty.Plus(dice);

    public static DamageFormula Of(int flat) => Empty.Plus(flat);

    public DamageFormula Plus(Dice dice) => new(Terms.Add(DamageTerm.Of(dice)));

    public DamageFormula Plus(int flat)
    {
        if (flat == 0)
        {
            return this;
        }

        if (!Terms.IsEmpty && Terms[^1] is { IsDice: false } last)
        {
            var sum = last.Flat + flat;
            var withoutLast = Terms.RemoveAt(Terms.Length - 1);
            return new DamageFormula(sum == 0 ? withoutLast : withoutLast.Add(DamageTerm.Of(sum)));
        }

        return new DamageFormula(Terms.Add(DamageTerm.Of(flat)));
    }

    public DamageFormula Plus(DamageFormula other) =>
        other.Terms.Aggregate(this, (formula, term) => term.Dice is { } dice ? formula.Plus(dice) : formula.Plus(term.Flat));

    public static DamageFormula operator +(DamageFormula formula, int flat) => formula.Plus(flat);

    public static DamageFormula operator +(DamageFormula formula, Dice dice) => formula.Plus(dice);

    public static DamageFormula operator +(DamageFormula left, DamageFormula right) => left.Plus(right);

    public static DamageFormula Add(DamageFormula left, DamageFormula right) => left + right;

    public bool Equals(DamageFormula? other) =>
        other is not null && Terms.SequenceEqual(other.Terms);

    public override int GetHashCode()
    {
        var hash = default(HashCode);
        foreach (var term in Terms)
        {
            hash.Add(term);
        }

        return hash.ToHashCode();
    }

    /// <summary>Mesmo texto da página: dados separados por "+", fixos com sinal, zero omitido.</summary>
    public override string ToString()
    {
        var text = new StringBuilder();
        foreach (var term in Terms)
        {
            if (term.Dice is { } dice)
            {
                if (text.Length > 0)
                {
                    text.Append('+');
                }

                text.Append(dice.ToString());
            }
            else
            {
                text.Append(term.Flat > 0 ? '+' : SignedNumber.Minus)
                    .Append(Math.Abs(term.Flat).ToString(CultureInfo.InvariantCulture));
            }
        }

        return text.ToString();
    }
}
