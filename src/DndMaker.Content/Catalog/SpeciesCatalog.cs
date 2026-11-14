using System.Collections.Immutable;
using DndMaker.Domain.Abilities;
using DndMaker.Domain.Catalog;
using DndMaker.Domain.Common;
using DndMaker.Domain.Effects;

namespace DndMaker.Content.Catalog;

/// <summary>Espécies dos livros oficiais liberados na mesa, na ordem da página original.</summary>
public static class SpeciesCatalog
{
    private const string Phb = "PHB 2024";
    private const string HorrorsWithin = "Horrors Within";
    private const string Multiverse = "Monsters of the Multiverse";

    private static readonly Meters Short = Meters.Of(18);
    private static readonly Meters Long = Meters.Of(36);

    private static Species Create(string id, string name, string source, Meters darkvision, string description) =>
        new(new SpeciesId(id), name, source, description) { Darkvision = darkvision };

    public static ImmutableArray<Species> All { get; } =
    [
        Create("humano", "Humano", Phb, Meters.Zero,
            "Talento de origem extra, +1 perícia e Inspiração Heroica a cada descanso longo.") with { GrantsExtraOriginFeat = true },
        Create("gnomo", "Gnomo", Phb, Short,
            "Vantagem em todos os testes de resistência de INT, SAB e CAR. Linhagem da floresta (Minor Illusion, Speak with Animals) ou das rochas.")
            with { Effects = [new SaveAdvantage(AbilitySet.Mental)] },
        Create("aasimar", "Aasimar", Phb, Short,
            "Resistência a dano necrótico e radiante, Healing Hands e, no nível 3, uma transformação de 1 minuto com dano extra igual ao PB uma vez por turno."),
        Create("elfo", "Elfo", Phb, Short,
            "Vantagem contra Enfeitiçado, uma perícia (Intuição, Percepção ou Sobrevivência) e magias da linhagem. Drow enxerga a 36 m.")
            with { Effects = [new ConditionAdvantage(Condition.Charmed)] },
        Create("anao", "Anão", Phb, Long,
            "+1 PV por nível, resistência a veneno e sentido sísmico.") with { Effects = [new HitPointsPerLevel(1)] },
        Create("orc", "Orc", Phb, Long,
            "Disparada de ação bônus com PV temporários; fica com 1 PV em vez de cair, 1x por descanso longo."),
        Create("halfling", "Halfling", Phb, Meters.Zero,
            "Rerrola 1 natural em testes d20 e tem vantagem contra Amedrontado."),
        Create("tiefling", "Tiefling", Phb, Short,
            "Legado ctônico: resistência necrótica, Chill Touch e False Life. Infernal: resistência a fogo e Darkness."),
        Create("golias", "Golias", Phb, Meters.Zero,
            "Poder de gigante (teleporte, dano extra, derrubar ou reduzir dano), PB vezes por descanso longo. Deslocamento 10,5 m."),
        Create("draconato", "Draconato", Phb, Short,
            "Sopro no lugar de um ataque, resistência a um tipo de dano e asas no nível 5."),
        Create("dhampir", "Dhampir", HorrorsWithin, Short,
            "Resistência necrótica, escalada (paredes e teto no nível 3) e mordida de 1d4 + CON que cura você.") with { DarkvisionNeedsConfirmation = true },
        Create("hexblood", "Hexblood", HorrorsWithin, Short,
            "Amuleto para mensagens ou espionagem a até 16 km. Disguise Self e Hex sempre preparadas, 1x cada por dia de graça.") with { DarkvisionNeedsConfirmation = true },
        Create("lupin", "Lupin", HorrorsWithin, Short,
            "Uivo (ação bônus): inimigos a 4,5 m fazem teste de SAB ou têm desvantagem em ataques e testes de resistência. Golpes desarmados que derrubam.") with { DarkvisionNeedsConfirmation = true },
        Create("reborn", "Reborn", HorrorsWithin, Short,
            "Resistência a frio, necrótico ou veneno, vantagem em testes contra a morte, descanso longo em 4 horas e +1d6 em testes de atributo falhos.") with { DarkvisionNeedsConfirmation = true },
        Create("shadarkai", "Shadar-kai", Multiverse, Short,
            "Resistência necrótica, vantagem contra Enfeitiçado e teleporte de 9 m como ação bônus; no nível 3, o teleporte dá resistência a todo dano por 1 turno.")
            with { Effects = [new ConditionAdvantage(Condition.Charmed)] },
        Create("githzerai", "Githzerai", Multiverse, Meters.Zero,
            "Vantagem contra Enfeitiçado e Amedrontado, resistência psíquica, Mage Hand, Shield (nível 3) e Detect Thoughts (nível 5).")
            with { Effects = [new ConditionAdvantage(Condition.Charmed)] },
        Create("owlin", "Owlin", "Strixhaven", Long,
            "Voo desde o nível 1 (sem armadura média ou pesada) e proficiência em Furtividade."),
        Create("genasi_terra", "Genasi da Terra", Multiverse, Short,
            "Blade Ward como ação bônus (PB vezes por dia), ignora terreno difícil de pedra e Pass without Trace no nível 5."),
        Create("genasi_fogo", "Genasi do Fogo", Multiverse, Short,
            "Resistência a fogo, Produce Flame, Burning Hands (nível 3) e Flame Blade (nível 5)."),
        Create("yuanti", "Yuan-ti", Multiverse, Short,
            "Vantagem em testes de resistência contra magias, resistência a veneno e Suggestion no nível 3."),
        Create("satiro", "Sátiro", Multiverse, Meters.Zero,
            "Tipo Fada (imune a efeitos que só afetam humanoides), vantagem contra magias, deslocamento 10,5 m."),
        Create("kalashtar", "Kalashtar", "Eberron", Meters.Zero,
            "Vantagem em todos os testes de resistência de Sabedoria, resistência psíquica e telepatia.")
            with { Effects = [new SaveAdvantage(AbilitySet.Wisdom)] },
        Create("harengon", "Harengon", Multiverse, Meters.Zero,
            "Soma o PB à iniciativa, proficiência em Percepção e salto de reação para escapar.")
            with { Effects = [new InitiativeBonus(InitiativeAmount.ProficiencyBonus)] },
        Create("duergar", "Duergar", Multiverse, Long,
            "Vantagem contra Enfeitiçado, resistência a veneno, Enlarge/Reduce e Invisibility.")
            with { Effects = [new ConditionAdvantage(Condition.Charmed)] },
        Create("bugbear", "Bugbear", Multiverse, Short,
            "+2d6 no primeiro acerto contra quem ainda não agiu, alcance maior e furtividade."),
        Create("warforged", "Warforged", "Forge of the Artificer", Meters.Zero,
            "+1 de CA, resistência a veneno e não dorme.") with { Effects = [new ArmorClassBonus(1)] },
    ];
}
