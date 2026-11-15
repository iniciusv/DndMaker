# Plano: DndMaker em C# com Avalonia

Programa de desktop que recria o comparador "Builds para Barovia" ([Artefato Base](../Artefato%20Base/LEIA-ME.md)) em .NET 10, com Avalonia 12, DDD e Clean Code.

## Andamento

| Fase | Situação |
|---|---|
| 0. Base | **Concluída em 2026-11-14** |
| 1. Núcleo do domínio | **Concluída em 2026-11-14** |
| 2. Modelo do domínio | **Concluída em 2026-11-14** |
| 3. Conteúdo e paridade | **Concluída em 2026-11-15** |
| 4. Application e Infrastructure | **Concluída em 2026-11-15** |
| 5. Interface | **Concluída em 2026-11-15** |
| 6. Correções | **Concluída em 2026-11-15** |
| 7. Extras | Opcional |

## Decisões tomadas

1. Conteúdo (builds, espécies, antecedentes, talentos) escrito em C#, não em JSON, na primeira versão.
2. Nomes do código em inglês. A interface pode ser em português ou misturada (termos de D&D em inglês); não é crítico.
3. Primeiro paridade exata com o `logic.js`, depois as correções. **As correções da seção 12 são obrigatórias**, cada uma registrada em teste.
4. `DataGrid` na tabela "Todas as builds", para ordenar por coluna.
5. Ao terminar uma fase, a seguinte pode começar sem esperar aprovação.

## 1. Objetivo

1. **Mesmos números do JavaScript.** Testes comparam cada resultado do C# com o `logic.js` (pasta `tools/golden`).
2. **Corrigir os 7 problemas da análise** (seção 12).
3. **Aproveitar o .NET:** tipos que impedem erros, motor de d20 exato com rerrolagens, regras escritas como dados que podem ser combinados.
4. **Espaço para crescer:** outras campanhas, críticos, distribuição de dano, edição manual de atributos.

## 2. Tecnologias

| Item | Escolha |
|---|---|
| Runtime | .NET 10 (LTS), C# 14, SDK fixado em `global.json` |
| Interface | Avalonia 12.1.2, tema Fluent personalizado, bindings compilados |
| Padrão da interface | MVVM com `CommunityToolkit.Mvvm` 8.4.2 |
| Injeção de dependência | `Microsoft.Extensions.DependencyInjection` |
| Testes | xUnit 2.9, FluentAssertions 8, `Avalonia.Headless` para os ViewModels |
| Qualidade | `Directory.Build.props`, `Directory.Packages.props`, `.editorconfig`, avisos viram erros, `AnalysisLevel=latest-recommended` |

## 3. Estrutura

```
DNDMaker/
├─ DndMaker.slnx
├─ Directory.Build.props / Directory.Packages.props / .editorconfig / global.json / nuget.config
├─ Artefato Base/                 referência original (não mexer)
├─ docs/PLANO.md
├─ src/
│  ├─ DndMaker.Domain/            regras genéricas de D&D 2024, sem dependências
│  ├─ DndMaker.Content/           as 10 builds, espécies, antecedentes, talentos, campanha
│  ├─ DndMaker.Application/       casos de uso, DTOs, interfaces de repositório
│  ├─ DndMaker.Infrastructure/    gravação em JSON, registro de dependências
│  └─ DndMaker.Desktop/           Avalonia: Views, ViewModels, tema, fontes
├─ tests/
│  ├─ DndMaker.Domain.Tests/
│  ├─ DndMaker.Content.Tests/     paridade com o JS
│  ├─ DndMaker.Application.Tests/
│  ├─ DndMaker.Infrastructure.Tests/
│  └─ DndMaker.Desktop.Tests/
└─ tools/golden/                  export-golden.js e golden.json
```

As dependências só apontam para dentro:

```
Desktop ──► Application ──► Domain ◄── Content
   │  │          ▲                        ▲
   │  └──► Infrastructure                 │
   └──────────────────────────────────────┘
```

- `Domain` não referencia nada. Conhece d20, atributos, efeitos e rotinas, mas não sabe o que é Strahd.
- `Content` é o pacote de regras da campanha. Uma segunda campanha seria outro projeto.
- `Desktop` é o único lugar onde as dependências são montadas (`CompositionRoot`).

## 4. Domínio

### 4.1 Linguagem

Glossário a manter em `docs/glossario.md`:

