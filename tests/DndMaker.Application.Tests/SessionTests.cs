using DndMaker.Application.Session;
using DndMaker.Content.Campaigns;
using DndMaker.Domain.Common;
using DndMaker.Domain.Routines;

namespace DndMaker.Application.Tests;

public sealed class SessionTests
{
    private static ComparisonSession NewSession() => new(CurseOfStrahd.Ruleset);

    [Fact]
    public void Comeca_na_comparacao_inicial_da_campanha()
    {
        var session = NewSession();

        session.Current.Level.Value.Should().Be(5);
        session.Current.Slots.Select(s => s.Archetype.Id.Value).Should().Equal("fwl", "valor", "pal");
        session.Current.Slots[1].ExtraOriginFeat!.Id.Value.Should().Be("mi_druid");
    }

    [Fact]
    public void Cada_mudanca_avisa()
    {
        var session = NewSession();
        var changes = 0;
        session.Changed += (_, _) => changes++;

        session.SetLevel(7);
        session.SetEnemyArmorClass(18);
        session.SetMaxTier(ResourceTier.Peak);

        changes.Should().Be(3);
        session.Current.Level.Value.Should().Be(7);
        session.Current.Assumptions.TargetArmorClass.Value.Should().Be(18);
        session.Current.MaxTier.Should().Be(ResourceTier.Peak);
    }

    [Fact]
    public void Trocar_a_build_mantem_especie_e_talento_extra()
    {
        var session = NewSession();
        session.SetSpecies(0, "gnomo");
        session.SetLevelChoice(0, 5, "fey");

        session.ChangeArchetype(0, "gloom");

        var build = session.Current.Slots[0];
        build.Archetype.Id.Value.Should().Be("gloom");
        build.Species.Id.Value.Should().Be("gnomo");
        build.ExtraOriginFeat!.Id.Value.Should().Be("mi_wizard");
        build.Background.Id.Value.Should().Be("andarilho");
        build.LevelChoices.Should().BeEmpty();
    }

    [Fact]
    public void Carregar_da_tabela_usa_todas_as_escolhas_padrao()
    {
        var session = NewSession();
        session.SetSpecies(1, "elfo");

        session.LoadDefaultCharacter(1, "twi");

        var build = session.Current.Slots[1];
        build.Archetype.Id.Value.Should().Be("twi");
        build.Species.Id.Value.Should().Be("humano");
        build.ExtraOriginFeat!.Id.Value.Should().Be("alert");
        build.Variant!.Id.Value.Should().Be("flame");
    }

    [Fact]
    public void Escolhas_por_carta()
    {
        var session = NewSession();

        session.SetVariant(2, "defense");
        session.SetBackground(2, "haunted");
        session.SetExtraOriginFeat(2, "tough");
        session.SetLevelChoice(2, 4, "res_wis");
        session.SetLevelChoice(2, 8, LevelChoiceIds.AbilityScoreImprovement);

        var build = session.Current.Slots[2];
        build.Variant!.Id.Value.Should().Be("defense");
        build.Background.Id.Value.Should().Be("haunted");
        build.ExtraOriginFeat!.Id.Value.Should().Be("tough");
        build.LevelChoices.Keys.Should().Equal(4);
        LevelChoiceIds.Of(build.ChoiceAt(4)).Should().Be("res_wis");
    }

    [Fact]
    public void Ids_invalidos_lancam_excecao_e_nao_mudam_nada()
    {
        var session = NewSession();
        var before = session.Current;

        session.Invoking(s => s.SetVariant(0, "arco")).Should().Throw<ArgumentException>();
        session.Invoking(s => s.SetSpecies(0, "elfo-do-mar")).Should().Throw<DomainException>();
        session.Invoking(s => s.SetExtraOriginFeat(0, "fey")).Should().Throw<ArgumentException>();
        session.Invoking(s => s.SetLevelChoice(0, 5, "dg_mist")).Should().Throw<ArgumentException>();
        session.Invoking(s => s.SetLevelChoice(0, 4, "fey")).Should().Throw<DomainException>("o fwl escolhe talento no 5 e no 9");
        session.Invoking(s => s.SetEnemyArmorClass(14)).Should().Throw<ArgumentOutOfRangeException>();
        session.Invoking(s => s.SetLevel(11)).Should().Throw<ArgumentOutOfRangeException>();

        session.Current.Should().BeSameAs(before);
    }
}

