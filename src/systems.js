/* ================= characters you talk to ================= */
const DLG={
  khidr:[[
    'هاا… گعدت؟ ثلاث ليالي وانت نايم يم الموقد، والنار ما انطفت. غريبة.',
    'لا تباوعلي هيچ، ما أعرفك. بس الجمرة اللي بصدرك… شفت مثلها مرة وحدة، من زمان.',
    'البلاد مو مثل ما چانت. اللي يموت يگوم، والذيابة نزلت من الجبل.',
    'إذا تريد تفهم شصار، انزل للجنوب. امشِ ويا الماي، الماي يعرف الدرب.',
    {t:'وقبل ما تعبر الجسر، احفظها: أهل زاخو يعلگون خرزة زرگة على أبوابهم ضد العين. بابين بس بقت عليهم خرزة، والورقة الأولى ورا الباب اللي يسمع خرير الماي.',note:'مام خدر: بابين عليهم خرزة زرگة. الورقة الأولى ورا الباب اللي يسمع خرير الماي.'},
    'بعد ما أگدر أگلك أكثر. اعبر جسر دلال… وانتبه لروحك، فرات.',
    'شنو؟ ما گلت شي. روح.'],
   ['خرزة زرگة على الباب، والباب يسمع الماي. افتح عينك زين.','امشِ ويا الماي، الماي يعرف الدرب.']],
  shirin:[[
    'اوگف! … لا، انت حي. آسفة، صار أسبوع ما شفت بشر يمشي على رجليه وهو بعقله.',
    'اسمي شيرين. چان عندي أربعين راس غنم، بقن أربعة. الذيابة والهايمين أخذوا الباقي.',
    {t:'وحدة منهن ما رجعت: مشت ورا الشگ اللي بالصخر، غرب القرية، وين الصخرة تشبه راس الذيب. اللي يلحگها يلگى أكثر من غنمة.',note:'شيرين: الشگ بالصخر غرب القرية، وين الصخرة تشبه راس الذيب. اللي يلحگ الغنمة يلگى أكثر من غنمة.'},
    {t:'وإذا رايح للقلعة فوگ، بابها حجر وما ينفتح بإيد. اسأل الدرويش الأعمى، گاعد يم الدرب قبل الباب.',note:'شيرين: باب القلعة حجر وما ينفتح بإيد. اسأل الدرويش الأعمى يم الدرب.'},
    'مرة مرت من هنا قبل الخراب، طويلة، بإيدها مفتاح، وسألت عن واحد يشبهك. گالت اسمها دجلة… لا، يمكن حلمت بيها.'],
   ['الشگ بالصخر، غرب القرية، يم راس الذيب.','الدرويش گاعد يم باب القلعة. لا تخاف منه، أعمى بس يشوف أكثر منا.']],
  derv:[[
    'خطوتك أعرفها… لا. خطوة واحد يشبهك. ذاك چان يمشي وهو يعرف وين رايح.',
    'تريد القلعة؟ فوگها إرّا، فارس العاصفة. يگولون چان حارس من حراس البوابة… وهسه ما يخلي أحد يعبر. وبابها ما يفتحه حديد ولا چتف.',
    {t:'اسمع: النار تمشي مثل الشمس، من مطلعها لمغيبها، وآخرها اللي ما يشوف الشمس أبد.',note:'الدرويش: النار تمشي مثل الشمس، من مطلعها لمغيبها، وآخرها اللي ما يشوف الشمس أبد.'},
    'ثلاث مشاعل. إذا غلطت الترتيب ينطفن كلهن وتبدي من جديد.',
    'الأعمى گلك اللي عنده، والباقي عليك.'],
   ['النار تمشي مثل الشمس: من مطلعها لمغيبها، وآخرها اللي ما يشوف الشمس أبد.'],
   ['سمعت الحجر يتحرك. الدرب مفتوح.','إرّا يعلّگ سيفه فوگ راسه ويستناك تتدحرج قبل وقتك. اصبر عليه. ومن يغضب، العاصفة تنزل ويّاه: لا توگف بمكانك.']]
};
let dlg=null;
function nearNPC(){for(let i=0;i<NPCS.length;i++){const n=NPCS[i];if(Math.abs(P.y-n.y)<30&&Math.hypot(P.x-n.x,P.z-n.z)<62)return i;}return -1;}
function addNote(t){if(!stats.notes)stats.notes=[];if(!stats.notes.includes(t))stats.notes.push(t);}
function talk(i){const n=NPCS[i],sets=DLG[n.id];let s=world.npc[n.id]?1:0;if(n.id==='derv'&&world.mgate)s=2;
  dlg={n,lines:sets[Math.min(s,sets.length-1)],i:0};paused=true;n.a=Math.atan2(P.z-n.z,P.x-n.x);showDlg();}