| Código | Na tela |
|---|---|
| `BuildArchetype` | Build |
| `CharacterBuild` | Personagem montado |
| `ResourceTier` | À vontade, concentração, pico |
| `AttackRoutine` | Rotina de dano |
| `CharmThreat` | Charme de Strahd |

### 4.2 Value objects

| Tipo | Responsabilidade | Substitui no JS |
|---|---|---|
| `Ability`, `AbilitySet` (`[Flags]`) | Seis atributos; conjuntos como `Mental` | `AB`, `['int','wis','cha']` |
| `AbilityMap<T>` | Mapa de 6 posições em `[InlineArray(6)]`, sem alocação | objetos `{str:..}` |
| `AbilityScore` | 1 a 30 (personagem até 20), `Modifier` | `mod()` |
| `Level` | 1 a 20, `ProficiencyBonus` | `pbOf()` |
| `Dice`, `DamageFormula` | `2d8`, média, operadores, `ToString()` | `diceAvg`, `terms` |
| `AttackBonus`, `DifficultyClass`, `ArmorClass` | Evitam trocar um número por outro | inteiros soltos |
| `Probability` | 0 a 1, `Or`, `Complement` | `clamp(...)` |
| `Meters` | Visão no escuro e alcances | `dv:18` |
| `SpeciesId`, `BackgroundId`, `FeatId`, `ArchetypeId`, `VariantId` | Identificadores com tipo próprio | chaves em texto |

### 4.3 Motor de d20

```csharp
public enum RollMode { Normal, Advantage, Disadvantage }
public enum NaturalRule { None, AttackRoll }   // ataque: 1 sempre erra, 20 sempre acerta

public sealed record D20Check(int Modifier, int Target, RollMode Mode, NaturalRule Naturals)
{
    public Probability Success() { /* percorre todos os resultados, exato */ }
}

public abstract record FailureRecovery;
public sealed record RerollWithAdvantage(string Source) : FailureRecovery;      // Countercharm
public sealed record AddBonusOnFail(int Bonus, string Source) : FailureRecovery; // Survivor
public sealed record SpendLuck(string Source) : FailureRecovery;                // Lucky
public sealed record RerollNaturalOne(string Source) : FailureRecovery;         // halfling
```

A resistência ao charme mostra dois números, **sem gastar nada** e **gastando recursos**, com a lista do que entrou na conta.

Como ficou na fase 1 (`src/DndMaker.Domain/Checks`):

- `D20Check(Modifier, Target, Mode, Naturals, RerollNaturalOnes)` calcula a chance exata. Como passar ou falhar só cresce com o valor do dado, vantagem é `1 − (1 − p)²` e desvantagem é `p²`.
- **Halfling:** `RerollNaturalOnes` rerrola o 1 natural só quando ele falharia.
- **Lucky:** é uma fonte de vantagem, via `WithAdvantage()` (que também cancela desvantagem).
- **Recuperações** (`SuccessWith`): `BonusOnFailure` (Survivor) soma na mesma rolagem; `RerollOnFailure` (Countercharm) é uma rolagem nova com o modificador original, aplicada depois dos bônus.
- Os testes conferem tudo contra uma enumeração de todos os resultados e contra as fórmulas do `logic.js` (ataque, falha do monstro, charme com Countercharm).
- A lista dos atributos em ordem é `AbilityList.All`, porque o nome `Abilities` colidia com o namespace.

### 4.4 Efeitos

Espécies, talentos e características de classe entregam uma lista de efeitos, aplicada pelo `CharacterSheetBuilder` em fases fixas (atributos → proficiências → derivados → defesas).

```csharp
public abstract record Effect;
public sealed record AbilityIncrease(Ability Ability, int Amount) : Effect;
public sealed record SaveProficiency(Ability Ability) : Effect;
public sealed record SaveAdvantage(AbilitySet Abilities) : Effect;
public sealed record ConditionAdvantage(Condition Condition) : Effect;
public sealed record HitPointsPerLevel(int Amount) : Effect;
public sealed record ArmorClassBonus(int Amount) : Effect;
public sealed record InitiativeBonus(InitiativeSource Source) : Effect;
public sealed record Darkvision(Meters Range, DarkvisionKind Kind) : Effect;
public sealed record SaveAura(Ability Source, int Minimum) : Effect;
public sealed record ConcentrationAdvantage : Effect;
public sealed record ConditionRecovery(Condition Condition, FailureRecovery Recovery) : Effect;
```

