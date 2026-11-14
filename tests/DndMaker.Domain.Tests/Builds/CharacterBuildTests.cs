using DndMaker.Domain.Builds;
using DndMaker.Domain.Catalog;
using DndMaker.Domain.Combat;
using DndMaker.Domain.Common;
using DndMaker.Domain.Comparison;
using DndMaker.Domain.Progression;
using DndMaker.Domain.Routines;
using DndMaker.Domain.Tests.Fixtures;
using static DndMaker.Domain.Tests.Fixtures.TestRules;

namespace DndMaker.Domain.Tests.Builds;

public sealed class CharacterBuildTests
{
    [Fact]
    public void Sem_variante_usa_a_primeira()
    {
        Paladin6().Variant!.Id.Should().Be(TestPaladin.Dueling);
    }

    [Fact]
    public void Recusa_variante_de_outra_build()
    {
        var foreign = new BuildVariant(new VariantId("arco"), "Arco");
        var act = () => Paladin6().WithVariant(foreign);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Talento_extra_precisa_ser_de_origem()
    {
        var general = () => Paladin6(extra: FeyTouched);
        var darkGift = () => Paladin6(extra: MistWalker);

        general.Should().Throw<DomainException>();
        darkGift.Should().Throw<DomainException>();
    }

    [Fact]
    public void Talento_extra_so_vale_para_quem_a_especie_concede_e_volta_ao_trocar_de_novo()
    {
        var gnome = Paladin6().WithSpecies(Gnome);

        gnome.EffectiveExtraOriginFeat.Should().BeNull();
        gnome.ExtraOriginFeat.Should().Be(Alert);
        gnome.WithSpecies(Human).EffectiveExtraOriginFeat.Should().Be(Alert);
    }

    [Fact]
    public void Escolha_so_nos_niveis_de_talento_da_build()
    {
        var build = Paladin6();

        build.WithLevelChoice(4, LevelChoice.ForFeat(FeyTouched)).ChoiceAt(4).Should().Be(LevelChoice.ForFeat(FeyTouched));
        build.Invoking(b => b.WithLevelChoice(5, LevelChoice.ForFeat(FeyTouched))).Should().Throw<DomainException>();
        build.Invoking(b => b.WithLevelChoice(4, LevelChoice.ForFeat(MistWalker))).Should().Throw<DomainException>();
    }

    [Fact]
    public void Voltar_para_aumento_de_atributo_remove_a_escolha()
    {
        var build = Paladin6().WithLevelChoice(4, LevelChoice.ForFeat(FeyTouched)).WithLevelChoice(4, LevelChoice.AbilityScoreImprovement);

        build.LevelChoices.Should().BeEmpty();
        build.ChoiceAt(4).Should().Be(LevelChoice.AbilityScoreImprovement);
    }

    [Fact]
    public void Trocar_de_build_mantem_especie_e_talento_extra_e_zera_o_resto()
    {
        var build = Paladin6(Gnome).WithLevelChoice(4, LevelChoice.ForFeat(FeyTouched));

        var changed = build.WithArchetype(FighterWarlockBuild, Haunted);

        changed.Archetype.Should().Be(FighterWarlockBuild);
        changed.Variant!.Id.Should().Be(TestFighterWarlock.Defense);
        changed.Species.Should().Be(Gnome);
        changed.ExtraOriginFeat.Should().Be(Alert);
        changed.Background.Should().Be(Haunted);
        changed.LevelChoices.Should().BeEmpty();
    }

    [Fact]
    public void Trocar_de_build_exige_o_antecedente_sugerido()
    {
        var act = () => Paladin6().WithArchetype(FighterWarlockBuild, Noble);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Mudancas_nao_alteram_o_original()
    {
        var original = Paladin6();
        _ = original.WithSpecies(Gnome).WithBackground(Farmer).WithLevelChoice(4, LevelChoice.ForFeat(FeyTouched));

        original.Species.Should().Be(Human);
        original.Background.Should().Be(Noble);
        original.LevelChoices.Should().BeEmpty();
    }
}

public sealed class BuildComparisonTests
{
    private static BuildComparison Create() => BuildComparison.Create(
        [Paladin6(), Paladin6(Gnome), CharacterBuild.Create(FighterWarlockBuild, Human, Haunted)],
        Level.Of(5), CombatAssumptions.Default, ResourceTier.Concentration);

    [Fact]
    public void Tem_exatamente_tres_vagas()
    {
        var act = () => BuildComparison.Create([Paladin6()], Level.Of(5), CombatAssumptions.Default, ResourceTier.AtWill);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Troca_uma_vaga_sem_mexer_nas_outras()
    {
        var comparison = Create();
        var changed = comparison.UpdateSlot(1, b => b.WithSpecies(Elf));

        changed.Slots[1].Species.Should().Be(Elf);
        changed.Slots[0].Should().BeSameAs(comparison.Slots[0]);
        comparison.Slots[1].Species.Should().Be(Gnome);
    }

    [Fact]
    public void Muda_nivel_inimigo_e_faixa()
    {
        var changed = Create().WithLevel(Level.Of(7)).WithAssumptions(new CombatAssumptions(new ArmorClass(18))).WithMaxTier(ResourceTier.Peak);

        changed.Level.Value.Should().Be(7);
        changed.Assumptions.TargetArmorClass.Value.Should().Be(18);
        changed.MaxTier.Should().Be(ResourceTier.Peak);
    }

    [Fact]
    public void Vaga_fora_do_intervalo()
    {
        var act = () => Create().WithSlot(3, Paladin6());
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
