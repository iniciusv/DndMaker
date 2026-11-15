using System.Collections.Immutable;
using DndMaker.Content.Archetypes;
using DndMaker.Domain.Builds;

namespace DndMaker.Content.Catalog;

/// <summary>
/// As builds na ordem da página original (finalistas primeiro). Os paladinos Ancients e Devotion foram
/// acrescentados depois, com as builds em análise, e não têm golden.
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
        new DevotionPaladin(),
        new ShadowSorcerer(),
        new BattleMasterFighter(),
    ];
}
