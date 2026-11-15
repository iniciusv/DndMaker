using System.Collections.Immutable;
using DndMaker.Domain.Analysis;
using DndMaker.Domain.Catalog;
using DndMaker.Domain.Combat;
using DndMaker.Domain.Progression;
using DndMaker.Domain.Routines;

namespace DndMaker.Domain.Rulesets;

/// <summary>Um trecho da campanha, por exemplo "Death House" nos níveis 1 a 3.</summary>
public sealed record CampaignPhase(int FromLevel, int ToLevel, string Name)
{
    public bool Contains(int level) => level >= FromLevel && level <= ToLevel;
}

/// <summary>Observação que a tela mostra para uma faixa de níveis ("janela que você quer forte").</summary>
public sealed record LevelRemark(int FromLevel, int ToLevel, string Text)
{
    public bool Contains(int level) => level >= FromLevel && level <= ToLevel;
}

/// <summary>Uma CA de inimigo oferecida na tela, com o exemplo que a explica.</summary>
public sealed record EnemyArmorClassOption(ArmorClass Value, string Label);

/// <summary>Um personagem da comparação inicial. Sem antecedente, usa o sugerido pela build.</summary>
public sealed record SlotPreset(
    ArchetypeId Archetype,
    VariantId? Variant,
    SpeciesId Species,
    BackgroundId? Background,
    FeatId? ExtraOriginFeat);

/// <summary>A comparação que aparece na primeira vez que o programa abre.</summary>
public sealed record ComparisonPreset(int Level, ResourceTier MaxTier, ImmutableArray<SlotPreset> Slots);

/// <summary>Um item das notas da campanha. <see cref="Lead"/> aparece em destaque antes do texto.</summary>
public sealed record NoteItem(string Text)
{
    public string? Lead { get; init; }

    public Uri? Link { get; init; }
}

public sealed record NoteSection(string Title, ImmutableArray<NoteItem> Items);

/// <summary>
/// O que a campanha decide sobre a comparação: a ameaça de charme, os níveis jogados, as CAs de inimigo,
/// a comparação inicial e as notas da página. O <see cref="Ruleset"/> confere a consistência.
/// </summary>
public sealed record Campaign
{
    public required string Name { get; init; }

    /// <summary>O charme que a comparação mede (no Curse of Strahd, o de Strahd: SAB, CD 17).</summary>
    public required ConditionThreat CharmThreat { get; init; }

    public required int MaxLevel { get; init; }

    /// <summary>Os níveis em que a campanha mais pesa e que a tela destaca.</summary>
    public ImmutableArray<int> HighlightLevels { get; init; } = [];

    public ImmutableArray<CampaignPhase> Phases { get; init; } = [];

    public ImmutableArray<LevelRemark> LevelRemarks { get; init; } = [];

    public required ImmutableArray<EnemyArmorClassOption> EnemyArmorClasses { get; init; }

    public required ArmorClass DefaultEnemyArmorClass { get; init; }

    public required ComparisonPreset Preset { get; init; }

    /// <summary>Espécie de quem entra na comparação com as escolhas padrão.</summary>
    public required SpeciesId DefaultSpecies { get; init; }

    /// <summary>Talento extra de quem entra na comparação com as escolhas padrão.</summary>
    public required FeatId DefaultExtraOriginFeat { get; init; }

    /// <summary>Talento extra usado nas tabelas com todas as builds.</summary>
    public required FeatId OverviewExtraOriginFeat { get; init; }

    public ImmutableArray<NoteSection> Notes { get; init; } = [];

    public IEnumerable<Level> Levels => Level.Range(Level.Min, MaxLevel);

    public bool IsHighlight(int level) => HighlightLevels.Contains(level);

    public CampaignPhase? PhaseAt(int level) => Phases.FirstOrDefault(p => p.Contains(level));

    public LevelRemark? RemarkAt(int level) => LevelRemarks.FirstOrDefault(r => r.Contains(level));
}