function showDlg(){const l=dlg.lines[dlg.i],line=typeof l==='string'?l:l.t;$('dN').textContent=dlg.n.name;$('dT').textContent=line;if(l.note)addNote(l.note);$('dlg').classList.add('on');voice(dlg.n.id,line);}
function nextDlg(){if(!dlg)return;voiceStop();dlg.i++;if(dlg.i>=dlg.lines.length&&dlg.done){const cb=dlg.done;dlg=null;$('dlg').classList.remove('on');cb();return;}
  if(dlg.i>=dlg.lines.length){world.npc[dlg.n.id]=1;dlg=null;$('dlg').classList.remove('on');paused=false;save();}else showDlg();}
/* ================= the three fire bowls ================= */
function nearBrz(){for(let i=0;i<BRZ.length;i++){const b=BRZ[i];if(Math.abs(P.y-b.y)<30&&Math.hypot(P.x-b.x,P.z-b.z)<52&&!brzLit(b))return i;}return -1;}
function brzLit(b){return world.mgate||world.brz.includes(b.id);}
function lightBrz(i){const b=BRZ[i];if(brzLit(b))return;world.brz.push(b.id);snd('rest');burst(b.x,b.y+44,b.z,'#ffb040',14,90,b.y);
  const want=['e','w','c'];
  const want2=brzWant();for(let k=0;k<world.brz.length;k++)if(world.brz[k]!==want2[k]){world.brz=[];say('انطفن كلهن… الترتيب غلط','#c8a888',3.2);snd('doorShut');save();return;}
  if(world.brz.length===3&&REG!=='duhok'){world.sec=true;if(SEC)SEC.off=true;say('من بعيد… درب مسدود انفتح','#e8c66a',4);snd('boss');shake=6;}
  else if(world.brz.length===3){world.mgate=true;if(MGATE)MGATE.off=true;say('من بعيد… صوت حجر يتحرك','#e8c66a',4);snd('boss');shake=6;}
  save();}
function drawBrz(){for(const b of BRZ){if(Math.hypot(b.x-P.x,b.z-P.z)>DR)continue;
  cyl(4,34,4,'#3a3038',b.x,b.y+17,b.z);cyl(16,3,16,'#3a3038',b.x,b.y+1.5,b.z);fru(20,8,20,'#55454c',b.x,b.y+37,b.z);
  if(brzLit(b)){const f=Math.sin(time*11+b.x)*2;con(13,22+f*2,13,'#f08a2a',b.x+Math.sin(time*7)*.6,b.y+52+f,b.z,1);con(7,13-f,7,'#ffe09a',b.x,b.y+48,b.z,1);}}}
/* ================= stones in the air ================= */
let shots=[];
function upShots(dt){for(let i=shots.length-1;i>=0;i--){const s=shots[i];s.x+=s.vx*dt;s.y+=s.vy*dt;s.z+=s.vz*dt;s.vy-=120*dt;s.l-=dt;
  if(s.l<=0||solidPt(s.x,s.y,s.z)){burst(s.x,s.y,s.z,'#8a8478',5,60);shots.splice(i,1);continue;}
  if(Math.hypot(s.x-P.x,s.z-P.z)<15&&Math.abs(s.y-(P.y+28))<36){if(hurtP(s.d,s.x-s.vx*.1,s.z-s.vz*.1,null)){burst(s.x,s.y,s.z,'#8a8478',5,60);shots.splice(i,1);}}}}
function sling(e){const dx=P.x-e.x,dz=P.z-e.z,d=Math.hypot(dx,dz)||1,t=d/340;
  shots.push({x:e.x+Math.cos(e.a)*10,y:e.y+36,z:e.z+Math.sin(e.a)*10,vx:dx/d*340,vz:dz/d*340,vy:(P.y+28-(e.y+36))/t+60*t,l:2.4,d:e.T.dmg});snd('sling');}