### 4.5 Dados de referência

| Tipo | Conteúdo |
|---|---|
| `Species` | id, nome, fonte, visão no escuro, efeitos, texto, `GrantsExtraOriginFeat`, `DarkvisionNeedsConfirmation` |
| `Background` | id, nome, 3 atributos permitidos, talento de origem, perícias |
| `Feat` | id, nome, categoria (`Origin`, `General`, `DarkGift`), regra de aumento de atributo, efeitos, texto |
| `Campaign` | fases, janela de destaque (5–6), `CharmThreat(DC 17, Wis)`, CAs sugeridas |

Catálogos em `FrozenDictionary<TId, T>`.

### 4.6 `BuildArchetype`

```csharp
public abstract class BuildArchetype
{
    public ArchetypeId Id { get; }
    public ArchetypeStatus Status { get; }
    public AbilityMap<int> BaseScores { get; }
    public AbilityPriority Priority { get; }
    public ClassProgression Progression { get; }   // ex.: Guerreiro 1 → Bruxo N
    public ImmutableArray<BuildVariant> Variants { get; }
    public BackgroundId SuggestedBackground { get; }
    public ImmutableArray<LevelMilestone> Milestones { get; }

    public abstract IEnumerable<Effect> FeaturesAt(BuildContext c);
    public abstract ArmorSetup ArmorAt(BuildContext c);
    public abstract IEnumerable<AttackRoutine> RoutinesAt(BuildContext c);
}
```

- `ClassProgression` substitui contas como `L-1`: `c.LevelIn(ClassId.Warlock)`, `c.PactSlotLevel`, `c.CantripTier`, dados de vida e níveis de talento.
- `ArmorTable` substitui `medium()`.
- Variantes declaram seus requisitos (Shillelagh exige `mi_druid`).

### 4.7 Rotinas de dano

```csharp
public sealed record AttackRoutine(string Name, ResourceTier Tier, ImmutableArray<RoutineStep> Steps,
                                   TargetScope Scope, Situation? Situation, string? Note);
public abstract record RoutineStep;
public sealed record WeaponAttack(int Count, AttackBonus Bonus, DamageFormula Damage) : RoutineStep;
public sealed record SpellAttack(int Count, AttackBonus Bonus, DamageFormula Damage) : RoutineStep;
public sealed record Rider(RoutineStep Trigger, DamageFormula Damage) : RoutineStep;
public sealed record SaveEffect(DifficultyClass Dc, DamageFormula Damage, bool HalfOnSuccess, int Targets) : RoutineStep;
```

- `DamageEstimator` devolve `RoutineEstimate` com o detalhe de cada etapa.
- `RoutineSelector.Best(estimates, maxTier)` ignora rotinas de vários alvos e situacionais.

### 4.8 Agregados

- **`CharacterBuild`**: variante pertence à build; talento extra só se a espécie der e nunca Dark Gift; escolhas só nos níveis de talento; `ChangeArchetype` mantém espécie e talento extra.
- **`BuildComparison`** (raiz salva): nível, `CombatAssumptions`, faixa de recurso, 3 vagas.

### 4.9 Serviços de domínio

| Serviço | Substitui no JS |
|---|---|
| `AbilityScoreAllocator` (com `IAsiPolicy`) | loop `asiLv` |
| `HitPointCalculator` | bloco `hp` |
| `SavingThrowCalculator` | `saves` |
| `CharmResistanceCalculator` | `strahd` |
| `ConcentrationCalculator` | `conc` |
| `CharacterAnalyzer` → `CharacterSheet` | `compute()` |

### 4.10 Como ficou na fase 2

