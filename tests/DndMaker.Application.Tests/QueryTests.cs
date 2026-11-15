using DndMaker.Application.Catalog;
using DndMaker.Application.Comparison;
using DndMaker.Application.Overview;
using DndMaker.Application.Sheets;
using DndMaker.Content.Campaigns;
using DndMaker.Domain.Analysis;
using DndMaker.Domain.Combat;
using DndMaker.Domain.Progression;
using DndMaker.Domain.Routines;

namespace DndMaker.Application.Tests;

/// <summary>
/// As consultas sobre o conteúdo real. Os números esperados vêm do logic.js original
/// (mesmas escolhas, nível e CA), calculados à parte.
/// </summary>
public sealed class QueryTests
{
    private const double Tolerance = 1e-9;

    private static readonly CharacterAnalyzer Analyzer = new(CurseOfStrahd.Campaign.CharmThreat);

    [Fact]
    public void Catalogo_oferece_as_opcoes_da_pagina()
    {
        var catalog = new GetCatalogQuery(CurseOfStrahd.Ruleset).Execute();

        catalog.MaxLevel.Should().Be(10);
        catalog.HighlightLevels.Should().Equal(5, 6);
        catalog.Archetypes.Should().HaveCount(10);
        catalog.Archetypes[0].Should().Match<ArchetypeOptionDto>(a => a.Id == "fwl" && a.IsFinalist && a.Status == "finalista");
        catalog.Archetypes[0].FeatLevels.Should().Equal(5, 9);
        catalog.Archetypes.Single(a => a.Id == "bm").FeatLevels.Should().Equal(4, 6, 8);
        catalog.Archetypes.Single(a => a.Id == "valor").Variants.Select(v => v.Id).Should().Equal("melee", "staff", "arco");
        catalog.Species.Should().HaveCount(26);
        catalog.Species[0].GrantsExtraOriginFeat.Should().BeTrue();
        catalog.Backgrounds.Should().HaveCount(20);
        catalog.Backgrounds.Single(b => b.Id == "nobre").Label.Should().Be("Nobre · FOR, INT, CAR · Skilled");
        catalog.ExtraOriginFeats.Select(f => f.Id).Should().StartWith(["alert", "lucky"]).And.HaveCount(12);
        catalog.LevelChoices[0].Should().Be(new OptionDto("asi", "Aumento de atributo (+2)"));
        catalog.LevelChoices.Should().Contain(new OptionDto("alert", "Alert (talento de origem)"));
        catalog.Tiers.Select(t => t.FilterLabel).Should().Equal("só à vontade", "até magia de concentração", "até o pico, gastando recursos");
        catalog.EnemyArmorClasses.Select(o => o.Value).Should().Equal(13, 15, 16, 18);
        catalog.DefaultEnemyArmorClass.Should().Be(15);
        catalog.Phases.Should().HaveCount(4);
        catalog.Notes.Should().HaveCount(8);
        catalog.Notes[^1].Items.Should().OnlyContain(i => i.Link != null);
    }

    [Fact]
    public void Comparacao_inicial_bate_com_a_pagina()
    {
        var result = new AnalyzeComparisonQuery(CurseOfStrahd.Ruleset, Analyzer).Execute(CurseOfStrahd.Ruleset.CreateDefaultComparison());

        result.Level.Should().Be(new LevelInfoDto(5, "Barovia e Vallaki", "janela que você quer forte", IsHighlight: true));
        result.EnemyArmorClass.Should().Be(15);
        result.MaxTier.Should().Be((int)ResourceTier.Concentration);

        result.Cards.Select(c => c.ArmorClass).Should().Equal(19, 19, 19);
        result.Cards.Select(c => c.HitPoints).Should().Equal(45, 33, 39);
        result.Cards.Select(c => c.Initiative).Should().Equal(1, 4, 3);
        result.Cards.Select(c => c.Charm.Chance).Should().SatisfyRespectively(
            p => p.Should().BeApproximately(0.20, Tolerance),
            p => p.Should().BeApproximately(0.25, Tolerance),
            p => p.Should().BeApproximately(0.40, Tolerance));
        result.Cards.Select(c => c.BestRoutine!.Name).Should().Equal(
            "Eldritch Blast (à distância)", "True Strike (bordão com Shillelagh)", "Espada longa");
        result.Cards[0].BestRoutine!.Expected.Should().BeApproximately(12.35, Tolerance);

        result.Leaders.Should().Be(new LeadersDto(ArmorClass: null, HitPoints: 0, Initiative: 1), "a CA empata nas três");
    }

