/* ================= depth: secrets, Dijla's letters, last words, spells, armour, new beasts, music, weather ================= */
/* per region: the kind of secret, what lies behind it, the boss's last words, and the weather and music of the land */
const DEP={
 duhok:{letter:'فرات، إذا لگيت هالورقة فانت حي. كتبتها بأول ليلة بعد ما استلمت القفل. أدابا يگول الحراس لازم ينسون أهلهم. ما أگدر أنساك.',lx:1235,ly:105,lz:905,wx:'rain',mq:'kurd'},
 muthanna:{letter:'أوروك أقدم من كل شي. بيها لگيت لوح مكتوب عليه: «اللي يفتح البوابة يحمل نار اللي يحبه». خفت من الجملة… ليش أدابا مخلي اللوح مخفي؟',lx:900,ly:0,lz:-150,mq:'hijaz'},
 nineveh:{sec:'wall',tab:'fire',letter:'بالموصل صلّيت بالجامع الكبير وطلبت إن أشوفك. سمعت أدابا يحچي ويه أحد بالليل… بس محد چان يمه.',last:'الوباء… مو أني اللي جبته. أني بس… حرسته.',wx:'rain',mq:'bayat'},
 erbil:{sec:'night',armor:'mail',letter:'القلعة هنا ما نامت من سبعة آلاف سنة. وأني ما نمت من گالولي إنك صرت بعيد. أدابا يبتسم من يسمع اسمك.',last:'القلعة… تبقى. حتى لو طحت أني.',wx:'rain',mq:'kurd'},
 sulay:{sec:'fires',mini:'wolfK',tab:'ward',letter:'بليلة نوروز شعلنا النار على الجبل. حسيت الجمرة اللي بصدري تتحرك، كأنها تعرف إنك قريب.',last:'المطرقة… صارت ثقيلة هواي.',wx:'snow',mq:'kurd'},
 halabja:{sec:'wall',armor:'marsh',letter:'هالوادي شاف حزن ما ينحچي. زرعت رمانة باسمك. إذا رجعت، تلگاها شجرة.',last:'البحيرة… ارجع للبحيرة…',wx:'snow',mq:'saba'},
 kirkuk:{sec:'night',tab:'bolt',letter:'النار الأزلية بكركوك ما تنطفي، ونار أدابا هم ما تنطفي. بس ناره باردة، يا فرات. باردة.',last:'النار… أخيراً تنطفي.',mq:'bayat'},
 saladin:{sec:'night',mini:'ghostK',letter:'صعدت الملوية وشفت بغداد من بعيد. فوگ البوابة چان اكو ضوه مو مال نار. أدابا ما گال شي لما سألته.',last:'القمر… شوفه، شگد بعيد.',wx:'sand',mq:'hijaz'},
 diyala:{sec:'fires',armor:'lamellar',letter:'البرتقال هنا ريحته مثل بيتنا. قشرت وحدة وتذكرت إيدك. اكو شي غلط بأدابا، بس ما أگدر أثبته بعد.',last:'السم… رجع عليّ.',wx:'rain',mq:'rast'},
 anbar:{sec:'wall',mini:'scorpK',tab:'drain',letter:'النواعير تدور حتى لو محد يسمعها. لگيت رسالة بخط أدابا مكتوبة لإرشكيگال. ما فهمت كلها… بس فهمت اسمي.',last:'الرمل… ياخذني.',wx:'sand',mq:'bayat'},
 karbala:{sec:'night',letter:'يم الواحة لگيت ماي بارد بنص الصحرا. هيچ چنت إنت بحياتي. إذا صار بيا شي، لا تصدّق اللي يگولون عليك.',last:'العطش… انتهى.',wx:'sand',mq:'hijaz'},
 babil:{sec:'fires',armor:'royal',letter:'بوابة عشتار زرگة مثل عيونك يوم كنا صغار. هنا عرفت: أدابا يريد يفتح البوابة، مو يحرسها.',last:'التاج… چان ثقيل… ومزيّف.',mq:'rast'},
 najaf:{sec:'night',letter:'بحر النجف يرجع ماي بالليل، بس للي يعرف يشوف. گلت لأدابا إني راح أخبر الحراس. شفت عيونه تغيرت.',last:'الحيرة… ضاعت قبلي.',wx:'sand',mq:'bayat'},
 qadis:{sec:'wall',letter:'الشلب ما طلع هالسنة. الأرض عطشانة مثلي. إذا ما رجعتلك، دوّر عليّ يم الماي.',last:'الحقول… عطشانة.',mq:'saba'},
 wasit:{sec:'fires',tab:'ward2',letter:'السدّة بالكوت تمسك الماي حتى ما يغرق كلشي. أني هم چنت سدّة. وأدابا يدفع.',last:'السدّة… انكسرت.',wx:'rain',mq:'rast'},
 maysan:{sec:'night',mini:'crocK',letter:'بالهور تعلمت أمشي بالمشحوف. بالليل الهور يحچي. حچالي إن الليلة الجاية هي الأخيرة.',last:'الهور… يرجع ياخذ ولده.',wx:'mist',mq:'saba'},
 dhiqar:{sec:'wall',armor:'zig',letter:'من فوگ الزقورة شفت أول مدينة بالدنيا. إذا متت، احرگ الرسائل… لا. خليها. خلّيه يعرف شگد حبيته.',last:'أور… ما راح تنسى.',wx:'mist',mq:'hijaz'},
 basra:{sec:'night',letter:'هذي آخر رسالة. قلادتي عند حارس الشط، بيها مفتاحي. أدابا جاي هسه. إذا لگيت الجمرة بصدرك… هي أني. سامحني.',last:'القلادة… ارجعها إلها.',wx:'rain',mq:'hijaz'},
 baghdad:{letter:'',last:'علّمتكم كلكم… ونسيتوا أهم درس: إن النار تحرگ اللي يمسكها.',wx:'rain',mq:'hijaz'}};
