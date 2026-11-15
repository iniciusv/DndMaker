using DndMaker.Application.Comparison;
using DndMaker.Application.Sheets;
using DndMaker.Desktop.Formatting;
using DndMaker.Domain.Routines;

namespace DndMaker.Desktop.ViewModels;

/// <summary>Faixa de uma barra de probabilidade: verde a partir de 60%, âmbar a partir de 40%.</summary>
internal enum MeterLevel
{
    Low,
    Mid,
    Good,
}

internal sealed record MeterViewModel(string Label, double Chance, string Hint, bool Advantage)
{
    public string PercentText => Format.Percent(Chance);

    public double Percent => Math.Round(Chance * 100);

    public MeterLevel Level => Chance >= 0.6 ? MeterLevel.Good : Chance >= 0.4 ? MeterLevel.Mid : MeterLevel.Low;

    public bool IsGood => Level == MeterLevel.Good;

    public bool IsMid => Level == MeterLevel.Mid;

    public string Caption => (Advantage ? "Com vantagem. " : string.Empty) + Hint;
}

internal sealed record StatTileViewModel(string Value, string Label, bool IsLeader);

internal sealed record AbilityCellViewModel(string Abbreviation, string Value, string Detail, bool Emphasis);

internal sealed record RoutineViewModel(RoutineDto Routine)
{
    public string Name => Routine.Name;

    public string ExpectedText => Format.Decimal(Routine.Expected);

    public string Formula => Routine.Formula;

    /// <summary><c>= 21 se tudo acertar · ataque +7 · CD 15</c>.</summary>
    public string Details
    {
        get
        {
            var parts = new List<string> { $"= {Format.Decimal(Routine.Raw)} se tudo acertar" };
            if (Routine.AttackBonuses.Count > 1)
            {
                // Problema 3 do plano: com dois bônus diferentes, mostra os dois.
                parts.Add("ataques " + string.Join(" e ", Routine.AttackBonuses.Select(Format.Signed)));
            }
            else if (Routine.AttackBonus is { } bonus)
            {
                parts.Add("ataque " + Format.Signed(bonus));
            }

            if (Routine.Dc is { } dc)
            {
                parts.Add("CD " + Format.Number(dc));
            }

            return string.Join(" · ", parts);
        }
    }

    public string TierName => Routine.TierName;

    public bool IsConcentration => Routine.Tier == ResourceTier.Concentration;

    public bool IsPeak => Routine.Tier == ResourceTier.Peak;

    public bool MultipleTargets => Routine.MultipleTargets;

    public string? Situation => Routine.Situation;

    public bool HasSituation => Routine.Situation is not null;

    public string Note => Routine.Note;

    public bool HasNote => Routine.Note.Length > 0;

    public bool Counted => Routine.Counted;

    public bool IsBest => Routine.IsBest;
}

internal sealed record MilestoneViewModel(int Level, string Text, MilestoneTime Time)
{
    public string LevelText => Format.Number(Level);

    public bool IsCurrent => Time == MilestoneTime.Current;

    public bool IsFuture => Time == MilestoneTime.Future;
}

/// <summary>Um item de lista com começo em destaque: "<b>Lead</b> texto".</summary>
internal sealed record LabeledTextViewModel(string Lead, string Text);

/// <summary>A parte de resultados de uma carta, com os textos já montados como na página.</summary>
internal sealed class SheetViewModel
{
    public SheetViewModel(SheetDto sheet, int index, LeadersDto leaders)
    {
        Sheet = sheet;
        Numeral = Format.Roman(index);
        Tiles =
        [
            new(Format.Number(sheet.ArmorClass), "CA", leaders.ArmorClass == index),
            new(Format.Number(sheet.HitPoints), "PV", leaders.HitPoints == index),
            new(Format.Signed(sheet.Initiative), "Iniciativa", leaders.Initiative == index),
            new(sheet.DarkvisionMeters > 0 ? $"{Format.Number(sheet.DarkvisionMeters)} m" : "não", "Visão no escuro", false),
        ];
        Abilities = [.. sheet.Abilities.Select(a => new AbilityCellViewModel(a.Abbreviation, Format.Number(a.Score), Format.Signed(a.Modifier), false))];
        Saves = [.. sheet.Saves.Select(s => new AbilityCellViewModel(
            s.Abbreviation,
            Format.Signed(s.Modifier),
            SaveDetail(s),
            s.Proficient))];
        Charm = new MeterViewModel(
            "Resiste ao charme de Strahd",
            sheet.Charm.Chance,
            CharmHint(sheet.Charm),
            sheet.Charm.Advantage);
        Concentration = sheet.Concentration is { } concentration
            ? new MeterViewModel(
                "Mantém a concentração",
                concentration.Chance,
                $"Teste de {Format.AbilityName(concentration.Ability)}, CD {concentration.Dc}, ao sofrer dano.",
                concentration.Advantage)
            : null;
        Routines = [.. sheet.Routines.Select(r => new RoutineViewModel(r))];
        Milestones = [.. sheet.Milestones.Select(m => new MilestoneViewModel(m.Level, m.Text, m.Time))];
        Picks = [.. sheet.Picks.Select(p => new LabeledTextViewModel(
            $"Nível {p.Level}, {p.ChoiceName}:",
            (p.Changes.Length > 0 ? p.Changes : "não muda atributos") + "."))];
        SpeciesAndFeats =
        [
            new($"{sheet.Species.Name}", $"({sheet.SpeciesSource}): {sheet.Species.Description}"),
            .. sheet.Feats.Select(f => new LabeledTextViewModel(f.Name + ":", f.Description)),
            new("Perícias do antecedente:", sheet.BackgroundSkills + "."),
        ];
        Balance =
        [
            new("A favor:", sheet.Pros),
            new("Contra:", sheet.Cons),
        ];
    }

