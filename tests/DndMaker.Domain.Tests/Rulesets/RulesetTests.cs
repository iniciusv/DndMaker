using DndMaker.Domain.Abilities;
using DndMaker.Domain.Analysis;
using DndMaker.Domain.Builds;
using DndMaker.Domain.Catalog;
using DndMaker.Domain.Common;
using DndMaker.Domain.Routines;
using DndMaker.Domain.Rulesets;
using static DndMaker.Domain.Tests.Fixtures.TestRules;

namespace DndMaker.Domain.Tests.Rulesets;

public sealed class RulesetTests
{
    private static readonly SlotPreset PaladinSlot = new(new("pal"), new("defense"), new("gnomo"), null, null);
    private static readonly SlotPreset WarlockSlot = new(new("fwl"), null, new("humano"), new("nobre"), new("skilled"));

    private static Campaign TestCampaign() => new()
    {
        Name = "Teste",
        CharmThreat = StrahdCharm,
        MaxLevel = 10,
        HighlightLevels = [5, 6],
        Phases = [new(1, 4, "Começo"), new(5, 10, "Fim")],
        LevelRemarks = [new(5, 6, "janela")],
        EnemyArmorClasses = [new(new(13), "13"), new(new(15), "15")],
        DefaultEnemyArmorClass = new(15),
        DefaultSpecies = new("humano"),
        DefaultExtraOriginFeat = new("alert"),
        OverviewExtraOriginFeat = new("skilled"),
        Preset = new ComparisonPreset(5, ResourceTier.Concentration, [PaladinSlot, WarlockSlot, PaladinSlot]),
    };

    private static Ruleset Create(
        Campaign? campaign = null,
        IEnumerable<Feat>? feats = null,
        IEnumerable<Feat>? extraOriginFeats = null,
        IEnumerable<LevelChoice>? levelChoices = null,
        IEnumerable<Background>? backgrounds = null) => new(
        campaign ?? TestCampaign(),
        [PaladinBuild, FighterWarlockBuild],
        [Human, Gnome],
        backgrounds ?? [Noble, Haunted],
        feats ?? [Alert, Skilled, Survivor, MistWalker, FeyTouched],
        extraOriginFeats ?? [Alert],
        levelChoices ?? [LevelChoice.AbilityScoreImprovement, LevelChoice.ForFeat(FeyTouched)]);

    [Fact]
    public void Busca_por_id_e_guarda_a_ordem()
    {
        var rules = Create();

        rules.Archetypes.Should().Equal(PaladinBuild, FighterWarlockBuild);
        rules.GetSpecies(new SpeciesId("gnomo")).Should().Be(Gnome);
        rules.GetFeat(new FeatId("fey")).Should().Be(FeyTouched);
        rules.FindBackground(new BackgroundId("eremita")).Should().BeNull();
        rules.SuggestedBackground(PaladinBuild).Should().Be(Noble);
    }

    [Fact]
    public void Id_desconhecido_lanca_excecao_de_dominio()
    {
        var act = () => Create().GetArchetype(new ArchetypeId("nada"));
        act.Should().Throw<DomainException>().WithMessage("*nada*");
    }

    [Fact]
    public void Personagem_padrao_usa_as_escolhas_da_campanha()
    {
        var rules = Create();
        var build = rules.DefaultCharacter(PaladinBuild);

        build.Species.Should().Be(Human);
        build.Background.Should().Be(Noble);
        build.Variant.Should().Be(PaladinBuild.DefaultVariant);
        build.EffectiveExtraOriginFeat.Should().Be(Alert);
        build.LevelChoices.Should().BeEmpty();
        rules.OverviewCharacter(PaladinBuild).EffectiveExtraOriginFeat.Should().Be(Skilled);
    }

    [Fact]
    public void Comparacao_inicial_segue_o_preset()
    {
        var comparison = Create().CreateDefaultComparison();

        comparison.Level.Value.Should().Be(5);
        comparison.MaxTier.Should().Be(ResourceTier.Concentration);
        comparison.Assumptions.TargetArmorClass.Value.Should().Be(15);

        var paladin = comparison.Slots[0];
        paladin.Archetype.Should().Be(PaladinBuild);
        paladin.Variant!.Id.Value.Should().Be("defense");
        paladin.Species.Should().Be(Gnome);
        paladin.Background.Should().Be(Noble, "sem antecedente no preset, vale o sugerido");
        paladin.ExtraOriginFeat.Should().Be(Alert, "sem talento no preset, vale o padrão da campanha");

        var warlock = comparison.Slots[1];
        warlock.Variant.Should().Be(FighterWarlockBuild.DefaultVariant);
        warlock.Background.Should().Be(Noble);
        warlock.ExtraOriginFeat.Should().Be(Skilled);
    }

