using System.Collections.Immutable;
using DndMaker.Content.Archetypes;
using DndMaker.Domain.Builds;

namespace DndMaker.Content.Catalog;

/// <summary>
/// As builds na ordem da página original (finalistas primeiro). O Paladino (Ancients) foi acrescentado depois,
/// com as builds em análise, e não tem golden.
/// </summary>
public static class ArchetypeCatalog
{
    public static ImmutableArray<BuildArchetype> All { get; } =
    [
        new FighterWarlockBlade(),
        new ValorBard(),
        new WatchersPaladin(),
        new TwilightCleric(),
        new GloomStalkerRanger(),
        new EldritchWarlock(),
        new SwordsBard(),
        new FighterValorBard(),
        new AncientsPaladin(),
        new ShadowSorcerer(),
        new BattleMasterFighter(),
    ];
}
