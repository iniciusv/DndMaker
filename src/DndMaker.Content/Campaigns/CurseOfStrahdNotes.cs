using System.Collections.Immutable;
using DndMaker.Domain.Rulesets;

namespace DndMaker.Content.Campaigns;

/// <summary>A seção "O que já sabemos" da página original: pedidos, decisões, regras da mesa e perguntas.</summary>
internal static class CurseOfStrahdNotes
{
    private static NoteItem Item(string text) => new(text);

    private static NoteItem Lead(string lead, string text) => new(text) { Lead = lead };

    public static ImmutableArray<NoteSection> All { get; } =
    [
        new("O que você pediu",
        [
            Item("Ser forte nos níveis 5 e 6, quando combatentes ganham o segundo ataque e conjuradores ganham magias de 3º círculo."),
            Item("Lutar você mesmo, de perto ou de longe, com magias e espaço para personalizar."),
            Item("Suporte aos aliados não é prioridade: a party já tem muito."),
            Item("Não depender de ficar concentrando no Hex."),
        ]),
        new("Decisões até aqui",
        [
            Lead("Finalistas:", "Guerreiro 1 → Bruxo com Pact of the Blade, Bardo do Valor e Paladino."),
            Lead("Descartadas:", "Battle Master com besta (cobra um talento e não tem magias) e Patrulheiro Beast Master (não agradou)."),
            Lead("Espécies em vista:", "humano (talento extra), gnomo (vantagem em testes mentais), Genasi da Terra, aasimar, Hexblood, Shadar-kai, Owlin."),
            Item("O Twilight Sanctuary e o Divine Smite radiante são as respostas mais fortes aos mortos-vivos; o Toll the Dead é necrótico e muitos deles resistem."),
        ]),
        new("Regras da mesa",
        [
            Item("Começa no nível 1 e vai até o 10. Atributos por rolagem ou compra de pontos."),
            Item("Valem todos os livros oficiais, de 2014 e de 2024. Nada de fãs ou de terceiros, o que tira o Blood Hunter."),
            Item("Ravenloft: The Horrors Within (2026) é o livro mais próximo da campanha: patrono Undead, Hollow Warden, Feiticeiro das Sombras, quatro espécies e os Dark Gifts."),
            Item("Multiclasse: testes de resistência e armadura pesada do guerreiro só vêm se ele for a primeira classe."),
        ]),
        new("A party",
        [
            Item("Clérigo da Forja, Paladino da Vingança, Ladino, Artífice, Mago das Espadas, Bardo e Druida das Estrelas."),
            Item("Sobram magia, cura e suporte. Faltam dano constante com arma, ataque à distância e um segundo tanque."),
            Item("Perícias já cobertas: armadilhas, conhecimento arcano e religioso, natureza e conversa. Valem as que cada um rola por si: Percepção, Furtividade, Intuição e Atletismo ou Acrobacia."),
            Item("O artífice pode fazer uma besta Repeating Shot, e a aura do paladino protege quem fica a 3 m dele a partir do nível 6."),
        ]),
        new("Talentos em destaque",
        [
            Lead("Alert:", "soma o bônus de proficiência à iniciativa (+2, depois +3) e deixa trocar de lugar na ordem com um aliado. Serve em qualquer build."),
            Lead("Fey Touched", "(nível 4 em diante): +1 em Inteligência, Sabedoria ou Carisma, Misty Step e uma magia de 1º círculo, cada uma grátis uma vez por dia e também com seus espaços. Ótimo para quem tem 17 no atributo principal."),
            Lead("Magic Initiate:", "no 2024 você escolhe o atributo. Mago dá Shield; Druida dá Shillelagh com Carisma."),
            Lead("Lucky e Survivor", "são as melhores defesas de talento contra o charme de Strahd."),
            Item("O talento extra do humano é de origem e não aumenta atributo. Os Dark Gifts não podem ser pegos por ele."),
        ]),
        new("Como os números são feitos",
        [
            Item("Conjunto padrão (15, 14, 13, 12, 10, 8) distribuído pela prioridade da build, com +2 e +1 do antecedente nos dois melhores atributos que ele permite."),
            Item("O aumento de atributo arredonda o principal e põe o ponto que sobra no próximo atributo ímpar."),
            Item("Dano esperado = dano médio vezes a chance de acertar a CA escolhida, sem críticos. Magias com teste assumem +2 no teste do inimigo."),
            Item("Armadura: cota de escamas até o nível 4 e meia armadura a partir do 5 para quem usa média; cota de malha para quem usa pesada com Força 13."),
            Item("Parte das regras veio de resumos públicos e parte de memória. Antes de fechar a ficha, confira no livro."),
        ]),
        new("Perguntas para o Mestre",
        [
            Item("Experiência ou marcos?"),
            Item("O Clérigo do Crepúsculo está liberado?"),
            Item("Qual ficha do Strahd vale, a de 2016 ou a nova?"),
            Item("Quais perícias cada colega já pegou?"),
        ]),
        new("Documentos completos",
        [
            Item("Classes, regras, party e ficha") with { Link = new Uri("https://claude.ai/artifact/UnMo8Dpx28gQV3vRSxtXfZ") },
            Item("Espécies e combinações") with { Link = new Uri("https://claude.ai/artifact/SLKsd1QLvpnMxRuqKhNeWs") },
        ]),
    ];
}
