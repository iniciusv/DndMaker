using DndMaker.Content.Tests.Golden;
using DndMaker.Domain.Analysis;
using DndMaker.Domain.Checks;
using DndMaker.Domain.Routines;

namespace DndMaker.Content.Tests.Parity;

/// <summary>
/// As diferenças propositais em relação ao logic.js (fase 6, seção 12 do plano). Cada regra ajusta só o campo
/// que muda e, sempre que dá, ainda confere o número antigo por outro caminho; o resto do caso continua
/// sendo comparado com o golden. O comportamento novo é testado em <see cref="DivergenceTests"/>.
/// </summary>
internal static class KnownDivergences
{
    private const string Countercharm = "Countercharm";
    private const string Staff = "staff";

    public static (GoldenOutput Expected, GoldenOutput Actual) Apply(GoldenCase golden, CharacterSheet sheet, GoldenOutput actual)
    {
        var expected = golden.Output;

        // Problema 7: as rotinas opcionais da espécie são novas; o resto da lista tem de bater.
        var optional = sheet.Routines.Select(r => r.Routine.Situation == Situation.SpeciesOption).ToList();
        actual = actual with { Routines = [.. actual.Routines.Where((_, i) => !optional[i])] };

        // Problema 1: Lucky, Survivor e Watcher's Will entram na chance. A conta antiga (só Countercharm),
        // refeita a partir da ficha, ainda tem de bater com o JS.
        if (sheet.Charm.Resources.Any(r => r != Countercharm))
        {
            actual = actual with { Strahd = actual.Strahd with { P = CountercharmOnly(sheet) } };
        }

        // Problema 2: "… contra morto-vivo" não entra no melhor turno. O esperado é refeito com os números
        // do próprio JS, sem as rotinas situacionais.
        var situational = sheet.Routines.Where(r => r.Routine.Situation == Situation.VsUndead).Select(r => r.Name).ToHashSet();
        if (situational.Count > 0)
        {
            expected = expected with { Best = [.. Enumerable.Range(0, 3).Select(tier => BestWithout(expected.Routines, tier, situational))] };
        }

        // Problema 4: o JS usava Shillelagh no Guerreiro 1 → Bardo do Valor só por ter Magic Initiate (Druida).
        // Agora isso exige a variante do bordão; a equivalência com o JS é conferida em DivergenceTests.
        if (golden.Input.B == "fval" && expected.Feats.Contains("mi_druid") && golden.Input.V != Staff)
        {
            actual = actual with { Routines = expected.Routines, Best = expected.Best };
        }

        return (expected, actual);
    }

    /// <summary>A chance contra o charme como o logic.js calculava: vantagem da espécie e Countercharm, nada mais.</summary>
    public static double CountercharmOnly(CharacterSheet sheet)
    {
        var threat = sheet.Charm.Threat;
        var save = sheet.SavingThrows[threat.Ability];
        var check = D20Check.Save(save.Modifier, threat.Dc, sheet.Charm.Advantage ? RollMode.Advantage : RollMode.Normal);
        return sheet.Charm.Resources.Contains(Countercharm)
            ? check.SuccessWith([new RerollOnFailure(RollMode.Advantage, Countercharm)]).Value
            : check.Success().Value;
    }

    /// <summary>O melhor turno do JS, sem as rotinas excluídas: maior dano esperado; no empate, a primeira.</summary>
    private static string BestWithout(IReadOnlyList<GoldenRoutine> routines, int tier, HashSet<string> excluded)
    {
        GoldenRoutine? best = null;
        foreach (var routine in routines.Where(r => r.Tier <= tier && !r.Multi && !excluded.Contains(r.N)))
        {
            if (best is null || routine.Exp > best.Exp)
            {
                best = routine;
            }
        }

        return best?.N ?? string.Empty;
    }
}
