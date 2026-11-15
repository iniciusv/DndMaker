using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DndMaker.Application.Catalog;
using DndMaker.Application.Comparison;
using DndMaker.Application.Overview;
using DndMaker.Application.Session;
using DndMaker.Desktop.Formatting;
using DndMaker.Desktop.Services;
using DndMaker.Domain.Comparison;
using DndMaker.Domain.Routines;

namespace DndMaker.Desktop.ViewModels;

/// <summary>Um degrau da estrada de níveis.</summary>
internal sealed record LevelStepViewModel(int Value, bool IsHighlight)
{
    public string Text => Format.Number(Value);

    public string AccessibleName => IsHighlight ? $"Nível {Value}, janela de destaque" : $"Nível {Value}";
}

/// <summary>Rótulo de uma fase da campanha embaixo da estrada, ocupando as colunas dos seus níveis.</summary>
internal sealed record PhaseLabelViewModel(string Name, int Column, int Span);

/// <summary>
/// A janela inteira. Os seletores do topo e das cartas mandam mudanças para a <see cref="ComparisonSession"/>;
/// a cada <see cref="ComparisonSession.Changed"/>, tudo é recalculado a partir do estado dela.
/// </summary>
internal sealed partial class MainViewModel : ObservableObject
{
    private readonly ComparisonSession _session;
    private readonly AnalyzeComparisonQuery _analyze;
    private readonly BuildsOverviewQuery _overview;
    private readonly DamageProgressionQuery _damage;
    private (int ArmorClass, ResourceTier Tier, int Level)? _damageKey;
    private bool _syncing;

    public MainViewModel(
        GetCatalogQuery catalog,
        AnalyzeComparisonQuery analyze,
        BuildsOverviewQuery overview,
        DamageProgressionQuery damage,
        ComparisonSession session,
        ComparisonPersistence persistence,
        IUiDispatcher dispatcher)
    {
        _session = session;
        _analyze = analyze;
        _overview = overview;
        _damage = damage;

        Catalog = catalog.Execute();
        Levels = [.. Enumerable.Range(1, Catalog.MaxLevel).Select(l => new LevelStepViewModel(l, Catalog.HighlightLevels.Contains(l)))];
        Phases = [.. Catalog.Phases.Select(p => new PhaseLabelViewModel(p.Name, p.FromLevel - 1, p.ToLevel - p.FromLevel + 1))];
        Cards = [.. Enumerable.Range(0, BuildComparison.SlotCount).Select(i => new CharacterCardViewModel(i, Catalog, session))];
        Overview = new BuildsOverviewViewModel(BuildComparison.SlotCount, session.LoadDefaultCharacter);

        session.Changed += (_, _) => Refresh();
        persistence.SaveFailed += (_, ex) => dispatcher.Post(() => SaveError = $"Não foi possível gravar a comparação: {ex.Message}");
        Refresh();
    }

    public CatalogDto Catalog { get; }

    public string Eyebrow => $"{Catalog.CampaignName} · regras de 2024 · níveis 1 a {Catalog.MaxLevel}";

    public IReadOnlyList<LevelStepViewModel> Levels { get; }

    public IReadOnlyList<PhaseLabelViewModel> Phases { get; }

    public IReadOnlyList<EnemyArmorClassDto> EnemyArmorClasses => Catalog.EnemyArmorClasses;

    public IReadOnlyList<TierOptionDto> Tiers => Catalog.Tiers;

    public IReadOnlyList<CharacterCardViewModel> Cards { get; }

    public BuildsOverviewViewModel Overview { get; }

    public DamageGridViewModel DamageGrid { get; } = new();

    public IReadOnlyList<NoteSectionDto> Notes => Catalog.Notes;

    [ObservableProperty]
    public partial LevelStepViewModel? SelectedLevel { get; set; }

    [ObservableProperty]
    public partial string LevelText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string LevelCaption { get; set; } = string.Empty;

    [ObservableProperty]
    public partial EnemyArmorClassDto? SelectedEnemyArmorClass { get; set; }

    [ObservableProperty]
    public partial TierOptionDto? SelectedTier { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSaveError))]
    public partial string? SaveError { get; set; }

    public bool HasSaveError => SaveError is not null;

    [RelayCommand]
    private void NextLevel() => StepLevel(+1);

    [RelayCommand]
    private void PreviousLevel() => StepLevel(-1);

    [RelayCommand]
    private void DismissSaveError() => SaveError = null;

    private void StepLevel(int delta)
    {
        var level = Math.Clamp(_session.Current.Level.Value + delta, 1, Catalog.MaxLevel);
        if (level != _session.Current.Level.Value)
        {
            _session.SetLevel(level);
        }
    }

    partial void OnSelectedLevelChanged(LevelStepViewModel? value)
    {
        if (!_syncing && value is not null)
        {
            _session.SetLevel(value.Value);
        }
    }

    partial void OnSelectedEnemyArmorClassChanged(EnemyArmorClassDto? value)
    {
        if (!_syncing && value is not null)
        {
            _session.SetEnemyArmorClass(value.Value);
        }
    }

    partial void OnSelectedTierChanged(TierOptionDto? value)
    {
        if (!_syncing && value is not null)
        {
            _session.SetMaxTier((ResourceTier)value.Value);
        }
    }

    /// <summary>Recalcula cartas, tabelas e seletores a partir do estado da sessão.</summary>
    private void Refresh()
    {
        var comparison = _session.Current;
        var result = _analyze.Execute(comparison);

        _syncing = true;
        try
        {
            SelectedLevel = Levels[comparison.Level.Value - 1];
            SelectedEnemyArmorClass = EnemyArmorClasses.First(o => o.Value == comparison.Assumptions.TargetArmorClass.Value);
            SelectedTier = Tiers.First(t => t.Value == (int)comparison.MaxTier);
            LevelText = Format.Number(result.Level.Value);
            LevelCaption = string.Join(" · ", new[] { result.Level.Phase, result.Level.Remark }.Where(s => !string.IsNullOrEmpty(s)));

            for (var i = 0; i < Cards.Count; i++)
            {
                Cards[i].Update(comparison.Slots[i], result.Cards[i], result.Leaders);
            }
        }
        finally
        {
            _syncing = false;
        }

        Overview.Update(_overview.Execute(comparison.Level, comparison.Assumptions.TargetArmorClass, comparison.MaxTier));

        var damageKey = (comparison.Assumptions.TargetArmorClass.Value, comparison.MaxTier, comparison.Level.Value);
        if (_damageKey != damageKey)
        {
            _damageKey = damageKey;
            DamageGrid.Update(_damage.Execute(comparison.Assumptions.TargetArmorClass, comparison.MaxTier), comparison.Level.Value);
        }
    }
}