const GUARD_WORDS=[
 ['…انت؟ لا… انت مو اللي قتلها.','الجمرة اللي بصدرك جمرتها. محد ينطي جمرته لقاتله.','خذ المفتاح. روح.'],
 ['أدابا گلنا انت خنتها، وصدّقنا لأنه معلمنا.','بس أدابا چان ينزل للبوابة بالليالي… وحده.'],
 ['دجلة چانت تكتبلك. أدابا حرگ الرسائل اللي لگاها.','بس مو كلها. دوّر عليها، بأماكن ما يوصلها هو.'],
 ['روح لبغداد يا فرات.','اللي ينتظرك هناك مو عدوّك الوحيد… هو العدو اللي علّمنا كلنا.']];
const SECHINT={wall:'وعلى طريق الحارس اكو صخرة مو صخرة. اضربها وتشوف.',night:'واكو درب ما يبين إلا بالليل. غيّر الوقت من الخريطة وارجع.',fires:'وبالبلدة ثلاث مشاعل: شعلها من مغيب الشمس لمطلعها، وينفتح درب كان مسدود.'};
/* ---------- secrets inside each generated region ---------- */
let SEC=null,LETTERS=[];
function depthRegion(g,D,b1,b2){
  const d=DEP[g.id]||{};LETTERS=[];SEC=null;REGC.wx=d.wx||null;REGC.mq=d.mq||'bayat';
  if(!d.sec)return;
  // a barrier on the road to the second branch: an illusory rock, a path that only exists at night, or a gate opened by fire
  const a=D[4],t=.42,x=lerp(a[0],b2[0],t),z=lerp(a[1],b2[1],t),y=lerp(a[3],b2[3],t),ax=Math.abs(b2[0]-a[0])>Math.abs(b2[1]-a[1]);
  const col=d.sec==='night'?'#3a3446':d.sec==='wall'?(REGC.dry?'#a89070':'#7a7468'):'#4a4038';
  SEC=ax?blk(x-16,z-200,x+16,z+200,y-10,y+190,col,'sec'):blk(x-200,z-16,x+200,z+16,y-10,y+190,col,'sec');SEC.st=d.sec;SEC.ax2=ax;
  if(d.sec==='fires'){const c=D[1];BRZ=[{id:'a',x:c[0]-300,y:c[3],z:c[1]+230},{id:'b',x:c[0],y:c[3],z:c[1]+300},{id:'c',x:c[0]+300,y:c[3],z:c[1]+230}];
    for(const q of BRZ){q.y=floorAt(q.x,q.z,q.y+40);blk(q.x-8,q.z-8,q.x+8,q.z+8,q.y,q.y+40,'','brz');}REGC.want=['a','b','c'];}
  // the hint joins the local's lines
  const nl=DLG[g.npc.id];if(nl){nl[0].splice(nl[0].length-1,0,{t:SECHINT[d.sec],note:g.npc.name.split('،')[0]+': '+SECHINT[d.sec]});nl[1].push(SECHINT[d.sec]);}
  // what waits behind it
  const px=b2[0],pz=b2[1],py=b2[3];
  ITEMS.push({k:'letter',x:px-40,y:py,z:pz+20,id:g.id});
  if(d.tab)ITEMS.push({k:'tab',t:d.tab,x:px+40,y:py,z:pz-10});
  if(d.armor)ITEMS.push({k:'armor',t:d.armor,x:px+40,y:py,z:pz-10});
  if(d.mini)SPAWN.push([d.mini,px+60,pz+60,py]);
  for(const it of ITEMS)it.y=floorAt(it.x,it.z,it.y+30);
  LABELS.push({x:px,z:pz+170,t:'؟'});
}
function depthFixed(id){const d=DEP[id];LETTERS=[];SEC=null;if(REGC){REGC.wx=d&&d.wx||null;REGC.mq=d&&d.mq;}
  if(d&&d.letter&&d.lx!=null){const it={k:'letter',x:d.lx,y:d.ly,z:d.lz,id};it.y=floorAt(it.x,it.z,it.y+30);ITEMS.push(it);}}
