using DndMaker.Domain.Builds;
using DndMaker.Domain.Catalog;
using DndMaker.Domain.Comparison;
using DndMaker.Domain.Progression;
using DndMaker.Domain.Routines;
using DndMaker.Domain.Rulesets;

namespace DndMaker.Application.Session;

/// <summary>
/// A comparação aberta na tela. É o único objeto da Application que muda de estado: cada operação troca
/// <see cref="Current"/> por uma nova comparação imutável e avisa por <see cref="Changed"/>.
/// Ids desconhecidos indicam erro de quem chama e lançam exceção.
/// </summary>
public sealed class ComparisonSession(Ruleset rules)
{
    private readonly ComparisonSnapshotMapper _mapper = new(rules);

    public event EventHandler? Changed;

    /// <summary>A comparação atual. Lida de qualquer thread: a troca da referência é atômica.</summary>
    public BuildComparison Current { get; private set; } = rules.CreateDefaultComparison();

    public Ruleset Rules => rules;

    /// <summary>Substitui a comparação pela gravada (limpa), ou pela inicial se não houver nada gravado.</summary>
    public void Restore(ComparisonSnapshot? snapshot) => Set(_mapper.FromSnapshot(snapshot));

    /// <summary>Volta à comparação inicial da campanha.</summary>
    public void Reset() => Set(rules.CreateDefaultComparison());

    public ComparisonSnapshot ToSnapshot() => ComparisonSnapshotMapper.ToSnapshot(Current);

    public void SetLevel(int level)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(level, rules.Campaign.MaxLevel);
        Set(Current.WithLevel(Level.Of(level)));
    }

    public void SetEnemyArmorClass(int armorClass)
    {
        var option = rules.Campaign.EnemyArmorClasses.FirstOrDefault(o => o.Value.Value == armorClass)
            ?? throw new ArgumentOutOfRangeException(nameof(armorClass), armorClass, "CA de inimigo fora das opções da campanha.");
        Set(Current.WithAssumptions(Current.Assumptions with { TargetArmorClass = option.Value }));
    }

    public void SetMaxTier(ResourceTier tier)
    {
        if (!Enum.IsDefined(tier))
        {
            throw new ArgumentOutOfRangeException(nameof(tier), tier, "Faixa de recurso desconhecida.");
        }

        Set(Current.WithMaxTier(tier));
    }

    /// <summary>
    /// Troca a build da carta mantendo espécie e talento extra; volta à variante padrão,
    /// ao antecedente sugerido e ao aumento de atributo em todos os níveis.
    /// </summary>
    public void ChangeArchetype(int slot, string archetypeId)
    {
        var archetype = rules.GetArchetype(new ArchetypeId(archetypeId));
        Update(slot, build => build.WithArchetype(archetype, rules.SuggestedBackground(archetype)));
    }

    /// <summary>Põe na carta a build com todas as escolhas padrão (o botão da tabela "Todas as builds").</summary>
    public void LoadDefaultCharacter(int slot, string archetypeId) =>
        Update(slot, _ => rules.DefaultCharacter(rules.GetArchetype(new ArchetypeId(archetypeId))));

    public void SetVariant(int slot, string variantId) =>
        Update(slot, build => build.WithVariant(
            build.Archetype.FindVariant(new VariantId(variantId))
            ?? throw new ArgumentException($"A variante {variantId} não pertence a {build.Archetype.Id}.", nameof(variantId))));

    public void SetSpecies(int slot, string speciesId) =>
        Update(slot, build => build.WithSpecies(rules.GetSpecies(new SpeciesId(speciesId))));

    public void SetBackground(int slot, string backgroundId) =>
        Update(slot, build => build.WithBackground(rules.GetBackground(new BackgroundId(backgroundId))));

    public void SetExtraOriginFeat(int slot, string featId)
    {
        var feat = rules.ExtraOriginFeats.FirstOrDefault(f => f.Id.Value == featId)
            ?? throw new ArgumentException($"{featId} não é um talento extra oferecido.", nameof(featId));
        Update(slot, build => build.WithExtraOriginFeat(feat));
    }

    /// <summary>Escolha num nível de talento: <see cref="LevelChoiceIds.AbilityScoreImprovement"/> ou o id de um talento.</summary>
    public void SetLevelChoice(int slot, int level, string choiceId)
    {
        var choice = LevelChoiceIds.Get(rules, choiceId);
        Update(slot, build => build.WithLevelChoice(level, choice));
    }

    private void Update(int slot, Func<CharacterBuild, CharacterBuild> change) => Set(Current.UpdateSlot(slot, change));

    private void Set(BuildComparison comparison)
    {
        Current = comparison;
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
