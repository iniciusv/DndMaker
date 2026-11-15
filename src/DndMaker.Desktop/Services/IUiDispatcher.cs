using Avalonia.Threading;

namespace DndMaker.Desktop.Services;

/// <summary>Leva uma ação para a thread da interface. Nos testes, roda na hora.</summary>
internal interface IUiDispatcher
{
    void Post(Action action);
}

internal sealed class AvaloniaUiDispatcher : IUiDispatcher
{
    public void Post(Action action) => Dispatcher.UIThread.Post(action);
}
