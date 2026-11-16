/* ================= the rest of Iraq: each governorate built from a short description ================= */
/* land types: colours, how high the land around the paths rises, what grows */
const TERC={
  mount:{g:[.36,.50,.25],d:[.56,.49,.36],rock:[.45,.42,.39],snow:455,rim:430,rimA:250,rough:1,grass:26,veg:'pine',vegN:300,path:[0,30,60,95,130,170,210,250]},
  hills:{g:[.44,.50,.27],d:[.62,.55,.38],rock:[.56,.50,.42],snow:9999,rim:240,rimA:120,rough:.6,grass:20,veg:'tree',vegN:160,path:[0,8,20,32,44,54,64,72]},
  plain:{g:[.40,.52,.25],d:[.60,.55,.38],rock:[.62,.56,.45],snow:9999,rim:110,rimA:60,rough:.35,grass:20,veg:'palm',vegN:220,path:[0,0,4,8,8,12,14,16]},
  desert:{g:[.74,.63,.43],d:[.80,.70,.50],rock:[.70,.58,.42],snow:9999,rim:150,rimA:90,rough:.5,grass:3,veg:'dtree',vegN:60,dry:1,path:[0,6,14,20,24,30,36,40]},
  marsh:{g:[.38,.50,.27],d:[.52,.50,.34],rock:[.42,.40,.32],snow:9999,rim:-30,rimA:0,rough:.25,grass:30,veg:'reed',vegN:260,water:1,path:[4,4,4,4,4,4,4,4]},
  city:{g:[.56,.52,.42],d:[.64,.58,.46],rock:[.62,.56,.46],snow:9999,rim:60,rimA:30,rough:0,grass:5,veg:'palm',vegN:70,dry:1,city:1,path:[0,0,0,0,0,0,0,0]}};
