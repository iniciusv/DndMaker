using System.Text.Json;
using System.Text.Json.Serialization;

namespace DndMaker.Content.Tests.Golden;

/// <summary>
/// Resultados do motor original em JavaScript, gerados por tools/golden/export-golden.js.
/// Os nomes das propriedades seguem o JSON (e o logic.js), não o domínio C#.
/// </summary>
public sealed record GoldenFile(GoldenMeta Meta, IReadOnlyList<GoldenCase> Cases)
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
    };

    private static readonly Lazy<GoldenFile> LazyInstance = new(Load);

    public static GoldenFile Instance => LazyInstance.Value;

    private static GoldenFile Load()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Golden", "golden.json");
        using var stream = File.OpenRead(path);
        return JsonSerializer.Deserialize<GoldenFile>(stream, Options)
            ?? throw new InvalidDataException($"golden.json vazio: {path}");
    }
}

public sealed record GoldenMeta(string Source, IReadOnlyList<string> Abilities, IReadOnlyList<string> Builds, int Count, string Note);

public sealed record GoldenCase(
    [property: JsonPropertyName("in")] GoldenInput Input,
    [property: JsonPropertyName("out")] GoldenOutput Output)
{
    public override string ToString() => Input.ToString();
}

public sealed record GoldenInput(string B, string? V, string Sp, string Bg, string Hf, IReadOnlyDictionary<int, string> G, int L, int TAc)
{
    public override string ToString()
    {
        var choices = G.Count == 0 ? "" : " " + string.Join(",", G.Select(kv => $"{kv.Key}:{kv.Value}"));
        return $"{B}[{V}] {Sp}/{Bg}/{Hf}{choices} L{L} CA{TAc}";
    }
}

public sealed record GoldenOutput(
    string? V,
    int Pb,
    string BgApplied,
    IReadOnlyList<int> Sc,
    IReadOnlyList<int> M,
    int Hp,
    GoldenArmor Ac,
    int Init,
    GoldenDarkvision Dark,
    int Sb,
    IReadOnlyList<GoldenSave> Saves,
    GoldenCharm Strahd,
    GoldenConcentration? Conc,
    IReadOnlyList<string> Feats,
    IReadOnlyList<GoldenPick> Picks,
    bool ShHint,
    IReadOnlyList<GoldenRoutine> Routines,
    IReadOnlyList<string> Best);

public sealed record GoldenArmor(int V, string D, bool Stealth, string Extra);

public sealed record GoldenDarkvision(int V, string D);

public sealed record GoldenSave(int V, bool Prof, bool Adv);

public sealed record GoldenCharm(double P, bool Adv, bool Cc);

public sealed record GoldenConcentration(double P, bool Adv);

public sealed record GoldenPick(int Lv, string G, string What);

public sealed record GoldenRoutine(string N, int Tier, bool Multi, string Note, string F, int? Hit, int? Dc, double Raw, double Exp);
