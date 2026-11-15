using DndMaker.Domain.Combat;
using DndMaker.Domain.Common;

namespace DndMaker.Domain.Checks;

public enum RollMode
{
    Normal,
    Advantage,
    Disadvantage,
}

/// <summary>Se os resultados naturais 1 e 20 decidem sozinhos.</summary>
public enum NaturalRule
{
    /// <summary>Testes de resistência e de atributo: só a soma conta.</summary>
    None,

    /// <summary>Jogada de ataque: 1 natural sempre erra, 20 natural sempre acerta.</summary>
    AttackRoll,
}

/// <summary>
/// Um teste de d20: soma <see cref="Modifier"/> e passa se alcançar <see cref="Target"/>.
/// A probabilidade é exata, sem simulação.
/// </summary>
public sealed record D20Check(
    int Modifier,
    int Target,
    RollMode Mode = RollMode.Normal,
    NaturalRule Naturals = NaturalRule.None,
    bool RerollNaturalOnes = false)
{
    private const int Faces = 20;

    /// <summary>Ataque contra uma CA.</summary>
    public static D20Check Attack(AttackBonus bonus, ArmorClass target, RollMode mode = RollMode.Normal) =>
        new(bonus.Value, target.Value, mode, NaturalRule.AttackRoll);

    /// <summary>Teste de resistência contra uma CD.</summary>
    public static D20Check Save(int modifier, DifficultyClass dc, RollMode mode = RollMode.Normal) =>
        new(modifier, dc.Value, mode);

    /// <summary>Junta uma fonte de vantagem: desvantagem vira normal, normal vira vantagem.</summary>
    public D20Check WithAdvantage() => this with
    {
        Mode = Mode == RollMode.Disadvantage ? RollMode.Normal : RollMode.Advantage,
    };

    public Probability Success()
    {
        var single = SingleDieSuccess();
        return Mode switch
        {
            RollMode.Normal => single,
            // Com resultado monótono no dado, ficar com o maior passa se qualquer um dos dois passar.
            RollMode.Advantage => new Probability(1 - ((1 - single.Value) * (1 - single.Value))),
            RollMode.Disadvantage => single.And(single),
            _ => throw new ArgumentOutOfRangeException(nameof(Mode), Mode, "Modo de rolagem desconhecido."),
        };
    }

    /// <summary>
    /// Sucesso considerando o que pode ser usado depois de uma falha.
    /// Bônus valem na mesma rolagem e são somados; rerrolagens vêm depois, cada uma uma rolagem nova.
    /// O bônus só é gasto quando transforma a falha em sucesso, e aí não há rerrolagem; se a rolagem
    /// falha mesmo com ele, o bônus continua guardado e vale na rerrolagem (Survivor com Countercharm).
    /// </summary>
    public Probability SuccessWith(IEnumerable<CheckRecovery> recoveries)
    {
        var list = recoveries.ToList();
        var bonus = list.OfType<BonusOnFailure>().Sum(b => b.Bonus);
        var withBonus = this with { Modifier = Modifier + bonus };
        var success = withBonus.Success();

        foreach (var reroll in list.OfType<RerollOnFailure>())
        {
            success = success.OrElse((withBonus with { Mode = reroll.Mode }).Success());
        }

        return success;
    }

    private Probability SingleDieSuccess()
    {
        var passingFaces = 0;
        for (var face = 1; face <= Faces; face++)
        {
            if (Passes(face))
            {
                passingFaces++;
            }
        }

        var chance = passingFaces / (double)Faces;
        if (RerollNaturalOnes && !Passes(1))
        {
            // Halfling: o 1 natural é rolado de novo e o novo resultado vale.
            chance += chance / Faces;
        }

        return new Probability(chance);
    }

    private bool Passes(int face) => Naturals switch
    {
        NaturalRule.AttackRoll => face == Faces || (face != 1 && face + Modifier >= Target),
        _ => face + Modifier >= Target,
    };
}