public sealed class SnapshotMapperTests
{
    private readonly ComparisonSnapshotMapper _mapper = new(CurseOfStrahd.Ruleset);

    private static ComparisonSnapshot Snapshot(params SlotSnapshot[] slots) => new(6, 16, 2, slots);

    private static SlotSnapshot Slot(string archetype = "pal", string? variant = "defense", string species = "gnomo",
        string background = "haunted", string? extra = "tough", Dictionary<int, string>? choices = null) =>
        new(archetype, variant, species, background, extra, choices ?? new Dictionary<int, string> { [4] = "fey" });

    [Fact]
    public void Ida_e_volta_preserva_tudo()
    {
        var session = new ComparisonSession(CurseOfStrahd.Ruleset);
        session.SetLevel(8);
        session.SetEnemyArmorClass(13);
        session.SetMaxTier(ResourceTier.AtWill);
        session.SetSpecies(0, "anao");
        session.SetLevelChoice(0, 5, "warcaster");
        session.SetLevelChoice(2, 8, "lucky");

        var snapshot = session.ToSnapshot();
        var restored = ComparisonSnapshotMapper.ToSnapshot(_mapper.FromSnapshot(snapshot));

        restored.Should().BeEquivalentTo(snapshot);
        snapshot.Slots[0].LevelChoices.Should().Equal(new Dictionary<int, string> { [5] = "warcaster" });
    }

    [Fact]
    public void Nada_gravado_ou_numero_errado_de_cartas_volta_ao_inicio()
    {
        var initial = ComparisonSnapshotMapper.ToSnapshot(CurseOfStrahd.Ruleset.CreateDefaultComparison());

        ComparisonSnapshotMapper.ToSnapshot(_mapper.FromSnapshot(null)).Should().BeEquivalentTo(initial);
        ComparisonSnapshotMapper.ToSnapshot(_mapper.FromSnapshot(Snapshot(Slot(), Slot()))).Should().BeEquivalentTo(initial);
    }

    [Fact]
    public void Valores_fora_das_opcoes_voltam_ao_padrao()
    {
        var comparison = _mapper.FromSnapshot(new ComparisonSnapshot(42, 14, 9, [Slot(), Slot(), Slot()]));

        comparison.Level.Value.Should().Be(5);
        comparison.Assumptions.TargetArmorClass.Value.Should().Be(15);
        comparison.MaxTier.Should().Be(ResourceTier.Concentration);
    }

    [Fact]
    public void Build_que_sumiu_volta_a_carta_inicial()
    {
        var comparison = _mapper.FromSnapshot(Snapshot(Slot(), Slot(archetype: "bladesinger"), Slot()));

        comparison.Slots[1].Archetype.Id.Value.Should().Be("valor");
        comparison.Slots[0].Archetype.Id.Value.Should().Be("pal");
    }

    [Fact]
    public void Ids_que_sumiram_viram_o_padrao_da_build()
    {
        var slot = Slot(variant: "arco", species: "elfo-do-mar", background: "pirata", extra: "fey",
            choices: new Dictionary<int, string> { [4] = "nada", [5] = "fey", [8] = "dg_mist" });

        var build = _mapper.FromSlot(slot)!;

        build.Variant!.Id.Value.Should().Be("dueling", "a variante padrão do paladino");
        build.Species.Id.Value.Should().Be("humano");
        build.Background.Id.Value.Should().Be("nobre");
        build.ExtraOriginFeat!.Id.Value.Should().Be("alert");
        build.LevelChoices.Should().BeEmpty("o 4 tem id desconhecido, o 5 não é nível de talento do paladino e Dark Gift não vale");
    }
}