let REGC=null,RIVZ=null,LABELS=[],REGB=null;
const OLD={robe:'#5a4a3a',vest:'#3c3428',sash:'#8a2a2a',skin:'#c49a72',beard:'#e8e4dc',hat:'turban',hatC:'#d8d0c0',staff:true};
const WOM={robe:'#2a2a3a',vest:'#3a2a3a',sash:'#8a6a2a',skin:'#c99a74',hat:'scarf',hatC:'#1a1a22',staff:false};
const YNG={robe:'#5a5040',vest:'#7a5a3a',sash:'#2a5a7a',skin:'#c08a64',beard:'#2a1c14',hat:'turban',hatC:'#e8e0d0',staff:true};
const FSH={robe:'#4a5a5a',vest:'#3a4a48',sash:'#c8a050',skin:'#a87a58',beard:'#bcb8b0',hat:'hood',hatC:'#3a4a48',staff:true};
/* {R} in a line becomes the riddle that points to the second map piece */
const GOV=[
 {id:'duhok',n:'زاخو · دهوك',lon:42.69,lat:37.14},
 {id:'nineveh',n:'الموصل · نينوى',lon:43.13,lat:36.34,ter:'hills',city:'الموصل القديمة',riv:1,lm:{k:'lamassu',n:'الثور المجنح'},
  npc:{id:'nin',name:'أبو يونس، صاحب الدكان',look:OLD,l:['هالدكان صار عمره مية سنة، وأول مرة يدخله واحد بعينه جمرة.','الحارس اللي بساحة الحدباء يگول انت خاين، وانت تبين تايه مو خاين.','{R}','دير بالك من صولجانه، ثقيل، بس بطيء بأول ضربة.']},
  boss:{n:'نرگال، سيد الوباء',w:'mace',el:'fire',hp:1000,look:{a:'#5a3a2a',a2:'#3a2418',t:'#b08a3a',c:'#4a1a12',sk:'#9a6a4a',bd:'#1a1210',helm:'horn',e:'#ff7a30'}}},
 {id:'erbil',n:'أربيل',lon:44.01,lat:36.19,ter:'hills',city:'تحت القلعة',lm:{k:'obelisk',n:'المسلة القديمة'},
  npc:{id:'erb',name:'كاكه رزگار، بياع العسل',look:YNG,l:['تفضل كاكه، عسل جبل، يرجّع الروح.','القلعة فوگنا أقدم من كل شي تعرفه. ساكنيها راحوا وبقى بيها حارس واحد.','{R}','الحارس يلعب بالرمح مثل ما الراعي يلعب بعصاه. لا تبعد عنه، الرمح يحب المسافة.']},
  boss:{n:'أربا-إيلو، حارس القلعة',w:'spear',el:'storm',hp:1050,look:{a:'#6a6458',a2:'#3e3a34',t:'#c8a050',c:'#2a3a4a',sk:'#b07a58',bd:'#2a1a12',helm:'crown',e:'#bfe4ff'}}},
 {id:'sulay',n:'السليمانية',lon:45.43,lat:35.56,ter:'mount',city:'سوق السليمانية',lm:{k:'fire',n:'نار نوروز'},
  npc:{id:'sul',name:'داده گلاله',look:WOM,l:['بنيّتي بعمرك صارت، وهالجبل بعده يحچي ويّاي.','بليلة نوروز چانت النار تشتعل على كل قمة. هسه وحدة بس بقت.','{R}','الحداد اللي فوگ ما ينام. مطرقته تنزل وبعدها النار.']},
  boss:{n:'كاوة، حداد الجبل',w:'mace',el:'fire',hp:1100,look:{a:'#4a4440',a2:'#2a2624',t:'#c86a2a',c:'#5a2a1a',sk:'#a87050',bd:'#3a1a10',helm:'open',e:'#ffa040'}}},
 {id:'halabja',n:'حلبجة',lon:45.99,lat:35.18,ter:'mount',city:'بساتين الرمان',lm:{k:'tree',n:'شجرة الرمان الوحيدة'},
  npc:{id:'hal',name:'شيرزاد، الفلاح',look:YNG,l:['هالأرض شافت حزن ما ينحچي. احنا نزرع رمان حتى نتذكر بلون أحلى.','الحارسة اللي على البحيرة چانت تحمي هالوادي، وهسه تحميه من الكل.','{R}','رمحها طويل وضربتها مثل الموج: وحدة ورا الثانية.']},
  boss:{n:'زريبار، سيدة البحيرة',w:'spear',el:'water',hp:1100,fem:1,look:{a:'#3a5a6a',a2:'#1e3240',t:'#d8d0c0',c:'#1a2a3a',sk:'#c49a78',helm:'hood',e:'#8ad8ff'}}},
 {id:'kirkuk',n:'كركوك',lon:44.39,lat:35.47,ter:'hills',city:'القلعة والسوق',lm:{k:'fire',n:'النار الأزلية'},
  npc:{id:'kir',name:'الحاج تحسين',look:OLD,l:['هنا تلگى تركماني وكردي وعربي يشربون چاي بنفس الگهوة. الجمرة اللي بصدرك ما تفرّق بيناتنا.','بابا گرگر، النار اللي ما تنطفي من آلاف السنين، هسه صارت تاكل كل من يقرب.','{R}','الحارس يحمل فأس ونار. كل ما يغضب، الگاع تحترگ تحتك.']},
  boss:{n:'بابا گرگر، حارس النار الأزلية',w:'axe',el:'fire',hp:1150,look:{a:'#3a2a24',a2:'#1a1210',t:'#e06a20',c:'#2a0a08',sk:'#7a4a34',bd:'#1a0a08',helm:'mask',e:'#ff6020'}}},
 {id:'saladin',n:'سامراء · صلاح الدين',lon:43.87,lat:34.2,ter:'plain',city:'سامراء',riv:1,lm:{k:'spiral',n:'الملوية'},
  npc:{id:'sal',name:'أم حسين',look:WOM,l:['يمه، اگعد اشرب ماي. وجهك تعبان مثل وجه ابني من رجع من الحرب.','الملوية تشوفها من بعيد، بس اللي يصعدها يشوف أبعد.','{R}','الحارس ما يستعجل. يمشي حواليك مثل الذيب، ومن تغلط يضرب.']},
  boss:{n:'سين، حارس القمر',w:'sword',el:'shadow',hp:1150,look:{a:'#4a4a5a',a2:'#24242e',t:'#c8c8d8',c:'#1a1a2a',sk:'#a87a5a',bd:'#2a2a2a',helm:'crown',e:'#c8c8ff'}}},
 {id:'diyala',n:'بعقوبة · ديالى',lon:44.66,lat:33.75,ter:'plain',city:'بساتين البرتقال',riv:1,lm:{k:'tree',n:'البرتقالة العتيگة'},
  npc:{id:'diy',name:'أبو عباس، صاحب البستان',look:OLD,l:['برتقال ديالى ما يشبهه شي بالدنيا. خذ وحدة، حلال عليك.','بالمستنقعات ورا البساتين صارت الأفاعي تمشي على رجلين.','{R}','سيد الأفاعي يلدغ وينسحب. اصبر لحد ما يخلص سمّه.']},
  boss:{n:'تيشپاك، سيد الأفاعي',w:'spear',el:'poison',hp:1150,look:{a:'#3a4a2a',a2:'#1e2a14',t:'#a8c050',c:'#1a2a10',sk:'#8a7a4a',helm:'mask',e:'#a8ff60'}}},
 {id:'anbar',n:'الرمادي · الأنبار',lon:43.3,lat:33.42,ter:'desert',city:'هيت',riv:1,lm:{k:'wheel',n:'الناعور'},
  npc:{id:'anb',name:'الشيخ مطلگ',look:OLD,l:['هلا بالضيف. الصحرا ما ترحم، بس أهلها يرحمون.','النواعير چانت تغني ليل نهار. سكتت من نزل فارس الرمل.','{R}','فارس الرمل يندفع مثل العاصفة. لا توگف گدامه، اطلع على جنبه.']},
  boss:{n:'فارس الرمل، حارس النواعير',w:'axe',el:'sand',hp:1200,look:{a:'#8a7050',a2:'#4a3a28',t:'#d8b878',c:'#6a5030',sk:'#9a6a48',bd:'#2a1a10',helm:'hood',e:'#ffd080'}}},
 {id:'karbala',n:'كربلاء',lon:44.02,lat:32.6,ter:'desert',city:'أطراف المدينة',lm:{k:'palm',n:'النخلة اليابسة'},
  npc:{id:'kar',name:'الحاج جواد، السقّا',look:OLD,l:['اشرب يا ولدي، الماي هنا عزيز.','ورا النخيل اكو واحة، وبالواحة حارس ما شاف أحد من سنين.','{R}','ضربته سريعة ومستقيمة، مثل خط الشمس على الرمل.']},
  boss:{n:'عين التمر، حارس الواحة',w:'sword',el:'sand',hp:1200,look:{a:'#6a5a3a',a2:'#3a3020',t:'#e0c070',c:'#4a3a24',sk:'#9a6a48',bd:'#1a120c',helm:'open',e:'#ffe0a0'}}},
 {id:'babil',n:'الحلة · بابل',lon:44.43,lat:32.48,ter:'plain',city:'الحلة',riv:1,lm:{k:'ishtar',n:'بوابة عشتار'},
  npc:{id:'bab',name:'الأستاذ صباح، معلم التاريخ',look:YNG,l:['تعرف شنو أول شي كتبوه البابليين؟ القوانين. حتى الملك چان يخاف منها.','الحارس هنا لابس تاج ملك، بس ما چان ملك يوم.','{R}','يرمي البرق مثل الآلهة اللي يقلّدها. لا توگف بنفس المكان.']},
  boss:{n:'المتوّج الكاذب، حارس بابل',w:'spear',el:'storm',hp:1250,look:{a:'#2a4a8a',a2:'#14244a',t:'#e0c050',c:'#1a2a5a',sk:'#a87a58',bd:'#1a1210',helm:'crown',e:'#bfe4ff'}}},
 {id:'najaf',n:'النجف',lon:44.33,lat:32.0,ter:'desert',city:'أطراف بحر النجف',lm:{k:'ruin',n:'أطلال الحيرة'},
  npc:{id:'naj',name:'أبو علي، صاحب الگهوة',look:OLD,l:['اگعد، الچاي على حسابي. شكلك جاي من بعيد… من زاخو؟ لا تگول!','بحر النجف چان ماي، وهسه رمل يتذكر الماي.','{R}','فارس الحيرة يحارب مثل ملوك المناذرة: سيف بإيد، وكبرياء بالثانية.']},
  boss:{n:'فارس الحيرة',w:'sword',el:'fire',hp:1250,look:{a:'#5a3a3a',a2:'#2a1a1a',t:'#d8a040',c:'#3a1010',sk:'#9a6a48',bd:'#1a0e0a',helm:'crown',e:'#ffb060'}}},
 {id:'qadis',n:'الديوانية · القادسية',lon:44.92,lat:31.99,ter:'plain',city:'الحقول',riv:1,lm:{k:'ruin',n:'تل إيسن'},
  npc:{id:'qad',name:'أم كريم',look:WOM,l:['الشلب هالسنة ما طلع. الماي يمشي بس ما يوصل للحقول.','بالتل القديم، حارس يحرس شي محد يتذكره.','{R}','فأسه ثقيل وسمّه أثقل. لا تخلي الوكت يطول.']},
  boss:{n:'حارس إيسن',w:'axe',el:'poison',hp:1300,look:{a:'#4a5a3a',a2:'#24301c',t:'#b8c070',c:'#2a3a1a',sk:'#8a6a48',bd:'#1a120c',helm:'horn',e:'#b0ff70'}}},
 {id:'wasit',n:'الكوت · واسط',lon:45.82,lat:32.5,ter:'plain',city:'الكوت',riv:1,lm:{k:'obelisk',n:'سدّة الكوت'},
  npc:{id:'was',name:'جاسم، صياد السمچ',look:YNG,l:['دجلة هنا عريض وهادئ، بس لا تنخدع، تحته تيار.','الحارس واگف على السدّة، يحرس الماي من الكل.','{R}','يضرب بموجة ورا موجة. اتنفس بين الموجتين.']},
  boss:{n:'حارس السدّة',w:'mace',el:'water',hp:1300,look:{a:'#3a5a6a',a2:'#1a2a34',t:'#a8d0e0',c:'#1a3a4a',sk:'#9a6a48',bd:'#1a1210',helm:'horn',e:'#80d0ff'}}},
 {id:'maysan',n:'العمارة · ميسان',lon:47.15,lat:31.84,ter:'marsh',city:'المضيف',lm:{k:'mudhif',n:'المضيف الكبير'},
  npc:{id:'may',name:'أبو حيدر، صاحب المشحوف',look:FSH,l:['هلا عيني. الهور يرحب بيك، بس لا تمشي إلا على الدروب اللي تعرفها.','الجاموس هسه خايف، ما يطلع من الماي.','{R}','صياد الهور يرمي الفالة من بعيد ويقرب بسرعة. لا تعطيه ظهرك.']},
  boss:{n:'صياد الهور',w:'spear',el:'water',hp:1350,look:{a:'#4a4a3a',a2:'#24241a',t:'#c8b070',c:'#2a3a3a',sk:'#7a5034',bd:'#1a120c',helm:'hood',e:'#a0e0ff'}}},
 {id:'dhiqar',n:'الناصرية · ذي قار',lon:46.26,lat:31.04,ter:'marsh',city:'أور',lm:{k:'ziggurat',n:'زقورة أور'},
  npc:{id:'dhi',name:'الحجية أم ستار',look:WOM,l:['تعرف ابني، أور أقدم مدينة بالدنيا؟ احنا عشنا هنا قبل ما يصير اسم للدنيا.','الملك اللي بنى الزقورة رجع، بس مو هو. بس شكله.','{R}','فأسه ينزل من فوگ مثل حجار الزقورة. اطلع على جنبه.']},
  boss:{n:'أور-نمّو، ملك الزقورة',w:'axe',el:'storm',hp:1400,look:{a:'#7a5a3a',a2:'#3a2a1a',t:'#e0c070',c:'#4a2a1a',sk:'#9a6a48',bd:'#1a120c',helm:'crown',e:'#ffe0a0'}}},
 {id:'muthanna',n:'أوروك · المثنى',lon:45.28,lat:31.31},
 {id:'basra',n:'البصرة',lon:47.78,lat:30.5,ter:'plain',city:'العشار',riv:1,lm:{k:'boat',n:'القارب القديم'},
  npc:{id:'bas',name:'عمو سلمان، النوخذة',look:FSH,l:['أهلا بيك بالبصرة، يا ابن الشمال. شكلك تعرف دجلة… أقصد النهر، لو المرة؟','هنا يلتقي الفرات ويه دجلة، ومن هنا انمشى أول قارب لبغداد.','{R}','الحارس اللي بالشط لابس قلادة مو إله. يگولون لمرة نوّرت هالمكان يوم.']},
  boss:{n:'السندباد الغريق، حارس الشط',w:'sword',el:'water',hp:1450,look:{a:'#2a4a5a',a2:'#14242e',t:'#e0c060',c:'#0e2230',sk:'#8a6040',bd:'#1a120c',helm:'open',e:'#80e0ff'}}},
 {id:'baghdad',n:'بغداد',lon:44.36,lat:33.31,ter:'city',city:'باب الطلسم',riv:1,lm:{k:'gate',n:'باب الطلسم'},
  npc:{id:'bag',name:'الوراق العجوز',look:OLD,l:['بشارع المتنبي چنا نبيع الكتب، وهسه بس الريح تقلّب صفحاتها.','أدابا… معلم الحراس. هو اللي كتب كل شي، وهو اللي شطب اسمك من كل شي.','{R}','البوابة تحت باب الطلسم. خمس مفاتيح، ولا وحدة أقل.']},
  boss:{n:'أدابا، معلم الحراس',w:'sword',el:'all',hp:2000,look:{a:'#e0d8c8',a2:'#6a6458',t:'#e0c050',c:'#f0e8d8',sk:'#b08460',bd:'#d8d0c0',helm:'crown',e:'#ffffff'}}}];
