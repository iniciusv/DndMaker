namespace DndMaker.Application.Session;

/// <summary>Um personagem da comparação como é gravado: só ids em texto, para sobreviver a mudanças no conteúdo.</summary>
public sealed record SlotSnapshot(
    string Archetype,
    string? Variant,
    string Species,
    string Background,
    string? ExtraOriginFeat,
    IReadOnlyDictionary<int, string> LevelChoices);

/// <summary>A comparação como é gravada. <see cref="MaxTier"/> é o número da faixa de recurso (0 a 2).</summary>
public sealed record ComparisonSnapshot(int Level, int EnemyArmorClass, int MaxTier, IReadOnlyList<SlotSnapshot> Slots);

/// <summary>Onde a comparação fica entre uma sessão e outra.</summary>
public interface IComparisonRepository
{
    /// <summary>A comparação gravada, ou nula se não houver nenhuma ou ela não puder ser lida.</summary>
    Task<ComparisonSnapshot?> LoadAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(ComparisonSnapshot snapshot, CancellationToken cancellationToken = default);
}
