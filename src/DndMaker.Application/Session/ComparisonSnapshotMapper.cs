using DndMaker.Domain.Builds;
using DndMaker.Domain.Catalog;
using DndMaker.Domain.Comparison;
using DndMaker.Domain.Progression;
using DndMaker.Domain.Routines;
using DndMaker.Domain.Rulesets;

namespace DndMaker.Application.Session;

/// <summary>
/// Converte a comparação de e para o formato gravado. Ao carregar, limpa o que não vale mais:
/// ids que sumiram do conteúdo, níveis e CAs fora das opções, escolhas em níveis sem talento.
/// O que não dá para aproveitar volta ao padrão da campanha, sem lançar exceção.
/// </summary>
public sealed class ComparisonSnapshotMapper(Ruleset rules)
{
    public static ComparisonSnapshot ToSnapshot(BuildComparison comparison) => new(
        comparison.Level.Value,
        comparison.Assumptions.TargetArmorClass.Value,
        (int)comparison.MaxTier,
        [.. comparison.Slots.Select(ToSnapshot)]);

    public static SlotSnapshot ToSnapshot(CharacterBuild build) => new(
        build.Archetype.Id.Value,
        build.Variant?.Id.Value,
        build.Species.Id.Value,
        build.Background.Id.Value,
        build.ExtraOriginFeat?.Id.Value,
        build.LevelChoices.ToDictionary(kv => kv.Key, kv => LevelChoiceIds.Of(kv.Value)));

    /// <summary>A comparação gravada, já limpa; sem nada gravado (ou com algo ilegível), a comparação inicial.</summary>
    public BuildComparison FromSnapshot(ComparisonSnapshot? snapshot)
    {
        var fallback = rules.CreateDefaultComparison();
        if (snapshot?.Slots is not { Count: BuildComparison.SlotCount } slots)
        {
            return fallback;
        }

        var campaign = rules.Campaign;
        var level = snapshot.Level >= Level.Min && snapshot.Level <= campaign.MaxLevel ? Level.Of(snapshot.Level) : fallback.Level;
        var armorClass = campaign.EnemyArmorClasses.Select(o => o.Value).FirstOrDefault(
            ac => ac.Value == snapshot.EnemyArmorClass, campaign.DefaultEnemyArmorClass);
        var tier = Enum.IsDefined((ResourceTier)snapshot.MaxTier) ? (ResourceTier)snapshot.MaxTier : fallback.MaxTier;

        return BuildComparison.Create(
            slots.Select((slot, i) => FromSlot(slot) ?? fallback.Slots[i]),
            level,
            new CombatAssumptions(armorClass),
            tier);
    }

    /// <summary>O personagem gravado, ou nulo se a build não existir mais.</summary>
    public CharacterBuild? FromSlot(SlotSnapshot? slot)
    {
        if (slot?.Archetype is null || rules.FindArchetype(new ArchetypeId(slot.Archetype)) is not { } archetype)
        {
            return null;
        }

        var defaults = rules.DefaultCharacter(archetype);
        var variant = Id(slot.Variant, s => archetype.FindVariant(new VariantId(s))) ?? defaults.Variant;
        var species = Id(slot.Species, s => rules.FindSpecies(new SpeciesId(s))) ?? defaults.Species;
        var background = Id(slot.Background, s => rules.FindBackground(new BackgroundId(s))) ?? defaults.Background;
        var extra = Id(slot.ExtraOriginFeat, s => rules.ExtraOriginFeats.FirstOrDefault(f => f.Id.Value == s)) ?? defaults.ExtraOriginFeat;

        var build = CharacterBuild.Create(archetype, species, background, extra, variant);
        var featLevels = archetype.FeatLevels(rules.Campaign.MaxLevel);
        foreach (var (featLevel, choiceId) in slot.LevelChoices ?? new Dictionary<int, string>())
        {
            if (featLevels.Contains(featLevel) && choiceId is not null && LevelChoiceIds.Find(rules, choiceId) is { } choice)
            {
                build = build.WithLevelChoice(featLevel, choice);
            }
        }

        return build;
    }

    private static T? Id<T>(string? id, Func<string, T?> find)
        where T : class =>
        string.IsNullOrWhiteSpace(id) ? null : find(id);
}
