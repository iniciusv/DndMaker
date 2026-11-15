using System.Collections.Immutable;
using DndMaker.Domain.Abilities;
using DndMaker.Domain.Catalog;
using DndMaker.Domain.Effects;

namespace DndMaker.Content.Catalog;

/// <summary>Talentos de origem, Dark Gifts e os talentos gerais considerados nos níveis de talento.</summary>
public static class FeatCatalog
{
    public static Feat Alert { get; } = new(new("alert"), "Alert", FeatCategory.Origin,
        "Soma o bônus de proficiência à iniciativa e pode trocar de iniciativa com um aliado.")
    {
        Effects = [new InitiativeBonus(InitiativeAmount.ProficiencyBonus)],
    };

    public static Feat Lucky { get; } = new(new("lucky"), "Lucky", FeatCategory.Origin,
        "Pontos de sorte iguais ao bônus de proficiência por descanso longo: vantagem num teste d20 seu ou desvantagem num ataque contra você.")
    {
        // Problema 1 do plano: a vantagem gasta com um ponto de sorte entra na chance "gastando recursos".
        Effects = [new SaveAdvantageResource(AbilitySet.All, "Lucky")],
    };

    public static Feat Tough { get; } = new(new("tough"), "Tough", FeatCategory.Origin, "+2 pontos de vida por nível.")
    {
        Effects = [new HitPointsPerLevel(2)],
    };

    public static Feat MagicInitiateWizard { get; } = new(new("mi_wizard"), "Magic Initiate (Mago)", FeatCategory.Origin,
        "2 truques e 1 magia de mago com o atributo que você escolher. Destaque: Shield (reação, +5 de CA até seu próximo turno), 1x por dia de graça e depois com seus espaços.");

    public static Feat MagicInitiateCleric { get; } = new(new("mi_cleric"), "Magic Initiate (Clérigo)", FeatCategory.Origin,
        "2 truques e 1 magia de clérigo. Destaques: Sacred Flame (dano radiante), Guidance, Bless ou Healing Word.");

    public static Feat MagicInitiateDruid { get; } = new(new("mi_druid"), "Magic Initiate (Druida)", FeatCategory.Origin,
        "2 truques e 1 magia de druida. Destaque: Shillelagh, que faz o bordão atacar com o atributo de conjuração escolhido (1d8; 1d10 no nível 5).");

    public static Feat Skilled { get; } = new(new("skilled"), "Skilled", FeatCategory.Origin, "Proficiência em 3 perícias ou ferramentas.");

    public static Feat SavageAttacker { get; } = new(new("savage"), "Savage Attacker", FeatCategory.Origin,
        "Uma vez por turno, rola o dano da arma duas vezes e fica com o melhor.");

    public static Feat Healer { get; } = new(new("healer"), "Healer", FeatCategory.Origin,
        "Cura com kit de primeiros socorros gastando um dado de vida do aliado; rerrola 1 nos dados de cura.");

    public static Feat Musician { get; } = new(new("musician"), "Musician", FeatCategory.Origin,
        "Após descansar, dá Inspiração Heroica a aliados (quantidade igual ao bônus de proficiência).");

    public static Feat TavernBrawler { get; } = new(new("tavern"), "Tavern Brawler", FeatCategory.Origin,
        "Soco de 1d4 + FOR, rerrola 1 no dano e empurra 1,5 m uma vez por turno.");

    public static Feat Crafter { get; } = new(new("crafter"), "Crafter", FeatCategory.Origin,
        "3 ferramentas de artesão, 20% de desconto em itens comuns e fabricação rápida.");

    public static Feat Survivor { get; } = new(new("survivor"), "Survivor", FeatCategory.Origin,
        "Rerrola iniciativa de 9 ou menos. 1x por descanso longo, soma o bônus de proficiência a um teste falho contra Enfeitiçado ou Amedrontado.")
    {
        // Problema 1 do plano: o bônus depois da falha entra na chance "gastando recursos".
        Effects =
        [
            new ProficiencyBonusOnFailure(Condition.Charmed, "Survivor"),
            new ProficiencyBonusOnFailure(Condition.Frightened, "Survivor"),
        ],
    };