function secOpen(){return !SEC||SEC.off;}
function upSecret(){if(!SEC)return;const open=world.sec||(SEC.st==='night'&&(stats.tod%3)===2);if(SEC.off!==open){SEC.off=open;if(open&&SEC.st==='night'&&!world.secSeen){world.secSeen=true;say('بضوه الگمر… انفتح درب','#c8c8ff',2.6);}}}
function hitSecret(a){if(!SEC||SEC.off||SEC.st!=='wall')return;const cx=(SEC.x0+SEC.x1)/2,cz=(SEC.z0+SEC.z1)/2,
  nx=clamp(P.x+Math.cos(P.a)*40,SEC.x0,SEC.x1),nz=clamp(P.z+Math.sin(P.a)*40,SEC.z0,SEC.z1);
  if(Math.hypot(nx-(P.x+Math.cos(P.a)*40),nz-(P.z+Math.sin(P.a)*40))<a.reach*.8){world.sec=true;SEC.off=true;puff(cx,SEC.b,cz,16,16,120);snd('land');shake=8;say('الصخرة… طلعت وهم','#e8c66a',2.6);save();}}
function brzWant(){return (REGC&&REGC.want)||['e','w','c'];}
/* ---------- Dijla's letters ---------- */
function readLetter(id){const d=DEP[id];if(!d||!d.letter)return;stats.letters=stats.letters||{};const first=!stats.letters[id];stats.letters[id]=1;
  dlg={n:{name:'رسالة من دجلة',id:'letter'},lines:[d.letter,first?'('+Object.keys(stats.letters).length+' من '+LETTER_N+' رسالة)':''].filter(Boolean),i:0};paused=true;showDlg();save();}
const LETTER_N=Object.values(DEP).filter(d=>d.letter).length;
/* ---------- last words: every boss, and the guardians tell the truth one piece at a time ---------- */
function lastWords(){const d=DEP[REG]||{},L=[];
  if(REG==='duhok')L.push('العاصفة… خفّت. انت… ريحتك ريحة نار أعرفها.');
  else if(REG==='muthanna')L.push('رسول… العالم السفلي… ما يموت. يرجع… لسيدته.');
  else if(d.last)L.push(d.last);
  if(winKey==='مفتاح الحارس'){const k=Math.min(4,stats.keys||1)-1;for(const t of GUARD_WORDS[k])L.push(t);}
  if(REG==='basra')L.push('القلادة… بيها مفتاحها. وبيها شي ثاني… ريحتها.');
  return L;}
