using DndMaker.Application.Sheets;
using DndMaker.Domain.Analysis;
using DndMaker.Domain.Rulesets;
using DcComparison = DndMaker.Domain.Comparison.BuildComparison;

namespace DndMaker.Application.Comparison;

/// <summary>O nível escolhido, com a fase da campanha e a observação que a tela mostra.</summary>
public sealed record LevelInfoDto(int Value, string? Phase, string? Remark, bool IsHighlight);

/// <summary>
/// Quem lidera em CA, PV e iniciativa: o índice da carta, ou nulo se houver empate no topo.
/// </summary>
public sealed record LeadersDto(int? ArmorClass, int? HitPoints, int? Initiative);

public sealed record ComparisonResultDto(
    LevelInfoDto Level,
    int EnemyArmorClass,
    int MaxTier,
    IReadOnlyList<SheetDto> Cards,
    LeadersDto Leaders);

/// <summary>Calcula as três cartas da comparação aberta.</summary>
public sealed class AnalyzeComparisonQuery(Ruleset rules, CharacterAnalyzer analyzer)
{
    public ComparisonResultDto Execute(DcComparison comparison)
    {
        var cards = comparison.Slots
            .Select(build => SheetMapper.ToDto(analyzer.Analyze(build, comparison.Level, comparison.Assumptions), comparison.MaxTier))
            .ToList();

        return new ComparisonResultDto(
            LevelInfo(rules.Campaign, comparison.Level.Value),
            comparison.Assumptions.TargetArmorClass.Value,
            (int)comparison.MaxTier,
            cards,
            new LeadersDto(
                UniqueLeader(cards, c => c.ArmorClass),
                UniqueLeader(cards, c => c.HitPoints),
                UniqueLeader(cards, c => c.Initiative)));
    }

    public static LevelInfoDto LevelInfo(Campaign campaign, int level) =>
        new(level, campaign.PhaseAt(level)?.Name, campaign.RemarkAt(level)?.Text, campaign.IsHighlight(level));

    /// <summary>Índice do maior valor, se só uma carta o tiver.</summary>
    private static int? UniqueLeader(IReadOnlyList<SheetDto> cards, Func<SheetDto, int> value)
    {
        var values = cards.Select(value).ToList();
        var max = values.Max();
        return values.Count(v => v == max) == 1 ? values.IndexOf(max) : null;
    }
}
