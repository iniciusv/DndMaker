namespace DndMaker.Domain.Checks;

/// <summary>Algo que pode ser usado depois de falhar num teste. <see cref="Source"/> aparece na explicação da conta.</summary>
public abstract record CheckRecovery(string Source);

/// <summary>Soma um bônus à rolagem que falhou. Exemplo: Survivor soma o bônus de proficiência.</summary>
public sealed record BonusOnFailure : CheckRecovery
{
    public BonusOnFailure(int bonus, string source)
        : base(source)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(bonus);
        Bonus = bonus;
    }

    public int Bonus { get; }
}

/// <summary>Rola de novo e fica com o novo resultado. Exemplo: Countercharm rerrola com vantagem.</summary>
public sealed record RerollOnFailure(RollMode Mode, string Source) : CheckRecovery(Source);
