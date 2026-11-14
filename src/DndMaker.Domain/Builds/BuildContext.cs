using System.Collections.Immutable;
using DndMaker.Domain.Abilities;
using DndMaker.Domain.Catalog;
using DndMaker.Domain.Classes;
using DndMaker.Domain.Combat;
using DndMaker.Domain.Progression;

namespace DndMaker.Domain.Builds;

/// <summary>
/// O personagem num nível, já com atributos finais e talentos: é o que as regras de uma build consultam.
/// </summary>
public sealed class BuildContext
{
    private readonly ImmutableHashSet<FeatId> _feats;
    private readonly Ability _spellcastingAbility;

    public BuildContext(
        Level level,
        AbilityMap<AbilityScore> scores,
        BuildVariant? variant,
        IEnumerable<FeatId> feats,
        Species species,
        ClassProgression progression,
        Ability spellcastingAbility)
    {
        Level = level;
        Scores = scores;
        Modifiers = scores.Select((_, score) => score.Modifier);
        Variant = variant;
        _feats = [.. feats];
        Species = species;
        Progression = progression;
        _spellcastingAbility = spellcastingAbility;
    }

    public Level Level { get; }

    public int ProficiencyBonus => Level.ProficiencyBonus;

    public AbilityMap<AbilityScore> Scores { get; }

    public AbilityMap<int> Modifiers { get; }

    public BuildVariant? Variant { get; }

    public Species Species { get; }

    public ClassProgression Progression { get; }

    /// <summary>Quantos dados os truques rolam: 1, e mais um nos níveis 5, 11 e 17.</summary>
    public int CantripDice => Level.Value switch
    {
        >= 17 => 4,
        >= 11 => 3,
        >= 5 => 2,
        _ => 1,
    };

    public int Mod(Ability ability) => Modifiers[ability];

    public AttackBonus AttackBonus(Ability ability) => new(ProficiencyBonus + Mod(ability));

    public AttackBonus SpellAttackBonus => AttackBonus(_spellcastingAbility);

    public DifficultyClass SpellSaveDc => DifficultyClass.ForSpell(ProficiencyBonus, Mod(_spellcastingAbility));

    public int SpellcastingModifier => Mod(_spellcastingAbility);

    public bool Has(FeatId feat) => _feats.Contains(feat);

    public bool IsVariant(VariantId id) => Variant?.Id == id;

    public int LevelIn(CharacterClass characterClass) => Progression.LevelIn(characterClass, Level);
}
