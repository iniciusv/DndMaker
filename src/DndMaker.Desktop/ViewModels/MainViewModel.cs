using CommunityToolkit.Mvvm.ComponentModel;

namespace DndMaker.Desktop.ViewModels;

internal sealed partial class MainViewModel : ObservableObject
{
    public string Title { get; } = "Quem eu levo para Barovia?";

    public string Subtitle { get; } = "Curse of Strahd · regras de 2024 · níveis 1 a 10";

    [ObservableProperty]
    private string _status = "Fase 0: estrutura da solution pronta. O motor de regras chega nas fases 1 a 3.";
}
