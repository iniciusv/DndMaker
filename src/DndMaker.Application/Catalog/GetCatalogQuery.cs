using DndMaker.Domain.Abilities;
using DndMaker.Domain.Builds;
using DndMaker.Domain.Catalog;
using DndMaker.Domain.Routines;
using DndMaker.Domain.Rulesets;

namespace DndMaker.Application.Catalog;

/// <summary>As opções dos seletores. O pacote de regras é imutável, então o resultado é calculado uma vez.</summary>
public sealed class GetCatalogQuery(Ruleset rules)
{
    private readonly Lazy<CatalogDto> _catalog = new(() => Build(rules));

    public CatalogDto Execute() => _catalog.Value;

    private static CatalogDto Build(Ruleset rules)
    {
        var campaign = rules.Campaign;
        return new CatalogDto(
            campaign.Name,
            campaign.MaxLevel,
            [.. campaign.HighlightLevels],
            [.. campaign.Phases.Select(p => new PhaseDto(p.FromLevel, p.ToLevel, p.Name))],
            [.. campaign.EnemyArmorClasses.Select(o => new EnemyArmorClassDto(o.Value.Value, o.Label))],
            campaign.DefaultEnemyArmorClass.Value,
            [.. Enum.GetValues<ResourceTier>().Select(t => new TierOptionDto((int)t, DisplayText.TierName(t), DisplayText.TierFilter(t)))],
            [.. rules.Archetypes.Select(a => Archetype(a, campaign.MaxLevel))],
            [.. rules.Species.Select(s => new SpeciesOptionDto(s.Id.Value, s.Name, s.Source, s.GrantsExtraOriginFeat))],
            [.. rules.Backgrounds.Select(Background)],
            [.. rules.ExtraOriginFeats.Select(f => new OptionDto(f.Id.Value, f.Name))],
            [.. rules.LevelChoices.Select(c => new OptionDto(LevelChoiceIds.Of(c), DisplayText.LevelChoiceName(c)))],
            [.. campaign.Notes.Select(n => new NoteSectionDto(n.Title, [.. n.Items.Select(i => new NoteItemDto(i.Lead, i.Text, i.Link))]))]);
    }

    private static ArchetypeOptionDto Archetype(BuildArchetype archetype, int maxLevel) => new(
        archetype.Id.Value,
        archetype.Name,
        DisplayText.Status(archetype.Profile.Status),
        archetype.Profile.Status == ArchetypeStatus.Finalist,
        [.. archetype.Variants.Select(v => new OptionDto(v.Id.Value, v.Name))],
        [.. archetype.FeatLevels(maxLevel)]);

    private static BackgroundOptionDto Background(Background background)
    {
        var abilities = string.Join(", ", background.Abilities.Members().Select(a => a.Abbreviation()));
        return new BackgroundOptionDto(
            background.Id.Value,
            background.Name,
            $"{background.Name} · {abilities} · {background.OriginFeat.Name}");
    }
}
