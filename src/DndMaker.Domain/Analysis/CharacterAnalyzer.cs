using System.Collections.Immutable;
using DndMaker.Domain.Abilities;
using DndMaker.Domain.Builds;
using DndMaker.Domain.Catalog;
using DndMaker.Domain.Effects;
using DndMaker.Domain.Equipment;
using DndMaker.Domain.Progression;
using DndMaker.Domain.Routines;

namespace DndMaker.Domain.Analysis;

/// <summary>A ficha calculada de um personagem num nível. Imutável.</summary>
public sealed record CharacterSheet
{
    public required CharacterBuild Build { get; init; }

    public required Level Level { get; init; }

    public required AbilityAllocation Allocation { get; init; }

    public required ImmutableArray<Feat> Feats { get; init; }

    public required int HitPoints { get; init; }

    public required ArmorSetup Armor { get; init; }

    public required int Initiative { get; init; }

    public required DarkvisionResult Darkvision { get; init; }

    public required AbilityMap<SavingThrow> SavingThrows { get; init; }

    /// <summary>Bônus que as auras somam a todos os testes de resistência.</summary>
    public required int SaveBonus { get; init; }

    public required ThreatResistance Charm { get; init; }

    /// <summary>Nulo para builds sem magias de concentração.</summary>
    public required ConcentrationResult? Concentration { get; init; }

    public required ImmutableArray<RoutineEstimate> Routines { get; init; }

    public int ProficiencyBonus => Level.ProficiencyBonus;

    public AbilityMap<AbilityScore> Scores => Allocation.Scores;

    public AbilityMap<int> Modifiers => Allocation.Scores.Select((_, score) => score.Modifier);

    public BuildVariant? Variant => Build.Variant;

    /// <summary>A variante escolhida pede um talento que o personagem não tem (ex.: Shillelagh sem Magic Initiate).</summary>
    public bool VariantRequirementMissing =>
        Variant?.RequiredFeat is { } required && !Feats.Any(f => f.Id == required);

    public RoutineEstimate? Best(ResourceTier maxTier) => RoutineSelector.Best(Routines, maxTier);
}

/// <summary>Monta a ficha: atributos, talentos, efeitos, defesas e dano.</summary>
public sealed class CharacterAnalyzer(ConditionThreat charmThreat, AbilityScoreAllocator allocator)
{
    public CharacterAnalyzer(ConditionThreat charmThreat)
        : this(charmThreat, AbilityScoreAllocator.Default)
    {
    }

    public CharacterSheet Analyze(CharacterBuild build, Level level, CombatAssumptions assumptions)
    {
        var archetype = build.Archetype;
        var allocation = allocator.Allocate(build, level);
        var feats = FeatsOf(build, allocation);
        var context = new BuildContext(
            level, allocation.Scores, build.Variant, feats.Select(f => f.Id), build.Species,
            archetype.Progression, archetype.Profile.SpellcastingAbility);

        var traits = CharacterTraits.From(
            build.Species.Effects
                .Concat(feats.SelectMany(f => f.Effects))
                .Concat(archetype.FeaturesAt(context)));

        var saveBonus = traits.SaveBonus(context.Modifiers);
        var saves = DefenseCalculator.SavingThrows(
            context.Modifiers,
            context.ProficiencyBonus,
            archetype.SavingThrows | traits.SaveProficiencies,
            traits.SaveAdvantages,
            saveBonus);

        return new CharacterSheet
        {
            Build = build,
            Level = level,
            Allocation = allocation,
            Feats = feats,
            HitPoints = HitPointCalculator.Calculate(
                archetype.Progression, level, context.Mod(Ability.Constitution), traits.BonusHitPointsPerLevel),
            Armor = archetype.ArmorAt(context).WithBonus(traits.BonusArmorClass),
            Initiative = context.Mod(Ability.Dexterity) + traits.InitiativeModifier(context.ProficiencyBonus, context.Modifiers),
            Darkvision = traits.Darkvision(build.Species.Darkvision),
            SavingThrows = saves,
            SaveBonus = saveBonus,
            Charm = DefenseCalculator.Resist(charmThreat, saves, traits),
            Concentration = archetype.Profile.UsesConcentration
                ? DefenseCalculator.Concentration(saves[Ability.Constitution], traits)
                : null,
            Routines = [.. archetype.RoutinesAt(context).Select(r => DamageEstimator.Estimate(r, assumptions))],
        };
    }

    /// <summary>Talento do antecedente, talento extra (se a espécie der) e talentos dos níveis, sem repetir.</summary>
    private static ImmutableArray<Feat> FeatsOf(CharacterBuild build, AbilityAllocation allocation)
    {
        var feats = new List<Feat> { build.Background.OriginFeat };
        if (build.EffectiveExtraOriginFeat is { } extra)
        {
            feats.Add(extra);
        }

        feats.AddRange(allocation.Picks.Select(p => p.Choice).OfType<FeatChoice>().Select(c => c.Feat));
        return [.. feats.Distinct()];
    }
}