    public SheetDto Sheet { get; }

    public string Numeral { get; }

    public string Epithet => Sheet.Epithet;

    public string Name => Sheet.Name;

    public string Status => Sheet.Status;

    public bool IsFinalist => Sheet.Status == "finalista";

    public bool IsDiscarded => Sheet.Status == "descartada";

    public string Summary => Sheet.Summary;

    public IReadOnlyList<StatTileViewModel> Tiles { get; }

    /// <summary>Armadura, Furtividade, extras e visão no escuro, na mesma frase da página.</summary>
    public string ArmorText
    {
        get
        {
            var text = Sheet.ArmorDescription + (Sheet.StealthDisadvantage ? " (desvantagem em Furtividade)" : string.Empty) + ".";
            if (!string.IsNullOrEmpty(Sheet.ArmorExtra))
            {
                text += $" {Sheet.ArmorExtra}.";
            }

            if (!string.IsNullOrEmpty(Sheet.DarkvisionNote))
            {
                text += $" {Sheet.DarkvisionNote}.";
            }
            else if (Sheet.DarkvisionNeedsConfirmation)
            {
                text += " Visão no escuro da espécie a confirmar no livro.";
            }

            return text;
        }
    }

    public IReadOnlyList<AbilityCellViewModel> Abilities { get; }

    public string BackgroundText => $"Antecedente: {Sheet.BackgroundBonus}.";

    public IReadOnlyList<LabeledTextViewModel> Picks { get; }

    public bool HasPicks => Picks.Count > 0;

    public IReadOnlyList<AbilityCellViewModel> Saves { get; }

    public string SavesText =>
        "Borda escura e \"prof.\" marcam proficiência; \"vant.\" marca vantagem da espécie."
        + (Sheet.SaveBonus > 0 ? $" Inclui +{Sheet.SaveBonus} da {string.Join(" e ", Sheet.SaveBonusSources)}." : string.Empty);

    public MeterViewModel Charm { get; }

    public MeterViewModel? Concentration { get; }

    public bool HasConcentration => Concentration is not null;

    public IReadOnlyList<RoutineViewModel> Routines { get; }

    public bool ShowShillelaghHint => Sheet.VariantRequirementMissing;

    public IReadOnlyList<MilestoneViewModel> Milestones { get; }

    public IReadOnlyList<LabeledTextViewModel> SpeciesAndFeats { get; }

    public IReadOnlyList<LabeledTextViewModel> Balance { get; }

    private static string SaveDetail(SaveDto save)
    {
        var parts = new List<string>();
        if (save.Proficient)
        {
            parts.Add("prof.");
        }

        if (save.Advantage)
        {
            parts.Add("vant.");
        }

        return parts.Count > 0 ? string.Join(" ", parts) : " ";
    }

    /// <summary>O que cada recurso gasta, para a explicação da conta.</summary>
    private static readonly Dictionary<string, string> ResourceCosts = new(StringComparer.Ordinal)
    {
        ["Countercharm"] = "Countercharm (reação)",
        ["Lucky"] = "Lucky (ponto de sorte)",
        ["Survivor"] = "Survivor (1x por descanso longo)",
        ["Watcher's Will"] = "Watcher's Will (Canalizar Divindade)",
    };

    /// <summary>
    /// Problema 1 do plano: a chance mostrada gasta os recursos da ficha; a explicação diz quais e quanto seria
    /// sem gastar nada. O que encerra o charme depois (Twilight Sanctuary) aparece à parte.
    /// </summary>
    private static string CharmHint(ResistanceDto charm)
    {
        if (charm.ImmunitySource is { } immunity)
        {
            return $"Imune a Enfeitiçado pela {immunity}: o teste de {Format.AbilityName(charm.Ability)}, CD {charm.Dc}, nem é feito.";
        }

        var hint = $"Teste de {Format.AbilityName(charm.Ability)}, CD {charm.Dc} (ficha de 2016).";
        if (charm.Resources.Count > 0)
        {
            var resources = charm.Resources.Select(r => ResourceCosts.TryGetValue(r, out var cost) ? cost : r);
            hint += $" Gastando {string.Join(", ", resources)}; sem gastar nada, {Format.Percent(charm.ChanceWithoutResources)}.";
        }

        if (charm.Remedies.Count > 0)
        {
            hint += $" {string.Join(" e ", charm.Remedies)} encerra o charme no fim do turno (fora da conta).";
        }

        return hint;
    }
}