| Plano | Implementado em `src/DndMaker.Domain` |
|---|---|
| Identificadores | `Catalog/Ids.cs` |
| `Species`, `Background`, `Feat`, `AbilityIncreaseRule` | `Catalog/CatalogEntries.cs` (igualdade pelo id; antecedente exige 3 atributos) |
| Efeitos e `CharacterSheetBuilder` | `Effects/Effect.cs` e `Effects/CharacterTraits.cs` (acumula os efeitos; efeito sem tratamento lança exceção) |
| `CharacterClass`, `ClassProgression` | `Classes/` (`Of`, `Dip`, `LevelIn`, `HitDice`, `FeatLevels`) |
| `ArmorTable`, `ArmorSetup` | `Equipment/Armor.cs` (`Wear`, `Best`; empate fica com a mais leve) |
| `BuildArchetype`, `ArchetypeProfile`, `BuildVariant`, `BuildContext` | `Builds/` |
| `CharacterBuild`, `LevelChoice` | `Builds/CharacterBuild.cs` (imutável; o talento extra guardado volta ao trocar para humano) |
| Rotinas, `DamageEstimator`, `RoutineSelector`, `RoutineFormatter`, `CombatAssumptions` | `Routines/` |
| `AbilityScoreAllocator`, `IAsiPolicy` | `Analysis/AbilityScoreAllocator.cs` (`RoundUpPriorityPolicy` copia o JS) |
| PV, testes de resistência, ameaças, concentração | `Analysis/Defenses.cs` (`HitPointCalculator`, `DefenseCalculator`, `ConditionThreat`, `ThreatResistance`) |
| `CharacterAnalyzer`, `CharacterSheet` | `Analysis/CharacterAnalyzer.cs` |
| `BuildComparison` | `Comparison/BuildComparison.cs` |

Os testes têm duas builds que copiam o paladino e o Guerreiro 1 → Bruxo do `logic.js` (`tests/DndMaker.Domain.Tests/Fixtures`). As fichas delas batem com os casos do golden, o que adianta a paridade da fase 3.

Preparado para as correções da fase 6, sem mudar a paridade ainda:

- `ThreatResistance` já separa `Passive` de `WithResources`. Lucky e Survivor só passam a contar quando o conteúdo ganhar os efeitos `ConditionRecovery` (problema 1).
- `AttackRoutine.When(Situation.VsUndead)` tira a rotina do melhor turno (problema 2).
- `RoutineEstimate.Steps` traz o bônus de cada ataque (problema 3).
- `BuildVariant.RequiredFeat` e `CharacterSheet.VariantRequirementMissing` (problema 4).
- `RoutineSelector.Best` filtra antes de comparar e devolve nulo se nada servir (problema 6).

## 5. Conteúdo

- `Species/`, `Backgrounds/`, `Feats/` em C#; textos em `Resources/Content.pt-BR.resx`.
- `Archetypes/`: uma classe por build.
- `Campaigns/CurseOfStrahd.cs`: fases, CD 17 e as notas da página.
- `AddCurseOfStrahdContent(IServiceCollection)`.

### 5.1 Como ficou na fase 3

| Plano | Implementado |
|---|---|
| Espécies, antecedentes, talentos | `src/DndMaker.Content/Catalog/` (`SpeciesCatalog`, `BackgroundCatalog`, `FeatCatalog`, `ClassCatalog`), na ordem da página |
| Uma classe por build | `src/DndMaker.Content/Archetypes/` (10 builds); regras repetidas em `ArchetypeRules` (Shillelagh, True Strike, armadura média, Mage Armor, Countercharm) |
| Campanha | `Campaigns/CurseOfStrahd.cs`: `Campaign` (charme CD 17 SAB, níveis 1–10, destaque 5–6, CAs 13/15/16/18) e o `Ruleset` completo |
| Catálogo para a Application | `Domain/Rulesets/Ruleset.cs`: listas na ordem de exibição, busca por id em `FrozenDictionary` e validação das referências na criação. A Application vai depender só dele, não do Content |
| Registro | `AddCurseOfStrahdContent()` registra `Ruleset` e `Campaign` |
| Textos da ficha | `Ability.Abbreviation()` (FOR…CAR), `BackgroundBonus.ToString()` ("CAR +2, CON +1"), `LevelPick.ChangesText` ("CAR 17 → 18") |
| Paridade | `tests/DndMaker.Content.Tests/Parity/`: um teste por build compara os 5.020 casos do golden campo a campo (tolerância 1e-9) e lista cada diferença |
| Exaustivo | `ExhaustiveTests`: as 1.040.000 combinações do `test.js`, em paralelo (cerca de 3 s) |

Decisões e observações:

- Os textos continuam em C#, sem `Content.pt-BR.resx` (decisão 1); a interface é só em português.
- O `fval` reproduz o problema 4 de propósito (Shillelagh só por ter Magic Initiate, sem variante), com comentário no código.
- O paladino mantém "… contra morto-vivo" sem `Situation.VsUndead` (problema 2), e nenhum conteúdo ganhou `ConditionRecovery` de Lucky ou Survivor (problema 1). As duas coisas mudam na fase 6.

