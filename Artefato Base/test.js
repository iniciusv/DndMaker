const {compute,BUILDS,BUILD_ORDER,SP,BG,GF,HUMAN_FEATS}=require('./logic.js');
const show=(sel,L)=>{const r=compute(sel,L,15);console.log(`\n${r.B.n} L${L} [${sel.v||''}] ${sel.sp||'humano'}/${r.bg.n} | sc`,Object.values(r.sc).join(','),`| CA ${r.ac.v} PV ${r.hp} init ${r.init} dv ${r.dark.v} | saves`,Object.entries(r.saves).map(([a,s])=>a+(s.v>=0?'+':'')+s.v+(s.prof?'*':'')).join(' '),`| strahd ${(r.strahd.p*100).toFixed(0)}% conc ${r.conc?(r.conc.p*100).toFixed(0):'-'}%`);
 r.routines.forEach(x=>console.log(`   t${x.tier} ${x.n}: ${x.f} = ${x.raw} | exp ${x.exp.toFixed(1)} (hit ${x.hit} dc ${x.dc})`));};
show({b:'fwl',v:'dueling',bg:'haunted'},3);show({b:'fwl',v:'dueling'},5);show({b:'fwl',v:'dueling'},6);show({b:'fwl',v:'defense',sp:'gnomo'},6);
show({b:'valor',v:'arco'},5);show({b:'valor',v:'arco'},6);show({b:'valor',v:'melee',hf:'mi_druid'},6);show({b:'valor',v:'melee'},3);
show({b:'pal'},3);show({b:'pal'},5);show({b:'pal'},6);
show({b:'twi',v:'toll'},3);show({b:'twi',v:'toll'},5);
show({b:'gloom'},3);show({b:'gloom'},5);show({b:'gloom',sp:'elfo'},7);
show({b:'lock'},3);show({b:'lock'},5);
show({b:'swords',v:'melee'},6);show({b:'swords',v:'arco'},6);show({b:'swords',v:'melee',hf:'mi_druid'},6);
show({b:'bm',v:'besta'},5);show({b:'bm',v:'besta',g:{4:'xbow'}},5);show({b:'bm',v:'rep'},5);
show({b:'fval',v:'defense'},7);show({b:'shadow'},5);
// smoke: every combination must not throw / NaN
let n=0;for(const b of BUILD_ORDER)for(const sp in SP)for(const bg in BG)for(let L=1;L<=10;L++){const B=BUILDS[b];for(const v of (B.variants||[{id:null}])){for(const g of Object.keys(GF)){const sel={b,sp,bg,v:v.id,hf:'mi_druid',g:{}};B.asiLv.forEach(l=>sel.g[l]=g);const r=compute(sel,L,15);n++;if(![r.hp,r.ac.v,r.init,...r.routines.map(x=>x.exp),...Object.values(r.saves).map(s=>s.v),...Object.values(r.sc)].every(Number.isFinite)||Object.values(r.sc).some(x=>x>20))throw new Error('bad '+JSON.stringify(sel)+L);}}}
console.log('\nsmoke ok',n);
