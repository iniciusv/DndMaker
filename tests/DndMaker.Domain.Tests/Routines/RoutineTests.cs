using DndMaker.Domain.Combat;
using DndMaker.Domain.Damage;
using DndMaker.Domain.Routines;

namespace DndMaker.Domain.Tests.Routines;

public sealed class DamageEstimatorTests
{
    private const double Tolerance = 1e-12;
    private static readonly CombatAssumptions Ac15 = CombatAssumptions.Default;

    [Fact]
    public void Ataque_multiplica_media_pela_chance_de_acertar()
    {
        var attack = new AttackStep(2, new AttackBonus(7), Dice.D8 + 6); // acerta 65%
        var estimate = DamageEstimator.Estimate(AttackRoutine.AtWill("x", attack), Ac15);

        estimate.Raw.Should().Be(21);
        estimate.Expected.Should().BeApproximately(2 * 0.65 * 10.5, Tolerance);
        estimate.Steps[0].Chance.Value.Should().BeApproximately(0.65, Tolerance);
    }

    [Fact]
    public void Dano_extra_entra_uma_vez_se_algum_ataque_acertar()
    {
        var attack = new AttackStep(2, new AttackBonus(7), Dice.D8 + 6);
        var estimate = DamageEstimator.Estimate(AttackRoutine.Peak("x", attack, new RiderStep(attack, 2 * Dice.D8)), Ac15);

        estimate.Steps[1].Chance.Value.Should().BeApproximately(1 - (0.35 * 0.35), Tolerance);
        estimate.Steps[1].Raw.Should().Be(9);
        estimate.Raw.Should().Be(30);
    }

    [Fact]
    public void Teste_de_resistencia_com_metade_no_sucesso_e_varios_alvos()
    {
        // CD 15 contra +2: o monstro falha com 12 ou menos no d20, 60%.
        var fireball = new SaveStep(new DifficultyClass(15), 8 * Dice.D6, halfOnSuccess: true, targets: 3);
        var estimate = DamageEstimator.Estimate(AttackRoutine.Peak("Fireball", fireball), Ac15);

        estimate.Steps[0].Chance.Value.Should().BeApproximately(0.6, Tolerance);
        estimate.Raw.Should().Be(84);
        estimate.Expected.Should().BeApproximately(28 * (0.6 + 0.2) * 3, Tolerance);
        estimate.Routine.MultipleTargets.Should().BeTrue();
        estimate.PrimaryDc.Should().Be(new DifficultyClass(15));
        estimate.PrimaryAttackBonus.Should().BeNull();
    }
}

public sealed class RoutineSelectorTests
{
    private static RoutineEstimate Estimate(AttackRoutine routine) => DamageEstimator.Estimate(routine, CombatAssumptions.Default);

    private static AttackStep Hit(int flat) => new(1, new AttackBonus(20), DamageFormula.Of(flat)); // sempre 95%

    [Fact]
    public void Respeita_a_faixa_de_recurso()
    {
        var routines = new[]
        {
            Estimate(AttackRoutine.AtWill("arma", Hit(10))),
            Estimate(AttackRoutine.Concentration("hex", Hit(14))),
            Estimate(AttackRoutine.Peak("smite", Hit(20))),
        };

        RoutineSelector.Best(routines, ResourceTier.AtWill)!.Name.Should().Be("arma");
        RoutineSelector.Best(routines, ResourceTier.Concentration)!.Name.Should().Be("hex");
        RoutineSelector.Best(routines, ResourceTier.Peak)!.Name.Should().Be("smite");
    }

    [Fact]
    public void Ignora_varios_alvos_e_situacionais()
    {
        var routines = new[]
        {
            Estimate(AttackRoutine.AtWill("arma", Hit(10))),
            Estimate(AttackRoutine.AtWill("área", new SaveStep(new DifficultyClass(20), DamageFormula.Of(30), targets: 3))),
            Estimate(AttackRoutine.AtWill("morto-vivo", Hit(50)).When(Situation.VsUndead)),
        };

        RoutineSelector.Best(routines, ResourceTier.Peak)!.Name.Should().Be("arma");
    }

    [Fact]
    public void Empate_fica_com_a_primeira_e_lista_sem_candidatos_da_nulo()
    {
        var tie = new[] { Estimate(AttackRoutine.AtWill("a", Hit(10))), Estimate(AttackRoutine.AtWill("b", Hit(10))) };

        RoutineSelector.Best(tie, ResourceTier.AtWill)!.Name.Should().Be("a");
        RoutineSelector.Best([Estimate(AttackRoutine.Peak("p", Hit(1)))], ResourceTier.AtWill).Should().BeNull();
    }
}

public sealed class RoutineFormatterTests
{
    [Fact]
    public void Escreve_como_a_pagina_original()
    {
        var sword = new AttackStep(2, new AttackBonus(7), Dice.D8 + 6);
        var spiritGuardians = new SaveStep(new DifficultyClass(14), 3 * Dice.D8, halfOnSuccess: true, targets: 3);
        var tollTheDead = new SaveStep(new DifficultyClass(14), 2 * Dice.D12);

        RoutineFormatter.Format(AttackRoutine.Peak("x", sword, new RiderStep(sword, 2 * Dice.D8))).Should().Be("2 × (1d8+6) +2d8");
        RoutineFormatter.Format(AttackRoutine.AtWill("x", new AttackStep(1, new AttackBonus(5), Dice.D8 + 3))).Should().Be("(1d8+3)");
        RoutineFormatter.Format(AttackRoutine.Concentration("x", spiritGuardians, tollTheDead)).Should().Be("3 × (3d8) + (2d12)");
    }
}
