using DndMaker.Content.Catalog;
using DndMaker.Domain.Abilities;
using DndMaker.Domain.Analysis;
using DndMaker.Domain.Builds;
using DndMaker.Domain.Combat;
using DndMaker.Domain.Effects;
using DndMaker.Domain.Routines;
using DndMaker.Domain.Rulesets;

namespace DndMaker.Content.Campaigns;

/// <summary>Curse of Strahd, níveis 1 a 10, com as regras de 2024 e os livros liberados na mesa.</summary>
public static class CurseOfStrahd
{
    /// <summary>CD do charme de Strahd (ação Charm, teste de Sabedoria, ficha de 2016).</summary>
    public const int CharmDc = 17;

    public static Campaign Campaign { get; } = new()
    {
        Name = "Curse of Strahd",
        CharmThreat = new ConditionThreat("Charme de Strahd", Ability.Wisdom, new DifficultyClass(CharmDc), Condition.Charmed),
        MaxLevel = 10,
        HighlightLevels = [5, 6],
        Phases =
        [
            new(1, 3, "Death House"),
            new(4, 6, "Barovia e Vallaki"),
            new(7, 8, "Krezk, Argynvostholt, Berez"),
            new(9, 10, "Amber Temple e Castle Ravenloft"),
        ],
        LevelRemarks =
        [
            new(5, 6, "janela que você quer forte"),
            new(7, 10, "final da campanha"),
        ],
        EnemyArmorClasses =
        [
            new(new ArmorClass(13), "13 (zumbis, lobos)"),
            new(new ArmorClass(15), "15 (típico)"),
            new(new ArmorClass(16), "16 (Strahd)"),
            new(new ArmorClass(18), "18 (blindado)"),
        ],
        DefaultEnemyArmorClass = new ArmorClass(15),
        DefaultSpecies = new("humano"),
        DefaultExtraOriginFeat = FeatCatalog.Alert.Id,
        OverviewExtraOriginFeat = FeatCatalog.Lucky.Id,
        Preset = new ComparisonPreset(
            Level: 5,
            MaxTier: ResourceTier.Concentration,
            Slots:
            [
                new(new("fwl"), new("defense"), new("humano"), new("haunted"), FeatCatalog.MagicInitiateWizard.Id),
                new(new("valor"), new("staff"), new("humano"), new("artista"), FeatCatalog.MagicInitiateDruid.Id),
                new(new("pal"), new("dueling"), new("humano"), new("nobre"), FeatCatalog.Alert.Id),
            ]),
        Notes = CurseOfStrahdNotes.All,
    };

    public static Ruleset Ruleset { get; } = new(
        Campaign,
        ArchetypeCatalog.All,
        SpeciesCatalog.All,
        BackgroundCatalog.All,
        FeatCatalog.All,
        FeatCatalog.ExtraOriginChoices,
        [LevelChoice.AbilityScoreImprovement, .. FeatCatalog.LevelChoices.Select(LevelChoice.ForFeat)]);
}