## 6. Application

| Caso de uso | Retorno |
|---|---|
| `GetCatalogQuery` | `CatalogDto` |
| `AnalyzeComparisonQuery` | `ComparisonResultDto` (3 fichas, líder em CA/PV/iniciativa, empate sem líder) |
| `BuildsOverviewQuery` | Linhas de "Todas as builds", com as escolhas padrão na legenda |
| `DamageProgressionQuery` | 10 builds × 10 níveis |
| `ComparisonSession` | Estado atual, mudanças, evento `Changed`, gravação |

## 7. Infrastructure

- `JsonComparisonRepository` em `%AppData%/DndMaker/comparison.json`, `System.Text.Json` com source generator, `schemaVersion` e limpeza ao carregar.
- Salvamento automático 500 ms após a última mudança.

### 7.1 Como ficou na fase 4

| Plano | Implementado |
|---|---|
| Catálogo para os seletores | `Application/Catalog/GetCatalogQuery` → `CatalogDto` (builds com variantes e níveis de talento, espécies, antecedentes com o rótulo "Nobre · FOR, INT, CAR · Skilled", talentos extras, escolhas de nível, CAs, faixas, fases e notas). Calculado uma vez |
| Cartas | `Comparison/AnalyzeComparisonQuery` → `ComparisonResultDto`: nível com fase e observação, 3 `SheetDto` e `LeadersDto` (nulo no empate) |
| Ficha da carta | `Sheets/SheetMapper` → `SheetDto` com tudo o que a carta da página mostra, inclusive "visão no escuro a confirmar", aviso de Shillelagh e as auras que somam nos testes |
| Todas as builds | `Overview/BuildsOverviewQuery`: linhas com as marcas de topo por coluna (empates incluídos, como na página) e a legenda com espécie, talento extra (Lucky) e CA |
| Dano por nível | `Overview/DamageProgressionQuery`: 10 builds × 10 níveis e o máximo para colorir |
| Sessão | `Session/ComparisonSession`: única peça com estado; troca a comparação imutável e avisa por `Changed`. Ids em texto vindos da tela |
| Gravação | `Session/ComparisonPersistence`: restaura ao abrir, grava 500 ms após a última mudança (`TimeProvider`, testado com relógio manual), `FlushAsync` e gravação ao descartar; falhas viram `SaveFailed` |
| Formato gravado | `ComparisonSnapshot` (só ids em texto); `ComparisonSnapshotMapper` limpa ao carregar: build sumida volta à carta inicial, ids sumidos viram o padrão, nível/CA/faixa fora das opções voltam ao padrão, escolhas inválidas somem |
| Repositório | `Infrastructure/Persistence/JsonComparisonRepository`: `%AppData%/DndMaker/comparison.json`, source generator, `schemaVersion` 1, escrita em arquivo temporário e troca atômica; arquivo ilegível ou de outra versão vale como "nada gravado" |
| Registro | `AddCurseOfStrahdContent()`, `AddApplication()`, `AddInfrastructure(caminho?)`, montados no `CompositionRoot`; o `App` restaura antes da primeira tela e grava ao fechar |

Mudanças em relação ao desenho:

- A `Campaign` virou um record com propriedades `required` e ganhou fases, observações por nível, rótulos das CAs, a comparação inicial da página (`ComparisonPreset`), espécie e talentos padrão e as notas "O que já sabemos". O `Ruleset` confere tudo na criação e sabe montar o personagem padrão (`DefaultCharacter`, `OverviewCharacter`) e a comparação inicial.
- `CharacterSheet.SaveBonusSources` diz de onde vem o bônus nos testes de resistência.
- Novo projeto `tests/DndMaker.Infrastructure.Tests`, com o teste do critério da fase: editar, fechar o container, abrir outro e ver a mesma comparação.
- A legenda de "Todas as builds" já informa o talento extra usado (Lucky), o que resolve a parte de exibição do problema 5.

## 8. Interface

```
┌ MainWindow ───────────────────────────────────────────────────────────┐
│ Cabeçalho                                                             │
│ LevelRoad: [1]…[10]   CA inimigo ▾   Rotinas ▾                        │
├───────────────┬───────────────┬───────────────────────────────────────┤
│ CharacterCard │ CharacterCard │ CharacterCard                         │
├───────────────┴───────────────┴───────────────────────────────────────┤
│ Abas: Todas as builds (ordenável) | Dano por nível | O que já sabemos │
└───────────────────────────────────────────────────────────────────────┘
```

