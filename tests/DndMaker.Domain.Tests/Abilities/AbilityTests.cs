using DndMaker.Domain.Abilities;

namespace DndMaker.Domain.Tests.Abilities;

public sealed class AbilityScoreTests
{
    [Theory]
    [InlineData(1, -5)]
    [InlineData(2, -4)]
    [InlineData(3, -4)]
    [InlineData(8, -1)]
    [InlineData(9, -1)]
    [InlineData(10, 0)]
    [InlineData(11, 0)]
    [InlineData(12, 1)]
    [InlineData(15, 2)]
    [InlineData(17, 3)]
    [InlineData(20, 5)]
    [InlineData(30, 10)]
    public void Modificador_segue_a_tabela_do_livro(int value, int modifier)
    {
        new AbilityScore(value).Modifier.Should().Be(modifier);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(31)]
    public void Recusa_valor_fora_do_intervalo(int value)
    {
        var create = () => new AbilityScore(value);
        create.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(15, 2, 17)]
    [InlineData(19, 2, 20)]
    [InlineData(20, 1, 20)]
    [InlineData(22, 2, 22)] // acima do teto por item mágico: não diminui
    public void Aumento_para_no_teto_do_personagem(int value, int amount, int expected)
    {
        new AbilityScore(value).Increase(amount).Value.Should().Be(expected);
    }
}

public sealed class AbilitySetTests
{
    [Fact]
    public void Mental_e_INT_SAB_CAR()
    {
        AbilitySet.Mental.Members().Should().Equal(Ability.Intelligence, Ability.Wisdom, Ability.Charisma);
        Ability.Wisdom.IsMental().Should().BeTrue();
        Ability.Constitution.IsMental().Should().BeFalse();
    }

    [Fact]
    public void Monta_conjunto_a_partir_de_lista()
    {
        var set = new[] { Ability.Constitution, Ability.Wisdom, Ability.Charisma }.ToAbilitySet();

        set.Contains(Ability.Wisdom).Should().BeTrue();
        set.Contains(Ability.Strength).Should().BeFalse();
        set.Members().Should().HaveCount(3);
    }

    [Fact]
    public void All_tem_os_seis_na_ordem_da_ficha()
    {
        AbilitySet.All.Members().Should().Equal(AbilityList.All);
    }
}

public sealed class AbilityMapTests
{
    private static readonly AbilityMap<int> Fighter = AbilityMap.Of(15, 13, 14, 8, 12, 10);

    [Fact]
    public void Of_segue_a_ordem_da_ficha()
    {
        Fighter[Ability.Strength].Should().Be(15);
        Fighter[Ability.Dexterity].Should().Be(13);
        Fighter[Ability.Constitution].Should().Be(14);
        Fighter[Ability.Intelligence].Should().Be(8);
        Fighter[Ability.Wisdom].Should().Be(12);
        Fighter[Ability.Charisma].Should().Be(10);
    }

    [Fact]
    public void With_devolve_copia_sem_mudar_o_original()
    {
        var changed = Fighter.With(Ability.Strength, 17);

        changed[Ability.Strength].Should().Be(17);
        Fighter[Ability.Strength].Should().Be(15);
    }

    [Fact]
    public void Igualdade_e_por_valor()
    {
        var same = AbilityMap.Of(15, 13, 14, 8, 12, 10);

        (same == Fighter).Should().BeTrue();
        same.GetHashCode().Should().Be(Fighter.GetHashCode());
        (Fighter.With(Ability.Wisdom, 13) != Fighter).Should().BeTrue();
    }

    [Fact]
    public void Select_transforma_cada_valor()
    {
        var modifiers = Fighter.Select((_, score) => new AbilityScore(score).Modifier);

        modifiers.Should().Be(AbilityMap.Of(2, 1, 2, -1, 1, 0));
    }

    [Fact]
    public void ToList_e_Uniform()
    {
        Fighter.ToList().Select(p => p.Key).Should().Equal(AbilityList.All);
        AbilityMap.Uniform(false).ToList().Should().OnlyContain(p => !p.Value);
    }

    [Fact]
    public void Funciona_com_tipos_de_referencia()
    {
        var names = AbilityMap.Create(a => a.ToString());
        names[Ability.Charisma].Should().Be("Charisma");
        names.With(Ability.Charisma, "CAR")[Ability.Charisma].Should().Be("CAR");
    }
}
