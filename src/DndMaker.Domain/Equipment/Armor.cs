using DndMaker.Domain.Combat;

namespace DndMaker.Domain.Equipment;

public enum ArmorCategory
{
    None,
    Light,
    Medium,
    Heavy,
}

/// <summary>Uma armadura do livro. <see cref="MaxDexterity"/> nulo soma a Destreza inteira.</summary>
public sealed record Armor(string Name, int BaseArmorClass, ArmorCategory Category, int? MaxDexterity, bool StealthDisadvantage)
{
    public int ArmorClassFor(int dexterityModifier) => Category switch
    {
        ArmorCategory.Heavy => BaseArmorClass,
        _ => BaseArmorClass + (MaxDexterity is { } max ? Math.Min(dexterityModifier, max) : dexterityModifier),
    };
}

/// <summary>A defesa final de um personagem, com a explicação que aparece na ficha.</summary>
public sealed record ArmorSetup(ArmorClass Value, string Description, bool StealthDisadvantage = false, string? Extra = null)
{
    public ArmorSetup WithBonus(int bonus) => bonus == 0 ? this : this with { Value = Value + bonus };
}

/// <summary>Armaduras e escudo do PHB, com os nomes da página original.</summary>
public static class ArmorTable
{
    public const int ShieldBonus = 2;
    public const string ShieldName = "escudo";

    public static Armor StuddedLeather { get; } = new("couro batido", 12, ArmorCategory.Light, null, false);

    public static Armor ScaleMail { get; } = new("cota de escamas", 14, ArmorCategory.Medium, 2, true);

    public static Armor HalfPlate { get; } = new("meia armadura", 15, ArmorCategory.Medium, 2, true);

    public static Armor ChainMail { get; } = new("cota de malha", 16, ArmorCategory.Heavy, null, true);

    public static Armor Splint { get; } = new("cota de talas", 17, ArmorCategory.Heavy, null, true);

    /// <summary>Veste <paramref name="armor"/>, com ou sem escudo. <paramref name="suffix"/> entra no fim da descrição.</summary>
    public static ArmorSetup Wear(Armor armor, int dexterityModifier, bool shield, int bonus = 0, string? suffix = null)
    {
        var description = armor.Name + (shield ? " + " + ShieldName : string.Empty) + (suffix ?? string.Empty);
        var value = armor.ArmorClassFor(dexterityModifier) + (shield ? ShieldBonus : 0) + bonus;
        return new ArmorSetup(new ArmorClass(value), description, armor.StealthDisadvantage);
    }

    /// <summary>
    /// A melhor das opções para essa Destreza. Em empate fica a primeira da lista,
    /// então liste a mais leve antes (sem desvantagem em Furtividade).
    /// </summary>
    public static ArmorSetup Best(IEnumerable<Armor> options, int dexterityModifier, bool shield)
    {
        Armor? best = null;
        foreach (var armor in options)
        {
            if (best is null || armor.ArmorClassFor(dexterityModifier) > best.ArmorClassFor(dexterityModifier))
            {
                best = armor;
            }
        }

        return best is null
            ? throw new ArgumentException("Nenhuma armadura informada.", nameof(options))
            : Wear(best, dexterityModifier, shield);
    }
}