/* ---------- spells from clay tablets ---------- */
const TAB={fire:{n:'لوح النار',c:16,d:'كرة نار ترميها على العدو'},bolt:{n:'لوح البرق',c:24,d:'صاعقة تنزل على العدو المقفول عليه'},ward:{n:'لوح الدرع',c:20,d:'يقلل الضرر عليك للنص ٨ ثواني'},
  drain:{n:'لوح الامتصاص',c:18,d:'يضرب كل اللي حواليك ويرجعلك صحة'},ward2:{n:'لوح الماي',c:14,d:'يرجعلك صحة بالتدريج ١٠ ثواني'}};
const maxFp=()=>60+stats.end*3;
let pfx=[];
function cast(){const k=stats.spell;if(!k||!TAB[k])return false;const T_=TAB[k];if((P.fp||0)<T_.c){say('ما عندك روح كافية','#9ab8ff',1.4);return true;}
  P.fp-=T_.c;if(lock)P.a=Math.atan2(lock.z-P.z,lock.x-P.x);else autoFace();setP('cast');P.spellK=k;snd('draw');return true;}
function spellFire(){const k=P.spellK,m=dmgMul()*(1+.08*(wlv('tab')-1));
  if(k==='fire'){const t=lock&&!lock.dead?lock:nearest(420);let dx=Math.cos(P.a),dz=Math.sin(P.a),dy=0;
    if(t){const tx=t.x-P.x,tz=t.z-P.z,ty=t.y+t.j+t.T.h*.5-(P.y+40),l=Math.hypot(tx,ty,tz)||1;dx=tx/l;dz=tz/l;dy=ty/l;}
    parrows.push({x:P.x+dx*16,y:P.y+42,z:P.z+dz*16,vx:dx*560,vy:dy*560,vz:dz*560,l:1.4,d:Math.round(42*m),fire:1});snd('gust');}
  else if(k==='bolt'){const t=lock&&!lock.dead?lock:nearest(460);if(!t){say('ماكو هدف','#9ab8ff',1.2);return;}pfx.push({k:'bolt',x:t.x,y:t.y,z:t.z,t:0,e:t,d:Math.round(60*m)});}
  else if(k==='ward'){P.ward=8;snd('parry');}
  else if(k==='ward2'){P.regen=10;snd('heal');}
  else if(k==='drain'){let got=0;for(const e of enemies){if(e.dead||Math.hypot(e.x-P.x,e.z-P.z)>e.T.r+95||Math.abs(e.y-P.y)>60)continue;const d=Math.round(26*m);hurtE(e,d,false);got+=d;}
    P.hp=Math.min(maxHp(),P.hp+got*.5);burst(P.x,P.y+30,P.z,'#b060ff',20,160,P.y);snd('hit');}
}
function upSpells(dt){
  if(stats.tabs&&Object.keys(stats.tabs).length){P.fp=Math.min(maxFp(),(P.fp==null?maxFp():P.fp)+2.2*dt);}
  if(P.ward>0)P.ward-=dt;if(P.regen>0){P.regen-=dt;P.hp=Math.min(maxHp(),P.hp+maxHp()*.025*dt);}
  for(let i=pfx.length-1;i>=0;i--){const f=pfx[i];f.t+=dt;if(f.k==='bolt'){if(f.t>.35&&!f.hit){f.hit=true;snd('thunder');shake=5;if(!f.e.dead){hurtE(f.e,f.d,true);f.x=f.e.x;f.z=f.e.z;f.y=f.e.y;}burst(f.x,f.y+10,f.z,'#bfe4ff',16,200,f.y);}if(f.t>.6)pfx.splice(i,1);}}
}
function drawSpellFx(){
  for(const f of pfx){if(f.k==='bolt'){if(f.hit)box(10,600,10,'#eaf6ff',f.x,f.y+300,f.z,1,.9);else cyl(60,.2,60,'#9ad8ff',f.e.x,f.e.y+1,f.e.z,1,.3);}}
  if(P.ward>0)sph(44,62,44,'#f0d080',P.x,P.y+30,P.z,1,.12+.04*Math.sin(time*6));
  if(P.regen>0)sph(40,58,40,'#80c8ff',P.x,P.y+30,P.z,1,.08);
  if(P.state==='cast'&&P.t<.4){const c=P.spellK==='fire'?'#ff8a30':P.spellK==='bolt'?'#bfe4ff':P.spellK==='drain'?'#b060ff':'#f0d080';sph(9,9,9,c,P.x+Math.cos(P.a)*18,P.y+48,P.z+Math.sin(P.a)*18,1,.9);}
}
/* ---------- armour ---------- */
const ARM={robe:{n:'ثوب الرحالة',def:1,roll:0,pal:{}},
  mail:{n:'درع الزرد',def:.82,roll:4,pal:{vest:'#6a6e78',cloth:'#9aa0aa',bisht:'#2a2c34',pants:'#5a5e66'}},
  marsh:{n:'ثوب القصب',def:.95,roll:-6,pal:{vest:'#6a6a3a',cloth:'#c8c090',bisht:'#3a3a22',pants:'#8a8458'}},
  lamellar:{n:'الدرع السومري',def:.72,roll:7,pal:{vest:'#8a6a3a',cloth:'#b89060',bisht:'#3a2a18',pants:'#6a5030',red:'#3a5a8a'}},
  royal:{n:'عباءة بابل',def:.88,roll:0,emb:1.25,pal:{vest:'#1a2a5a',cloth:'#d8c890',bisht:'#14204a',pants:'#3a3a5a',red:'#e0c050'}},
  zig:{n:'درع أور',def:.65,roll:9,pal:{vest:'#7a5a2a',cloth:'#a88850',bisht:'#4a2a14',pants:'#5a4024',red:'#c8a040',gold:'#f0d070'}}};