const GOVM={};for(const g of GOV)GOVM[g.id]=g;
const ORDER=['duhok','nineveh','erbil','sulay','halabja','kirkuk','saladin','diyala','anbar','karbala','babil','najaf','qadis','wasit','maysan','dhiqar','muthanna','basra','baghdad'];
function hashStr(s){let h=2166136261;for(let i=0;i<s.length;i++){h^=s.charCodeAt(i);h=Math.imul(h,16777619);}return h>>>0;}
function rng(seed){let s=seed||1;return ()=>{s^=s<<13;s>>>=0;s^=s>>>17;s^=s<<5;s>>>=0;return s/4294967296;};}
const DIRN=['صوب مطلع الشمس','بين مطلع الشمس والجنوب','صوب الجنوب','بين الجنوب ومغيب الشمس','صوب مغيب الشمس','بين مغيب الشمس والشمال','صوب الشمال','بين الشمال ومطلع الشمس'];
function dirName(dx,dz){const a=Math.atan2(dz,dx);return DIRN[((Math.round(a/(PI/4))%8)+8)%8];}
/* walkable discs joined by corridors; mf decides what the land around them looks like */
function buildLand(D,L,mf){
  const pathH=(x,z)=>{let best=1e9,bh=0;
    for(const d of D){const q=Math.hypot(x-d[0],z-d[1])-d[2];if(q<best){best=q;bh=d[3];}}
    for(const l of L){const a=D[l[0]],b=D[l[1]],vx=b[0]-a[0],vz=b[1]-a[1],len=Math.hypot(vx,vz),t=clamp(((x-a[0])*vx+(z-a[1])*vz)/(len*len),0,1),
      q=Math.hypot(x-a[0]-vx*t,z-a[1]-vz*t)-l[2]/2;
      if(q<best){best=q;const t0=a[2]/len,t1=1-b[2]/len;bh=lerp(a[3],b[3],clamp((t-t0)/Math.max(.01,t1-t0),0,1));}}
    return [bh,Math.max(0,best)];};
  const dist=new Float32Array(HG.length);
  for(let j=J0;j<=J1;j++)for(let i=0;i<=GI;i++){const x=i*C,z=j*C,p=pathH(x,z),o=(j-J0)*(GI+1)+i,s=clamp(p[1]/150,0,1);
    dist[o]=p[1];HG[o]=p[1]<=0?p[0]:lerp(p[0],mf(x,z,p[0]),s*s*(3-2*s));}
  for(let j=J0;j<J1;j++)for(let i=0;i<GI;i++){const o=(j-J0)*(GI+1)+i;if(dist[o]>40&&dist[o+1]>40&&dist[o+GI+1]>40&&dist[o+GI+2]>40)nogo[cidx(i,j)]=1;}
}
function regGen(g){
  const R=rng(hashStr(g.id)),T=TERC[g.ter];REGC=T;RNAME=g.n;LABELS=[];
  HG=new Float32Array((GI+1)*(GJ+1));
  const H=T.path,n=7,D=[];
  for(let k=0;k<n;k++){const x=430+k*(WX-980)/(n-1),z=clamp(180+(k%2?1:-1)*(170+R()*190)+(R()-.5)*120,-420,760);
    D.push([x,z,k===0?240:k===1?380:k===n-1?335:150+R()*80,H[k]]);}
  const b1=[D[2][0]+(R()-.5)*160,D[2][1]-560,175,H[2]],b2=[D[4][0]+(R()-.5)*160,D[4][1]-540,150,H[4]];
  b1[1]=Math.max(b1[1],-860);b2[1]=Math.max(b2[1],-860);D.push(b1,b2);
  const L=[];for(let k=0;k<n-1;k++)L.push([k,k+1,150+R()*30]);L.push([2,n,130],[4,n+1,125]);
  const mf=(x,z,p0)=>T.water?-40:T.city?p0:g.ter==='mount'?Math.max(300+250*vn(x/430,z/430)+120*vn(x/170+9,z/170+3),p0+175)
    :g.ter==='hills'?p0+110+120*vn(x/300+2,z/300+5)+40*vn(x/90,z/90):g.ter==='desert'?p0+70+110*vn(x/260+4,z/260+1)
    :p0+50+60*vn(x/220+7,z/220+3);
  buildLand(D,L,mf);REGC.D=D;
  // a river along the south edge for the lands it runs through: seen, never reached
  RIVZ=null;
  if(g.riv&&!T.water){RIVZ=1335;for(let i=0;i<=GI;i++)for(const j of[14,15])HG[(j-J0)*(GI+1)+i]=-70;for(let i=0;i<GI;i++)for(const j of[13,14,15])nogo[cidx(i,j)]=1;
    WATER=[{x0:-900,x1:WX+900,z0:1240,z1:1440,y:-24}];}
  if(T.water)WATER=[{x0:-1200,x1:WX+1200,z0:WZ0-1200,z1:WZ1+1200,y:-8}];
  const S=g.ter==='desert'||g.ter==='city'||g.ter==='plain'?['#b89a6a','#a88a5a','#c4a878']:['#8c867a','#7e786e','#958e80'];
  // the town: a street through the second disc, houses facing it, one you can enter
  {const [cx,cz,,y]=D[1];
    hhouse(cx-205,cz-185,cx-55,cz-55,'n',cx-130,y,S[0]);houseXZ(cx+45,cz-175,cx+185,cz-55,S[1],null,y,1);
    houseXZ(cx-195,cz+55,cx-60,cz+170,S[2],null,y,-1);houseXZ(cx+50,cz+55,cx+190,cz+175,S[0],null,y,-1);
    LIGHTS.push({x:cx,y:y+60,z:cz,w:1.1});LABELS.push({x:cx,z:cz+250,t:g.city});
    FRAGS=[{x:cx-130,y,z:cz-115,n:'القطعة الأولى'},{x:b1[0],y:b1[3],z:b1[1]-40,n:'القطعة الثانية'}];}
  LABELS.push({x:b1[0],z:b1[1]+130,t:g.lm.n});
  SHR=[{x:D[0][0]-60,y:D[0][3],z:D[0][1],yaw:0},{x:D[3][0],y:D[3][3],z:D[3][1]+40,yaw:0},{x:D[5][0],y:D[5][3],z:D[5][1],yaw:0}];
  for(let i=0;i<SHR.length;i++)LIGHTS.push({x:SHR[i].x,y:SHR[i].y+40,z:SHR[i].z,w:1.8,shr:i});
  const A=D[n-1];AC={x:A[0],z:A[1],r:290,y:A[3]};LABELS.push({x:A[0],z:A[1]+A[2]+30,t:'ساحة الحارس'});
  for(let k=0;k<11;k++){const a=k/11*2*PI+.3,x=AC.x+Math.cos(a)*305,z=AC.z+Math.sin(a)*305;if(Math.abs(wrap(a-PI))<.5)continue;blk(x-9,z-9,x+9,z+9,AC.y,AC.y+50+hash(k+R())*80,T.dry?'#a8946a':'#7a7468','col');}
  // the guide at the first fire, and the riddle for the second piece
  const nl=g.npc,dir=dirName(b1[0]-SHR[1].x,b1[1]-SHR[1].z),rid='القطعة الثانية من خريطتك يم '+g.lm.n+'، '+dir+' من الموقد الثاني.';
  const lines=nl.l.map(t=>t==='{R}'?{t:'وإذا تدوّر على الخريطة: '+rid,note:nl.name.split('،')[0]+': '+rid}:t);
  DLG[nl.id]=[lines,[rid,nl.l[nl.l.length-1]]];
  NPCS=[{id:nl.id,name:nl.name,x:D[0][0]+70,y:D[0][3],z:D[0][1]+70,a:2.4,look:nl.look}];
  for(const q of NPCS)blk(q.x-9,q.z-9,q.x+9,q.z+9,q.y,q.y+54,'','npc');
  FLAGS=[{x:D[1][0]-20,y:D[1][3],z:D[1][1]-40,ry:-PI/2}];for(const f of FLAGS)blk(f.x-4,f.z-4,f.x+4,f.z+4,f.y,f.y+230,'#8a8a92','flag');
  REGC.lm={k:g.lm.k,x:b1[0]+40,y:b1[3],z:b1[1]+20};
  blk(b1[0]+40-28,b1[1]+20-28,b1[0]+40+28,b1[1]+20+28,b1[3],b1[3]+(g.lm.k==='ziggurat'?140:g.lm.k==='spiral'?200:90),'#a8946a','lm');
  // enemies: what lives in this land, harder the further south you go
  const idx=ORDER.indexOf(g.id),pool=g.ter==='mount'?['wolf','husk','sling','archer','brute']:g.ter==='desert'?['husk','scorp','archer','brute','scorp','spear']
    :g.ter==='marsh'?['husk','croc','sling','archer','croc']:g.ter==='city'?['spear','archer','brute','ghost','ghost']:['husk','spear','archer','brute','wolf',g.id==='saladin'?'ghost':'husk'];
  SPAWN=[];const put=(k,d,m)=>{for(let q=0;q<m;q++){const a=R()*6.283,r=d[2]*(.25+R()*.5);SPAWN.push([pool[Math.floor(R()*pool.length)],d[0]+Math.cos(a)*r,d[1]+Math.sin(a)*r,d[3]]);}};
  put(0,D[1],3);put(0,D[2],3);put(0,D[3],2);put(0,D[4],3);put(0,D[5],2);put(0,b1,3);put(0,b2,3);SPAWN.push(['brute',D[4][0],D[4][1],D[4][3]]);
  ITEMS=[{k:'shard',x:b2[0],y:b2[3],z:b2[1]-30,n:2},{k:'shard',x:D[3][0]+80,y:D[3][3],z:D[3][1]-60},{k:'arrows',x:D[2][0]-60,y:D[2][3],z:D[2][1]+40,n:6}];
  if(idx>=8)ITEMS.push({k:'dshard',x:b2[0]+50,y:b2[3],z:b2[1]+30});
  for(const it of ITEMS)it.y=floorAt(it.x,it.z,it.y+30);
  REGS={hs:1+idx*.07,ds:1+idx*.05};depthRegion(g,D,b1,b2);
  // trees, palms, reeds and rocks; solid on the paths, scenery beyond
  mkCW();
  for(let k=0;k<T.vegN;k++){const x=40+R()*(WX-80),z=WZ0+40+R()*(WZ1-WZ0-80),c=cellOf(x,z);if(c<0)continue;if(RIVZ&&Math.abs(z-RIVZ)<160)continue;
    const y=hv(x,z);if(T.water&&y<-6&&T.veg!=='reed')continue;const kind=T.veg==='reed'&&y>-6?'tree':T.veg,h=kind==='palm'?70+R()*30:kind==='pine'?70+R()*45:kind==='reed'?26+R()*14:44+R()*16;
    if(nogo[c]){if(T.city)continue;DECO.push({k:kind,x,y:kind==='reed'?-8:y,z,h});continue;}
    if(R()<.55||blocked(x,z,y,34)||Math.hypot(x-AC.x,z-AC.z)<AC.r+20)continue;
    if(SHR.some(s=>Math.hypot(s.x-x,s.z-z)<120)||FRAGS.some(s=>Math.hypot(s.x-x,s.z-z)<70)||NPCS.some(s=>Math.hypot(s.x-x,s.z-z)<70)||Math.hypot(x-D[1][0],z-D[1][1])<D[1][2])continue;
    if(kind==='reed'){DECO.push({k:'reed',x,y,z,h});continue;}
    blk(x-5,z-5,x+5,z+5,y,y+h,'#4a3626',kind);}
  for(let k=0;k<(T.water?40:110);k++){const x=40+R()*(WX-80),z=WZ0+40+R()*(WZ1-WZ0-80),c=cellOf(x,z);if(c<0||(RIVZ&&Math.abs(z-RIVZ)<160))continue;
    if(!nogo[c]&&(blocked(x,z,hgt(x,z),40)||R()<.6))continue;if(T.city&&nogo[c])continue;DECO.push({k:'rock',x,y:hv(x,z)-3,z,h:8+R()*20});}
  // Baghdad: the city fills everything that is not street, and the gate waits before the last square
  if(T.city){for(let j=J0;j<J1;j++)for(let i=0;i<GI;i++){const c=cidx(i,j);if(!nogo[c]||(RIVZ&&j>=13))continue;
      const edge=[[1,0],[-1,0],[0,1],[0,-1]].some(([a,b])=>{const ii=i+a,jj=j+b;return ii>=0&&ii<GI&&jj>=J0&&jj<J1&&!nogo[cidx(ii,jj)];});
      if(!edge&&hash(i*3.3+j*7.1)<.55)continue;const h=edge?110+hash(i+j*13)*120:80+hash(i*7+j)*60;
      blk(i*C+3,j*C+3,(i+1)*C-3,(j+1)*C-3,0,h,['#b8a07a','#a88c64','#c4ae86','#9a8462'][Math.floor(hash(i*1.7+j*2.9)*4)],'cb');}
    const G=D[n-2],a=Math.atan2(A[1]-G[1],A[0]-G[0]),gx=lerp(G[0],A[0],.55),gz=lerp(G[1],A[1],.55);
    const ax=Math.abs(A[0]-G[0])>Math.abs(A[1]-G[1]);MGATE=ax?blk(gx-14,gz-270,gx+14,gz+270,G[3],G[3]+190,'#4a4038','mgate'):blk(gx-270,gz-14,gx+270,gz+14,G[3],G[3]+190,'#4a4038','mgate');MGATE.ax2=ax;REGC.gate={x:gx,z:gz};}
}
let REGS={hs:1,ds:1};
/* ---------- landmarks ---------- */
function decoLm(){const L=REGC&&REGC.lm;if(!L)return;const {x,y,z}=L,s=(a,b,c,col,px,py,pz,o)=>st(a,col,b[0],b[1],b[2],x+px,y+py,z+pz,o);
  switch(L.k){
    case 'lamassu':sb(80,40,30,'#b8a888',x,y+20,z);sb(60,50,26,'#c8b898',x+6,y+65,z);st('sph','#c8b898',22,26,20,x+40,y+92,z);sb(8,40,14,'#c8b898',x+44,y+118,z);
      for(const q of[-1,1])st('sph','#b8a888',50,10,40,x-10,y+100,z+q*18,{rz:.5});for(const q of[-30,-10,15,35])sc(10,40,10,'#a89878',x+q,y-0,z+(q>0?10:-10));break;
    case 'obelisk':sb(46,10,46,'#8a8070',x,y+5,z);st('fru','#a89878',26,150,26,x,y+85,z);st('con','#a89878',20,22,20,x,y+171,z);break;
    case 'fire':for(let q=0;q<6;q++)st('sph','#3a3430',40,22,40,x+Math.cos(q)*36,y+4,z+Math.sin(q)*36);sb(30,6,30,'#2a2420',x,y+3,z);st('con','#ff8a20',36,70,36,x,y+40,z,{em:1});st('con','#ffe08a',18,40,18,x,y+30,z,{em:1});
      LIGHTS.push({x,y:y+60,z,w:2.2});break;
    case 'tree':sc(14,70,14,'#4a3626',x,y+35,z);for(let q=0;q<4;q++)st('sph',q%2?'#3f6b35':'#4c7a3c',80-q*10,55,80-q*10,x+Math.cos(q*1.7)*20,y+90+q*8,z+Math.sin(q*1.7)*20);
      for(let q=0;q<8;q++)st('sph',L.k==='tree'&&REG==='diyala'?'#f09020':'#a01828',7,7,7,x+Math.cos(q)*36,y+80+(q%3)*14,z+Math.sin(q)*36,{em:.3});break;
    case 'spiral':for(let q=0;q<6;q++){const r=60-q*9;sc(r*2,34,r*2,'#c8ac78',x,y+17+q*34,z);}sc(16,30,16,'#b89c68',x,y+218,z);break;
    case 'wheel':st('cyl','#6a5038',160,10,160,x,y+60,z,{rx:PI/2});for(let q=0;q<10;q++)st('box','#5a4028',150,4,4,x,y+60,z,{rz:q*PI/10});sb(20,70,60,'#8a7a5a',x,y+35,z-40);break;
    case 'palm':sc(10,90,10,'#6a5a4a',x,y+45,z);for(let q=0;q<5;q++)st('box','#7a6a50',50,2,6,x+Math.cos(q*1.3)*20,y+88,z+Math.sin(q*1.3)*20,{ry:q*1.3,rz:-.6});break;
    case 'ishtar':sb(140,150,30,'#2a5aa8',x,y+75,z);sb(60,96,34,'#1a1410',x,y+48,z);for(let q=0;q<6;q++){sb(8,8,32,'#e0c050',x-56+q*22,y+130,z,1);sb(10,6,32,'#e0c050',x-56+q*22,y+100,z,1);}
      for(const q of[-1,1])sb(30,190,40,'#244e96',x+q*86,y+95,z);break;
    case 'ruin':for(let q=0;q<7;q++)sb(20+hash(q)*30,30+hash(q*3)*70,20,'#a8946a',x+Math.cos(q)*60,y+20,z+Math.sin(q)*60);sb(120,14,90,'#b8a47a',x,y+7,z);break;
    case 'mudhif':for(let q=0;q<9;q++){const a=PI*q/8;st('box','#c8b070',8,4,120,x+Math.cos(a)*55-0,y+Math.sin(a)*55,z,{rz:0});}st('cyl','#c8b070',110,120,110,x,y+2,z,{rx:PI/2,rz:PI/2});sb(116,4,120,'#a89050',x,y,z);break;
    case 'ziggurat':sb(220,40,220,'#b89a6a',x,y+20,z);sb(160,40,160,'#a88a5a',x,y+60,z);sb(100,40,100,'#b89a6a',x,y+100,z);sb(60,30,60,'#a88a5a',x,y+135,z);sb(30,80,120,'#9a7c50',x+120,y+40,z,0);break;
    case 'boat':st('sph','#6a4a2a',90,20,26,x,y+10,z);sb(80,3,22,'#8a6a40',x,y+18,z);sc(4,70,4,'#5a3a20',x,y+50,z);st('box','#e0d8c8',2,50,40,x+6,y+60,z);break;
    case 'gate':sb(60,180,60,'#6a5848',x-60,y+90,z);sb(60,180,60,'#6a5848',x+60,y+90,z);sb(180,40,60,'#5a4838',x,y+200,z);
      for(let q=0;q<5;q++)sb(10,10,62,'#e0c050',x-40+q*20,y+200,z,1);break;}
}
/* ---------- Baghdad's city blocks ---------- */
function cityBlock(b){const w=b.x1-b.x0,d=b.z1-b.z0,cx=(b.x0+b.x1)/2,cz=(b.z0+b.z1)/2;
  sb(w,b.t,d,b.col,cx,b.t/2,cz);sb(w+4,6,d+4,'#7a6448',cx,b.t-3,cz);
  for(let k=0;k<Math.floor(b.t/50);k++){const y=30+k*50;sb(14,22,d+1,'#2a2018',cx-w*.25,y,cz);sb(14,22,d+1,'#2a2018',cx+w*.25,y,cz);sb(w+1,22,14,'#2a2018',cx,y,cz-d*.25);sb(w+1,22,14,'#2a2018',cx,y,cz+d*.25);}
  if(hash(cx*.3+cz)<.25){st('sph','#2f8f98',w*.7,w*.5,d*.7,cx,b.t,cz);}}
