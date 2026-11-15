namespace DndMaker.Application.Catalog;

/// <summary>Uma opção de lista: id para gravar e nome para mostrar.</summary>
public sealed record OptionDto(string Id, string Name);

public sealed record ArchetypeOptionDto(
    string Id,
    string Name,
    string Status,
    bool IsFinalist,
    IReadOnlyList<OptionDto> Variants,
    IReadOnlyList<int> FeatLevels);

public sealed record SpeciesOptionDto(string Id, string Name, string Source, bool GrantsExtraOriginFeat);

/// <summary>Antecedente com o rótulo da lista: <c>Nobre · FOR, INT, CAR · Skilled</c>.</summary>
public sealed record BackgroundOptionDto(string Id, string Name, string Label);

public sealed record TierOptionDto(int Value, string Name, string FilterLabel);

public sealed record EnemyArmorClassDto(int Value, string Label);

public sealed record PhaseDto(int FromLevel, int ToLevel, string Name);

public sealed record NoteItemDto(string? Lead, string Text, Uri? Link);

public sealed record NoteSectionDto(string Title, IReadOnlyList<NoteItemDto> Items);

/// <summary>Tudo o que os seletores da tela oferecem, na ordem de exibição.</summary>
public sealed record CatalogDto(
    string CampaignName,
    int MaxLevel,
    IReadOnlyList<int> HighlightLevels,
    IReadOnlyList<PhaseDto> Phases,
    IReadOnlyList<EnemyArmorClassDto> EnemyArmorClasses,
    int DefaultEnemyArmorClass,
    IReadOnlyList<TierOptionDto> Tiers,
    IReadOnlyList<ArchetypeOptionDto> Archetypes,
    IReadOnlyList<SpeciesOptionDto> Species,
    IReadOnlyList<BackgroundOptionDto> Backgrounds,
    IReadOnlyList<OptionDto> ExtraOriginFeats,
    IReadOnlyList<OptionDto> LevelChoices,
    IReadOnlyList<NoteSectionDto> Notes);
