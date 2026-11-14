using DndMaker.Domain.Abilities;
using DndMaker.Domain.Classes;

namespace DndMaker.Content.Catalog;

/// <summary>As classes do PHB 2024 usadas pelas builds.</summary>
public static class ClassCatalog
{
    public static CharacterClass Fighter { get; } = new(new("fighter"), "Guerreiro", 10, AbilitySet.Strength | AbilitySet.Constitution)
    {
        // O guerreiro tem dois níveis de talento a mais, no 6 e no 14.
        FeatLevels = [4, 6, 8, 12, 14, 16, 19],
    };

    public static CharacterClass Warlock { get; } = new(new("warlock"), "Bruxo", 8, AbilitySet.Wisdom | AbilitySet.Charisma);

    public static CharacterClass Bard { get; } = new(new("bard"), "Bardo", 8, AbilitySet.Dexterity | AbilitySet.Charisma);

    public static CharacterClass Paladin { get; } = new(new("paladin"), "Paladino", 10, AbilitySet.Wisdom | AbilitySet.Charisma);

    public static CharacterClass Cleric { get; } = new(new("cleric"), "Clérigo", 8, AbilitySet.Wisdom | AbilitySet.Charisma);

    public static CharacterClass Ranger { get; } = new(new("ranger"), "Patrulheiro", 10, AbilitySet.Strength | AbilitySet.Dexterity);

    public static CharacterClass Sorcerer { get; } = new(new("sorcerer"), "Feiticeiro", 6, AbilitySet.Constitution | AbilitySet.Charisma);
}
