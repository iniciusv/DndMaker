using System.Collections.Immutable;
using DndMaker.Domain.Abilities;
using DndMaker.Domain.Checks;
using DndMaker.Domain.Common;

namespace DndMaker.Domain.Effects;

/// <summary>O resultado de aplicar todos os efeitos de um personagem, na ordem em que vieram.</summary>
public sealed class CharacterTraits
{
    private readonly ImmutableArray<Effect> _effects;

    private CharacterTraits(ImmutableArray<Effect> effects)
    {
        _effects = effects;
        foreach (var effect in effects)
        {
            switch (effect)
            {
                case SaveProficiency p: SaveProficiencies = SaveProficiencies.With(p.Ability); break;
                case SaveAdvantage a: SaveAdvantages |= a.Abilities; break;
                case ConditionAdvantage c: ConditionAdvantages = ConditionAdvantages.Add(c.Condition); break;
                case HitPointsPerLevel h: BonusHitPointsPerLevel += h.Amount; break;
                case ArmorClassBonus b: BonusArmorClass += b.Amount; break;
                case ConcentrationAdvantage: HasConcentrationAdvantage = true; break;
                case InitiativeBonus or InitiativeAbilityBonus or DarkvisionGrant or SaveAura or ConditionRecovery: break;
                default: throw new DomainException($"Efeito sem tratamento: {effect.GetType().Name}.");
            }
        }
    }

    public AbilitySet SaveProficiencies { get; }

    public AbilitySet SaveAdvantages { get; }

    public ImmutableHashSet<Condition> ConditionAdvantages { get; } = [];

    public int BonusHitPointsPerLevel { get; }

    public int BonusArmorClass { get; }

    public bool HasConcentrationAdvantage { get; }

    public static CharacterTraits From(IEnumerable<Effect> effects) => new([.. effects]);

    public int InitiativeModifier(int proficiencyBonus, AbilityMap<int> modifiers) =>
        _effects.Sum(effect => effect switch
        {
            InitiativeBonus { Amount: InitiativeAmount.ProficiencyBonus } => proficiencyBonus,
            InitiativeBonus { Amount: InitiativeAmount.HalfProficiencyBonus } => proficiencyBonus / 2,
            InitiativeAbilityBonus a => modifiers[a.Ability],
            _ => 0,
        });

    /// <summary>Soma das auras; cada uma vale o modificador do atributo, no mínimo o piso dela.</summary>
    public int SaveBonus(AbilityMap<int> modifiers) =>
        _effects.OfType<SaveAura>().Sum(aura => Math.Max(aura.Minimum, modifiers[aura.Source]));

    public IReadOnlyList<SaveAura> SaveAuras => [.. _effects.OfType<SaveAura>()];

    public IReadOnlyList<CheckRecovery> RecoveriesAgainst(Condition condition) =>
        [.. _effects.OfType<ConditionRecovery>().Where(r => r.Condition == condition).Select(r => r.Recovery)];

    /// <summary>Aplica as concessões em ordem sobre a visão no escuro da espécie. A última nota vale.</summary>
    public DarkvisionResult Darkvision(Meters speciesRange)
    {
        var range = speciesRange;
        string? note = null;
        foreach (var grant in _effects.OfType<DarkvisionGrant>())
        {
            range = grant.Kind switch
            {
                DarkvisionKind.Minimum => Meters.Max(range, grant.Range),
                DarkvisionKind.Extension => range + grant.Range,
                _ => throw new DomainException($"Tipo de visão no escuro desconhecido: {grant.Kind}."),
            };
            note = grant.Note ?? note;
        }

        return new DarkvisionResult(range, note);
    }
}

public sealed record DarkvisionResult(Meters Range, string? Note);