/* ---------- the generic warrior boss, dressed for his land ---------- */
function bossWeapon(k){
  if(k==='axe')return namAxe;if(k==='sword')return errSword;
  if(k==='spear')return ()=>{cyl(1.8,96,1.8,'#3a2a1c',0,-30,0);push();T(0,-80,0);RZ(PI);con(3.6,22,1.2,'#c8ccd2',0,-11,0);pop();cyl(2.6,3,2.6,'#c8a050',0,-68,0);};
  return ()=>{cyl(2.2,54,2.2,'#3a2a1c',0,-14,0);push();T(0,-42,0);sph(9,12,9,'#5a5650',0,0,0);for(let q=0;q<6;q++){push();RY(q*1.05);RZ(PI/2);con(2.4,8,2.4,'#8a8680',0,8,0);pop();}pop();};}
function drawWarrior(e){
  const fall=mobFx(e,2.2);if(fall<0)return;const L=e.look||{},st=e.state,p2=e.p2,sp=(p2?1.18:1)*(e.spd||1);
  let key=null,u=0,lean=0,crouch=0;const A=NM[st];
  if(A){key=A.key;const w=A.w/sp;
    if(st==='sl'){const h=e.hold||0,k=e.t-w-h;u=e.t<w?e.t/w:e.t<w+h?1:k<A.a?1+k/A.a:Math.min(3,2+(k-A.a)/A.r);}
    else u=e.t<w?e.t/w:e.t<w+A.a?1+(e.t-w)/A.a:Math.min(3,2+(e.t-w-A.a)/(A.r/sp));}
  else if(st==='leapW'){key='H';u=Math.min(1,e.t/.55)*.9;crouch=-6;}
  else if(st==='leap'){key='S';u=e.t<.55?1:1+Math.min(1,(e.t-.55)/.15);}
  else if(st==='charge')lean=e.t<.6?.15:-.42;
  else if(st==='stag'){lean=.6;crouch=-14;}
  else if(st==='trans'||st==='stomp')lean=-.3+Math.sin(e.t*9)*.04;
  else if(st==='grab'||st==='held')lean=st==='grab'?-.25:-.05;
  const p=key?keyPose(key,u):{t:.5,y:.35,e:.45,w:.5,r:0,ln:0};if(key)lean+=p.ln;
  let m=p,o=L.two?{t:p.t-.25,y:p.y-.35,e:p.e+.45,w:0,r:p.r}:{t:.6+p.t*.15,y:-.1,e:.9,w:0};
  if(st==='grab'){const k=clamp(e.t/.5,0,1);o={t:lerp(.4,1.55,k),y:-.2,e:lerp(.8,.1,k),w:0};}
  if(st==='held')o={t:2.4,y:-.35,e:.25,w:0};
  if(st==='trans'||st==='stomp'){m={t:3.0,y:.1,e:.2,w:.1};o={t:2.6,y:.5,e:.4,w:0};}
  if(st==='stag'){m={t:.2,y:.3,e:.3,w:.9};o={t:.6,y:0,e:.4,w:0};}
  const mvk=(st==='chase'||st==='charge')?1:0,wk=e.walk,ar=L.a||'#5a5048',a2=L.a2||'#2a2420',tr=L.t||'#b08a3a',ck=L.c||'#2a1a14',sk=L.sk||'#a8765a',ey=L.e||'#ffb040';
  push();T(e.x,e.y+e.j,e.z);RY(-e.a);if(fall)RZ(fall);const S=L.fem?1.75:1.95;Sc(S,S,S);T(0,crouch*.4,0);
  for(const sd of[-1,1]){const ph=wk+(sd>0?0:PI),hip=Math.sin(ph)*.55*mvk+(st==='stag'&&sd>0?1.4:0),knee=.1+.75*Math.max(0,Math.cos(ph))*mvk+(st==='stag'&&sd>0?1.7:0);
    push();T(0,24.4,sd*4.2);RZ(hip);sph(6.4,14,6.8,a2,0,-6,0);T(0,-11.5,0);RZ(-knee);sph(5.6,12.8,6,ar,0,-5,0);cyl(6.4,2,6.6,tr,0,-1,0);T(0,-10.3,0);RZ(knee-hip);sph(9.4,4.6,6.4,'#2a201a',1.6,0,0);pop();}
  for(let q=0;q<10;q++){const a=q/10*6.283;push();T(Math.cos(a)*6.4,23,Math.sin(a)*7.8);RX(Math.sin(a)*.18);RZ(-Math.cos(a)*.18-(Math.cos(a)<0?mvk*.25:0));box(1.2,L.fem?24:19,5.4,q%2?ar:a2,0,L.fem?-9:-7,0);pop();}
  push();T(0,30,0);RZ(lean);T(0,-30,0);
  push();T(-7.5,46,0);RZ(-.2-Math.sin(time*2.4)*.06-mvk*.22);for(let q=0;q<5;q++){const l=38+hash(q*3.1)*14;box(2,l,7.4,ck,0,-l/2,(q-2)*7);if(p2)box(2.2,5,7.6,ey,.2,-l+3,(q-2)*7,1,.7);}pop();
  sph(12.5,17.5,15.5,ar,0,36.5,0);for(let q=0;q<4;q++)cyl(12.8-q*.4,.8,15.8-q*.4,tr,0,28+q*4.2,0);cyl(13,3.6,16,'#2a201a',0,24.5,0);sph(4.4,4.6,3,tr,6,24.5,0);
  for(const sd of[-1,1]){push();T(0,44.5,sd*9.6);sph(10,5,10,tr,0,0,0);sph(8.6,4.4,8.6,ar,0,-3,0);pop();}
  push();T(.6,47,0);cyl(5,4,5,sk,0,0,0);sph(8.2,9.6,8,sk,.8,6,0);sph(2.4,3,2.2,sk,5.6,5.4,0);
  if(L.bd){sph(7.6,7.2,8.4,L.bd,2.4,2,0);sph(4.6,5.4,5.4,L.bd,4.2,-.4,0);}
  for(const sd of[-1,1]){sph(1.2,1.2,1.8,p2?ey:'#e8e0d0',5,7.4,sd*2,p2?1:0);sph(.8,.9,1.1,'#0a0808',5.4,7.4,sd*2);}
  const h=L.helm;
  if(h==='horn'){sph(9.6,7,9.4,a2,.3,10.4,0);for(const sd of[-1,1]){push();T(-1,11,sd*7.5);RX(-sd*.9);RZ(.35);con(3.2,15,3.2,'#cfc6b0',0,7,0);pop();}}
  else if(h==='crown'){cyl(9.6,5,9.4,tr,.3,12,0);for(let q=0;q<8;q++){const a=q/8*6.283;con(1.6,5,1.6,tr,.3+Math.cos(a)*8.4,16,Math.sin(a)*8.4);}}
  else if(h==='hood'){sph(10.6,11,10.4,ck,-1.2,7.4,0);fru(13,16,13,ck,-3,1,0);}
  else if(h==='mask'){sph(9.8,11,9.6,a2,.3,7.6,0);box(1.2,2,8,ey,8.8,7.6,0,1);for(let q=0;q<5;q++){push();T(-2-q*1.4,14,0);RZ(.8+q*.15);con(2,10-q,1.4,tr,0,4,0);pop();}}
  else{sph(9.4,6.4,9.2,a2,.3,10.4,0);cyl(9.6,1.4,9.4,tr,.3,8.6,0);}
  if(L.fem&&h!=='hood')for(let q=0;q<5;q++)sph(3,12,3,'#1a1210',-6+q*.5,2-q,(q-2)*3);
  pop();
  namArm(-1,o,null,ar);namArm(1,m,st==='held'?null:bossWeapon(e.wpn),ar);
  pop();pop();gAlpha=1;gFlash=false;
}
/* ---------- the map of Iraq: where you have been, where you can go ---------- */
const IRQ=[[42.4,37.1],[43.0,37.35],[44.2,37.3],[44.8,37.15],[45.0,36.7],[45.4,36.0],[45.9,35.8],[46.2,35.1],[45.6,34.6],[45.6,34.0],[46.2,33.4],[47.0,32.9],[47.6,32.4],[47.8,31.7],[47.7,31.0],[48.0,30.5],[48.5,29.95],[47.9,29.9],[47.4,30.1],[46.5,29.1],[44.7,29.2],[42.1,31.1],[41.0,31.95],[39.2,32.2],[38.8,33.4],[41.0,34.4],[41.3,35.6],[41.4,36.5],[42.4,37.1]];
const TIG=[[42.4,37.1],[42.7,36.9],[43.1,36.35],[43.3,35.5],[43.6,34.9],[43.85,34.2],[44.2,33.6],[44.4,33.3],[44.6,33.0],[45.2,32.6],[45.8,32.5],[46.5,32.0],[47.2,31.6],[47.4,31.0],[47.5,30.9]];
const EUP=[[41.0,34.4],[41.9,34.4],[42.5,33.9],[43.3,33.4],[43.8,32.9],[44.1,32.6],[44.4,32.2],[44.9,31.9],[45.3,31.3],[46.1,31.0],[46.7,31.0],[47.4,31.0],[47.5,30.9],[47.8,30.5],[48.3,30.0]];
let iqOpen=false,IQP=null;
function regIdx(id){return ORDER.indexOf(id);}
function canGo(id){const i=regIdx(id);if(i<0)return false;if(id==='baghdad')return !!(stats.done&&stats.done.basra);return i<=(stats.prog||0);}
function openIraq(){iqOpen=true;$('shrine').classList.remove('on');$('iraq').classList.add('on');drawIraq();}
function closeIraq(){iqOpen=false;$('iraq').classList.remove('on');if(paused&&nearShrine>=0){renderShrine();$('shrine').classList.add('on');}}
function drawIraq(){
  const cv=$('iqc'),w=RW,h=RH;cv.width=Math.round(w*DPR);cv.height=Math.round(h*DPR);const m=cv.getContext('2d');m.setTransform(DPR,0,0,DPR,0,0);
  m.fillStyle='#17120e';m.fillRect(0,0,w,h);
  const top=40,bot=34,lo0=38.6,lo1=48.8,la0=29,la1=37.6,sc=Math.min((w-40)/(lo1-lo0),(h-top-bot)/(la1-la0)*1.0),ox=(w-(lo1-lo0)*sc)/2,oy=top+((h-top-bot)-(la1-la0)*sc)/2;
  const X=lo=>ox+(lo-lo0)*sc,Y=la=>oy+(la1-la)*sc;IQP={X,Y};
  m.fillStyle='#d8c896';m.strokeStyle='#5a4428';m.lineWidth=2;m.beginPath();IRQ.forEach((p,i)=>i?m.lineTo(X(p[0]),Y(p[1])):m.moveTo(X(p[0]),Y(p[1])));m.closePath();m.fill();m.stroke();
  m.strokeStyle='#4a8ab4';m.lineWidth=3;for(const R of[TIG,EUP]){m.beginPath();R.forEach((p,i)=>i?m.lineTo(X(p[0]),Y(p[1])):m.moveTo(X(p[0]),Y(p[1])));m.stroke();}
  // the road south, then the boat back up the river
  m.setLineDash([4,4]);m.strokeStyle='rgba(90,60,30,.55)';m.lineWidth=1.5;m.beginPath();
  ORDER.slice(0,-1).forEach((id,i)=>{const g=GOVM[id];i?m.lineTo(X(g.lon),Y(g.lat)):m.moveTo(X(g.lon),Y(g.lat));});m.stroke();
  m.strokeStyle='rgba(40,90,140,.7)';m.beginPath();m.moveTo(X(GOVM.basra.lon),Y(GOVM.basra.lat));TIG.slice().reverse().filter(p=>p[1]<33.4).forEach(p=>m.lineTo(X(p[0]),Y(p[1])));m.lineTo(X(GOVM.baghdad.lon),Y(GOVM.baghdad.lat));m.stroke();m.setLineDash([]);
  m.font='700 11px '+FONT;m.textAlign='center';m.textBaseline='middle';try{m.direction='ltr';}catch(_){}
  for(const id of ORDER){const g=GOVM[id],x=X(g.lon),y=Y(g.lat),done=stats.done&&stats.done[id],ok=canGo(id),cur=id===REG;
    m.beginPath();m.arc(x,y,cur?9:7,0,7);m.fillStyle=done?'#e8701c':ok?'#f0dca0':'#8a7a62';m.fill();m.lineWidth=cur?3:1.5;m.strokeStyle=cur?'#c81e28':'#3a2a18';m.stroke();
    if(id==='baghdad'){m.strokeStyle='#3a2a18';m.lineWidth=1.5;m.strokeRect(x-10,y-10,20,20);}
    if(!ok&&!done&&id!=='baghdad')continue;const nm=g.n.split(' · ').pop(),rt=g.lon>=44.4;m.textAlign=rt?'left':'right';m.lineWidth=3;m.strokeStyle='rgba(216,200,150,.95)';m.strokeText(nm,x+(rt?10:-10),y);m.fillStyle=cur?'#8a1018':'#2a1c10';m.fillText(nm,x+(rt?10:-10),y);m.textAlign='center';}
  // the map grows with the journey: only the lands you have reached, and the next one, are drawn; the rest is still dark
  {const f=document.createElement('canvas');f.width=cv.width;f.height=cv.height;const g=f.getContext('2d');g.setTransform(DPR,0,0,DPR,0,0);
    g.fillStyle='#17120e';g.fillRect(0,0,w,h);g.globalCompositeOperation='destination-out';
    const R=Math.max(34,sc*.95),hole=(x,y,r)=>{const gr=g.createRadialGradient(x,y,r*.35,x,y,r);gr.addColorStop(0,'rgba(0,0,0,1)');gr.addColorStop(1,'rgba(0,0,0,0)');g.fillStyle=gr;g.beginPath();g.arc(x,y,r,0,7);g.fill();};
    let prev=null;
    for(const id of ORDER){const gv=GOVM[id],seen=(stats.done&&stats.done[id])||id===REG||canGo(id);if(!seen)continue;const x=X(gv.lon),y=Y(gv.lat);hole(x,y,R);
      if(prev){const n=6;for(let k=1;k<n;k++)hole(lerp(prev[0],x,k/n),lerp(prev[1],y,k/n),R*.8);}prev=[x,y];}
    g.globalCompositeOperation='source-over';m.drawImage(f,0,0,w,h);
    const all=ORDER.length,got=ORDER.filter(id=>(stats.done&&stats.done[id])||id===REG||canGo(id)).length;
    m.fillStyle='#a8967a';m.font='600 11px '+FONT;m.textAlign='center';m.fillText('انكشف '+got+' من '+all+' محافظة',w/2,top+4);}
  m.fillStyle='#e8dcc0';m.font='600 13px '+FONT;
  m.fillText('المفاتيح: '+(stats.keys||0)+' من 5 · البرتقالي: خلصته · الفاتح: تگدر تروحله · الغامق: بعده مسدود',w/2,h-14);
}
function iraqTap(e){if(!IQP)return;const p=loc(e);let best=null,bd=22;
  for(const id of ORDER){const g=GOVM[id],d=Math.hypot(IQP.X(g.lon)-p[0],IQP.Y(g.lat)-p[1]);if(d<bd){bd=d;best=id;}}
  if(!best||!canGo(best)||best===REG)return;iqOpen=false;$('iraq').classList.remove('on');paused=false;travel(best);}
