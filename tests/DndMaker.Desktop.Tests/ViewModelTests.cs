using DndMaker.Application.Session;
using DndMaker.Desktop.Formatting;
using DndMaker.Desktop.Tests.Support;
using DndMaker.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace DndMaker.Desktop.Tests;

/// <summary>Os ViewModels sobre o conteúdo real, sem janela. Os números conferem com a página original.</summary>
public sealed class ViewModelTests : IAsyncDisposable
{
    private readonly MemoryRepository _repository = new();
    private readonly ServiceProvider _services;
    private readonly MainViewModel _main;

    public ViewModelTests()
    {
        _services = TestServices.Build(_repository);
        _main = _services.GetRequiredService<MainViewModel>();
    }

    private ComparisonSession Session => _services.GetRequiredService<ComparisonSession>();

    public ValueTask DisposeAsync() => _services.DisposeAsync();

    [Fact]
    public void Abre_na_comparacao_inicial_da_pagina()
    {
        _main.Eyebrow.Should().Be("Curse of Strahd · regras de 2024 · níveis 1 a 10");
        _main.LevelText.Should().Be("5");
        _main.LevelCaption.Should().Be("Barovia e Vallaki · janela que você quer forte");
        _main.SelectedLevel!.Value.Should().Be(5);
        _main.SelectedEnemyArmorClass!.Label.Should().Be("15 (típico)");
        _main.SelectedTier!.FilterLabel.Should().Be("até magia de concentração");
        _main.Levels.Where(l => l.IsHighlight).Select(l => l.Value).Should().Equal(5, 6);
        _main.Phases.Select(p => (p.Column, p.Span)).Should().Equal((0, 3), (3, 3), (6, 2), (8, 2));

        _main.Cards.Select(c => c.Sheet!.Name).Should().Equal("Guerreiro 1 → Bruxo", "Bardo do Valor", "Paladino (Watchers)");
        _main.Cards.Select(c => c.Numeral).Should().Equal("I", "II", "III");
    }

    [Fact]
    public void Carta_monta_os_textos_como_a_pagina()
    {
        var warlock = _main.Cards[0].Sheet!;

        warlock.Tiles.Select(t => (t.Value, t.Label, t.IsLeader)).Should().Equal(
            ("19", "CA", false), ("45", "PV", true), ("+1", "Iniciativa", false), ("36 m", "Visão no escuro", false));
        warlock.ArmorText.Should().Be(
            "cota de malha + escudo + Defense (desvantagem em Furtividade). Armor of Agathys: +10 PV temporários, sem concentração. "
            + "Devil's Sight: enxerga também em escuridão mágica.");
        warlock.BackgroundText.Should().Be("Antecedente: CAR +2, CON +1.");
        warlock.Picks.Should().ContainSingle().Which.Should().Be(new LabeledTextViewModel("Nível 5, Aumento de atributo (+2):", "CAR 17 → 18, CON 15 → 16."));
        warlock.Saves.Select(s => s.Detail).Should().Equal("prof.", " ", "prof.", " ", " ", " ");
        warlock.Charm.PercentText.Should().Be("20%");
        warlock.Charm.Level.Should().Be(MeterLevel.Low);
        warlock.Charm.Caption.Should().Be("Teste de Sabedoria, CD 17 (ficha de 2016).");
        warlock.Concentration!.Caption.Should().Be("Teste de Constituição, CD 10, ao sofrer dano.");
        warlock.Routines.Single(r => r.IsBest).Name.Should().Be("Eldritch Blast (à distância)");
        warlock.Routines.Single(r => r.IsBest).ExpectedText.Should().Be("12,4");
        warlock.SpeciesAndFeats[0].Should().Be(new LabeledTextViewModel("Humano", "(PHB 2024): Talento de origem extra, +1 perícia e Inspiração Heroica a cada descanso longo."));
        warlock.Milestones.Single(m => m.IsCurrent).Level.Should().Be(5);
    }

    [Fact]
    public void Paladino_no_6_mostra_a_aura_e_o_selo()
    {
        _main.SelectedLevel = _main.Levels[5];
        var paladin = _main.Cards[2].Sheet!;

        paladin.SavesText.Should().EndWith("Inclui +3 da Aura of Protection.");
        paladin.IsFinalist.Should().BeTrue();
        paladin.Charm.PercentText.Should().Be("55%");
        paladin.Charm.Level.Should().Be(MeterLevel.Mid);
    }

    [Fact]
    public void Estrada_de_niveis_e_atalhos()
    {
        _main.NextLevelCommand.Execute(null);
        _main.LevelText.Should().Be("6");

        _main.SelectedLevel = _main.Levels[9];
        _main.NextLevelCommand.Execute(null);
        _main.LevelText.Should().Be("10", "não passa do último nível");
        _main.LevelCaption.Should().Be("Amber Temple e Castle Ravenloft · final da campanha");

        _main.SelectedLevel = _main.Levels[0];
        _main.PreviousLevelCommand.Execute(null);
        Session.Current.Level.Value.Should().Be(1);
    }

