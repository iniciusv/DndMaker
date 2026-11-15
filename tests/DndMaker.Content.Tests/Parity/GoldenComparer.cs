using System.Globalization;
using DndMaker.Content.Tests.Golden;

namespace DndMaker.Content.Tests.Parity;

/// <summary>
/// Compara a saída do C# com a do logic.js campo a campo e descreve cada diferença.
/// Números quebrados (probabilidades e dano esperado) usam tolerância.
/// </summary>
internal static class GoldenComparer
{
    public const double Tolerance = 1e-9;

    public static IEnumerable<string> Differences(GoldenOutput expected, GoldenOutput actual)
    {
        var diffs = new List<string>();

        void Check<T>(string field, T js, T cs)
        {
            if (!EqualityComparer<T>.Default.Equals(js, cs))
            {
                diffs.Add($"{field}: JS={Show(js)} C#={Show(cs)}");
            }
        }

        void CheckNumber(string field, double js, double cs)
        {
            if (Math.Abs(js - cs) > Tolerance)
            {
                diffs.Add($"{field}: JS={js.ToString("R", CultureInfo.InvariantCulture)} C#={cs.ToString("R", CultureInfo.InvariantCulture)}");
            }
        }

        void CheckList<T>(string field, IReadOnlyList<T> js, IReadOnlyList<T> cs)
        {
            if (!js.SequenceEqual(cs))
            {
                diffs.Add($"{field}: JS=[{string.Join(", ", js)}] C#=[{string.Join(", ", cs)}]");
            }
        }

        Check("v", expected.V, actual.V);
        Check("pb", expected.Pb, actual.Pb);
        Check("bgApplied", expected.BgApplied, actual.BgApplied);
        CheckList("sc", expected.Sc, actual.Sc);
        CheckList("m", expected.M, actual.M);
        Check("hp", expected.Hp, actual.Hp);
        Check("ac", expected.Ac, actual.Ac);
        Check("init", expected.Init, actual.Init);
        Check("dark", expected.Dark, actual.Dark);
        Check("sb", expected.Sb, actual.Sb);
        CheckList("saves", expected.Saves, actual.Saves);

        CheckNumber("strahd.p", expected.Strahd.P, actual.Strahd.P);
        Check("strahd.adv", expected.Strahd.Adv, actual.Strahd.Adv);
        Check("strahd.cc", expected.Strahd.Cc, actual.Strahd.Cc);

        Check("conc existe", expected.Conc is not null, actual.Conc is not null);
        if (expected.Conc is { } jsConc && actual.Conc is { } csConc)
        {
            CheckNumber("conc.p", jsConc.P, csConc.P);
            Check("conc.adv", jsConc.Adv, csConc.Adv);
        }

        CheckList("feats", expected.Feats, actual.Feats);
        CheckList("picks", expected.Picks, actual.Picks);
        Check("shHint", expected.ShHint, actual.ShHint);

        Check("rotinas", string.Join(" | ", expected.Routines.Select(r => r.N)), string.Join(" | ", actual.Routines.Select(r => r.N)));
        foreach (var (js, cs, i) in expected.Routines.Zip(actual.Routines).Select((p, i) => (p.First, p.Second, i)))
        {
            var prefix = $"rotina {i} ({js.N})";
            Check($"{prefix}.tier", js.Tier, cs.Tier);
            Check($"{prefix}.multi", js.Multi, cs.Multi);
            Check($"{prefix}.note", js.Note, cs.Note);
            Check($"{prefix}.f", js.F, cs.F);
            Check($"{prefix}.hit", js.Hit, cs.Hit);
            Check($"{prefix}.dc", js.Dc, cs.Dc);
            CheckNumber($"{prefix}.raw", js.Raw, cs.Raw);
            CheckNumber($"{prefix}.exp", js.Exp, cs.Exp);
        }

        CheckList("best", expected.Best, actual.Best);
        return diffs;
    }

    private static string Show<T>(T value) => value switch
    {
        null => "null",
        string s => $"\"{s}\"",
        _ => value.ToString() ?? string.Empty,
    };
}
