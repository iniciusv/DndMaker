using System.Collections.Concurrent;
using DndMaker.Content.Campaigns;
using DndMaker.Content.Catalog;
using DndMaker.Domain.Abilities;
using DndMaker.Domain.Analysis;
using DndMaker.Domain.Builds;
using DndMaker.Domain.Routines;

namespace DndMaker.Content.Tests.Parity;

/// <summary>
/// O mesmo teste de fumaça do test.js original: toda build × variante × espécie × antecedente × nível × escolha
/// de nível de talento (a mesma em todos os níveis), com Magic Initiate (Druida) como talento extra.
/// Nada pode lançar exceção, todo número tem de ser finito e nenhum atributo passa de 20.
/// </summary>
public sealed class ExhaustiveTests
{
    /// <summary>
    /// 21 variantes × 26 espécies × 20 antecedentes × 10 níveis × 10 escolhas: as 1.040.000 do test.js,
    /// mais 52.000 da variante de bordão do Guerreiro 1 → Bardo do Valor (problema 4 do plano)
    /// e 52.000 do Paladino (Ancients), que não tem variantes.
    /// </summary>
    private const int ExpectedCombinations = 1_144_000;

    private const int MaxReported = 10;

    [Fact]
    public void Todas_as_combinacoes_geram_ficha_valida()
    {
        var rules = CurseOfStrahd.Ruleset;
        var campaign = rules.Campaign;
        var analyzer = new CharacterAnalyzer(campaign.CharmThreat);
        var assumptions = new CombatAssumptions(campaign.DefaultEnemyArmorClass);
        var levels = campaign.Levels.ToList();
        var failures = new ConcurrentQueue<string>();
        var count = 0;

        var seeds =
            from archetype in rules.Archetypes
            from variant in archetype.Variants.IsEmpty ? [null] : archetype.Variants.Cast<BuildVariant?>()
            from species in rules.Species
            select (archetype, variant, species);

        Parallel.ForEach(seeds, seed =>
        {
            foreach (var background in rules.Backgrounds)
            {
                foreach (var choice in rules.LevelChoices)
                {
                    var build = WithChoiceEverywhere(
                        CharacterBuild.Create(seed.archetype, seed.species, background, FeatCatalog.MagicInitiateDruid, seed.variant),
                        choice,
                        campaign.MaxLevel);

                    foreach (var level in levels)
                    {
                        Interlocked.Increment(ref count);
                        if (Problem(analyzer, build, level, assumptions) is { } problem)
                        {
                            failures.Enqueue($"{seed.archetype.Id}[{seed.variant?.Id}] {seed.species.Id}/{background.Id} {Label(choice)} L{level}: {problem}");
                        }
                    }
                }
            }
        });

        count.Should().Be(ExpectedCombinations);
        failures.Should().BeEmpty("{0} combinações falharam, por exemplo:\n{1}", failures.Count, string.Join("\n", failures.Take(MaxReported)));
    }

    private static CharacterBuild WithChoiceEverywhere(CharacterBuild build, LevelChoice choice, int maxLevel) =>
        build.Archetype.FeatLevels(maxLevel).Aggregate(build, (current, level) => current.WithLevelChoice(level, choice));

    private static string Label(LevelChoice choice) => choice is FeatChoice feat ? feat.Feat.Id.Value : GoldenAdapter.AbilityScoreImprovementId;

    private static string? Problem(CharacterAnalyzer analyzer, CharacterBuild build, Domain.Progression.Level level, CombatAssumptions assumptions)
    {
        try
        {
            var sheet = analyzer.Analyze(build, level, assumptions);
            if (AbilityList.All.Any(a => sheet.Scores[a].Value > AbilityScore.CharacterMax))
            {
                return "atributo acima de 20";
            }

            if (sheet.Routines.IsEmpty)
            {
                return "nenhuma rotina de dano";
            }

            if (sheet.Routines.Any(r => !double.IsFinite(r.Expected) || !double.IsFinite(r.Raw)))
            {
                return "dano não finito";
            }

            return Enum.GetValues<ResourceTier>().Any(t => sheet.Best(t) is null) ? "sem melhor rotina" : null;
        }
#pragma warning disable CA1031 // Toda exceção é uma falha a relatar, junto com as demais.
        catch (Exception ex)
#pragma warning restore CA1031
        {
            return $"{ex.GetType().Name}: {ex.Message}";
        }
    }
}
