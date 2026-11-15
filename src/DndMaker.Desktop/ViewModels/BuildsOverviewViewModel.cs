using System.Collections.ObjectModel;
using System.Windows.Input;
using Avalonia.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DndMaker.Application.Overview;
using DndMaker.Desktop.Formatting;

namespace DndMaker.Desktop.ViewModels;

/// <summary>Botão "pôr esta build na carta I/II/III".</summary>
internal sealed record SlotButtonViewModel(string Numeral, string AccessibleName, ICommand Command);

/// <summary>Uma linha de "Todas as builds". Os números crus servem para ordenar; os textos, para mostrar.</summary>
internal sealed class OverviewRowViewModel(OverviewRowDto row, IReadOnlyList<SlotButtonViewModel> slots)
{
    public string ArchetypeId => row.ArchetypeId;

    public string Name => row.Name;

    public string Status => row.Status;

    public bool IsFinalist => row.Status == "finalista";

    public bool IsDiscarded => row.Status == "descartada";

    public int ArmorClass => row.ArmorClass;

    public int HitPoints => row.HitPoints;

    public int WisdomSave => row.WisdomSave;

    public string WisdomSaveText => Format.Signed(row.WisdomSave);

    public int ConstitutionSave => row.ConstitutionSave;

    public string ConstitutionSaveText => Format.Signed(row.ConstitutionSave);

    public int Initiative => row.Initiative;

    public string InitiativeText => Format.Signed(row.Initiative);

    public double CharmChance => row.CharmChance;

    public string CharmText => Format.Percent(row.CharmChance);

    public double BestExpected => row.BestExpected;

    public string BestExpectedText => Format.Decimal(row.BestExpected);

    public string BestRoutine => row.BestRoutine;

    public string Milestone => row.Milestone;

    public bool TopArmorClass => row.TopArmorClass;

    public bool TopHitPoints => row.TopHitPoints;

    public bool TopWisdomSave => row.TopWisdomSave;

    public bool TopConstitutionSave => row.TopConstitutionSave;

    public bool TopInitiative => row.TopInitiative;

    public bool TopExpected => row.TopExpected;

    public IReadOnlyList<SlotButtonViewModel> Slots => slots;
}

/// <summary>
/// A tabela "Todas as builds". As linhas são trocadas no lugar, então a ordenação escolhida na tabela
/// sobrevive às atualizações.
/// </summary>
internal sealed partial class BuildsOverviewViewModel : ObservableObject
{
    private readonly ObservableCollection<OverviewRowViewModel> _rows = [];
    private readonly Action<int, string> _loadIntoSlot;
    private readonly int _slotCount;

    public BuildsOverviewViewModel(int slotCount, Action<int, string> loadIntoSlot)
    {
        _slotCount = slotCount;
        _loadIntoSlot = loadIntoSlot;
        Rows = new DataGridCollectionView(_rows);
    }

    public DataGridCollectionView Rows { get; }

    public IReadOnlyList<OverviewRowViewModel> Items => _rows;

    [ObservableProperty]
    public partial string Title { get; set; } = "Todas as builds";

    [ObservableProperty]
    public partial string Caption { get; set; } = string.Empty;

    public void Update(BuildsOverviewDto overview)
    {
        Title = $"Todas as builds no nível {overview.Level}";
        Caption = $"Escolhas padrão em todas: {overview.SpeciesName.ToLowerInvariant()} com {overview.ExtraOriginFeatName} como talento extra, "
            + "antecedente sugerido, primeira variante e aumento de atributo nos níveis de talento. "
            + $"Dano esperado contra CA {overview.EnemyArmorClass}, um alvo, rotinas até \"{overview.MaxTierName}\".";

        var rows = overview.Rows.Select(r => new OverviewRowViewModel(r, SlotButtons(r))).ToList();
        if (_rows.Count != rows.Count)
        {
            _rows.Clear();
            rows.ForEach(_rows.Add);
            return;
        }

        foreach (var row in rows)
        {
            var index = _rows.Select((r, i) => (r, i)).First(x => x.r.ArchetypeId == row.ArchetypeId).i;
            _rows[index] = row;
        }
    }

    private List<SlotButtonViewModel> SlotButtons(OverviewRowDto row) =>
        [.. Enumerable.Range(0, _slotCount).Select(slot => new SlotButtonViewModel(
            Format.Roman(slot),
            $"Pôr {row.Name} na carta {Format.Roman(slot)}",
            new RelayCommand(() => _loadIntoSlot(slot, row.ArchetypeId))))];
}
