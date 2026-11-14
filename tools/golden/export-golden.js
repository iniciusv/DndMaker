#!/usr/bin/env node
/*
 * Gera tools/golden/golden.json a partir do motor original (Artefato Base/logic.js).
 * Os testes de paridade em tests/DndMaker.Content.Tests comparam o C# com estes casos.
 *
 * Uso: node tools/golden/export-golden.js [caminho/para/logic.js] [saida.json]
 *
 * O arquivo tem um caso por linha, para que um diff mostre exatamente o que mudou.
 */
'use strict';
const fs = require('fs');
const path = require('path');

const logicPath = path.resolve(process.argv[2] || path.join(__dirname, '..', '..', 'Artefato Base', 'logic.js'));
const outPath = path.resolve(process.argv[3] || path.join(__dirname, 'golden.json'));
const { compute, BUILDS, BUILD_ORDER, SP, BG, GF, HUMAN_FEATS, AB } = require(logicPath);

const LEVELS = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];
const ENEMY_AC = [13, 15, 16, 18];
const DEFAULT_AC = 15;

const variantsOf = b => (BUILDS[b].variants || [{ id: null }]).map(v => v.id);
const baseSel = b => ({ b, v: variantsOf(b)[0], sp: 'humano', bg: BUILDS[b].defBg, hf: 'alert', g: {} });

/* ---------- conjunto de casos ---------- */
const cases = new Map();
const add = (sel, L, tAC) => {
  const s = { b: sel.b, v: sel.v, sp: sel.sp, bg: sel.bg, hf: sel.hf, g: sel.g || {} };
  const key = JSON.stringify([s, L, tAC]);
  if (!cases.has(key)) cases.set(key, { sel: s, L, tAC });
};

for (const b of BUILD_ORDER) {
  const B = BUILDS[b];
  // 1. Toda variante, todo nível, todas as CAs de inimigo da interface.
  for (const v of variantsOf(b)) for (const L of LEVELS) for (const ac of ENEMY_AC) add({ ...baseSel(b), v }, L, ac);
  // 2. Toda espécie nos níveis 1, 3, 5, 7 e 10 (visão no escuro, PV, CA e vantagens mudam com o nível).
  for (const sp in SP) for (const L of [1, 3, 5, 7, 10]) add({ ...baseSel(b), sp }, L, DEFAULT_AC);
  // 3. Todo antecedente nos níveis 1 e 10 (distribuição de atributos e talento de origem).
  for (const bg in BG) for (const L of [1, 10]) add({ ...baseSel(b), bg }, L, DEFAULT_AC);
  // 4. Todo talento extra do humano em toda variante, nos níveis 1, 5 e 6 (Shillelagh, Alert, Tough...).
  for (const hf of HUMAN_FEATS) for (const v of variantsOf(b)) for (const L of [1, 5, 6]) add({ ...baseSel(b), v, hf }, L, DEFAULT_AC);
  // 5. Toda escolha de nível de talento: em cada nível sozinha e em todos ao mesmo tempo.
  for (const g of Object.keys(GF)) {
    for (const lv of B.asiLv) add({ ...baseSel(b), g: { [lv]: g } }, 10, DEFAULT_AC);
    const all = {}; B.asiLv.forEach(lv => (all[lv] = g));
    for (const L of LEVELS) add({ ...baseSel(b), g: all }, L, DEFAULT_AC);
  }
  // 6. Combinações cruzadas usadas no test.js original e na interface.
  for (const sp of ['gnomo', 'elfo', 'anao', 'kalashtar', 'harengon', 'warforged'])
    for (const bg of ['haunted', 'andarilho', 'guia', 'soldado'])
      for (const L of [4, 6, 8]) add({ ...baseSel(b), sp, bg, hf: 'mi_druid', g: Object.fromEntries(B.asiLv.map(lv => [lv, 'fey'])) }, L, DEFAULT_AC);
}

/* ---------- saída ---------- */
const round = x => (Number.isInteger(x) ? x : Number(x.toPrecision(15)));
const output = (sel, L, tAC) => {
  const r = compute(sel, L, tAC);
  return {
    v: r.v,
    pb: r.pb,
    bgApplied: r.bgApplied,
    sc: AB.map(a => r.sc[a]),
    m: AB.map(a => r.m[a]),
    hp: r.hp,
    ac: { v: r.ac.v, d: r.ac.d, stealth: !!r.ac.stealth, extra: r.ac.extra || '' },
    init: r.init,
    dark: { v: r.dark.v, d: r.dark.d || '' },
    sb: r.sb,
    saves: AB.map(a => ({ v: r.saves[a].v, prof: r.saves[a].prof, adv: r.saves[a].adv })),
    strahd: { p: round(r.strahd.p), adv: r.strahd.adv, cc: r.strahd.cc },
    conc: r.conc ? { p: round(r.conc.p), adv: r.conc.adv } : null,
    feats: [...r.feats].sort(),
    picks: r.picks.map(p => ({ lv: p.lv, g: p.g, what: p.what })),
    shHint: r.shHint,
    routines: r.routines.map(x => ({
      n: x.n, tier: x.tier, multi: x.multi, note: x.note, f: x.f,
      hit: x.hit, dc: x.dc, raw: round(x.raw), exp: round(x.exp),
    })),
    best: [0, 1, 2].map(t => r.best(t).n),
  };
};

const list = [...cases.values()].map(c => ({ in: { ...c.sel, L: c.L, tAC: c.tAC }, out: output(c.sel, c.L, c.tAC) }));
const meta = {
  source: path.relative(path.dirname(outPath), logicPath).replace(/\\/g, '/'),
  abilities: AB,
  builds: BUILD_ORDER,
  count: list.length,
  note: 'Gerado por tools/golden/export-golden.js. Não editar à mão.',
};
const body = list.map(c => '  ' + JSON.stringify(c)).join(',\n');
fs.writeFileSync(outPath, `{\n"meta": ${JSON.stringify(meta)},\n"cases": [\n${body}\n]\n}\n`);
console.log(`golden: ${list.length} casos -> ${path.relative(process.cwd(), outPath)}`);
