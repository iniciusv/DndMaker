using DndMaker.Domain.Combat;
using DndMaker.Domain.Common;
using DndMaker.Domain.Progression;

namespace DndMaker.Domain.Tests.Common;

public sealed class LevelTests
{
    [Fact]
    public void Bonus_de_proficiencia_por_nivel()
    {
        Level.Range(1, 20).Select(l => l.ProficiencyBonus).Should().Equal(
            2, 2, 2, 2, 3, 3, 3, 3, 4, 4, 4, 4, 5, 5, 5, 5, 6, 6, 6, 6);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(21)]
    public void Recusa_nivel_fora_do_intervalo(int value)
    {
        var create = () => Level.Of(value);
        create.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Compara_com_inteiros()
    {
        var level = Level.Of(5);

        (level >= 5).Should().BeTrue();
        (level < 6).Should().BeTrue();
        (level == 5).Should().BeTrue();
        (level > Level.Of(4)).Should().BeTrue();
    }
}

public sealed class ProbabilityTests
{
    [Theory]
    [InlineData(-0.01)]
    [InlineData(1.01)]
    [InlineData(double.NaN)]
    public void Recusa_valor_fora_de_zero_a_um(double value)
    {
        var create = () => new Probability(value);
        create.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Operacoes()
    {
        var half = new Probability(0.5);

        half.Complement.Value.Should().Be(0.5);
        half.AtLeastOnceIn(2).Value.Should().Be(0.75);
        half.AtLeastOnceIn(0).Value.Should().Be(0);
        half.OrElse(new Probability(0.5)).Value.Should().Be(0.75);
        half.And(new Probability(0.5)).Value.Should().Be(0.25);
        Probability.Clamp(1.2).Should().Be(Probability.One);
        Probability.Clamp(0.99, 0.05, 0.95).Value.Should().Be(0.95);
    }
}

public sealed class CombatValueTests
{
    [Fact]
    public void Bonus_com_sinal_tipografico()
    {
        new AttackBonus(5).ToString().Should().Be("+5");
        new AttackBonus(0).ToString().Should().Be("+0");
        new AttackBonus(-1).ToString().Should().Be("−1");
    }

    [Fact]
    public void CD_de_magia()
    {
        DifficultyClass.ForSpell(3, 4).Value.Should().Be(15);
    }

    [Fact]
    public void Metros()
    {
        Meters.Max(Meters.Of(18), Meters.Of(36)).Should().Be(Meters.Of(36));
        (Meters.Of(18) + Meters.Of(18)).Value.Should().Be(36);
        Meters.Zero.IsZero.Should().BeTrue();
    }
}
