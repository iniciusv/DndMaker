using DndMaker.Domain.Builds;
using DndMaker.Domain.Catalog;
using DndMaker.Domain.Routines;

namespace DndMaker.Application;

/// <summary>Textos que a tela mostra para valores do domínio, como na página original.</summary>
public static class DisplayText
{
    public const string AbilityScoreImprovement = "Aumento de atributo (+2)";

    /// <summary>Nome curto da faixa de recurso, usado em cada rotina.</summary>
    public static string TierName(ResourceTier tier) => tier switch
    {
        ResourceTier.AtWill => "à vontade",
        ResourceTier.Concentration => "com magia de concentração",
        ResourceTier.Peak => "pico (gasta recurso)",
        _ => throw new ArgumentOutOfRangeException(nameof(tier), tier, "Faixa de recurso desconhecida."),
    };

    /// <summary>Texto do filtro "contar rotinas até esta faixa".</summary>
    public static string TierFilter(ResourceTier tier) => tier switch
    {
        ResourceTier.AtWill => "só à vontade",
        ResourceTier.Concentration => "até magia de concentração",
        ResourceTier.Peak => "até o pico, gastando recursos",
        _ => throw new ArgumentOutOfRangeException(nameof(tier), tier, "Faixa de recurso desconhecida."),
    };

    /// <summary>Por que a rotina fica fora do melhor turno.</summary>
    public static string Situation(Situation situation) => situation switch
    {
        Domain.Routines.Situation.VsUndead => "só contra mortos-vivos",
        Domain.Routines.Situation.SpeciesOption => "opcional da espécie",
        _ => throw new ArgumentOutOfRangeException(nameof(situation), situation, "Situação desconhecida."),
    };

    public static string Status(ArchetypeStatus status) => status switch
    {
        ArchetypeStatus.Finalist => "finalista",
        ArchetypeStatus.UnderReview => "em análise",
        ArchetypeStatus.LittleExplored => "pouco explorada",
        ArchetypeStatus.Discarded => "descartada",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Situação de build desconhecida."),
    };

    /// <summary>Nome da escolha num nível de talento. Talentos de origem levam o aviso, como na página.</summary>
    public static string LevelChoiceName(LevelChoice choice) => choice switch
    {
        FeatChoice { Feat: { Category: FeatCategory.Origin } feat } => $"{feat.Name} (talento de origem)",
        FeatChoice feat => feat.Feat.Name,
        _ => AbilityScoreImprovement,
    };
}