    [Fact]
    public void Campanha_responde_fase_e_observacao_do_nivel()
    {
        var campaign = TestCampaign();

        campaign.PhaseAt(5)!.Name.Should().Be("Fim");
        campaign.RemarkAt(6)!.Text.Should().Be("janela");
        campaign.RemarkAt(7).Should().BeNull();
        campaign.IsHighlight(5).Should().BeTrue();
        campaign.Levels.Select(l => l.Value).Should().Equal(Enumerable.Range(1, 10));
    }

    [Fact]
    public void Recusa_ids_repetidos()
    {
        var act = () => Create(feats: [Alert, Alert, Skilled, Survivor]);
        act.Should().Throw<DomainException>().WithMessage("*alert*");
    }

    [Fact]
    public void Recusa_antecedente_sugerido_que_nao_existe()
    {
        var act = () => Create(backgrounds: [Haunted]);
        act.Should().Throw<DomainException>().WithMessage("*nobre*");
    }

    [Fact]
    public void Recusa_talento_de_antecedente_fora_do_catalogo()
    {
        var act = () => Create(feats: [Alert, Survivor, FeyTouched]);
        act.Should().Throw<DomainException>().WithMessage("*skilled*");
    }

    [Fact]
    public void Talento_extra_precisa_ser_de_origem()
    {
        var act = () => Create(extraOriginFeats: [FeyTouched]);
        act.Should().Throw<DomainException>().WithMessage("*Fey Touched*");
    }

    [Fact]
    public void Dark_Gift_nao_pode_ser_escolha_de_nivel()
    {
        var act = () => Create(levelChoices: [LevelChoice.ForFeat(MistWalker)]);
        act.Should().Throw<DomainException>().WithMessage("*Dark Gift*");
    }

    [Fact]
    public void Campanha_exige_CA_padrao_entre_as_oferecidas()
    {
        var act = () => Create(TestCampaign() with { DefaultEnemyArmorClass = new(14) });
        act.Should().Throw<DomainException>().WithMessage("*CAs*");
    }

    [Fact]
    public void Campanha_exige_fases_sem_buraco()
    {
        var act = () => Create(TestCampaign() with { Phases = [new(1, 4, "Começo"), new(6, 10, "Fim")] });
        act.Should().Throw<DomainException>().WithMessage("*fases*");
    }

    [Fact]
    public void Preset_precisa_de_tres_personagens_validos()
    {
        var withTwo = () => Create(TestCampaign() with { Preset = new ComparisonPreset(5, ResourceTier.AtWill, [PaladinSlot, PaladinSlot]) });
        withTwo.Should().Throw<DomainException>().WithMessage("*3 personagens*");

        var wrongVariant = PaladinSlot with { Variant = new("arco") };
        var withWrongVariant = () => Create(TestCampaign() with { Preset = new ComparisonPreset(5, ResourceTier.AtWill, [wrongVariant, PaladinSlot, PaladinSlot]) });
        withWrongVariant.Should().Throw<DomainException>().WithMessage("*arco*");
    }

    [Fact]
    public void Talento_padrao_da_campanha_precisa_ser_de_origem()
    {
        var act = () => Create(TestCampaign() with { DefaultExtraOriginFeat = new("fey") });
        act.Should().Throw<DomainException>().WithMessage("*origem*");
    }
}

public sealed class SheetTextTests
{
    [Theory]
    [InlineData(Ability.Strength, "FOR")]
    [InlineData(Ability.Dexterity, "DES")]
    [InlineData(Ability.Constitution, "CON")]
    [InlineData(Ability.Intelligence, "INT")]
    [InlineData(Ability.Wisdom, "SAB")]
    [InlineData(Ability.Charisma, "CAR")]
    public void Sigla_do_atributo(Ability ability, string expected) =>
        ability.Abbreviation().Should().Be(expected);

    [Fact]
    public void Textos_do_antecedente_e_dos_niveis_de_talento()
    {
        new BackgroundBonus(Ability.Charisma, Ability.Constitution).ToString().Should().Be("CAR +2, CON +1");

        var pick = new LevelPick(4, LevelChoice.AbilityScoreImprovement,
            [new ScoreChange(Ability.Charisma, 17, 18), new ScoreChange(Ability.Constitution, 15, 16)]);
        pick.ChangesText.Should().Be("CAR 17 → 18, CON 15 → 16");

        new LevelPick(8, LevelChoice.AbilityScoreImprovement, []).ChangesText.Should().BeEmpty();
    }
}
