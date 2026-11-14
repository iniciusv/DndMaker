using System.Collections.Immutable;
using DndMaker.Domain.Abilities;
using DndMaker.Domain.Catalog;
using static DndMaker.Domain.Abilities.AbilitySet;

namespace DndMaker.Content.Catalog;

/// <summary>Antecedentes do PHB 2024 e de Ravenloft, na ordem da página original.</summary>
public static class BackgroundCatalog
{
    private static Background Create(string id, string name, AbilitySet abilities, Feat feat, string skills) =>
        new(new BackgroundId(id), name, abilities, feat, skills);

    public static ImmutableArray<Background> All { get; } =
    [
        Create("acolito", "Acólito", Intelligence | Wisdom | Charisma, FeatCatalog.MagicInitiateCleric, "Intuição, Religião"),
        Create("andarilho", "Andarilho (Wayfarer)", Dexterity | Wisdom | Charisma, FeatCatalog.Lucky, "Intuição, Furtividade"),
        Create("artesao", "Artesão", Strength | Dexterity | Intelligence, FeatCatalog.Crafter, "Investigação, Persuasão"),
        Create("artista", "Artista (Entertainer)", Strength | Dexterity | Charisma, FeatCatalog.Musician, "Acrobacia, Atuação"),
        Create("charlatao", "Charlatão", Dexterity | Constitution | Charisma, FeatCatalog.Skilled, "Enganação, Prestidigitação"),
        Create("criminoso", "Criminoso", Dexterity | Constitution | Intelligence, FeatCatalog.Alert, "Prestidigitação, Furtividade"),
        Create("eremita", "Eremita", Constitution | Wisdom | Charisma, FeatCatalog.Healer, "Medicina, Religião"),
        Create("escriba", "Escriba", Dexterity | Intelligence | Wisdom, FeatCatalog.Skilled, "Investigação, Percepção"),
        Create("fazendeiro", "Fazendeiro", Strength | Constitution | Wisdom, FeatCatalog.Tough, "Lidar com Animais, Natureza"),
        Create("guarda", "Guarda", Strength | Intelligence | Wisdom, FeatCatalog.Alert, "Atletismo, Percepção"),
        Create("guia", "Guia", Dexterity | Constitution | Wisdom, FeatCatalog.MagicInitiateDruid, "Furtividade, Sobrevivência"),
        Create("marinheiro", "Marinheiro", Strength | Dexterity | Wisdom, FeatCatalog.TavernBrawler, "Acrobacia, Percepção"),
        Create("mercador", "Mercador", Constitution | Intelligence | Charisma, FeatCatalog.Lucky, "Lidar com Animais, Persuasão"),
        Create("nobre", "Nobre", Strength | Intelligence | Charisma, FeatCatalog.Skilled, "História, Persuasão"),
        Create("sabio", "Sábio", Constitution | Intelligence | Wisdom, FeatCatalog.MagicInitiateWizard, "Arcanismo, História"),
        Create("soldado", "Soldado", Strength | Dexterity | Constitution, FeatCatalog.SavageAttacker, "Atletismo, Intimidação"),
        Create("haunted", "Haunted One (Ravenloft)", Constitution | Wisdom | Charisma, FeatCatalog.Survivor, "Arcanismo, Sobrevivência"),
        Create("investigador", "Investigador (Ravenloft)", Intelligence | Wisdom | Charisma, FeatCatalog.SharpEye, "Intuição, Investigação"),
        Create("brumas", "Andarilho das Brumas (Ravenloft)", Dexterity | Constitution | Wisdom, FeatCatalog.MistWalker, "Sobrevivência, Furtividade"),
        Create("medium", "Médium (Ravenloft)", Constitution | Intelligence | Wisdom, FeatCatalog.GatheredWhispers, "Intuição, Religião"),
    ];
}
