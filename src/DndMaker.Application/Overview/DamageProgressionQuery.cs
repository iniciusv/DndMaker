using DndMaker.Domain.Analysis;
using DndMaker.Domain.Combat;
using DndMaker.Domain.Routines;
using DndMaker.Domain.Rulesets;

namespace DndMaker.Application.Overview;

/// <summary>Dano esperado do melhor turno de uma build em cada nível (índice 0 = nível 1).</summary>
public sealed record DamageRowDto(string ArchetypeId, string Name, IReadOnlyList<double> Expected);

/// <summary>A grade "Dano esperado em cada nível". <see cref="Max"/> serve para colorir as células.</summary>
public sealed record DamageProgressionDto(
    IReadOnlyList<int> Levels,
    IReadOnlyList<int> HighlightLevels,
    int EnemyArmorClass,
    int MaxTier,
    double Max,
    IReadOnlyList<DamageRowDto> Rows);

/// <summary>Todas as builds, com as escolhas padrão, em todos os níveis da campanha.</summary>
public sealed class DamageProgressionQuery(Ruleset rules, CharacterAnalyzer analyzer)
{
    public DamageProgressionDto Execute(ArmorClass enemyArmorClass, ResourceTier maxTier)
    {
        var campaign = rules.Campaign;
        var assumptions = new CombatAssumptions(enemyArmorClass);
        var levels = campaign.Levels.ToList();

        var rows = rules.Archetypes.Select(archetype =>
        {
            var build = rules.OverviewCharacter(archetype);
            var expected = levels.Select(level => analyzer.Analyze(build, level, assumptions).Best(maxTier)?.Expected ?? 0).ToList();
            return new DamageRowDto(archetype.Id.Value, archetype.Name, expected);
        }).ToList();

        return new DamageProgressionDto(
            [.. levels.Select(l => l.Value)],
            [.. campaign.HighlightLevels],
            enemyArmorClass.Value,
            (int)maxTier,
            rows.SelectMany(r => r.Expected).DefaultIfEmpty(0).Max(),
            rows);
    }
}
