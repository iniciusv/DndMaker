using DndMaker.Domain.Damage;

namespace DndMaker.Domain.Tests.Damage;

public sealed class DiceTests
{
    [Theory]
    [InlineData("1d4", 1, 4, 2.5)]
    [InlineData("2d8", 2, 8, 9)]
    [InlineData("8d6", 8, 6, 28)]
    [InlineData(" 3D10 ", 3, 10, 16.5)]
    public void Le_o_formato_NdX(string text, int count, int sides, double average)
    {
        var dice = Dice.Parse(text);

        dice.Should().Be(new Dice(count, sides));
        dice.Average.Should().Be(average);
    }

    [Theory]
    [InlineData("")]
    [InlineData("d8")]
    [InlineData("2d")]
    [InlineData("0d6")]
    [InlineData("2d1")]
    [InlineData("2x6")]
    [InlineData("-1d6")]
    [InlineData("1d6d6")]
    public void Recusa_texto_invalido(string text)
    {
        Dice.TryParse(text, out _).Should().BeFalse();
    }

    [Fact]
    public void Multiplicar_aumenta_a_quantidade()
    {
        (3 * Dice.D8).Should().Be(new Dice(3, 8));
        (3 * Dice.D8).ToString().Should().Be("3d8");
    }

    [Theory]
    [InlineData(1, 6, 3, 4.0)]
    [InlineData(2, 6, 3, 8.0)]
    [InlineData(1, 12, 3, 6.75)]
    [InlineData(1, 8, 1, 4.5)]
    [InlineData(1, 4, 4, 4.0)]
    public void Face_minima_bate_com_a_enumeracao(int count, int sides, int minimum, double expected)
    {
        var perDie = Enumerable.Range(1, sides).Average(face => Math.Max(face, minimum));
        var dice = new Dice(count, sides).WithMinimumFace(minimum);

        dice.Average.Should().BeApproximately(count * perDie, 1e-12);
        dice.Average.Should().BeApproximately(expected, 1e-12);
    }

    [Fact]
    public void Face_minima_sobrevive_a_multiplicacao_e_1_e_o_mesmo_que_nenhuma()
    {
        (2 * Dice.D6.WithMinimumFace(3)).Should().Be(new Dice(2, 6).WithMinimumFace(3));
        Dice.D8.WithMinimumFace(1).Should().Be(Dice.D8);
        Dice.D6.WithMinimumFace(3).Should().NotBe(Dice.D6);
        Dice.D6.WithMinimumFace(3).ToString().Should().Be("1d6");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(7)]
    public void Face_minima_fora_do_dado_e_recusada(int minimum)
    {
        var create = () => Dice.D6.WithMinimumFace(minimum);
        create.Should().Throw<ArgumentOutOfRangeException>();
    }
}

public sealed class DamageFormulaTests
{
    [Fact]
    public void Escreve_como_a_pagina_original()
    {
        (Dice.D8 + 4 + Dice.D6).ToString().Should().Be("1d8+4+1d6");
        (2 * Dice.D10).ToFormula().ToString().Should().Be("2d10");
        (Dice.D8 + (-1)).ToString().Should().Be("1d8−1");
        DamageFormula.Of(4).ToString().Should().Be("+4");
    }

    [Fact]
    public void Soma_fixos_vizinhos_e_omite_zero()
    {
        // logic.js recebe c.m.str + du como um único número.
        (Dice.D8 + 3 + 2).ToString().Should().Be("1d8+5");
        (Dice.D10 + 0).ToString().Should().Be("1d10");
        (Dice.D8 + 3 + (-3)).ToString().Should().Be("1d8");
        (Dice.D8 + 3 + (-3)).Should().Be(Dice.D8.ToFormula());
    }

    [Fact]
    public void Nao_junta_dados_mesmo_que_iguais()
    {
        (Dice.D6 + 6 + Dice.D6).ToString().Should().Be("1d6+6+1d6");
    }

    [Fact]
    public void Media_soma_as_parcelas()
    {
        (Dice.D8 + 4 + Dice.D6).Average.Should().Be(4.5 + 4 + 3.5);
        DamageFormula.Empty.Average.Should().Be(0);
    }

    [Fact]
    public void Igualdade_compara_as_parcelas()
    {
        (Dice.D8 + 3).Should().Be(Dice.D8 + 3);
        (Dice.D8 + 3).GetHashCode().Should().Be((Dice.D8 + 3).GetHashCode());
        (Dice.D8 + 3).Should().NotBe(Dice.D8 + 4);
        (Dice.D8 + Dice.D6).Should().NotBe(Dice.D6 + Dice.D8);
    }

    [Fact]
    public void Somar_formulas_preserva_a_ordem()
    {
        var weapon = Dice.D8 + 3;
        var hex = Dice.D6.ToFormula();

        (weapon + hex).ToString().Should().Be("1d8+3+1d6");
        (weapon + DamageFormula.Of(2)).ToString().Should().Be("1d8+5");
    }
}
