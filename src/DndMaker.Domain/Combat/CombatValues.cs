using System.Globalization;

namespace DndMaker.Domain.Combat;

/// <summary>Bônus total de um ataque (proficiência + modificador + outros).</summary>
public readonly record struct AttackBonus(int Value)
{
    public static AttackBonus operator +(AttackBonus bonus, int extra) => new(bonus.Value + extra);

    public static AttackBonus Add(AttackBonus bonus, int extra) => bonus + extra;

    public override string ToString() => SignedNumber.Format(Value);
}

/// <summary>Classe de dificuldade de um teste.</summary>
public readonly record struct DifficultyClass
{
    public DifficultyClass(int value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);
        Value = value;
    }

    public int Value { get; }

    /// <summary>CD de magia: 8 + proficiência + modificador do atributo de conjuração.</summary>
    public static DifficultyClass ForSpell(int proficiencyBonus, int abilityModifier) =>
        new(8 + proficiencyBonus + abilityModifier);

    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}

/// <summary>Classe de armadura.</summary>
public readonly record struct ArmorClass : IComparable<ArmorClass>
{
    public ArmorClass(int value)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(value);
        Value = value;
    }

    public int Value { get; }

    public static ArmorClass operator +(ArmorClass armorClass, int bonus) => new(armorClass.Value + bonus);

    public static ArmorClass Add(ArmorClass armorClass, int bonus) => armorClass + bonus;

    public int CompareTo(ArmorClass other) => Value.CompareTo(other.Value);

    public static bool operator <(ArmorClass left, ArmorClass right) => left.Value < right.Value;

    public static bool operator >(ArmorClass left, ArmorClass right) => left.Value > right.Value;

    public static bool operator <=(ArmorClass left, ArmorClass right) => left.Value <= right.Value;

    public static bool operator >=(ArmorClass left, ArmorClass right) => left.Value >= right.Value;

    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}

/// <summary>Formata bônus com sinal, usando o sinal de menos tipográfico (−), como a página original.</summary>
public static class SignedNumber
{
    public const char Minus = '−';

    public static string Format(int value) =>
        value >= 0
            ? "+" + value.ToString(CultureInfo.InvariantCulture)
            : Minus + Math.Abs(value).ToString(CultureInfo.InvariantCulture);
}
