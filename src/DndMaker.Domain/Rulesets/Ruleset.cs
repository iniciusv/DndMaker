using System.Collections.Frozen;
using System.Collections.Immutable;
using DndMaker.Domain.Analysis;
using DndMaker.Domain.Builds;
using DndMaker.Domain.Catalog;
using DndMaker.Domain.Common;
using DndMaker.Domain.Comparison;
using DndMaker.Domain.Progression;
using DndMaker.Domain.Routines;

namespace DndMaker.Domain.Rulesets;

/// <summary>
/// Um pacote de regras completo: campanha, builds, espécies, antecedentes, talentos e as escolhas oferecidas.
/// As listas guardam a ordem de exibição; as buscas por id usam dicionários congelados.
/// Tudo é conferido na criação, então uma referência quebrada no conteúdo falha cedo.
/// </summary>
public sealed class Ruleset
{
    private readonly FrozenDictionary<ArchetypeId, BuildArchetype> _archetypes;
    private readonly FrozenDictionary<SpeciesId, Species> _species;
    private readonly FrozenDictionary<BackgroundId, Background> _backgrounds;
    private readonly FrozenDictionary<FeatId, Feat> _feats;

    public Ruleset(
        Campaign campaign,
        IEnumerable<BuildArchetype> archetypes,
        IEnumerable<Species> species,
        IEnumerable<Background> backgrounds,
        IEnumerable<Feat> feats,
        IEnumerable<Feat> extraOriginFeats,
        IEnumerable<LevelChoice> levelChoices)
    {
        Campaign = campaign;
        Archetypes = [.. archetypes];
        Species = [.. species];
        Backgrounds = [.. backgrounds];
        Feats = [.. feats];
        ExtraOriginFeats = [.. extraOriginFeats];
        LevelChoices = [.. levelChoices];

        _archetypes = Index(Archetypes, a => a.Id, "build");
        _species = Index(Species, s => s.Id, "espécie");
        _backgrounds = Index(Backgrounds, b => b.Id, "antecedente");
        _feats = Index(Feats, f => f.Id, "talento");

        Validate();
    }

    public Campaign Campaign { get; }

    public ImmutableArray<BuildArchetype> Archetypes { get; }

    public ImmutableArray<Species> Species { get; }

    public ImmutableArray<Background> Backgrounds { get; }

    /// <summary>Todos os talentos conhecidos (de origem, Dark Gifts e gerais).</summary>
    public ImmutableArray<Feat> Feats { get; }

    /// <summary>Os talentos oferecidos como talento extra de origem (humano).</summary>
    public ImmutableArray<Feat> ExtraOriginFeats { get; }

    /// <summary>As escolhas oferecidas nos níveis de talento, começando pelo aumento de atributo.</summary>
    public ImmutableArray<LevelChoice> LevelChoices { get; }

    public BuildArchetype GetArchetype(ArchetypeId id) => Get(_archetypes, id, "build");

    public Species GetSpecies(SpeciesId id) => Get(_species, id, "espécie");

    public Background GetBackground(BackgroundId id) => Get(_backgrounds, id, "antecedente");

    public Feat GetFeat(FeatId id) => Get(_feats, id, "talento");

    public BuildArchetype? FindArchetype(ArchetypeId id) => _archetypes.GetValueOrDefault(id);

    public Species? FindSpecies(SpeciesId id) => _species.GetValueOrDefault(id);

    public Background? FindBackground(BackgroundId id) => _backgrounds.GetValueOrDefault(id);

    public Feat? FindFeat(FeatId id) => _feats.GetValueOrDefault(id);

    /// <summary>O antecedente sugerido pela build.</summary>
    public Background SuggestedBackground(BuildArchetype archetype) => GetBackground(archetype.Profile.SuggestedBackground);

    /// <summary>
    /// A build com as escolhas padrão da campanha: espécie padrão, antecedente sugerido, primeira variante,
    /// talento extra padrão e aumento de atributo em todos os níveis de talento.
    /// </summary>
    public CharacterBuild DefaultCharacter(BuildArchetype archetype) =>
        CharacterBuild.Create(archetype, GetSpecies(Campaign.DefaultSpecies), SuggestedBackground(archetype), GetFeat(Campaign.DefaultExtraOriginFeat));

    /// <summary>
    /// O analisador da campanha: o charme dela e a divisão do antecedente planejada até o último nível
    /// (problema 8 do plano).
    /// </summary>
    public CharacterAnalyzer CreateAnalyzer() =>
        new(Campaign.CharmThreat, AbilityScoreAllocator.PlanningUpTo(Level.Of(Campaign.MaxLevel)));

    /// <summary>Como <see cref="DefaultCharacter"/>, mas com o talento extra das tabelas com todas as builds.</summary>
    public CharacterBuild OverviewCharacter(BuildArchetype archetype) =>
        DefaultCharacter(archetype).WithExtraOriginFeat(GetFeat(Campaign.OverviewExtraOriginFeat));

    /// <summary>A comparação da primeira abertura do programa.</summary>
    public BuildComparison CreateDefaultComparison()
    {
        var preset = Campaign.Preset;
        return BuildComparison.Create(
            preset.Slots.Select(FromPreset),
            Level.Of(preset.Level),
            new CombatAssumptions(Campaign.DefaultEnemyArmorClass),
            preset.MaxTier);
    }

    private CharacterBuild FromPreset(SlotPreset slot)
    {
        var archetype = GetArchetype(slot.Archetype);
        var variant = slot.Variant is { } id ? archetype.FindVariant(id) : null;
        return CharacterBuild.Create(
            archetype,
            GetSpecies(slot.Species),
            slot.Background is { } background ? GetBackground(background) : SuggestedBackground(archetype),
            slot.ExtraOriginFeat is { } feat ? GetFeat(feat) : GetFeat(Campaign.DefaultExtraOriginFeat),
            variant);
    }