- ViewModels: `MainViewModel`, `CharacterCardViewModel`, `SheetViewModel`, `BuildsOverviewViewModel`, `DamageGridViewModel`.
- Controles: `LevelRoad`, `ProbabilityMeter`, `StatTile`, `RoutineList`, `HeatCell`.
- Tema Tarokka claro e escuro (`ThemeDictionaries`), fontes IM Fell English e Alegreya Sans (OFL) embutidas.

### 8.1 Como ficou na fase 5

| Plano | Implementado em `src/DndMaker.Desktop` |
|---|---|
| `MainViewModel` | Seletores do topo (nível, CA, faixa), três cartas, tabelas e notas; recalcula tudo a cada `ComparisonSession.Changed`. Comandos `NextLevel`/`PreviousLevel` e aviso de falha ao gravar |
| `CharacterCardViewModel` | Seletores da carta (build, variante, espécie, talento extra só para quem tem, antecedente, níveis de talento da build) e a ficha; só reflete o estado que volta da sessão |
| `SheetViewModel` | Os textos da carta montados como no `ui.js` (armadura, picks, testes, charme, rotinas, progressão, espécie e talentos, balanço) |
| `BuildsOverviewViewModel` | `DataGridCollectionView` com linhas trocadas no lugar, para a ordenação escolhida sobreviver às atualizações; botões I/II/III por linha |
| `DamageGridViewModel` | Células com intensidade 0–0,34 (a mistura de cor da página), janela 5–6 e nível atual marcados |
| Controles | `StatTile`, `ProbabilityMeter`, `RoutineList`, `HeatCell` em `Controls/`; a estrada de níveis ficou na própria janela (um `ListBox` em grade, navegável pelas setas) |
| Tema e fontes | `Themes/Tarokka.axaml` (paleta clara e escura, acento do Fluent em sangue) e `Themes/TarokkaStyles.axaml` (as classes do CSS original); fontes e licenças OFL em `Assets/Fonts` |
| Formatação | `Formatting/Format`: pt-BR com uma casa (igual ao `toLocaleString` do JS), porcentagem inteira, sinal tipográfico |

Teclado: Tab percorre todos os seletores, as setas mudam o nível na estrada, Ctrl+← e Ctrl+→ mudam o nível de qualquer lugar, e cada seletor tem nome de acessibilidade.

Testes (`tests/DndMaker.Desktop.Tests`): os ViewModels sobre o conteúdo real, sem janela, e a janela renderizada com `Avalonia.Headless` + Skia nos temas claro e escuro. O teste falha se qualquer ligação de dados registrar aviso e salva capturas em `bin/.../screenshots` para conferência visual.

Outras mudanças: o `App` só monta os serviços no modo desktop (nos testes sem janela ele serve só de tema), e o despachante `IUiDispatcher` leva o aviso de falha ao gravar para a thread da interface.

## 9. Testes

1. **Paridade:** `node tools/golden/export-golden.js` gera ~5.000 casos; `Content.Tests` compara com tolerância de 1e-9.
2. **Diferenças propositais:** testes `Diverge_*` com comentário; nos casos afetados, só o campo que muda sai da comparação (`KnownDivergences`).
3. **Unidade:** d20 contra enumeração, `Dice`, modificadores, alocador, regras do `CharacterBuild`.
4. **Exaustivo:** as 1.040.000 combinações do `test.js` (1.092.000 depois da variante de bordão do problema 4).
5. **ViewModels:** `Avalonia.Headless`.

## 10. Clean Code

- Nullable, avisos viram erros, analisadores recomendados.
- Imutável por padrão; só ViewModels e `ComparisonSession` mudam de estado.
- Sem números mágicos; métodos curtos; uma classe por build; nenhum `switch` por id de build fora da própria build.
- `DomainException` para regra violada (indica bug).
- Comentários explicam a regra do livro ou uma diferença em relação ao JS.

## 11. Fases