/* ---------- the boat from Basra, and the end ---------- */
function boatFilm(){if(!RIVZ)return;const z=RIVZ-10,y=-24;
  cine={i:0,t:0,boat:1,shots:[
    {d:6,a:[300,30,z+120,700,y+8,z],b:[900,26,z+110,1300,y+8,z],t:'من شط العرب، قارب قديم بلا مجداف… والماي يعرف الدرب.',tod:1},
    {d:6,a:[1400,60,z-160,1900,y+8,z],b:[2000,50,z-150,2500,y+8,z],t:'الماي ياخذه للشمال، ضد التيار، مثل ما يرجع الواحد لبيته.',tod:2},
    {d:5,a:[2600,40,z+90,3000,y+20,z],b:[2900,36,z+80,3300,y+30,z],t:'« دجلة… أني جاي. »',v:1,tod:2}]};
  paused=true;root.classList.add('cine');cineShot();music(true);}
function drawBoat(){if(!cine||!cine.boat)return;const s=cine.shots[cine.i],k=Math.min(1,cine.t/s.d),x=lerp(s.a[3],s.b[3],k)+40,z=RIVZ-10,y=-22+Math.sin(time*2)*1.2;
  push();T(x,y,z);RZ(Math.sin(time*1.3)*.03);sph(90,20,28,'#5a3a20',0,4,0);box(80,3,24,'#7a5a34',0,12,0);cyl(3,60,3,'#4a2a18',10,40,0);
  sph(10,14,10,'#1c1622',-20,22,0);sph(6,7,6,'#c98d5c',-20,34,0);pop();}
