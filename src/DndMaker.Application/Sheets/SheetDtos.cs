using DndMaker.Domain.Routines;

namespace DndMaker.Application.Sheets;

public sealed record AbilityDto(string Abbreviation, int Score, int Modifier);

/// <summary>O que um nível de talento fez. <see cref="Changes"/> vazio quer dizer que não mudou atributo.</summary>
public sealed record PickDto(int Level, string ChoiceId, string ChoiceName, string Changes);

public sealed record SaveDto(string Abbreviation, int Modifier, bool Proficient, bool Advantage);

/// <summary>
/// Chance de passar num teste (charme ou concentração): <see cref="Chance"/> gastando recursos e
/// <see cref="ChanceWithoutResources"/> sem gastar nada. <see cref="Resources"/> é o que entrou na conta;
/// <see cref="Remedies"/>, o que encerra a condição depois, fora da conta.
/// </summary>
public sealed record ResistanceDto(
    string Ability,
    int Dc,
    double Chance,
    double ChanceWithoutResources,
    bool Advantage,
    IReadOnlyList<string> Resources,
    IReadOnlyList<string> Remedies)
{
    /// <summary>De onde vem a imunidade (Aura of Devotion); nulo se o teste for feito.</summary>
    public string? ImmunitySource { get; init; }
}

/// <summary>
/// Uma rotina de dano. <see cref="AttackBonus"/> é o do primeiro ataque (como na página); <see cref="AttackBonuses"/>
/// traz todos os diferentes. <see cref="Situation"/> marca as rotinas que não entram no melhor turno.
/// </summary>
public sealed record RoutineDto(
    string Name,
    ResourceTier Tier,
    string TierName,
    bool MultipleTargets,
    string Note,
    string Formula,
    double Raw,
    double Expected,
    int? AttackBonus,
    int? Dc,
    bool Counted,
    bool IsBest)
{
    public IReadOnlyList<int> AttackBonuses { get; init; } = [];

    public string? Situation { get; init; }
}

public enum MilestoneTime
{
    Past,
    Current,
    Future,
}

public sealed record MilestoneDto(int Level, string Text, MilestoneTime Time);

public sealed record DescriptionDto(string Name, string Description);

/// <summary>Tudo o que uma carta mostra sobre um personagem num nível.</summary>
public sealed record SheetDto
{
    public required string ArchetypeId { get; init; }

    public required string Name { get; init; }

    public required string Epithet { get; init; }

    public required string Summary { get; init; }

    public required string Status { get; init; }

    public required string? VariantId { get; init; }

    public required int Level { get; init; }

    public required int ProficiencyBonus { get; init; }

    public required int ArmorClass { get; init; }

    public required string ArmorDescription { get; init; }

    public required bool StealthDisadvantage { get; init; }

    public required string? ArmorExtra { get; init; }

    public required int HitPoints { get; init; }

    public required int Initiative { get; init; }

    /// <summary>Alcance da visão no escuro em metros; zero se não tiver.</summary>
    public required int DarkvisionMeters { get; init; }

    public required string? DarkvisionNote { get; init; }

    /// <summary>O alcance da espécie ainda precisa ser conferido no livro (e nada da build o substitui).</summary>
    public required bool DarkvisionNeedsConfirmation { get; init; }

    public required IReadOnlyList<AbilityDto> Abilities { get; init; }

    /// <summary>Texto do bônus do antecedente: <c>CAR +2, CON +1</c>.</summary>
    public required string BackgroundBonus { get; init; }

    public required IReadOnlyList<PickDto> Picks { get; init; }

    public required IReadOnlyList<SaveDto> Saves { get; init; }

    public required int SaveBonus { get; init; }

    public required IReadOnlyList<string> SaveBonusSources { get; init; }

    public required ResistanceDto Charm { get; init; }

    /// <summary>Nulo para builds sem magias de concentração.</summary>
    public required ResistanceDto? Concentration { get; init; }

    public required IReadOnlyList<RoutineDto> Routines { get; init; }

    /// <summary>A rotina de maior dano esperado até a faixa escolhida; nula se nenhuma servir.</summary>
    public required RoutineDto? BestRoutine { get; init; }

    /// <summary>A variante pede um talento que o personagem não tem (Shillelagh sem Magic Initiate).</summary>
    public required bool VariantRequirementMissing { get; init; }

    public required IReadOnlyList<MilestoneDto> Milestones { get; init; }

    public required DescriptionDto Species { get; init; }

    public required string SpeciesSource { get; init; }

    public required IReadOnlyList<DescriptionDto> Feats { get; init; }

    public required string BackgroundSkills { get; init; }

    public required string Pros { get; init; }

    public required string Cons { get; init; }
}
