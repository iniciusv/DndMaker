using CommunityToolkit.Mvvm.ComponentModel;
using DndMaker.Application;
using DndMaker.Application.Catalog;
using DndMaker.Application.Comparison;
using DndMaker.Application.Session;
using DndMaker.Application.Sheets;
using DndMaker.Desktop.Formatting;
using DndMaker.Domain.Builds;

namespace DndMaker.Desktop.ViewModels;

/// <summary>Uma escolha de nível de talento na carta ("Escolha do nível 4").</summary>
internal sealed partial class LevelChoiceViewModel : ObservableObject
{
    private readonly Action<int, string> _choose;
    private bool _syncing;

    public LevelChoiceViewModel(int level, IReadOnlyList<OptionDto> options, OptionDto selected, Action<int, string> choose)
    {
        Level = level;
        Options = options;
        _choose = choose;
        Sync(selected);
    }

    public int Level { get; }

    public string Label => $"Escolha do nível {Level}";

    public IReadOnlyList<OptionDto> Options { get; }

    [ObservableProperty]
    public partial OptionDto? Selected { get; set; }

    public void Sync(OptionDto selected)
    {
        _syncing = true;
        try
        {
            Selected = selected;
        }
        finally
        {
            _syncing = false;
        }
    }

    partial void OnSelectedChanged(OptionDto? value)
    {
        if (!_syncing && value is not null)
        {
            _choose(Level, value.Id);
        }
    }
}

/// <summary>
/// Uma carta: os seletores (build, variante, espécie, talento extra, antecedente, níveis de talento) e a ficha.
/// Mudanças nos seletores vão para a <see cref="ComparisonSession"/>; a carta só reflete o estado que volta dela.
/// </summary>
internal sealed partial class CharacterCardViewModel : ObservableObject
{
    private readonly ComparisonSession _session;
    private readonly CatalogDto _catalog;
    private string? _syncedArchetypeId;
    private bool _syncing;

    public CharacterCardViewModel(int index, CatalogDto catalog, ComparisonSession session)
    {
        Index = index;
        _catalog = catalog;
        _session = session;
    }

    public int Index { get; }

    public string Numeral => Format.Roman(Index);

    public string AccessibleName => $"Carta {Numeral}";

    public IReadOnlyList<ArchetypeOptionDto> Archetypes => _catalog.Archetypes;

    public IReadOnlyList<SpeciesOptionDto> Species => _catalog.Species;

    public IReadOnlyList<BackgroundOptionDto> Backgrounds => _catalog.Backgrounds;

    public IReadOnlyList<OptionDto> ExtraOriginFeats => _catalog.ExtraOriginFeats;

    [ObservableProperty]
    public partial ArchetypeOptionDto? SelectedArchetype { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasVariants))]
    public partial IReadOnlyList<OptionDto> Variants { get; set; } = [];

    public bool HasVariants => Variants.Count > 0;

    [ObservableProperty]
    public partial OptionDto? SelectedVariant { get; set; }

    [ObservableProperty]
    public partial SpeciesOptionDto? SelectedSpecies { get; set; }

    [ObservableProperty]
    public partial bool ShowsExtraOriginFeat { get; set; }

    [ObservableProperty]
    public partial OptionDto? SelectedExtraOriginFeat { get; set; }

    [ObservableProperty]
    public partial BackgroundOptionDto? SelectedBackground { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<LevelChoiceViewModel> LevelChoices { get; set; } = [];

    [ObservableProperty]
    public partial SheetViewModel? Sheet { get; set; }

    /// <summary>Atualiza seletores e ficha com o estado da sessão, sem mandar nada de volta para ela.</summary>
    public void Update(CharacterBuild build, SheetDto sheet, LeadersDto leaders)
    {
        _syncing = true;
        try
        {
            var archetype = _catalog.Archetypes.First(a => a.Id == build.Archetype.Id.Value);
            // Compara com a última build sincronizada: quando o usuário troca a build, SelectedArchetype já vem com a nova.
            var archetypeChanged = archetype.Id != _syncedArchetypeId;
            _syncedArchetypeId = archetype.Id;
            SelectedArchetype = archetype;
            if (archetypeChanged)
            {
                Variants = archetype.Variants;
            }

            SelectedVariant = Variants.FirstOrDefault(v => v.Id == build.Variant?.Id.Value);
            SelectedSpecies = _catalog.Species.First(s => s.Id == build.Species.Id.Value);
            ShowsExtraOriginFeat = SelectedSpecies.GrantsExtraOriginFeat;
            SelectedExtraOriginFeat = _catalog.ExtraOriginFeats.FirstOrDefault(f => f.Id == build.ExtraOriginFeat?.Id.Value);
            SelectedBackground = _catalog.Backgrounds.First(b => b.Id == build.Background.Id.Value);
            SyncLevelChoices(archetype, build, archetypeChanged);
            Sheet = new SheetViewModel(sheet, Index, leaders);
        }
        finally
        {
            _syncing = false;
        }
    }

    private void SyncLevelChoices(ArchetypeOptionDto archetype, CharacterBuild build, bool rebuild)
    {
        OptionDto ChoiceAt(int level) =>
            _catalog.LevelChoices.First(o => o.Id == LevelChoiceIds.Of(build.ChoiceAt(level)));

        if (rebuild || LevelChoices.Count != archetype.FeatLevels.Count)
        {
            LevelChoices = [.. archetype.FeatLevels.Select(level => new LevelChoiceViewModel(
                level, _catalog.LevelChoices, ChoiceAt(level), (l, id) => _session.SetLevelChoice(Index, l, id)))];
            return;
        }

        foreach (var choice in LevelChoices)
        {
            choice.Sync(ChoiceAt(choice.Level));
        }
    }

    partial void OnSelectedArchetypeChanged(ArchetypeOptionDto? value) =>
        Send(value, v => _session.ChangeArchetype(Index, v.Id));

    partial void OnSelectedVariantChanged(OptionDto? value) =>
        Send(value, v => _session.SetVariant(Index, v.Id));

    partial void OnSelectedSpeciesChanged(SpeciesOptionDto? value) =>
        Send(value, v => _session.SetSpecies(Index, v.Id));

    partial void OnSelectedExtraOriginFeatChanged(OptionDto? value) =>
        Send(value, v => _session.SetExtraOriginFeat(Index, v.Id));

    partial void OnSelectedBackgroundChanged(BackgroundOptionDto? value) =>
        Send(value, v => _session.SetBackground(Index, v.Id));

    /// <summary>Repassa à sessão o que o usuário escolheu. Ignora a sincronização e o nulo que a lista manda ao trocar de itens.</summary>
    private void Send<T>(T? value, Action<T> apply)
        where T : class
    {
        if (!_syncing && value is not null)
        {
            apply(value);
        }
    }
}
