using DndMaker.Domain.Builds;
using DndMaker.Domain.Rulesets;

namespace DndMaker.Application;

/// <summary>
/// Ids em texto das escolhas de nível de talento, usados pela tela e pela gravação:
/// o id do talento ou <see cref="AbilityScoreImprovement"/>.
/// </summary>
public static class LevelChoiceIds
{
    public const string AbilityScoreImprovement = "asi";

    public static string Of(LevelChoice choice) => choice is FeatChoice feat ? feat.Feat.Id.Value : AbilityScoreImprovement;

    /// <summary>A escolha com esse id entre as oferecidas pelo pacote de regras, ou nulo se não houver.</summary>
    public static LevelChoice? Find(Ruleset rules, string id) =>
        rules.LevelChoices.FirstOrDefault(choice => Of(choice) == id);

    public static LevelChoice Get(Ruleset rules, string id) =>
        Find(rules, id) ?? throw new ArgumentException($"Escolha de nível desconhecida: {id}.", nameof(id));
}
