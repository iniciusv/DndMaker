using DndMaker.Domain.Abilities;
using DndMaker.Domain.Analysis;
using DndMaker.Domain.Combat;
using DndMaker.Domain.Progression;
using DndMaker.Domain.Routines;
using DndMaker.Domain.Rulesets;

namespace DndMaker.Application.Overview;

/// <summary>Uma linha de "Todas as builds". Os campos <c>Top*</c> marcam o maior valor da coluna (empates incluídos).</summary>
public sealed record OverviewRowDto
{
    public required string ArchetypeId { get; init; }

    public required string Name { get; init; }

    public required string Status { get; init; }

    public required int ArmorClass { get; init; }

    public required int HitPoints { get; init; }

    public required int WisdomSave { get; init; }

    public required int ConstitutionSave { get; init; }

    public required int Initiative { get; init; }

    public required double CharmChance { get; init; }

    public required double BestExpected { get; init; }

    public required string BestRoutine { get; init; }

    /// <summary>O que a build ganha neste nível; vazio se nada.</summary>
    public required string Milestone { get; init; }

    public bool TopArmorClass { get; init; }

    public bool TopHitPoints { get; init; }

    public bool TopWisdomSave { get; init; }

    public bool TopConstitutionSave { get; init; }

    public bool TopInitiative { get; init; }

    public bool TopExpected { get; init; }
}

/// <summary>
/// A tabela com todas as builds num nível. <see cref="SpeciesName"/>, <see cref="ExtraOriginFeatName"/> e a CA
/// descrevem as escolhas padrão usadas, para a legenda.
/// </summary>
public sealed record BuildsOverviewDto(
    int Level,
    int EnemyArmorClass,
    int MaxTier,
    string MaxTierName,
    string SpeciesName,
    string ExtraOriginFeatName,
    IReadOnlyList<OverviewRowDto> Rows);

/// <summary>Todas as builds com as escolhas padrão da campanha, num nível e contra uma CA.</summary>
public sealed class BuildsOverviewQuery(Ruleset rules, CharacterAnalyzer analyzer)
{
    public BuildsOverviewDto Execute(Level level, ArmorClass enemyArmorClass, ResourceTier maxTier)
    {
        var assumptions = new CombatAssumptions(enemyArmorClass);
        var rows = rules.Archetypes
            .Select(archetype => Row(analyzer.Analyze(rules.OverviewCharacter(archetype), level, assumptions), maxTier))
            .ToList();

        var maxArmor = rows.Max(r => r.ArmorClass);
        var maxHitPoints = rows.Max(r => r.HitPoints);
        var maxWisdom = rows.Max(r => r.WisdomSave);
        var maxConstitution = rows.Max(r => r.ConstitutionSave);
        var maxInitiative = rows.Max(r => r.Initiative);
        var maxExpected = rows.Max(r => r.BestExpected);

        var campaign = rules.Campaign;
        return new BuildsOverviewDto(
            level.Value,
            enemyArmorClass.Value,
            (int)maxTier,
            DisplayText.TierName(maxTier),
            rules.GetSpecies(campaign.DefaultSpecies).Name,
            rules.GetFeat(campaign.OverviewExtraOriginFeat).Name,
            [.. rows.Select(r => r with
            {
                TopArmorClass = r.ArmorClass == maxArmor,
                TopHitPoints = r.HitPoints == maxHitPoints,
                TopWisdomSave = r.WisdomSave == maxWisdom,
                TopConstitutionSave = r.ConstitutionSave == maxConstitution,
                TopInitiative = r.Initiative == maxInitiative,
                TopExpected = r.BestExpected.Equals(maxExpected),
            })]);
    }

    private static OverviewRowDto Row(CharacterSheet sheet, ResourceTier maxTier)
    {
        var profile = sheet.Build.Archetype.Profile;
        var best = sheet.Best(maxTier);
        return new OverviewRowDto
        {
            ArchetypeId = profile.Id.Value,
            Name = profile.Name,
            Status = DisplayText.Status(profile.Status),
            ArmorClass = sheet.Armor.Value.Value,
            HitPoints = sheet.HitPoints,
            WisdomSave = sheet.SavingThrows[Ability.Wisdom].Modifier,
            ConstitutionSave = sheet.SavingThrows[Ability.Constitution].Modifier,
            Initiative = sheet.Initiative,
            CharmChance = sheet.Charm.WithResources.Value,
            BestExpected = best?.Expected ?? 0,
            BestRoutine = best?.Name ?? string.Empty,
            Milestone = profile.Milestones.FirstOrDefault(m => m.Level == sheet.Level.Value)?.Text ?? string.Empty,
        };
    }
}