function armDef(){return (ARM[stats.arm]||ARM.robe).def;}
let HC0=null;
function dressHero(){if(!HC0)HC0=Object.assign({},HC);Object.assign(HC,HC0,(ARM[stats.arm]||ARM.robe).pal);}
/* ---------- new beasts ---------- */
function drawScorp(e){if(Math.hypot(e.x-P.x,e.z-P.z)>DR)return;const fall=mobFx(e,1.1);if(fall<0)return;const S=e.T.sc||1,st=e.state,c=e.T.sc>1?'#3a1a14':'#6a4a2a',d=e.T.sc>1?'#1a0a08':'#3a2414',wk=e.walk*2;
  push();T(e.x,e.y,e.z);RY(-e.a);if(fall)RX(fall);Sc(S,S,S);
  sph(30,10,20,c,0,10,0);for(let q=0;q<4;q++)sph(9,7,18-q*2,d,-14-q*7,10,0);
  for(const sd of[-1,1])for(let q=0;q<4;q++){push();T(8-q*7,8,sd*9);RX(sd*(.9+Math.sin(wk+q)*.25));RZ(.2);sph(2.4,14,2.4,d,0,-7,0);pop();}
  for(const sd of[-1,1]){push();T(16,10,sd*9);RY(sd*-.4+(st==='wind'?sd*.4:0));sph(14,4,4,c,6,0,0);sph(10,7,6,d,15,0,sd*2);pop();}
  const sw=st==='wind'?Math.min(1,e.t/e.T.wind)*.8:st==='strike'?-.6:0;
  push();T(-38,12,0);for(let q=0;q<5;q++){RZ(.55+sw*.25);T(0,7,0);sph(5.6-q*.6,8,5.6-q*.6,q%2?c:d,0,0,0);}con(2.4,8,2.4,'#e0d8a0',0,6,0,st==='wind'?.6:0);pop();
  for(const sd of[-1,1])sph(1.4,1.4,1.4,'#ff3020',18,14,sd*3,1);pop();gFlash=false;gAlpha=1;}
