using CommunityToolkit.Mvvm.ComponentModel;
using DndMaker.Application.Overview;
using DndMaker.Desktop.Formatting;

namespace DndMaker.Desktop.ViewModels;

/// <summary>Cabeçalho de uma coluna de nível.</summary>
internal sealed record LevelColumnViewModel(int Level, bool IsHighlight, bool IsCurrent)
{
    public string Text => Format.Number(Level);
}

/// <summary>
/// Uma célula colorida da grade. <see cref="Intensity"/> vai de 0 a 0,34, a mesma mistura de cor da página.
/// </summary>
internal sealed record HeatCellViewModel(double Value, double Intensity, bool IsHighlight, bool IsCurrent)
{
    public const double MaxIntensity = 0.34;

    public string Text => Format.Decimal(Value);
}

internal sealed record DamageRowViewModel(string Name, IReadOnlyList<HeatCellViewModel> Cells);

/// <summary>A grade "Dano esperado em cada nível".</summary>
internal sealed partial class DamageGridViewModel : ObservableObject
{
    [ObservableProperty]
    public partial IReadOnlyList<LevelColumnViewModel> Columns { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<DamageRowViewModel> Rows { get; set; } = [];

    [ObservableProperty]
    public partial string Caption { get; set; } = string.Empty;

    public void Update(DamageProgressionDto grid, int currentLevel)
    {
        bool Highlight(int level) => grid.HighlightLevels.Contains(level);

        Columns = [.. grid.Levels.Select(l => new LevelColumnViewModel(l, Highlight(l), l == currentLevel))];
        Rows = [.. grid.Rows.Select(r => new DamageRowViewModel(
            r.Name,
            [.. r.Expected.Select((value, i) => new HeatCellViewModel(
                value,
                grid.Max > 0 ? Math.Clamp(value / grid.Max, 0, 1) * HeatCellViewModel.MaxIntensity : 0,
                Highlight(grid.Levels[i]),
                grid.Levels[i] == currentLevel))]))];

        var highlight = string.Join(" e ", grid.HighlightLevels);
        Caption = $"As colunas {highlight} são a janela em que você quer estar forte. Do nível {grid.HighlightLevels.DefaultIfEmpty(0).Max() + 1} em diante já é o final da campanha.";
    }
}
