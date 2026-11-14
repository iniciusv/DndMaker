/* ---------- interface ---------- */
const $ = id => document.getElementById(id);
const esc = s => String(s).replace(/[&<>"]/g, ch => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;'}[ch]));
const num = (x, d) => x.toLocaleString('pt-BR', {maximumFractionDigits:d==null?1:d, minimumFractionDigits:0});
const sgn = v => (v>=0?'+':'−') + Math.abs(v);
const pct = p => Math.round(p*100) + '%';
const ROMAN = ['I','II','III'];
const PHASES = [[1,3,'Death House'],[4,6,'Barovia e Vallaki'],[7,8,'Krezk, Argynvostholt, Berez'],[9,10,'Amber Temple e Castle Ravenloft']];
const KEY = 'barovia-builds-v1';

const defaultsFor = b => { const B = BUILDS[b]; return {b, v:B.variants?B.variants[0].id:null, sp:'humano', bg:B.defBg, hf:'alert', g:{}}; };
let state = {
  L:5, ac:15, tier:1,
  cols:[
    {b:'fwl', v:'defense', sp:'humano', bg:'haunted', hf:'mi_wizard', g:{}},
    {b:'valor', v:'staff', sp:'humano', bg:'artista', hf:'mi_druid', g:{}},
    {b:'pal', v:'dueling', sp:'humano', bg:'nobre', hf:'alert', g:{}}
  ]
};
function sane(s){
  if (!s || !Array.isArray(s.cols) || s.cols.length!==3) return null;
  s.L = clamp(parseInt(s.L,10)||5,1,10); s.ac = clamp(parseInt(s.ac,10)||15,10,22); s.tier = clamp(parseInt(s.tier,10)||0,0,2);
  s.cols = s.cols.map(c => BUILDS[c.b] ? Object.assign(defaultsFor(c.b), c, {g:c.g||{}}) : defaultsFor('fwl'));
  return s;
}
function persist(){ try { localStorage.setItem(KEY, JSON.stringify(state)); } catch(e){} }

function opt(v, label, cur){ return `<option value="${esc(v)}"${v===cur?' selected':''}>${esc(label)}</option>`; }

function controls(i){
  const s = state.cols[i], B = BUILDS[s.b], sp = SP[s.sp];
  const groups = {};
  Object.keys(SP).forEach(k => (groups[SP[k].src] = groups[SP[k].src] || []).push(k));
  let h = `<label class="f"><span>Build</span><select id="c${i}-b" data-i="${i}" data-k="b">` +
    BUILD_ORDER.map(k => opt(k, BUILDS[k].n + (BUILDS[k].tag==='finalista'?' ★':''), s.b)).join('') + `</select></label>`;
  if (B.variants) h += `<label class="f"><span>Variante</span><select id="c${i}-v" data-i="${i}" data-k="v">` + B.variants.map(v => opt(v.id, v.n, s.v)).join('') + `</select></label>`;
  h += `<label class="f"><span>Espécie</span><select id="c${i}-sp" data-i="${i}" data-k="sp">` +
    Object.keys(groups).map(g => `<optgroup label="${esc(g)}">` + groups[g].map(k => opt(k, SP[k].n, s.sp)).join('') + `</optgroup>`).join('') + `</select></label>`;
  if (sp.extraFeat) h += `<label class="f"><span>Talento extra do humano</span><select id="c${i}-hf" data-i="${i}" data-k="hf">` + HUMAN_FEATS.map(k => opt(k, OF[k].n, s.hf)).join('') + `</select></label>`;
  h += `<label class="f"><span>Antecedente</span><select id="c${i}-bg" data-i="${i}" data-k="bg">` +
    Object.keys(BG).map(k => opt(k, `${BG[k].n} · ${BG[k].ab.map(a => ABL[a]).join(', ')} · ${OF[BG[k].feat].n}`, s.bg)).join('') + `</select></label>`;
  B.asiLv.forEach(lv => {
    h += `<label class="f"><span>Escolha do nível ${lv}</span><select id="c${i}-g${lv}" data-i="${i}" data-k="g" data-lv="${lv}">` +
      Object.keys(GF).map(k => opt(k, GF[k].n, s.g[lv]||'asi')).join('') + `</select></label>`;
  });
  return h;
}

function meter(label, p, adv, hint){
  const cls = p>=.6?'good':p>=.4?'mid':'low';
  return `<div class="meter ${cls}"><div class="meter-top"><span>${esc(label)}</span><b>${pct(p)}</b></div>` +
    `<div class="bar" role="img" aria-label="${pct(p)}"><i style="width:${Math.round(p*100)}%"></i></div>` +
    `<small>${adv?'Com vantagem. ':''}${esc(hint)}</small></div>`;
}

function results(i, r, tops){
  const B = r.B, L = state.L, top = k => tops[k]===i ? ' top' : '';
  let h = `<header class="card-head"><span class="rn">${ROMAN[i]}</span><div><p class="ep">${esc(B.ep)}</p><h2>${esc(B.n)}</h2></div><span class="tag t-${B.tag.replace(/\s/g,'')}">${esc(B.tag)}</span></header>`;
  h += `<p class="sum">${esc(B.sum)}</p>`;
  h += `<div class="tiles">` +
    `<div class="tile${top('ac')}"><b>${r.ac.v}</b><span>CA</span></div>` +
    `<div class="tile${top('hp')}"><b>${r.hp}</b><span>PV</span></div>` +
    `<div class="tile${top('init')}"><b>${sgn(r.init)}</b><span>Iniciativa</span></div>` +
    `<div class="tile"><b>${r.dark.v?r.dark.v+' m':'não'}</b><span>Visão no escuro</span></div></div>`;
  h += `<p class="fine">${esc(r.ac.d)}${r.ac.stealth?' (desvantagem em Furtividade)':''}. ${r.ac.extra?esc(r.ac.extra)+'. ':''}${r.dark.d?esc(r.dark.d)+'.':''}${r.sp.dvq&&!r.dark.d?'Visão no escuro da espécie a confirmar no livro.':''}</p>`;

  h += `<h3>Atributos</h3><div class="six">` + AB.map(a => `<div><span>${ABL[a]}</span><b>${r.sc[a]}</b><i>${sgn(r.m[a])}</i></div>`).join('') + `</div>`;
  h += `<p class="fine">Antecedente: ${esc(r.bgApplied)}.</p>` +
    (r.picks.length ? `<ul class="plain">` + r.picks.map(p => `<li><b>Nível ${p.lv}, ${esc(GF[p.g].n)}:</b> ${p.what?esc(p.what):'não muda atributos'}.</li>`).join('') + `</ul>` : '');

  h += `<h3>Testes de resistência</h3><div class="six sv">` + AB.map(a => { const s = r.saves[a];
    return `<div class="${s.prof?'prof':''}"><span>${ABL[a]}</span><b>${sgn(s.v)}</b><i>${s.prof?'prof.':'\u00a0'}${s.adv?' vant.':''}</i></div>`; }).join('') + `</div>`;
  h += `<p class="fine">Borda escura e "prof." marcam proficiência; "vant." marca vantagem da espécie.${r.sb?` Inclui +${r.sb} da ${esc(B.saveBonusName)}.`:''}</p>`;
  h += `<div class="meters">` + meter('Resiste ao charme de Strahd', r.strahd.p, r.strahd.adv, 'Teste de Sabedoria, CD 17 (ficha de 2016).' + (r.strahd.cc ? ' Inclui o Countercharm, que gasta a reação.' : '')) +
    (r.conc ? meter('Mantém a concentração', r.conc.p, r.conc.adv, 'Teste de Constituição, CD 10, ao sofrer dano.') : '') + `</div>`;

  h += `<h3>Dano em um turno</h3><ul class="rt">` + r.routines.map(x => {
    const isBest = x === r.best(state.tier);
    return `<li class="${x.tier>state.tier?'off':''}${isBest?' pick':''}"><div class="rt-top"><span>${esc(x.n)}</span><b>${num(x.exp)}</b></div>` +
      `<div class="rt-sub"><code>${esc(x.f)}</code> = ${num(x.raw)} se tudo acertar · ${x.hit!=null?'ataque '+sgn(x.hit):''}${x.hit!=null&&x.dc?' · ':''}${x.dc?'CD '+x.dc:''}</div>` +
      `<div class="rt-sub"><span class="pill p${x.tier}">${TIERS[x.tier]}</span>${x.multi?' <span class="pill">vários alvos</span>':''} ${esc(x.note)}</div></li>`; }).join('') + `</ul>`;
  if (r.shHint) h += `<p class="fine">O bordão com Shillelagh exige o talento Magic Initiate (Druida), pelo humano ou pelo antecedente Guia. Sem ele, a conta usa a arma comum.</p>`;

  h += `<h3>Progressão</h3><ol class="prog">` + B.feats.map(([lv, t]) => `<li class="${lv===L?'now':lv>L?'later':''}"><span>${lv}</span>${esc(t)}</li>`).join('') + `</ol>`;

  h += `<h3>Espécie e talentos</h3><ul class="plain"><li><b>${esc(r.sp.n)}</b> (${esc(r.sp.src)}): ${esc(r.sp.t)}</li>` +
    r.feats.map(f => { const o = OF[f] || GF[f]; return o && o.t ? `<li><b>${esc(o.n)}</b>: ${esc(o.t)}</li>` : ''; }).join('') +
    `<li><b>Perícias do antecedente</b>: ${esc(r.bg.sk)}.</li></ul>`;
  h += `<h3>Balanço</h3><ul class="plain"><li><b>A favor:</b> ${esc(B.pros)}</li><li><b>Contra:</b> ${esc(B.cons)}</li></ul>`;
  return h;
}

function tint(v, max){ return `background:color-mix(in srgb, var(--blood) ${Math.round(clamp(v/max,0,1)*34)}%, transparent)`; }

function matrices(){
  const L = state.L, t = state.tier;
  const rows = BUILD_ORDER.map(b => ({b, r:compute(Object.assign(defaultsFor(b), {hf:'lucky'}), L, state.ac)}));
  const mx = k => Math.max(...rows.map(k));
  const mAC = mx(x => x.r.ac.v), mHP = mx(x => x.r.hp), mW = mx(x => x.r.saves.wis.v), mC = mx(x => x.r.saves.con.v), mI = mx(x => x.r.init), mD = mx(x => x.r.best(t).exp);
  const td = (v, m, txt) => `<td class="n${v===m?' top':''}">${txt}</td>`;
  $('m1-title').textContent = `Todas as builds no nível ${L}`;
  $('m1').innerHTML = `<thead><tr><th>Build</th><th class="n">CA</th><th class="n">PV</th><th class="n">SAB</th><th class="n">CON</th><th class="n">Inic.</th><th class="n">Resiste a Strahd</th><th class="n">Dano esperado</th><th>Melhor turno</th><th>Marco do nível</th><th>Comparar em</th></tr></thead><tbody>` +
    rows.map(({b, r}) => { const bst = r.best(t), ft = r.B.feats.find(f => f[0]===L);
      return `<tr><th scope="row">${esc(r.B.n)}<span class="tag t-${r.B.tag.replace(/\s/g,'')}">${esc(r.B.tag)}</span></th>` +
        td(r.ac.v, mAC, r.ac.v) + td(r.hp, mHP, r.hp) + td(r.saves.wis.v, mW, sgn(r.saves.wis.v)) + td(r.saves.con.v, mC, sgn(r.saves.con.v)) + td(r.init, mI, sgn(r.init)) +
        `<td class="n">${pct(r.strahd.p)}</td>` + td(bst.exp, mD, num(bst.exp)) + `<td>${esc(bst.n)}</td><td class="wide">${esc(ft?ft[1]:'')}</td>` +
        `<td class="go">${ROMAN.map((x, i) => `<button type="button" data-load="${b}" data-col="${i}" aria-label="Pôr ${esc(r.B.n)} na carta ${x}">${x}</button>`).join('')}</td></tr>`; }).join('') + `</tbody>`;

  const grid = BUILD_ORDER.map(b => { const s = Object.assign(defaultsFor(b), {hf:'lucky'}); return {b, v:Array.from({length:10}, (_, k) => compute(s, k+1, state.ac).best(t).exp)}; });
  const gmax = Math.max(...grid.flatMap(g => g.v));
  $('m2').innerHTML = `<thead><tr><th>Build</th>` + Array.from({length:10}, (_, k) => `<th class="n${k+1===L?' cur':''}${k+1===5||k+1===6?' win':''}">${k+1}</th>`).join('') + `</tr></thead><tbody>` +
    grid.map(g => `<tr><th scope="row">${esc(BUILDS[g.b].n)}</th>` + g.v.map((v, k) => `<td class="n${k+1===L?' cur':''}${k+1===5||k+1===6?' win':''}" style="${tint(v, gmax)}">${num(v)}</td>`).join('') + `</tr>`).join('') + `</tbody>`;
  $('m-cap').textContent = `Escolhas padrão em todas: humano, antecedente sugerido, primeira variante e aumento de atributo nos níveis de talento. Dano esperado contra CA ${state.ac}, um alvo, rotinas até "${TIERS[t]}".`;
}

function renderResults(){
  const rs = state.cols.map(c => compute(c, state.L, state.ac));
  const tops = {};
  [['ac', r => r.ac.v], ['hp', r => r.hp], ['init', r => r.init]].forEach(([k, f]) => { const vs = rs.map(f), m = Math.max(...vs); tops[k] = vs.filter(v => v===m).length===1 ? vs.indexOf(m) : -1; });
  rs.forEach((r, i) => { $('res'+i).innerHTML = results(i, r, tops); });
  matrices();
  document.querySelectorAll('#levels button').forEach(b => b.setAttribute('aria-pressed', String(+b.dataset.l===state.L)));
  $('lv-now').textContent = state.L;
  const ph = PHASES.find(p => state.L>=p[0] && state.L<=p[1]);
  $('lv-phase').textContent = ph[2] + (state.L===5||state.L===6 ? ' · janela que você quer forte' : state.L>=7 ? ' · final da campanha' : '');
  persist();
}
function renderControls(i, focusId){ $('ctl'+i).innerHTML = controls(i); if (focusId && $(focusId)) $(focusId).focus(); }

function start(saved){
  const s = sane(saved && saved.cols ? saved : (() => { try { return JSON.parse(localStorage.getItem(KEY)); } catch(e){ return null; } })());
  if (s) state = s;
  $('levels').innerHTML = Array.from({length:10}, (_, k) => `<button type="button" data-l="${k+1}" class="${k+1===5||k+1===6?'win':''}" aria-pressed="false">${k+1}</button>`).join('');
  $('phases').innerHTML = PHASES.map(p => `<span style="grid-column:${p[0]} / ${p[1]+1}">${esc(p[2])}</span>`).join('');
  $('cols').innerHTML = [0,1,2].map(i => `<article class="card" aria-label="Carta ${ROMAN[i]}"><div class="ctl" id="ctl${i}"></div><div class="res" id="res${i}"></div></article>`).join('');
  $('ac').value = state.ac; $('tier').value = state.tier;
  [0,1,2].forEach(i => renderControls(i));
  renderResults();

  $('levels').addEventListener('click', e => { const b = e.target.closest('button'); if (!b) return; state.L = +b.dataset.l; renderResults(); });
  $('ac').addEventListener('change', e => { state.ac = +e.target.value; renderResults(); });
  $('tier').addEventListener('change', e => { state.tier = +e.target.value; renderResults(); });
  $('cols').addEventListener('change', e => {
    const el = e.target; if (!el.dataset.k) return;
    const i = +el.dataset.i, k = el.dataset.k, c = state.cols[i];
    if (k==='b'){ state.cols[i] = Object.assign(defaultsFor(el.value), {sp:c.sp, hf:c.hf}); renderControls(i, el.id); }
    else if (k==='g'){ c.g[el.dataset.lv] = el.value; }
    else { c[k] = el.value; if (k==='sp') renderControls(i, el.id); }
    renderResults();
  });
  $('m1').addEventListener('click', e => { const b = e.target.closest('button[data-load]'); if (!b) return;
    const i = +b.dataset.col; state.cols[i] = defaultsFor(b.dataset.load); renderControls(i); renderResults();
    $('res'+i).closest('.card').scrollIntoView({block:'start', behavior:matchMedia('(prefers-reduced-motion: reduce)').matches?'auto':'smooth'}); });
  if (window.claude && window.claude.hot && window.claude.hot.snapshot) window.claude.hot.snapshot(() => state);
}
if (window.claude && window.claude.hot && window.claude.hot.ready) window.claude.hot.ready(start);
else start((window.claude && window.claude.hot && window.claude.hot.data) || {});