    private void Validate()
    {
        if (Archetypes.IsEmpty || Species.IsEmpty || Backgrounds.IsEmpty)
        {
            throw new DomainException("O pacote de regras precisa de pelo menos uma build, uma espécie e um antecedente.");
        }

        ValidateCampaign();

        foreach (var archetype in Archetypes)
        {
            Require(_backgrounds, archetype.Profile.SuggestedBackground, $"antecedente sugerido de {archetype.Id}");
            foreach (var variant in archetype.Variants.Where(v => v.RequiredFeat is not null))
            {
                Require(_feats, variant.RequiredFeat!.Value, $"talento exigido pela variante {variant.Id} de {archetype.Id}");
            }
        }

        foreach (var background in Backgrounds)
        {
            Require(_feats, background.OriginFeat.Id, $"talento de origem de {background.Id}");
        }

        foreach (var feat in ExtraOriginFeats)
        {
            Require(_feats, feat.Id, "talento extra de origem");
            if (feat.Category != FeatCategory.Origin)
            {
                throw new DomainException($"{feat.Name} não é talento de origem e não pode ser o talento extra.");
            }
        }

        foreach (var feat in LevelChoices.OfType<FeatChoice>().Select(c => c.Feat))
        {
            Require(_feats, feat.Id, "escolha de nível de talento");
            if (feat.Category == FeatCategory.DarkGift)
            {
                throw new DomainException($"{feat.Name} é um Dark Gift e não pode ser escolha de nível de talento.");
            }
        }
    }

    private void ValidateCampaign()
    {
        var campaign = Campaign;
        if (campaign.MaxLevel is < Level.Min or > Level.Max)
        {
            throw new DomainException($"O nível máximo de {campaign.Name} precisa estar entre {Level.Min} e {Level.Max}.");
        }

        bool InRange(int level) => level >= Level.Min && level <= campaign.MaxLevel;

        if (!campaign.HighlightLevels.All(InRange)
            || !campaign.LevelRemarks.All(r => InRange(r.FromLevel) && InRange(r.ToLevel) && r.FromLevel <= r.ToLevel))
        {
            throw new DomainException($"Os níveis destacados de {campaign.Name} precisam estar entre 1 e {campaign.MaxLevel}.");
        }

        if (!campaign.Phases.IsEmpty
            && Enumerable.Range(Level.Min, campaign.MaxLevel).Any(l => campaign.Phases.Count(p => p.Contains(l)) != 1))
        {
            throw new DomainException($"As fases de {campaign.Name} precisam cobrir cada nível exatamente uma vez.");
        }

        var armorClasses = campaign.EnemyArmorClasses.Select(o => o.Value).ToList();
        if (armorClasses.Count == 0 || armorClasses.Distinct().Count() != armorClasses.Count || !armorClasses.Contains(campaign.DefaultEnemyArmorClass))
        {
            throw new DomainException($"As CAs de inimigo de {campaign.Name} precisam ser distintas e incluir a padrão ({campaign.DefaultEnemyArmorClass}).");
        }

        Require(_species, campaign.DefaultSpecies, "espécie padrão da campanha");
        RequireOriginFeat(campaign.DefaultExtraOriginFeat, "talento extra padrão da campanha");
        RequireOriginFeat(campaign.OverviewExtraOriginFeat, "talento extra das tabelas da campanha");

        var preset = campaign.Preset;
        if (!InRange(preset.Level) || preset.Slots.Length != BuildComparison.SlotCount)
        {
            throw new DomainException($"A comparação inicial de {campaign.Name} precisa de um nível válido e {BuildComparison.SlotCount} personagens.");
        }

        foreach (var slot in preset.Slots)
        {
            Require(_archetypes, slot.Archetype, "build da comparação inicial");
            Require(_species, slot.Species, "espécie da comparação inicial");
            if (slot.Background is { } background)
            {
                Require(_backgrounds, background, "antecedente da comparação inicial");
            }

            if (slot.ExtraOriginFeat is { } feat)
            {
                RequireOriginFeat(feat, "talento extra da comparação inicial");
            }

            if (slot.Variant is { } variant && _archetypes[slot.Archetype].FindVariant(variant) is null)
            {
                throw new DomainException($"A variante {variant} da comparação inicial não pertence a {slot.Archetype}.");
            }
        }
    }

    private void RequireOriginFeat(FeatId id, string what)
    {
        Require(_feats, id, what);
        if (_feats[id].Category != FeatCategory.Origin)
        {
            throw new DomainException($"O {what} ({id}) precisa ser um talento de origem.");
        }
    }

    private static FrozenDictionary<TId, T> Index<TId, T>(ImmutableArray<T> items, Func<T, TId> idOf, string kind)
        where TId : notnull
    {
        var duplicate = items.GroupBy(idOf).FirstOrDefault(g => g.Count() > 1);
        if (duplicate is not null)
        {
            throw new DomainException($"Há mais de um(a) {kind} com o id {duplicate.Key}.");
        }

        return items.ToFrozenDictionary(idOf);
    }

    private static T Get<TId, T>(FrozenDictionary<TId, T> items, TId id, string kind)
        where TId : notnull =>
        items.TryGetValue(id, out var item) ? item : throw new DomainException($"{kind} desconhecido(a): {id}.");

    private static void Require<TId, T>(FrozenDictionary<TId, T> items, TId id, string what)
        where TId : notnull
    {
        if (!items.ContainsKey(id))
        {
            throw new DomainException($"O {what} ({id}) não está no pacote de regras.");
        }
    }
}