/* ================= Anzu, the storm bird ================= */
function inArena(x,z,m){const dx=x-AC.x,dz=z-AC.z,d=Math.hypot(dx,dz),R=AC.r-m;return d>R?[AC.x+dx/d*R,AC.z+dz/d*R]:[x,z];}
function upAnzu(b,dt){
  const dx=P.x-b.x,dz=P.z-b.z,ad=Math.hypot(dx,dz),ang=Math.atan2(dz,dx),ph2=b.hp<b.T.hp*.5,sp=ph2?1.3:1,alive=P.state!=='dead';
  b.cd-=dt;let mvs=0;
  if(!b.p2&&ph2&&b.state!=='stag'&&b.state!=='dive'&&b.state!=='fly'){b.p2=true;setE(b,'trans');snd('screech');snd('thunder');}
  switch(b.state){
    case 'sleep':if(world.locked){setE(b,'intro');snd('screech');}break;
    case 'trans':b.a=turn(b.a,ang,2*dt);b.j=Math.min(40,b.j+60*dt);shake=Math.max(shake,3);
      if(b.t>.8&&b.n<1){b.n=1;for(let k=0;k<5;k++){const a=k/5*6.283;waves.push({k:'b',x:b.x+Math.cos(a)*120,z:b.z+Math.sin(a)*120,y:b.y,t:0,d:.5});}}
      if(b.t>2.2){b.n=0;b.j=0;setE(b,'chase');b.cd=.2;say('العاصفة تشتد','#9ad8ff',2.2);}break;
    case 'intro':b.a=turn(b.a,ang,4*dt);if(b.t>1.5){setE(b,'chase');b.cd=.5;}break;
    case 'chase':
      if(!alive)break;
      b.a=turn(b.a,ang,6*dt);if(ad>84)mvs=ph2?108:82;
      if(b.cd<=0){const r=Math.random();
        if(ph2&&r<.22){setE(b,'storm');b.n=0;snd('screech');}
        else if(ad<108)setE(b,r<.6?'clawW':r<.82?'gustW':'fly');
        else if(r<.5)setE(b,'gustW');else setE(b,'fly');}
      break;
    case 'clawW':if(b.t<.3)b.hold=.2+Math.random()*(ph2?.7:.45);b.a=turn(b.a,ang,(b.t<.4?3:1)*dt);if(b.t>=.5/sp+b.hold){setE(b,'claw');snd('swingH');}break;
    case 'claw':mvs=115;if(!b.hit&&hitP(b,92,1.2,24))b.hit=true;if(b.t>=.2){if(ph2&&!b.cc){b.cc=1;setE(b,'clawW');b.hold=0;}else{b.cc=0;bRec(b,.7);}}break;
    case 'gustW':b.a=turn(b.a,ang,4*dt);if(b.t>=.75/sp){setE(b,'gust');snd('gust');}break;
    case 'gust':
      if(!b.hit&&ad<280&&adiff(ang,b.a)<.85&&Math.abs(P.y-b.y)<60){
        if(hurtP(12,b.x,b.z,null)){b.hit=true;if(P.state==='hurt')P.kbs=430;else moveEnt(P,P.x+Math.cos(ang)*46,P.z+Math.sin(ang)*46,P.r);}}
      if(Math.random()<dt*60)parts.push({x:b.x+Math.cos(b.a)*30,y:b.y+30+Math.random()*40,z:b.z+Math.sin(b.a)*30,vx:Math.cos(b.a+(Math.random()-.5)*1.4)*380,vz:Math.sin(b.a+(Math.random()-.5)*1.4)*380,vy:20,l:.5,m:.5,col:'#d8d2c4',s:2.4,g:40,f:-999});
      if(b.t>=.45)bRec(b,.85);break;
    case 'fly':b.j=Math.min(130,b.j+240*dt);b.a=turn(b.a,ang,5*dt);b.x=lerp(b.x,P.x,dt*1.5);b.z=lerp(b.z,P.z,dt*1.5);
      if(b.t>=1.15/sp){setE(b,'dive');const q=inArena(P.x,P.z,40);b.tx=q[0];b.tz=q[1];b.sx=b.x;b.sz=b.z;snd('screech');}break;
    case 'dive':{const k=Math.min(1,b.t/.55);b.x=lerp(b.sx,b.tx,k);b.z=lerp(b.sz,b.tz,k);b.j=130*(1-k*k);
      if(k>=1){b.j=0;shake=12;snd('boss');burst(b.x,b.y+4,b.z,'#8a7a70',18,160,b.y);
        if(Math.hypot(P.x-b.x,P.z-b.z)<90&&P.y-b.y<18)hurtP(32,b.x,b.z);bRec(b,1.2);}
      break;}
    case 'storm':b.a=turn(b.a,ang,3*dt);
      if(b.n<6&&b.t>.4+b.n*.5){b.n++;const q=inArena(P.x+(Math.random()-.5)*40,P.z+(Math.random()-.5)*40,20);waves.push({k:'b',x:q[0],z:q[1],y:b.y,t:0,d:.85});}
      if(b.t>=3.6)bRec(b,.7);break;
    case 'rec':if(b.t>=b.recT){setE(b,'chase');b.cd=ph2?.35:.75;}break;
    case 'stag':b.j=Math.max(0,b.j-300*dt);if(b.t>=2.3){setE(b,'chase');b.cd=.3;}break;
  }
  if(mvs){const nx=b.x+Math.cos(b.a)*mvs*dt,nz=b.z+Math.sin(b.a)*mvs*dt;if(Math.hypot(P.x-nx,P.z-nz)>=P.r+b.T.r-1||P.state==='roll'){b.x=nx;b.z=nz;b.walk+=mvs*dt*.09;}}
  const q=inArena(b.x,b.z,34);b.x=q[0];b.z=q[1];b.y=AC.y;
}
/* ================= opening film ================= */
let cine=null,todO=null;
/* the opening film starts under Baghdad, where it all happened, then goes north to Zakho where he sleeps */
const CINE=[
  {d:7,reg:'baghdad',tod:0,mk:()=>{const G=REGC.gate,D=REGC.D;return [[D[3][0],700,D[3][1]+900,G.x,60,G.z],[D[4][0],560,D[4][1]+760,G.x,60,G.z]];},t:'من زمان، تحت بغداد، ورا باب الطلسم، اكو بوابة تفصل دنيانا عن العالم السفلي… عالم الموتى.'},
  {d:7,reg:'baghdad',tod:0,mk:()=>{const G=REGC.gate,b=MGATE,w=b&&b.ax2===false;return w?[[G.x-120,90,G.z+420,G.x,100,G.z],[G.x+60,70,G.z+330,G.x,110,G.z]]:[[G.x-420,90,G.z-120,G.x,100,G.z],[G.x-330,70,G.z+60,G.x,110,G.z]];},t:'خمس حراس قفلوها بخمس مفاتيح، وكل واحد أخذ مفتاحه لطرف من أطراف البلاد.'},
  {d:6,reg:'baghdad',tod:1,mk:()=>{const b=boss,G=REGC.gate;if(b){b.x=lerp(G.x,AC.x,.45);b.z=lerp(G.z,AC.z,.45);b.y=floorAt(b.x,b.z,40);b.a=Math.atan2(G.z-b.z,G.x-b.x)+PI;b.state='chase';}
     const x=b?b.x:AC.x,z=b?b.z:AC.z,a=b?b.a:0;return [[x+Math.cos(a)*300+Math.sin(a)*120,70,z+Math.sin(a)*300-Math.cos(a)*120,x,120,z],[x+Math.cos(a)*230+Math.sin(a)*60,60,z+Math.sin(a)*230-Math.cos(a)*60,x,130,z]];},t:'بليلة وحدة… انفتحت. واللي يعرف منو فتحها… ساكت.',fx:1},
  {d:5,reg:'baghdad',tod:1,mk:()=>{const b=boss,G=REGC.gate;if(!b)return [[G.x-90,145,G.z+130,G.x,125,G.z],[G.x-45,120,G.z+85,G.x,125,G.z]];const h=b.T.h||180;return [[b.x+Math.cos(b.a+PI)*260,b.y+h+55,b.z+Math.sin(b.a+PI)*260,b.x,b.y+h*.76,b.z],[b.x+Math.cos(b.a+PI)*185,b.y+h*.38,b.z+Math.sin(b.a+PI)*185,b.x,b.y+h*.68,b.z]];},t:'أدابا… آخر زعماء بغداد. لا يحرس الباب وحده؛ يحرس الحقيقة المدفونة تحته.',fx:1},
  {d:6,reg:'duhok',a:[640,78,-590,652,26,-300],b:[650,54,-480,652,26,-300],tod:1,t:'من ذيچ الليلة انطفت النار بكل البلاد، والموتى گاموا من قبورهم.'},
  {d:7,reg:'duhok',a:[700,230,-690,475,10,-915],b:[590,95,-800,475,10,-915],tod:0,t:'وبأبعد نقطة بالشمال، بزاخو، رجّال نايم يم آخر موقد… وما يتذكر شي.'},
  {d:4.5,reg:'duhok',a:[452,62,-978,475,8,-915],b:[458,50,-966,475,8,-915],tod:0,t:'« فرات… گوم. »',v:1}];
