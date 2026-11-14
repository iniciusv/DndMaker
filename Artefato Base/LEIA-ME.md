# Builds para Barovia: código-fonte

Comparador de builds de D&D 2024 para uma campanha de Curse of Strahd (níveis 1 a 10).

## Arquivos

- `logic.js`: o motor. Dados (BUILDS, SP = espécies, BG = antecedentes, OF = talentos de origem, GF = escolhas dos níveis de talento) e a função `compute(sel, L, tAC)`. Não depende de navegador.
- `ui.js`: a interface. Lê os seletores, chama `compute` e desenha as cartas e as matrizes.
- `template.html`: HTML e CSS, com os marcadores `/*LOGIC*/` e `/*UI*/` onde os dois scripts são inseridos.
- `index.html`: a página pronta (template + logic + ui). Abre direto no navegador.
- `test.js`: imprime vários casos e roda todas as combinações para garantir que nada quebra (`node test.js`).

## Como `compute` funciona

Entrada `sel`: `{ b: build, v: variante, sp: espécie, bg: antecedente, hf: talento extra do humano, g: { nível: escolha } }`, mais o nível `L` e a CA do inimigo `tAC`.

1. Atributos base da build + antecedente (+2 e +1 nos dois atributos de maior prioridade que o antecedente permite).
2. Em cada nível de talento: aumento de atributo (arredonda o principal, o ponto que sobra vai para o próximo ímpar) ou talento (Fey Touched e War Caster dão +1 no atributo mental ímpar mais importante; Resilient dá +1 e proficiência).
3. PV: dado máximo no nível 1, média arredondada para cima nos demais, + CON; Tough +2 por nível; anão +1 por nível.
4. CA, iniciativa e visão no escuro vêm de funções da build e da espécie.
5. Rotinas de dano: partes `atk` (n ataques), `rider` (dano extra uma vez se algum ataque acertar) e `save` (CD contra teste com +2, metade no sucesso se `half`). Dano esperado = média × chance de acertar, sem críticos; acerto limitado entre 5% e 95%.
6. Charme de Strahd: Sabedoria contra CD 17; vantagem pela espécie; Countercharm dá uma segunda rolagem com vantagem.

## Pedido sugerido para o Claude no VS Code

> Leia LEIA-ME.md e logic.js. Converta o motor para C# (.NET 8) como uma biblioteca de classes, com records para Build, Species, Background e Feat, e o cálculo num método que devolve um objeto Result. Crie um projeto de console que imprime as mesmas saídas do test.js e um projeto de testes que confere esses números. Mantenha os textos em português.
