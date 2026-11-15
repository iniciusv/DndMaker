using DndMaker.Domain.Analysis;
using DndMaker.Domain.Progression;
using static DndMaker.Domain.Abilities.Ability;

namespace DndMaker.Domain.Tests.Analysis;

/// <summary>Divisão do +2/+1 do antecedente (problema 8 do plano), com modificadores inventados por nível.</summary>
public sealed class BackgroundBonusPolicyTests
{
    private static readonly BackgroundBonus LogicJs = new(Charisma, Dexterity);
    private static readonly BackgroundBonus Swapped = new(Dexterity, Charisma);
    private static readonly BackgroundBonus[] Candidates = [LogicJs, Swapped];
    private static readonly Level Planning = Level.Of(10);

    private static Func<BackgroundBonus, Level, int[]> Modifiers(int[] logicJsAt1, int[] logicJsAt10, int[] swappedAt1, int[] swappedAt10) =>
        (bonus, level) => (bonus == LogicJs, level.Value) switch
        {
            (true, 1) => logicJsAt1,
            (true, _) => logicJsAt10,
            (false, 1) => swappedAt1,
            _ => swappedAt10,
        };

    private static BackgroundBonus Best(Func<BackgroundBonus, Level, int[]> modifiers) =>
        new BestModifiersBackgroundPolicy(Planning).Choose(Candidates, modifiers);

    [Fact]
    public void Logic_js_fica_com_a_primeira_divisao()
    {
        PriorityBackgroundPolicy.Instance.Choose(Candidates, (_, _) => throw new InvalidOperationException("não simula"))
            .Should().Be(LogicJs);
    }

    [Fact]
    public void Empate_no_fim_decide_pelo_nivel_1()
    {
        // CAR 17 e DES 15 contra 16 e 16; no 10 as duas chegam a CAR 20 e DES 16.
        Best(Modifiers([3, 2], [5, 3], [3, 3], [5, 3])).Should().Be(Swapped);
    }

    [Fact]
    public void O_ultimo_nivel_planejado_vale_mais_que_o_nivel_1()
    {
        // Com Fey Touched no 5, CAR 17 → 18 termina com +5 e CAR 16 → 17 com +4, mesmo perdendo DES no começo.
        Best(Modifiers([3, 2], [5, 2], [3, 3], [4, 3])).Should().Be(LogicJs);
    }

    [Fact]
    public void Empate_em_tudo_fica_com_a_divisao_do_logic_js()
    {
        Best(Modifiers([3, 2], [5, 3], [3, 2], [5, 3])).Should().Be(LogicJs);
    }
}