function cineRegion(id){   // swap the world under the film without touching the save
  if(REG===id)return;const f=$('cFade');if(f){f.classList.remove('go');void f.offsetWidth;f.classList.add('go');}
  loadRegion(id);applyWorld();setEnvironment(REG);spawnAll();DR=4000;
  if(id==='duhok'){setP('sleep');P.x=475;P.z=-915;P.y=floorAt(475,-915,20);P.ry=P.y;P.a=2.82;P.inv=99;}
  else{P.x=-1800;P.z=-1800;P.y=0;P.ry=0;}ugS=0;}
function startCine(){cine={i:0,t:0};DR=4000;paused=true;root.classList.add('cine');setP('sleep');P.inv=99;cineShot();music(true);}
function startBossFinisher(e){
  if(!e||!e.boss||e.dead||cine||e.finisherUsed||e.armor!==0||e.weakT<=0)return false;
  e.finisherUsed=true;e.finishing=true;e.state='finisher';e.t=0;
  const weapon=stats.wpn||'shield';
  const weaponStyle=weapon==='dual'?{key:'F2',hand:2,wind:.52,act:.24,rec:.68,impact:'#bce8ff',line:'نصلان… وضربة واحدة.'}:
    weapon==='glaive'?{key:'F3',hand:0,wind:.56,act:.26,rec:.66,impact:'#ff9b58',line:'من مدى الموت… لا مهرب.'}:
    weapon==='axe'?{key:'F4',hand:0,wind:.6,act:.24,rec:.64,impact:'#ff7048',line:'سأحطم عرشك!' }:
    weapon==='spear'?{key:'F5',hand:0,wind:.5,act:.22,rec:.7,impact:'#9de8e1',line:'اخترق درعك… وانتهى أمرك.'}:
    {key:'F1',hand:0,wind:.55,act:.24,rec:.68,impact:'#f2d18a',line:'بحدّ هذا السيف… انتهى عهدك.'};
  const name=e.T.name||'الزعيم';
  const finisherRegion={
    baghdad:{title:'خاتمة باب الطلسم',line:'أدابا… لن تحميك مفاتيح بغداد بعد الآن.',color:'#e8c66a',rise:1.18},
    duhok:{title:'خاتمة فارس العاصفة',line:'إرّا… عاصفتك انتهت، والخابور عاد هادئاً.',color:'#8ad9ef',rise:1.34},
    muthanna:{title:'خاتمة رسول العالم السفلي',line:'نمتار… ارجع إلى ظلام العالم السفلي.',color:'#b78cff',rise:.88},
    nineveh:{title:'خاتمة ظلال الحدباء',line:'نرگال… لن تبقى ظلال الحدباء تحت سلطانك.',color:'#9c83d8',rise:1.12},
    erbil:{title:'خاتمة أسوار القلعة',line:'حارس القلعة… هذه الأسوار تحررت منك.',color:'#d6bd8a',rise:1.1},
    sulay:{title:'خاتمة حداد الجبل',line:'كاوة… انطفأت نار مطرقتك.',color:'#d18a65',rise:1.06},
    halabja:{title:'خاتمة سيدة البحيرة',line:'زريبار… البحيرة لن تحمل لعنتك بعد اليوم.',color:'#77d6d0',rise:1.08},
    kirkuk:{title:'خاتمة نار بابا گرگر',line:'بابا گرگر… خمدت نارك.',color:'#f08b45',rise:1.2},
    saladin:{title:'خاتمة حارس القمر',line:'سين… غاب قمرك عن هذه الأرض.',color:'#a9b8e8',rise:1.14},
    diyala:{title:'خاتمة سيد الأفاعي',line:'تيشپاك… انتهى سمّك.',color:'#a6c66a',rise:1.05},
    anbar:{title:'خاتمة فارس الرمل',line:'فارس الرمل… ابتلعتك الصحراء.',color:'#d5ad72',rise:1.12},
    karbala:{title:'خاتمة حارس الواحة',line:'عين التمر… عادت الواحة لأهلها.',color:'#d8b66b',rise:1.02},
    babil:{title:'خاتمة بابل',line:'المتوّج الكاذب… سقط تاجك.',color:'#d9a34c',rise:1.18},
    najaf:{title:'خاتمة فارس الحيرة',line:'فارس الحيرة… انتهى حرس الرمال.',color:'#c9b18a',rise:1.1},
    qadis:{title:'خاتمة حارس إيسن',line:'حارس إيسن… ابتلعك تراب مدينتك.',color:'#c6a06a',rise:1.06},
    wasit:{title:'خاتمة حارس السدّة',line:'حارس السدّة… جرى الماء من جديد.',color:'#72c9d6',rise:1.04},
    maysan:{title:'خاتمة صياد الهور',line:'صياد الهور… الهور لفظك إلى اليابسة.',color:'#6fc5a1',rise:1.02},
    dhiqar:{title:'خاتمة ملك أور',line:'أور-نمّو… انتهى عهد الزقورة.',color:'#d7ad63',rise:1.2},
    basra:{title:'خاتمة حارس الشط',line:'السندباد الغريق… انتهت رحلتك.',color:'#65b9d2',rise:1.08}
  }[REG]||{title:'خاتمة حارس المنطقة',line:name+'… انتهى عهدك.',color:'#e6b66b',rise:1};
  const bossLine=finisherRegion.line;
  P.a=Math.atan2(e.z-P.z,e.x-P.x);P.finisherKey=weaponStyle.key;P.finisherWeapon=weapon;P.atk={...CRIT,key:weaponStyle.key,hand:weaponStyle.hand,wind:weaponStyle.wind,act:weaponStyle.act,rec:weaponStyle.rec};P.combo=0;setP('atk');P.t=0;P.inv=99;
  const fx=Math.cos(e.a),fz=Math.sin(e.a),x=e.x,z=e.z,y=e.y;
  const close=weapon==='glaive'||weapon==='spear'?150:weapon==='axe'?112:weapon==='dual'?88:102;
  const shots=[
    {d:1.35,a:[x+fx*285-fz*150,y+185,z+fz*285+fx*150,x,y+78,z],b:[x+fx*220-fz*115,y+145,z+fz*220+fx*115,x,y+85,z],t:finisherRegion.title+'…',v:1,fx:true},
    {d:1.25,a:[x+fx*(close+55)-fz*34,y+105,z+fz*(close+55)+fx*34,x,y+e.T.h*.65*finisherRegion.rise,z],b:[x+fx*close-fz*22,y+78*finisherRegion.rise,z+fz*close+fx*22,x,y+e.T.h*.7*finisherRegion.rise,z],t:bossLine},
    {d:1.35,a:[P.x+fx*close-fz*42,P.y+105,P.z+fz*close+fx*42,x,y+e.T.h*.55,z],b:[P.x+fx*(close-24)-fz*20,P.y+76,P.z+fz*(close-24)+fx*20,x,y+e.T.h*.6,z],t:weaponStyle.line,fx:true,impact:true,col:weaponStyle.impact,bossCol:finisherRegion.color}
  ];
  cine={i:0,t:0,boss:e,finisher:true,finisherWeapon:weapon,shots};paused=true;root.classList.add('cine');cineShot();bossMusic(true);snd('boss');return true;
}
function miniBossFilm(e,m){
  if(cine||!e||!m)return false;
  const fx=Math.cos(e.a),fz=Math.sin(e.a),x=e.x,z=e.z,y=e.y;
  const shots=[
    {d:2.0,a:[x+fx*310-fz*170,y+185,z+fz*310+fx*170,x,y+42,z],b:[x+fx*245-fz*125,y+150,z+fz*245+fx*125,x,y+55,z],t:m.n+'… ساحة الحارس.',v:1,fx:true},
    {d:1.8,a:[x+fx*145-fz*36,y+92,z+fz*145+fx*36,x,y+e.T.h*.55,z],b:[x+fx*105-fz*22,y+76,z+fz*105+fx*22,x,y+e.T.h*.58,z],t:m.line}
  ];
  cine={i:0,t:0,boss:e,mini:true,shots};paused=true;root.classList.add('cine');cineShot();bossMusic(true,m.k);return true;
}
function cineShot(){const s=(cine.shots||CINE)[cine.i];if(s.reg)cineRegion(s.reg);if(s.mk){const q=s.mk();s.a=q[0];s.b=q[1];}todO=s.tod==null?null:s.tod;const c=$('cT');c.textContent=s.t;c.classList.toggle('v',!!s.v);c.classList.remove('in');void c.offsetWidth;c.classList.add('in');if(s.fx){shake=9;snd('boss');}if(cine.finisher&&s.impact&&cine.boss){const b=cine.boss;burst(b.x,b.y+b.T.h*.68,b.z,s.col||'#f2d18a',42,260,b.y);if(s.bossCol)burst(b.x,b.y+b.T.h*.68,b.z,s.bossCol,34,190,b.y);shake=16;}voice('narrator',s.t);}
function upCine(dt){cine.t+=dt;if(cine.finisher){const a=cine.boss&&cine.boss.finisherPose;P.t=Math.min(a||1.45,P.t+dt);if(cine.boss)cine.boss.t+=dt;}const L=cine.shots||CINE,s=L[cine.i];
  if(!cine.finisher&&cine.boss&&cine.boss.k==='boss'&&cine.i===0){const b=cine.boss;b.t+=dt;b.state='trans';if(b.t>1.6)b.state='intro';}
  if(cine.t>=s.d){cine.i++;cine.t=0;if(cine.i>=L.length){endCine();return;}cineShot();}}