function drawCroc(e){if(Math.hypot(e.x-P.x,e.z-P.z)>DR)return;const fall=mobFx(e,1.1);if(fall<0)return;const S=e.T.sc||1,st=e.state,c=e.T.sc>1?'#2a3a24':'#4a5a34',d='#2a3420',wk=e.walk*1.6;
  const open=st==='wind'?Math.min(1,e.t/e.T.wind)*.7:st==='strike'?.1:.05;
  push();T(e.x,e.y,e.z);RY(-e.a);if(fall)RX(fall);Sc(S,S,S);
  sph(54,12,22,c,0,9,0);for(let q=0;q<7;q++)con(2.4,4,2.4,d,20-q*7,15,0);
  push();T(-26,9,0);RY(Math.sin(time*2+e.hx)*.25);sph(46,8,10,c,-20,0,0);sph(20,5,5,d,-44,0,0);pop();
  push();T(26,10,0);RZ(open*.5);sph(30,5,13,c,14,1,0);pop();push();T(26,8,0);RZ(-open*.5);sph(28,3.6,12,'#6a7a4a',13,-1,0);pop();
  for(const sd of[-1,1]){sph(3,3,3,'#e0c040',30,15,sd*7,1);for(const fx of[14,-14]){push();T(fx,6,sd*12);RX(sd*(.9+Math.sin(wk+fx)*.3));sph(3.6,10,3.6,d,0,-4,0);pop();}}
  pop();gFlash=false;gAlpha=1;}
function drawGhost(e){if(Math.hypot(e.x-P.x,e.z-P.z)>DR)return;const fall=mobFx(e,1.4);if(fall<0)return;const S=e.T.sc||1,st=e.state,al=e.T.sc>1?.7:.55,c=e.T.sc>1?'#c8b8ff':'#d0d8e8';
  gAlpha=Math.min(gAlpha,al+(st==='wind'?.3:0));const fl=6+Math.sin(time*2+e.hx)*4;
  push();T(e.x,e.y+fl,e.z);RY(-e.a);Sc(S,S,S);
  for(let q=0;q<6;q++){const a=q/6*6.283+time;box(1.6,26+Math.sin(time*3+q)*4,6,c,Math.cos(a)*7,8,Math.sin(a)*8,0,al*.8);}
  sph(13,22,15,c,0,26,0,.3);sph(8,9,8,c,1,44,0,.3);for(const sd of[-1,1])sph(1.8,2.4,1.8,'#0a0a20',7,45,sd*2.6,1);
  const arm=st==='wind'?2.4:st==='strike'?1.2:.5;for(const sd of[-1,1]){push();T(0,36,sd*10);RZ(arm);sph(3,18,3,c,0,-8,0,.3);pop();}
  pop();gFlash=false;gAlpha=1;}