    public static Feat SharpEye { get; } = new(new("sharpeye"), "Sharp Eye", FeatCategory.Origin,
        "Vantagem em Procurar ou Estudar, PB vezes por descanso longo (só gasta se der certo).");

    public static Feat MistWalker { get; } = new(new("dg_mist"), "Dark Gift: Mist Walker", FeatCategory.DarkGift,
        "Teleporte de 4,5 m como reação ao sofrer dano (usos limitados). Descanso curto perto do centro de um domínio pode não dar benefício.");

    public static Feat GatheredWhispers { get; } = new(new("dg_whispers"), "Dark Gift: Gathered Whispers", FeatCategory.DarkGift,
        "Truque Message, Augury grátis 1x por dia e reação para somar o PB à CA. No 1 natural, pode ficar surdo.");

    public static Feat FeyTouched { get; } = new(new("fey"), "Fey Touched", FeatCategory.General,
        "+1 no atributo mental. Misty Step e 1 magia de 1º círculo de adivinhação ou encantamento (Silvery Barbs, Bless, Command, Dissonant Whispers), cada uma 1x por dia de graça e também com seus espaços, inclusive os de pacto.")
    {
        Increase = AbilityIncreaseRule.BestOddMental,
    };

    public static Feat WarCaster { get; } = new(new("warcaster"), "War Caster", FeatCategory.General,
        "+1 no atributo mental. Vantagem nos testes de concentração, magia como ataque de oportunidade e gestos com as mãos ocupadas.")
    {
        Increase = AbilityIncreaseRule.BestOddMental,
        Effects = [new ConcentrationAdvantage()],
    };

    public static Feat ResilientConstitution { get; } = new(new("res_con"), "Resilient (Constituição)", FeatCategory.General,
        "+1 CON e proficiência no teste de resistência de Constituição.")
    {
        Increase = AbilityIncreaseRule.Fixed(Ability.Constitution),
        Effects = [new SaveProficiency(Ability.Constitution)],
    };

    public static Feat ResilientWisdom { get; } = new(new("res_wis"), "Resilient (Sabedoria)", FeatCategory.General,
        "+1 SAB e proficiência no teste de resistência de Sabedoria.")
    {
        Increase = AbilityIncreaseRule.Fixed(Ability.Wisdom),
        Effects = [new SaveProficiency(Ability.Wisdom)],
    };

    public static Feat CrossbowExpert { get; } = new(new("xbow"), "Crossbow Expert", FeatCategory.General,
        "+1 DES. Ignora a recarga das bestas e não tem desvantagem atirando em corpo a corpo.")
    {
        Increase = AbilityIncreaseRule.Fixed(Ability.Dexterity),
    };

    public static Feat Sharpshooter { get; } = new(new("sharp"), "Sharpshooter", FeatCategory.General,
        "+1 DES. Ignora meia cobertura e três quartos; sem desvantagem em alcance longo.")
    {
        Increase = AbilityIncreaseRule.Fixed(Ability.Dexterity),
    };

    public static ImmutableArray<Feat> All { get; } =
    [
        Alert, Lucky, Tough, MagicInitiateWizard, MagicInitiateCleric, MagicInitiateDruid, Skilled, SavageAttacker,
        Healer, Musician, TavernBrawler, Crafter, Survivor, SharpEye, MistWalker, GatheredWhispers,
        FeyTouched, WarCaster, ResilientConstitution, ResilientWisdom, CrossbowExpert, Sharpshooter,
    ];

    /// <summary>Talentos oferecidos como talento extra do humano, na ordem da página original.</summary>
    public static ImmutableArray<Feat> ExtraOriginChoices { get; } =
    [
        Alert, Lucky, MagicInitiateWizard, MagicInitiateDruid, MagicInitiateCleric, Tough,
        Skilled, SavageAttacker, Survivor, SharpEye, Healer, Musician,
    ];

    /// <summary>Talentos oferecidos nos níveis de talento, além do aumento de atributo.</summary>
    public static ImmutableArray<Feat> LevelChoices { get; } =
    [
        FeyTouched, WarCaster, ResilientConstitution, ResilientWisdom, CrossbowExpert, Sharpshooter,
        Alert, Lucky, Tough,
    ];
}