    [Fact]
    public void CA_e_faixa_mudam_cartas_e_tabelas()
    {
        _main.SelectedEnemyArmorClass = _main.EnemyArmorClasses[0];
        _main.SelectedTier = _main.Tiers[2];

        Session.Current.Assumptions.TargetArmorClass.Value.Should().Be(13);
        _main.Overview.Caption.Should().Contain("CA 13").And.Contain("pico (gasta recurso)");
        _main.Cards[2].Sheet!.Routines.Should().OnlyContain(r => r.Counted);
        _main.DamageGrid.Rows.Should().HaveCount(10);
    }

    [Fact]
    public void Trocar_a_build_refaz_variantes_e_niveis_de_talento()
    {
        var card = _main.Cards[0];
        card.LevelChoices.Select(c => c.Level).Should().Equal(5, 9);

        card.SelectedArchetype = card.Archetypes.Single(a => a.Id == "gloom");

        card.Sheet!.Name.Should().Be("Patrulheiro Gloom Stalker");
        card.HasVariants.Should().BeFalse();
        card.LevelChoices.Select(c => c.Level).Should().Equal(4, 8);
        card.SelectedBackground!.Id.Should().Be("andarilho");
        card.SelectedExtraOriginFeat!.Id.Should().Be("mi_wizard", "o talento extra fica ao trocar a build");
    }

    [Fact]
    public void Especie_sem_talento_extra_esconde_o_seletor()
    {
        var card = _main.Cards[1];

        card.SelectedSpecies = card.Species.Single(s => s.Id == "gnomo");

        card.ShowsExtraOriginFeat.Should().BeFalse();
        card.Sheet!.ShowShillelaghHint.Should().BeTrue("sem humano não há Magic Initiate (Druida) para o bordão");
    }

    [Fact]
    public void Escolha_de_nivel_vai_para_a_ficha()
    {
        var card = _main.Cards[0];

        card.LevelChoices[0].Selected = card.LevelChoices[0].Options.Single(o => o.Id == "fey");

        card.Sheet!.Picks[0].Should().Be(new LabeledTextViewModel("Nível 5, Fey Touched:", "CAR 17 → 18."));
        card.LevelChoices[0].Selected!.Id.Should().Be("fey");
    }

    [Fact]
    public void Tabela_poe_a_build_na_carta_com_as_escolhas_padrao()
    {
        _main.Overview.Title.Should().Be("Todas as builds no nível 5");
        _main.Overview.Items.Should().HaveCount(10);
        var rows = _main.Overview.Rows;
        var twilight = _main.Overview.Items.Single(r => r.ArchetypeId == "twi");

        twilight.Slots[1].Command.Execute(null);

        _main.Cards[1].Sheet!.Name.Should().Be("Clérigo do Crepúsculo");
        _main.Cards[1].SelectedSpecies!.Id.Should().Be("humano");
        _main.Overview.Rows.Should().BeSameAs(rows, "a tabela é atualizada no lugar e mantém a ordenação");
        _main.Overview.Items.Should().HaveCount(10);
    }

    [Fact]
    public void Grade_de_dano_marca_o_nivel_atual_e_a_janela()
    {
        var grid = _main.DamageGrid;

        grid.Columns.Single(c => c.IsCurrent).Level.Should().Be(5);
        grid.Columns.Where(c => c.IsHighlight).Select(c => c.Level).Should().Equal(5, 6);
        grid.Rows.SelectMany(r => r.Cells).Max(c => c.Intensity).Should().BeApproximately(HeatCellViewModel.MaxIntensity, 1e-9);
        grid.Caption.Should().Be("As colunas 5 e 6 são a janela em que você quer estar forte. Do nível 7 em diante já é o final da campanha.");
    }

    [Fact]
    public async Task Falha_ao_gravar_aparece_na_tela()
    {
        _repository.Fail = true;
        Session.SetLevel(7);

        await _services.GetRequiredService<ComparisonPersistence>().FlushAsync();

        _main.HasSaveError.Should().BeTrue();
        _main.SaveError.Should().Contain("Disco cheio.");
        _main.DismissSaveErrorCommand.Execute(null);
        _main.HasSaveError.Should().BeFalse();
    }
}

public sealed class FormatTests
{
    [Theory]
    [InlineData(12.35, "12,4")] // igual ao toLocaleString do JavaScript
    [InlineData(0.25, "0,3")]
    [InlineData(2.25, "2,3")]
    [InlineData(18, "18")]
    [InlineData(8.450000000000001, "8,5")]
    [InlineData(0, "0")]
    public void Decimal_com_uma_casa(double value, string expected) => Format.Decimal(value).Should().Be(expected);

    [Theory]
    [InlineData(0.2, "20%")]
    [InlineData(0.555, "56%")]
    [InlineData(0.6375, "64%")]
    public void Porcentagem_inteira(double value, string expected) => Format.Percent(value).Should().Be(expected);

    [Fact]
    public void Sinal_tipografico() => Format.Signed(-1).Should().Be("−1");
}