    [Fact]
    public void Carta_traz_os_detalhes_da_ficha()
    {
        var comparison = CurseOfStrahd.Ruleset.CreateDefaultComparison().WithLevel(Level.Of(6));
        var paladin = new AnalyzeComparisonQuery(CurseOfStrahd.Ruleset, Analyzer).Execute(comparison).Cards[2];

        paladin.Name.Should().Be("Paladino (Watchers)");
        paladin.BackgroundBonus.Should().Be("FOR +2, CAR +1");
        paladin.Picks.Should().ContainSingle().Which.Should().Be(new PickDto(4, "asi", "Aumento de atributo (+2)", "FOR 17 → 18, CAR 15 → 16"));
        paladin.SaveBonus.Should().Be(3);
        paladin.SaveBonusSources.Should().Equal("Aura of Protection");
        paladin.Saves.Single(s => s.Abbreviation == "SAB").Should().Be(new SaveDto("SAB", 7, Proficient: true, Advantage: false));
        paladin.Charm.Should().Match<ResistanceDto>(c => c.Ability == "SAB" && c.Dc == 17);
        paladin.Concentration!.Dc.Should().Be(10);
        paladin.Milestones.Should().Contain(m => m.Level == 6 && m.Time == MilestoneTime.Current);
        paladin.Milestones.Should().Contain(m => m.Level == 7 && m.Time == MilestoneTime.Future);
        paladin.Feats.Select(f => f.Name).Should().Equal("Skilled", "Alert");
        paladin.BackgroundSkills.Should().Be("História, Persuasão");

        var routines = paladin.Routines;
        routines.Should().ContainSingle(r => r.IsBest).Which.Name.Should().Be("Espada longa");
        routines.Where(r => !r.Counted).Should().OnlyContain(r => r.Tier == ResourceTier.Peak);
        routines[0].AttackBonus.Should().Be(7);
    }

    [Fact]
    public void Shillelagh_sem_Magic_Initiate_avisa_na_carta()
    {
        var comparison = CurseOfStrahd.Ruleset.CreateDefaultComparison();
        var bard = comparison.Slots[1].WithExtraOriginFeat(CurseOfStrahd.Ruleset.GetFeat(new("alert")));
        var card = new AnalyzeComparisonQuery(CurseOfStrahd.Ruleset, Analyzer).Execute(comparison.WithSlot(1, bard)).Cards[1];

        card.VariantRequirementMissing.Should().BeTrue();
        card.BestRoutine!.Name.Should().Be("True Strike (rapieira)");
    }

    [Fact]
    public void Visao_no_escuro_a_confirmar_so_quando_a_build_nao_substitui()
    {
        var rules = CurseOfStrahd.Ruleset;
        var lupin = rules.GetSpecies(new("lupin"));
        var comparison = rules.CreateDefaultComparison();
        comparison = comparison.WithSlot(2, comparison.Slots[2].WithSpecies(lupin));

        var query = new AnalyzeComparisonQuery(rules, Analyzer);
        query.Execute(comparison).Cards[2].DarkvisionNeedsConfirmation.Should().BeTrue("o paladino não tem visão no escuro própria");

        var warlock = comparison.WithSlot(2, comparison.Slots[0].WithSpecies(lupin));
        query.Execute(warlock).Cards[2].DarkvisionNeedsConfirmation.Should().BeFalse("o Devil's Sight já explica a visão no escuro");
    }

    [Fact]
    public void Todas_as_builds_usam_Lucky_e_marcam_o_topo()
    {
        var overview = new BuildsOverviewQuery(CurseOfStrahd.Ruleset, Analyzer)
            .Execute(Level.Of(5), new ArmorClass(15), ResourceTier.Concentration);

        overview.ExtraOriginFeatName.Should().Be("Lucky");
        overview.SpeciesName.Should().Be("Humano");
        overview.MaxTierName.Should().Be("com magia de concentração");
        overview.Rows.Select(r => r.ArchetypeId).Should().Equal("fwl", "valor", "pal", "twi", "gloom", "lock", "swords", "fval", "shadow", "bm");

        var paladin = overview.Rows.Single(r => r.ArchetypeId == "pal");
        paladin.Should().Match<OverviewRowDto>(r => r.ArmorClass == 19 && r.HitPoints == 39 && r.Initiative == 0
            && r.WisdomSave == 4 && r.ConstitutionSave == 1 && r.BestRoutine == "Espada longa");
        paladin.CharmChance.Should().BeApproximately(0.40, Tolerance);
        paladin.Milestone.Should().Be("Extra Attack; magias de 2º círculo");

        var gloom = overview.Rows.Single(r => r.ArchetypeId == "gloom");
        gloom.Initiative.Should().Be(7);
        gloom.BestRoutine.Should().Be("Arco longo + Hunter's Mark");
        gloom.BestExpected.Should().BeApproximately(18, Tolerance);

        overview.Rows.Should().Contain(r => r.TopArmorClass).And.Contain(r => r.TopExpected);
        overview.Rows.Where(r => r.TopInitiative).Should().OnlyContain(r => r.Initiative == overview.Rows.Max(x => x.Initiative));
    }

    [Fact]
    public void Dano_por_nivel_cobre_todas_as_builds_e_niveis()
    {
        var grid = new DamageProgressionQuery(CurseOfStrahd.Ruleset, Analyzer).Execute(new ArmorClass(13), ResourceTier.Peak);

        grid.Levels.Should().Equal(Enumerable.Range(1, 10));
        grid.HighlightLevels.Should().Equal(5, 6);
        grid.Rows.Should().HaveCount(10).And.OnlyContain(r => r.Expected.Count == 10);
        grid.Rows.Single(r => r.ArchetypeId == "shadow").Expected[4].Should().BeApproximately(22.4, Tolerance);
        grid.Max.Should().Be(grid.Rows.SelectMany(r => r.Expected).Max());

        var concentration = new DamageProgressionQuery(CurseOfStrahd.Ruleset, Analyzer).Execute(new ArmorClass(15), ResourceTier.Concentration);
        concentration.Rows.Single(r => r.ArchetypeId == "twi").Expected[5].Should().BeApproximately(16.2, Tolerance);
    }
}
