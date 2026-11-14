using System.Collections.Immutable;
using DndMaker.Domain.Common;
using DndMaker.Domain.Progression;

namespace DndMaker.Domain.Classes;

/// <summary>Um trecho da progressão: <see cref="Levels"/> níveis numa classe, ou o resto da carreira se for nulo.</summary>
public sealed record ClassSegment(CharacterClass Class, int? Levels);

/// <summary>
/// A ordem em que o personagem ganha níveis de classe, por exemplo Guerreiro 1 → Bruxo até o fim.
/// Responde "quantos níveis de bruxo no nível 6?" sem contas soltas como "nível − 1".
/// </summary>
public sealed class ClassProgression
{
    private readonly ImmutableArray<ClassSegment> _segments;

    public ClassProgression(IEnumerable<ClassSegment> segments)
    {
        _segments = [.. segments];
        if (_segments.IsEmpty)
        {
            throw new DomainException("A progressão precisa de pelo menos uma classe.");
        }

        if (_segments[^1].Levels is not null)
        {
            throw new DomainException("O último trecho da progressão precisa ir até o fim (Levels nulo).");
        }

        if (_segments.Take(_segments.Length - 1).Any(s => s.Levels is not > 0))
        {
            throw new DomainException("Só o último trecho pode ter duração aberta, e os outros precisam de pelo menos 1 nível.");
        }
    }

    public static ClassProgression Of(CharacterClass characterClass) => new([new ClassSegment(characterClass, null)]);

    /// <summary>Começa com <paramref name="levels"/> níveis em <paramref name="first"/> e segue em <paramref name="then"/>.</summary>
    public static ClassProgression Dip(CharacterClass first, int levels, CharacterClass then) =>
        new([new ClassSegment(first, levels), new ClassSegment(then, null)]);

    /// <summary>A primeira classe define os testes de resistência e as proficiências de armadura.</summary>
    public CharacterClass FirstClass => _segments[0].Class;

    public IEnumerable<CharacterClass> Classes => _segments.Select(s => s.Class).Distinct();

    /// <summary>A classe que recebe o nível de personagem <paramref name="characterLevel"/>.</summary>
    public CharacterClass ClassGainingLevel(int characterLevel)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(characterLevel, Level.Min);
        var remaining = characterLevel;
        foreach (var segment in _segments)
        {
            if (segment.Levels is not { } levels || remaining <= levels)
            {
                return segment.Class;
            }

            remaining -= levels;
        }

        throw new DomainException("Progressão sem trecho aberto.");
    }

    /// <summary>Níveis na classe ao chegar ao nível de personagem <paramref name="level"/>.</summary>
    public int LevelIn(CharacterClass characterClass, Level level) =>
        Enumerable.Range(1, level.Value).Count(l => ClassGainingLevel(l) == characterClass);

    /// <summary>O dado de vida de cada nível, do 1º até <paramref name="level"/>.</summary>
    public IEnumerable<int> HitDice(Level level) =>
        Enumerable.Range(1, level.Value).Select(l => ClassGainingLevel(l).HitDie);

    /// <summary>Níveis de personagem, até <paramref name="maxLevel"/>, em que se escolhe talento ou aumento de atributo.</summary>
    public ImmutableArray<int> FeatLevels(int maxLevel)
    {
        var levels = ImmutableArray.CreateBuilder<int>();
        for (var characterLevel = Level.Min; characterLevel <= maxLevel; characterLevel++)
        {
            var characterClass = ClassGainingLevel(characterLevel);
            if (characterClass.FeatLevels.Contains(LevelIn(characterClass, Level.Of(characterLevel))))
            {
                levels.Add(characterLevel);
            }
        }

        return levels.ToImmutable();
    }

    public override string ToString() =>
        string.Join(" → ", _segments.Select(s => s.Levels is { } n ? $"{s.Class.Name} {n}" : s.Class.Name));
}
