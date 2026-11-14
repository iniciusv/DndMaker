using DndMaker.Domain.Checks;
using DndMaker.Domain.Combat;

namespace DndMaker.Domain.Tests.Checks;

public sealed class D20CheckTests
{
    private const double Tolerance = 1e-12;
    private static readonly RollMode[] Modes = [RollMode.Normal, RollMode.Advantage, RollMode.Disadvantage];
    private static readonly NaturalRule[] Rules = [NaturalRule.None, NaturalRule.AttackRoll];

    [Fact]
    public void Bate_com_a_enumeracao_de_todos_os_resultados()
    {
        var failures = new List<string>();
        foreach (var mode in Modes)
        foreach (var rule in Rules)
        foreach (var halfling in new[] { false, true })
        for (var modifier = -5; modifier <= 15; modifier++)
        for (var target = 2; target <= 32; target++)
        {
            var check = new D20Check(modifier, target, mode, rule, halfling);
            var expected = D20Oracle.Success(modifier, target, mode, rule, halfling);
            var actual = check.Success().Value;
            if (Math.Abs(actual - expected) > Tolerance)
            {
                failures.Add($"{check}: esperado {expected}, obtido {actual}");
            }
        }

        failures.Should().BeEmpty();
    }

    [Theory]
    [InlineData(5, 15, 0.55)]
    [InlineData(5, 30, 0.05)] // só o 20 natural acerta
    [InlineData(20, 10, 0.95)] // o 1 natural sempre erra
    [InlineData(0, 21, 0.05)]
    public void Ataque_respeita_os_naturais(int bonus, int armorClass, double expected)
    {
        D20Check.Attack(new AttackBonus(bonus), new ArmorClass(armorClass)).Success().Value
            .Should().BeApproximately(expected, Tolerance);
    }

    [Fact]
    public void Ataque_e_igual_a_formula_do_logic_js()
    {
        // logic.js: clamp((21 + hit - tAC) / 20, .05, .95)
        for (var hit = -2; hit <= 14; hit++)
        for (var ac = 10; ac <= 24; ac++)
        {
            var js = Math.Clamp((21.0 + hit - ac) / 20, 0.05, 0.95);
            D20Check.Attack(new AttackBonus(hit), new ArmorClass(ac)).Success().Value
                .Should().BeApproximately(js, Tolerance, $"ataque {hit} contra CA {ac}");
        }
    }

    [Fact]
    public void Falha_do_monstro_e_igual_a_formula_do_logic_js()
    {
        // logic.js: pf = clamp((dc - 1 - MONSTER_SAVE) / 20, 0, 1), com MONSTER_SAVE = 2
        const int monsterSave = 2;
        for (var dc = 8; dc <= 25; dc++)
        {
            var js = Math.Clamp((dc - 1.0 - monsterSave) / 20, 0, 1);
            D20Check.Save(monsterSave, new DifficultyClass(dc)).Success().Complement.Value
                .Should().BeApproximately(js, Tolerance, $"CD {dc}");
        }
    }

    [Fact]
    public void Charme_de_Strahd_com_Countercharm_e_igual_ao_logic_js()
    {
        // logic.js: p0 = clamp((21 + wis - 17) / 20, 0, 1); pS = adv ? 1 - (1 - p0)² : p0;
        //           com Countercharm: pS + (1 - pS)(1 - (1 - p0)²)
        var countercharm = new RerollOnFailure(RollMode.Advantage, "Countercharm");
        foreach (var advantage in new[] { false, true })
        for (var wis = -2; wis <= 12; wis++)
        {
            var p0 = Math.Clamp((21.0 + wis - 17) / 20, 0, 1);
            var pS = advantage ? 1 - ((1 - p0) * (1 - p0)) : p0;
            var js = pS + ((1 - pS) * (1 - ((1 - p0) * (1 - p0))));

            var check = D20Check.Save(wis, new DifficultyClass(17), advantage ? RollMode.Advantage : RollMode.Normal);
            check.SuccessWith([countercharm]).Value.Should().BeApproximately(js, Tolerance, $"SAB {wis}, vantagem {advantage}");
        }
    }

    [Fact]
    public void Bonus_depois_da_falha_equivale_a_somar_ao_modificador()
    {
        var survivor = new BonusOnFailure(3, "Survivor");
        var check = D20Check.Save(1, new DifficultyClass(17));

        check.SuccessWith([survivor]).Should().Be((check with { Modifier = 4 }).Success());
    }

    [Fact]
    public void Bonus_entra_antes_das_rerrolagens_e_a_rerrolagem_usa_o_modificador_original()
    {
        var check = D20Check.Save(1, new DifficultyClass(17));
        var withBonus = (check with { Modifier = 4 }).Success();
        var reroll = (check with { Mode = RollMode.Advantage }).Success();

        var actual = check.SuccessWith([new RerollOnFailure(RollMode.Advantage, "Countercharm"), new BonusOnFailure(3, "Survivor")]);

        actual.Value.Should().BeApproximately(withBonus.Value + ((1 - withBonus.Value) * reroll.Value), Tolerance);
    }

    [Fact]
    public void Sem_recuperacoes_e_o_proprio_teste()
    {
        var check = D20Check.Save(2, new DifficultyClass(15), RollMode.Advantage);
        check.SuccessWith([]).Should().Be(check.Success());
    }

    [Theory]
    [InlineData(RollMode.Normal, RollMode.Advantage)]
    [InlineData(RollMode.Advantage, RollMode.Advantage)]
    [InlineData(RollMode.Disadvantage, RollMode.Normal)]
    public void Vantagem_cancela_desvantagem(RollMode from, RollMode expected)
    {
        (new D20Check(0, 10, from).WithAdvantage().Mode).Should().Be(expected);
    }

    [Fact]
    public void Halfling_nao_rerrola_um_1_que_ja_passa()
    {
        var check = new D20Check(20, 15, RerollNaturalOnes: true);
        check.Success().Value.Should().Be(1);
    }

    [Fact]
    public void Bonus_negativo_nao_existe()
    {
        var create = () => new BonusOnFailure(-1, "x");
        create.Should().Throw<ArgumentOutOfRangeException>();
    }
}

/// <summary>Referência independente: percorre cada resultado de cada dado.</summary>
internal static class D20Oracle
{
    public static double Success(int modifier, int target, RollMode mode, NaturalRule rule, bool halfling)
    {
        bool Passes(int face) => rule == NaturalRule.AttackRoll
            ? face == 20 || (face != 1 && face + modifier >= target)
            : face + modifier >= target;

        // Distribuição de um dado, já com a rerrolagem do 1 do halfling quando ela ajuda.
        var die = new double[21];
        for (var face = 1; face <= 20; face++)
        {
            if (halfling && face == 1 && !Passes(1))
            {
                for (var reroll = 1; reroll <= 20; reroll++)
                {
                    die[reroll] += 1.0 / 400;
                }
            }
            else
            {
                die[face] += 1.0 / 20;
            }
        }

        if (mode == RollMode.Normal)
        {
            return Enumerable.Range(1, 20).Where(Passes).Sum(face => die[face]);
        }

        var total = 0.0;
        for (var a = 1; a <= 20; a++)
        for (var b = 1; b <= 20; b++)
        {
            var kept = mode == RollMode.Advantage ? Math.Max(a, b) : Math.Min(a, b);
            if (Passes(kept))
            {
                total += die[a] * die[b];
            }
        }

        return total;
    }
}
