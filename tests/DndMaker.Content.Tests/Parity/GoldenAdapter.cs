using DndMaker.Content.Campaigns;
using DndMaker.Content.Tests.Golden;
using DndMaker.Domain.Abilities;
using DndMaker.Domain.Analysis;
using DndMaker.Domain.Builds;
using DndMaker.Domain.Catalog;
using DndMaker.Domain.Combat;
using DndMaker.Domain.Progression;
using DndMaker.Domain.Routines;
using DndMaker.Domain.Rulesets;

namespace DndMaker.Content.Tests.Parity;

/// <summary>
/// Traduz entre o golden (ids em texto do logic.js) e o domínio C#:
/// monta o personagem a partir da entrada e projeta a ficha no formato da saída.
/// </summary>
internal static class GoldenAdapter
{
    /// <summary>Id que o logic.js usa para o aumento de atributo.</summary>
    public const string AbilityScoreImprovementId = "asi";

    private static readonly Ruleset Rules = CurseOfStrahd.Ruleset;
    private static readonly CharacterAnalyzer Analyzer = new(CurseOfStrahd.Campaign.CharmThreat);

    public static CharacterSheet Analyze(GoldenInput input) =>
        Analyzer.Analyze(ToBuild(input), Level.Of(input.L), new CombatAssumptions(new ArmorClass(input.TAc)));

    public static CharacterBuild ToBuild(GoldenInput input)
    {
        var archetype = Rules.GetArchetype(new ArchetypeId(input.B));
        var variant = input.V is null ? null : archetype.FindVariant(new VariantId(input.V));
        var build = CharacterBuild.Create(
            archetype,
            Rules.GetSpecies(new SpeciesId(input.Sp)),
            Rules.GetBackground(new BackgroundId(input.Bg)),
            Rules.GetFeat(new FeatId(input.Hf)),
            variant);

        foreach (var (level, choice) in input.G)
        {
            build = build.WithLevelChoice(level, ToChoice(choice));
        }

        return build;
    }

    public static GoldenOutput Project(CharacterSheet sheet) => new(
        V: sheet.Variant?.Id.Value,
        Pb: sheet.ProficiencyBonus,
        BgApplied: sheet.Allocation.BackgroundBonus.ToString(),
        Sc: [.. AbilityList.All.Select(a => sheet.Scores[a].Value)],
        M: [.. AbilityList.All.Select(a => sheet.Modifiers[a])],
        Hp: sheet.HitPoints,
        Ac: new GoldenArmor(sheet.Armor.Value.Value, sheet.Armor.Description, sheet.Armor.StealthDisadvantage, sheet.Armor.Extra ?? string.Empty),
        Init: sheet.Initiative,
        Dark: new GoldenDarkvision(sheet.Darkvision.Range.Value, sheet.Darkvision.Note ?? string.Empty),
        Sb: sheet.SaveBonus,
        Saves: [.. AbilityList.All.Select(a => sheet.SavingThrows[a]).Select(s => new GoldenSave(s.Modifier, s.Proficient, s.Advantage))],
        Strahd: new GoldenCharm(sheet.Charm.WithResources.Value, sheet.Charm.Advantage, !sheet.Charm.Resources.IsEmpty),
        Conc: sheet.Concentration is { } c ? new GoldenConcentration(c.Success.Value, c.Advantage) : null,
        Feats: [.. sheet.Feats.Select(f => f.Id.Value).Order(StringComparer.Ordinal)],
        Picks: [.. sheet.Allocation.Picks.Select(p => new GoldenPick(p.Level, ChoiceId(p.Choice), p.ChangesText))],
        ShHint: sheet.VariantRequirementMissing,
        Routines: [.. sheet.Routines.Select(Project)],
        Best: [.. Enum.GetValues<ResourceTier>().Select(t => sheet.Best(t)?.Name ?? string.Empty)]);

    private static GoldenRoutine Project(RoutineEstimate estimate) => new(
        N: estimate.Name,
        Tier: (int)estimate.Tier,
        Multi: estimate.Routine.MultipleTargets,
        Note: estimate.Routine.Note,
        F: estimate.Formula,
        Hit: estimate.PrimaryAttackBonus?.Value,
        Dc: estimate.PrimaryDc?.Value,
        Raw: estimate.Raw,
        Exp: estimate.Expected);

    private static LevelChoice ToChoice(string id) =>
        id == AbilityScoreImprovementId
            ? LevelChoice.AbilityScoreImprovement
            : LevelChoice.ForFeat(Rules.GetFeat(new FeatId(id)));

    private static string ChoiceId(LevelChoice choice) => choice switch
    {
        FeatChoice feat => feat.Feat.Id.Value,
        _ => AbilityScoreImprovementId,
    };
}