function endCine(){voiceStop();if(cine&&cine.finisher){
  const b=cine.boss;cine=null;todO=null;root.classList.remove('cine');paused=false;P.inv=0;P.finisherKey=null;P.finisherWeapon=null;setP('idle');
  cam.x=P.x;cam.y=P.y+58;cam.z=P.z;cam.d=165;waves=[];bossMusic(false);snd('rest');b.finishing=false;b.armor=0;b.weakT=0;
  hurtE(b,b.hp,true,true);$('cT').textContent='';save();return;
}if(!(cine&&cine.mini))setTimeout(()=>regionMusic(true),900);if(cine&&!cine.boss&&!cine.boat&&REG!=='duhok')cineRegion('duhok');if(cine&&cine.boss){const b=cine.boss,isMini=!!cine.mini;cine=null;todO=null;root.classList.remove('cine');paused=false;setE(b,'chase');b.cd=.6;b.hit=false;if(isMini){const m=MINI_GUARDS[REG];bossMusic(true,m&&m.k);if(m)voice('boss',m.line);}$('cT').textContent='';return;}
  cine=null;todO=null;setQ();root.classList.remove('cine');stats.intro=true;stats.tod=0;setTod();paused=false;music(false);
  P.inv=3;setP('wake');snd('hurt');cam.yaw=1.25;cam.pitch=.5;cam.pe=.5;cam.x=P.x;cam.y=P.y+30;cam.z=P.z;cam.d=150;save();}
function cineCam(e3,tg){const s=(cine.shots||CINE)[cine.i],k=Math.min(1,cine.t/s.d),q=k*k*(3-2*k);for(let i=0;i<3;i++){e3[i]=lerp(s.a[i],s.b[i],q);tg[i]=lerp(s.a[i+3],s.b[i+3],q);}if(HG){let top=hv(e3[0],e3[2]);for(let k=0;k<8;k++)top=Math.max(top,hv(e3[0]+Math.cos(k*.785)*110,e3[2]+Math.sin(k*.785)*110));e3[1]=Math.max(e3[1],top+45);}}   // never inside the mountain
