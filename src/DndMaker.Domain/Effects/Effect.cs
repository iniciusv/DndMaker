using DndMaker.Domain.Abilities;
using DndMaker.Domain.Checks;
using DndMaker.Domain.Common;
using DndMaker.Domain.Damage;
using DndMaker.Domain.Routines;

namespace DndMaker.Domain.Effects;

/// <summary>Condições que importam para as defesas calculadas.</summary>
public enum Condition
{
    Charmed,
    Frightened,
}

/// <summary>
/// Algo que uma espécie, um talento ou uma característica de classe muda na ficha.
/// Todas as origens usam os mesmos efeitos; o cálculo não precisa saber de onde vieram.
/// </summary>
public abstract record Effect;

/// <summary>Proficiência num teste de resistência (Resilient, Iron Mind).</summary>
public sealed record SaveProficiency(Ability Ability) : Effect;

/// <summary>Vantagem em todos os testes de resistência desses atributos (gnomo, kalashtar).</summary>
public sealed record SaveAdvantage(AbilitySet Abilities) : Effect;

/// <summary>Vantagem nos testes contra uma condição (elfo contra Enfeitiçado).</summary>
public sealed record ConditionAdvantage(Condition Condition) : Effect;

/// <summary>Pontos de vida extras por nível (anão +1, Tough +2).</summary>
public sealed record HitPointsPerLevel(int Amount) : Effect;

/// <summary>Bônus fixo na CA (Warforged).</summary>
public sealed record ArmorClassBonus(int Amount) : Effect;

public enum InitiativeAmount
{
    ProficiencyBonus,
    HalfProficiencyBonus,
}

/// <summary>Soma o bônus de proficiência, inteiro ou pela metade, à iniciativa (Alert, Harengon, Jack of All Trades).</summary>
public sealed record InitiativeBonus(InitiativeAmount Amount) : Effect;

/// <summary>Soma o modificador de um atributo à iniciativa (Gloom Stalker).</summary>
public sealed record InitiativeAbilityBonus(Ability Ability) : Effect;

public enum DarkvisionKind
{
    /// <summary>Garante pelo menos este alcance.</summary>
    Minimum,

    /// <summary>Soma ao alcance que já existe (ou concede, se não houver).</summary>
    Extension,
}

/// <summary>Visão no escuro concedida. <see cref="Note"/> explica o que ela tem de especial.</summary>
public sealed record DarkvisionGrant(Meters Range, DarkvisionKind Kind, string? Note = null) : Effect;

/// <summary>Soma o modificador de um atributo (no mínimo <see cref="Minimum"/>) a todos os testes de resistência.</summary>
public sealed record SaveAura(Ability Source, int Minimum, string Name) : Effect;

/// <summary>Vantagem nos testes de concentração (War Caster).</summary>
public sealed record ConcentrationAdvantage : Effect;

/// <summary>Algo que pode ser usado depois de falhar contra uma condição (Countercharm).</summary>
public sealed record ConditionRecovery(Condition Condition, CheckRecovery Recovery) : Effect;

/// <summary>Soma o bônus de proficiência a um teste falho contra a condição (Survivor, 1x por descanso longo).</summary>
public sealed record ProficiencyBonusOnFailure(Condition Condition, string Source) : Effect;

/// <summary>
/// Vantagem que pode ser gasta em testes de resistência desses atributos: Lucky (pontos de sorte, todos os atributos)
/// e Watcher's Will (Canalizar Divindade, INT, SAB e CAR). Só conta na chance "gastando recursos".
/// </summary>
public sealed record SaveAdvantageResource(AbilitySet Abilities, string Source) : Effect;

/// <summary>
/// Encerra a condição depois que ela pega, sem mudar a chance do teste (Twilight Sanctuary no fim do turno).
/// Aparece ao lado da chance, não dentro dela.
/// </summary>
public sealed record ConditionRemedy(Condition Condition, string Source) : Effect;

/// <summary>
/// Dano extra opcional, como o da espécie (Celestial Revelation, Surprise Attack, Giant Ancestry).
/// Vira uma rotina a mais, marcada como opcional, que não entra no melhor turno.
/// </summary>
public sealed record ExtraDamageOption(string Name, DamageFormula Damage, ResourceTier Tier, string Note) : Effect
{
    /// <summary>Nível a partir do qual a opção existe.</summary>
    public int MinimumLevel { get; init; } = 1;

    /// <summary>Soma o bônus de proficiência ao dano (Celestial Revelation).</summary>
    public bool AddsProficiencyBonus { get; init; }
}
