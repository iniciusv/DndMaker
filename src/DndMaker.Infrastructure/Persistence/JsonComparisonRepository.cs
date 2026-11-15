using System.Text.Json;
using System.Text.Json.Serialization;
using DndMaker.Application.Session;

namespace DndMaker.Infrastructure.Persistence;

/// <summary>O arquivo gravado: a versão do formato e a comparação.</summary>
public sealed record ComparisonDocument(int SchemaVersion, ComparisonSnapshot? Comparison);

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(ComparisonDocument))]
internal sealed partial class PersistenceJsonContext : JsonSerializerContext;

/// <summary>
/// Grava a comparação num arquivo JSON. A escrita vai para um arquivo temporário e depois substitui o original,
/// para que uma queda no meio não deixe o arquivo pela metade. Arquivo ausente, corrompido ou de outra versão
/// do formato é lido como "nada gravado"; a limpeza dos ids fica com a Application.
/// </summary>
public sealed class JsonComparisonRepository(string filePath) : IComparisonRepository
{
    /// <summary>Versão do formato. Mude quando o formato deixar de ser compatível.</summary>
    public const int SchemaVersion = 1;

    public string FilePath { get; } = Path.GetFullPath(filePath);

    /// <summary><c>%AppData%/DndMaker/comparison.json</c>.</summary>
    public static string DefaultFilePath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DndMaker", "comparison.json");

    public async Task<ComparisonSnapshot?> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(FilePath))
        {
            return null;
        }

        try
        {
            var stream = File.OpenRead(FilePath);
            await using (stream.ConfigureAwait(false))
            {
                var document = await JsonSerializer.DeserializeAsync(stream, PersistenceJsonContext.Default.ComparisonDocument, cancellationToken)
                    .ConfigureAwait(false);
                return document?.SchemaVersion == SchemaVersion ? document.Comparison : null;
            }
        }
        catch (JsonException)
        {
            return null;
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    public async Task SaveAsync(ComparisonSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var directory = Path.GetDirectoryName(FilePath)!;
        Directory.CreateDirectory(directory);
        var temporary = Path.Combine(directory, $".{Path.GetFileName(FilePath)}.{Guid.NewGuid():N}.tmp");

        try
        {
            var stream = File.Create(temporary);
            await using (stream.ConfigureAwait(false))
            {
                await JsonSerializer.SerializeAsync(stream, new ComparisonDocument(SchemaVersion, snapshot), PersistenceJsonContext.Default.ComparisonDocument, cancellationToken)
                    .ConfigureAwait(false);
            }

            File.Move(temporary, FilePath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporary))
            {
                File.Delete(temporary);
            }
        }
    }
}
