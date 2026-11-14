namespace DndMaker.Domain.Abilities;

/// <summary>Conjunto de atributos, por exemplo os três permitidos por um antecedente.</summary>
[Flags]
public enum AbilitySet
{
    None = 0,
    Strength = 1 << Ability.Strength,
    Dexterity = 1 << Ability.Dexterity,
    Constitution = 1 << Ability.Constitution,
    Intelligence = 1 << Ability.Intelligence,
    Wisdom = 1 << Ability.Wisdom,
    Charisma = 1 << Ability.Charisma,

    Physical = Strength | Dexterity | Constitution,
    Mental = Intelligence | Wisdom | Charisma,
    All = Physical | Mental,
}

public static class AbilitySetExtensions
{
    public static AbilitySet ToSet(this Ability ability) => (AbilitySet)(1 << ability.Index());

    public static bool Contains(this AbilitySet set, Ability ability) => (set & ability.ToSet()) != 0;

    public static AbilitySet With(this AbilitySet set, Ability ability) => set | ability.ToSet();

    /// <summary>Os atributos do conjunto, na ordem da ficha.</summary>
    public static IEnumerable<Ability> Members(this AbilitySet set) =>
        AbilityList.All.Where(ability => set.Contains(ability));

    public static AbilitySet ToAbilitySet(this IEnumerable<Ability> abilities) =>
        abilities.Aggregate(AbilitySet.None, (set, ability) => set.With(ability));
}
