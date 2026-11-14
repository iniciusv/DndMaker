# DndMaker

Comparador de builds de D&D 2024 para uma campanha de Curse of Strahd (níveis 1 a 10), em C# com Avalonia.
O plano completo e o andamento estão em [docs/PLANO.md](docs/PLANO.md). A versão original em HTML e JavaScript está em [Artefato Base](Artefato%20Base/LEIA-ME.md).

## Requisitos

- .NET SDK 10.0.401 ou mais novo da linha 10 (fixado em `global.json`)
- Node.js, só para gerar de novo os resultados de referência

## Comandos

```sh
dotnet build DndMaker.slnx            # compila tudo; qualquer aviso falha o build
dotnet test DndMaker.slnx             # roda os testes
dotnet run --project src/DndMaker.Desktop
node tools/golden/export-golden.js    # gera de novo tools/golden/golden.json a partir do logic.js
```

## Projetos

| Projeto | Papel |
|---|---|
| `DndMaker.Domain` | Regras genéricas: atributos, d20, efeitos, rotinas. Não depende de nada. |
| `DndMaker.Content` | As builds, espécies, antecedentes, talentos e a campanha. |
| `DndMaker.Application` | Casos de uso e DTOs para a interface. |
| `DndMaker.Infrastructure` | Gravação em JSON e registro das dependências. |
| `DndMaker.Desktop` | Interface Avalonia. |
