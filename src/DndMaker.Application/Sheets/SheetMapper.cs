using DndMaker.Domain.Abilities;
using DndMaker.Domain.Analysis;
using DndMaker.Domain.Routines;

namespace DndMaker.Application.Sheets;

/// <summary>Converte a ficha do domínio no que a carta mostra.</summary>
public static class SheetMapper
{
    public static SheetDto ToDto(CharacterSheet sheet, ResourceTier maxTier)
    {
        var build = sheet.Build;
        var profile = build.Archetype.Profile;
        var level = sheet.Level.Value;
        var best = sheet.Best(maxTier);

        return new SheetDto
        {
            ArchetypeId = profile.Id.Value,
            Name = profile.Name,
            Epithet = profile.Epithet,
            Summary = profile.Summary,
            Status = DisplayText.Status(profile.Status),
            VariantId = sheet.Variant?.Id.Value,
            Level = level,
            ProficiencyBonus = sheet.ProficiencyBonus,
            ArmorClass = sheet.Armor.Value.Value,
            ArmorDescription = sheet.Armor.Description,
            StealthDisadvantage = sheet.Armor.StealthDisadvantage,
            ArmorExtra = sheet.Armor.Extra,
            HitPoints = sheet.HitPoints,
            Initiative = sheet.Initiative,
            DarkvisionMeters = sheet.Darkvision.Range.Value,
            DarkvisionNote = sheet.Darkvision.Note,
            DarkvisionNeedsConfirmation = build.Species.DarkvisionNeedsConfirmation && sheet.Darkvision.Note is null,
            Abilities = [.. AbilityList.All.Select(a => new AbilityDto(a.Abbreviation(), sheet.Scores[a].Value, sheet.Modifiers[a]))],
            BackgroundBonus = sheet.Allocation.BackgroundBonus.ToString(),
            Picks = [.. sheet.Allocation.Picks.Select(Pick)],
            Saves = [.. AbilityList.All.Select(a => Save(a, sheet.SavingThrows[a]))],
            SaveBonus = sheet.SaveBonus,
            SaveBonusSources = [.. sheet.SaveBonusSources],
            Charm = Charm(sheet.Charm),
            Concentration = sheet.Concentration is { } concentration ? Concentration(concentration) : null,
            Routines = [.. sheet.Routines.Select(r => Routine(r, maxTier, ReferenceEquals(r, best)))],
            BestRoutine = best is null ? null : Routine(best, maxTier, isBest: true),
            VariantRequirementMissing = sheet.VariantRequirementMissing,
            Milestones = [.. profile.Milestones.Select(m => new MilestoneDto(m.Level, m.Text, Time(m.Level, level)))],
            Species = new DescriptionDto(build.Species.Name, build.Species.Description),
            SpeciesSource = build.Species.Source,
            Feats = [.. sheet.Feats.Where(f => f.Description.Length > 0).Select(f => new DescriptionDto(f.Name, f.Description))],
            BackgroundSkills = build.Background.Skills,
            Pros = profile.Pros,
            Cons = profile.Cons,
        };
    }

    public static RoutineDto Routine(RoutineEstimate estimate, ResourceTier maxTier, bool isBest) => new(
        estimate.Name,
        estimate.Tier,
        DisplayText.TierName(estimate.Tier),
        estimate.Routine.MultipleTargets,
        estimate.Routine.Note,
        estimate.Formula,
        estimate.Raw,
        estimate.Expected,
        estimate.PrimaryAttackBonus?.Value,
        estimate.PrimaryDc?.Value,
        estimate.Tier <= maxTier,
        isBest)
    {
        AttackBonuses = [.. estimate.AttackBonuses.Select(b => b.Value)],
        Situation = estimate.Routine.Situation is { } situation ? DisplayText.Situation(situation) : null,
    };

    private static PickDto Pick(LevelPick pick) =>
        new(pick.Level, LevelChoiceIds.Of(pick.Choice), DisplayText.LevelChoiceName(pick.Choice), pick.ChangesText);

    private static SaveDto Save(Ability ability, SavingThrow save) =>
        new(ability.Abbreviation(), save.Modifier, save.Proficient, save.Advantage);

    private static ResistanceDto Charm(ThreatResistance charm) => new(
        charm.Threat.Ability.Abbreviation(),
        charm.Threat.Dc.Value,
        charm.WithResources.Value,
        charm.Passive.Value,
        charm.Advantage,
        [.. charm.Resources],
        [.. charm.Remedies])
    {
        ImmunitySource = charm.ImmunitySource,
    };

    private static ResistanceDto Concentration(ConcentrationResult concentration) => new(
        Ability.Constitution.Abbreviation(),
        DefenseCalculator.ConcentrationDc,
        concentration.Success.Value,
        concentration.Success.Value,
        concentration.Advantage,
        [],
        []);

    private static MilestoneTime Time(int milestoneLevel, int level) =>
        milestoneLevel == level ? MilestoneTime.Current
        : milestoneLevel < level ? MilestoneTime.Past
        : MilestoneTime.Future;
}