/* ---------- music: a few notes of a maqam over a quiet drone, different in each land ---------- */
const MQ={hijaz:[0,1,4,5,7,8,10,12],bayat:[0,1.5,3,5,7,8,10,12],saba:[0,1.5,3,4,7,8,10,12],rast:[0,2,3.5,5,7,9,10.5,12],kurd:[0,1,3,5,7,8,10,12]};
let RM=null,musicOn=true;
function regionMusic(on){
  if(!AUC)return;try{if(RM){clearTimeout(RM.to);RM.g.gain.linearRampToValueAtTime(0,AUC.currentTime+1.5);const m=RM;setTimeout(()=>{try{m.o.forEach(o=>o.stop());}catch(_){}} ,1700);RM=null;}
    if(!on||!musicOn||cine)return;const sc=MQ[(REGC&&REGC.mq)||'bayat'],base=REG==='baghdad'?110:REG==='duhok'?130.8:123.5;
    const g=AUC.createGain(),t=AUC.currentTime;g.gain.setValueAtTime(0,t);g.gain.linearRampToValueAtTime(.05,t+4);g.connect(MG);
    const o=[];for(const f of[base/2,base*.75]){const os=AUC.createOscillator(),fl=AUC.createBiquadFilter();os.type='sawtooth';os.frequency.value=f;fl.type='lowpass';fl.frequency.value=260;os.connect(fl);fl.connect(g);os.start();o.push(os);}
    RM={g,o,to:0};let last=0;
    const pluck=()=>{if(!RM)return;if(!paused&&!world.locked){let i=clamp(last+Math.round((Math.random()-.5)*3),0,sc.length-1);last=i;const f=base*Math.pow(2,sc[i]/12)*(Math.random()<.25?2:1),n=1+(Math.random()<.3?1:0);
        for(let k=0;k<n;k++){const s=AUC.currentTime+k*.18,os=AUC.createOscillator(),gg=AUC.createGain(),fl=AUC.createBiquadFilter();os.type='triangle';os.frequency.setValueAtTime(f*1.01,s);os.frequency.exponentialRampToValueAtTime(f,s+.06);
          fl.type='lowpass';fl.frequency.value=1800;gg.gain.setValueAtTime(.0001,s);gg.gain.linearRampToValueAtTime(.11,s+.01);gg.gain.exponentialRampToValueAtTime(.0005,s+1.6);os.connect(fl);fl.connect(gg);gg.connect(MG);os.start(s);os.stop(s+1.7);}}
      RM.to=setTimeout(pluck,900+Math.random()*2400);};
    RM.to=setTimeout(pluck,2500);}catch(_){}
}
/* ---------- weather ---------- */
let WTH={on:false,t:0,k:null,drops:[]};
function upWeather(dt){const k=REGC&&REGC.wx;if(!k){WTH.on=false;return;}WTH.t-=dt;if(WTH.t<=0){WTH.on=!WTH.on;WTH.t=WTH.on?70+Math.random()*60:40+Math.random()*50;WTH.k=k;}
  if(WTH.on&&WTH.drops.length<(k==='sand'?90:k==='mist'?40:160)){for(let i=0;i<8;i++)WTH.drops.push({x:VE[0]+(Math.random()-.5)*900,y:VE[1]+Math.random()*400,z:VE[2]+(Math.random()-.5)*900,s:Math.random()});}
  if(!WTH.on&&WTH.drops.length)WTH.drops.length=Math.max(0,WTH.drops.length-6);
  const fallV=k==='rain'?-620:k==='snow'?-55:k==='sand'?-20:-4,side=k==='sand'?320:k==='snow'?20:40;
  for(const d of WTH.drops){d.y+=fallV*dt;d.x+=side*dt;if(d.y<VE[1]-250||Math.abs(d.x-VE[0])>520||Math.abs(d.z-VE[2])>520){d.x=VE[0]+(Math.random()-.5)*900;d.z=VE[2]+(Math.random()-.5)*900;d.y=VE[1]+150+Math.random()*250;}}}
function drawWeather(){if(!WTH.drops.length)return;const k=WTH.k;
  for(const d of WTH.drops){if(k==='rain')box(.6,22,.6,'#b8c8d8',d.x,d.y,d.z,1,.35);else if(k==='snow')sph(2.2,2.2,2.2,'#ffffff',d.x,d.y,d.z,1,.8);
    else if(k==='sand')box(10,1.6,1.6,'#d8c090',d.x,d.y,d.z,1,.35);else sph(60,18,60,'#d8e0d8',d.x,d.y-200,d.z,1,.05);}}
function wxFog(fr,fg,fb){if(!WTH.on||!WTH.k)return [fr,fg,fb,1];const k=WTH.k,a=Math.min(1,WTH.drops.length/80);
  if(k==='sand')return [lerp(fr,.72,a*.7),lerp(fg,.6,a*.7),lerp(fb,.42,a*.7),1-.45*a];
  if(k==='rain')return [lerp(fr,.45,a*.5),lerp(fg,.48,a*.5),lerp(fb,.52,a*.5),1-.2*a];
  if(k==='mist')return [lerp(fr,.75,a*.6),lerp(fg,.78,a*.6),lerp(fb,.76,a*.6),1-.35*a];
  return [lerp(fr,.85,a*.4),lerp(fg,.87,a*.4),lerp(fb,.9,a*.4),1-.2*a];}