| Fase | Entrega | Pronto quando |
|---|---|---|
| 0. Base | Git, solution, props, `nuget.config`, golden, janela vazia | Build sem avisos; golden gerado; testes do golden passando |
| 1. Núcleo do domínio | Value objects, `AbilityMap<T>`, `Dice`/`DamageFormula`, motor de d20 | Testes do núcleo passam |
| 2. Modelo do domínio | Efeitos, catálogo, `BuildArchetype`, `ClassProgression`, `ArmorTable`, rotinas, agregados, serviços | Uma build de teste gera a ficha completa |
| 3. Conteúdo | Espécies, antecedentes, talentos, 10 builds, campanha | 100% de paridade e teste exaustivo passando |
| 4. Application e Infrastructure | Casos de uso, DTOs, sessão, repositório, DI | Comparação sobrevive a fechar e abrir |
| 5. Interface | Janela completa, tema, fontes | Mesmo conteúdo da página, utilizável no teclado |
| 6. Correções | Problemas 1 a 7 | Paridade com as diferenças documentadas |
| 7. Extras | Seção 13 | Item a item |

## 12. Problemas do artefato que serão corrigidos

1. Lucky, Survivor, Watcher's Will e Twilight Sanctuary não entram na chance contra o charme.
2. A rotina "contra morto-vivo" do paladino vira "melhor turno" na tabela.
3. Rotinas com dois ataques mostram só o bônus do primeiro.
4. Guerreiro 1 → Bardo do Valor usa Shillelagh sem escolher variante e soma Dueling ao bordão.
5. As tabelas usam Lucky como talento extra e a legenda não diz.
6. `best()` parte sempre da primeira rotina, sem conferir faixa nem alvos.
7. Dano extra de espécies (Aasimar, Bugbear, Golias) não é calculado nem avisado.

### 12.1 Como ficou na fase 6

| Problema | Correção | Teste |
|---|---|---|
| 1 | A chance mostrada é a "gastando recursos": Lucky e Watcher's Will (`SaveAdvantageResource`) dão vantagem se ainda não houver; Survivor (`ProficiencyBonusOnFailure`) soma o PB depois da falha; Countercharm continua rerrolando. A carta diz o que entrou e quanto seria sem gastar nada. Twilight Sanctuary (`ConditionRemedy`) encerra o charme depois e aparece à parte, fora da conta, porque não muda a chance do teste | `Diverge_1_*` |
| 2 | "… contra morto-vivo" é `Situation.VsUndead`: aparece na lista, com etiqueta, mas não no melhor turno | `Diverge_2_*` |
| 3 | `RoutineEstimate.AttackBonuses` traz todos os bônus diferentes; a carta mostra "ataques +6 e +7" | `Diverge_3_*`, `Rotina_com_dois_ataques_mostra_os_dois_bonus` |
| 4 | Nova variante "Bordão com Shillelagh (Dueling)", que exige Magic Initiate (Druida). Defense e Dueling usam a espada. O Dueling no bordão ficou: numa mão, com escudo, as regras de 2024 permitem | `Diverge_4_*` (a variante reproduz o bordão do JS em todos os casos) |
| 5 | A legenda das tabelas diz "humano com Lucky como talento extra" (feito na fase 4) | testes de `BuildsOverviewQuery` e do `BuildsOverviewViewModel` |
| 6 | `RoutineSelector.Best` filtra faixa, alvos e situação antes de comparar e devolve nulo se nada servir (feito na fase 2). Com o conteúdo atual não muda nenhum número | `RoutineTests` no Domain |
| 7 | `ExtraDamageOption` nas espécies: Celestial Revelation (Aasimar, +PB a partir do 3), Surprise Attack (Bugbear, +2d6) e Fire's Burn (Golias, +1d10). Cada uma vira uma rotina opcional sobre o melhor turno com ataque, marcada "opcional da espécie", fora do melhor turno | `Diverge_7_*`, `Dano_extra_da_especie_aparece_como_opcional` |

A paridade continua comparando todos os 5.020 casos: `KnownDivergences` ajusta só o campo que cada correção muda e, quando dá, confere o número antigo por outro caminho (a chance só com Countercharm, o melhor turno do JS sem a rotina situacional). O `golden.json` não muda: ele é o retrato do `logic.js`.

Decisão registrada: o dano extra das espécies ficou como rotina opcional, fora do melhor turno, para não mudar a comparação entre builds; contar ou não no melhor turno pode virar uma opção da tela nos extras.

## 13. Extras

- Opção de contar críticos.
- Distribuição de dano: chance de derrubar um alvo com X PV.
- Antecedente +1/+1/+1 e editor manual de atributos.
- Dano extra de espécies como rotinas opcionais.
- Exportar ficha em Markdown ou JSON.
- Segunda campanha.