function endChoice(){paused=true;$('end3').hidden=Object.keys(stats.letters||{}).length<12;$('end').classList.add('on');}
function ending(k){const t=$('endT');$('endC').hidden=true;$('endR').hidden=false;
  t.textContent=k===3?'فرات ما ختمها وما نزل. وگف على عتبة البوابة وگرا رسائل دجلة بصوت عالي، وحدة ورا الثانية، لحد ما الظلام سكت. ومن آخر رسالة… طلعت إيد تعرفها. دجلة رجعت، والجمرة رجعت لصدرها، والبوابة انسدت بإيدين اثنين. يگولون من يومها النهرين يمشون سوا لحد البصرة.':k===1?'فرات حط الجمرة اللي بصدره بقفل البوابة، وانطفت. البوابة انسدت للأبد. وبالسكون اللي بعدها، سمع صوت مألوف يناديه من جهة الماي… وگعد يمه. البلاد رجعت ترتاح، وموتاها ناموا.'
    :'فرات ما گدر يطفي الجمرة. خلّى البوابة مفتوحة، ونزل لتحت يدوّر على دجلة بنفسه. يگولون، بالليالي اللي يغيب بيها الگمر، تشوف ضوه جمرة تمشي ببطء ورا نهر دجلة… للجنوب.';
  stats.ended=k;save();}
