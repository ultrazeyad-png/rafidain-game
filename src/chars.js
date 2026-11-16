/* ---------- characters (rounded) ---------- */
const HC={skin:'#c98d5c',skin2:'#b57c4e',cloth:'#dccfae',vest:'#5c3a28',red:'#b3232f',gold:'#d4ac50',white:'#efe7d8',beard:'#1a120e',bisht:'#1c1622',boot:'#2c1e17',pants:'#a39678',steel:'#e8ecf4',wood:'#5a3a22'};
function saif(){   // curved Arab saif: wrapped grip, hooked pommel, quillons, ridged blade with a bright edge
  cyl(1.8,7,1.8,'#33201a',0,.8,0);for(let i=0;i<3;i++)cyl(2.05,.55,2.05,HC.gold,0,-1.2+i*2,0);
  push();T(0,4.4,0);RZ(.6);sph(2.4,3.6,2.2,HC.gold,0,.9,0);sph(1.5,1.5,1.5,'#b3232f',.2,2.6,0);pop();
  sph(9.6,1.9,2.5,HC.gold,0,-3.3,0);sph(1.9,1.9,1.9,HC.gold,4.9,-3.3,0);sph(1.9,1.9,1.9,HC.gold,-4.9,-3.3,0);sph(2,4.6,2.7,HC.gold,0,-4.8,0);
  push();T(0,-4.6,0);const L=5.3;
  for(let i=0;i<6;i++){const w=3.1-i*.2;
    box(w,L+.5,.8,'#dfe4ee',0,-L/2,0);box(.95,L+.5,1.35,'#a9b1c2',w/2-.45,-L/2,0);
    box(.55,L+.5,.9,'#8791a6',.25,-L/2,0);box(.5,L+.5,.36,'#ffffff',-(w/2-.15),-L/2,0);
    T(0,-L,0);RZ(.085);}
  RZ(PI);con(2,6.5,.8,'#dfe4ee',0,3.2,0);pop();
  push();T(0,-4.6,0);lvlLook(stats.wpn==='dual'?'dual':'sword',30);pop();
}
function bowW(){for(let q=-3;q<=3;q++){push();T(Math.cos(q*.17)*-4+4,q*5.4,0);RZ(q*.17);cyl(1.5,6,1.5,q?'#5a3a22':'#2a1c14',0,0,0);pop();}box(.4,34,.4,'#e0d8c8',-.2,0,0);}
function bowHeld(){push();RX(PI/2);RZ(PI/2);bowW();pop();}
function flask(){push();RZ(-1.2);T(0,-2,0);sph(4.4,5.6,4.4,'#c87a20',0,-4,0,.25);sph(3.4,4.3,3.4,'#f0a030',0,-4.3,0,.7);cyl(1.6,3.4,1.6,'#c87a20',0,1.6,0,.2);cyl(1.9,1.6,1.9,'#5a3a22',0,3.6,0);pop();}
function wl(k){return (stats.wl&&stats.wl[k])||1;}
function lvlLook(k,len){const L=wl(k);if(L<4)return;   // engraved lines from level 4, a glowing edge from 7, a living flame at 10
  for(let i=1;i<5;i++)box(.4,len*.12,.9,'#d4ac50',-.6,-len*i*.18,0,L>=7?1:0);
  if(L>=7){const g=L>=10?.9:.45;box(.5,len,.5,L>=10?'#ffd27a':'#bfe0ff',-1.5,-len*.5,0,1,g);}}
function axeW(){cyl(1.9,50,1.9,'#4a3424',0,-14,0);for(let i=0;i<3;i++)cyl(2.2,.8,2.2,'#2a1c14',0,4-i*5,0);
  push();T(0,-34,0);box(4,10,2.6,'#5a5550',2,0,0);sph(13,15,1.6,'#9aa0a8',8,0,0);sph(12,13,1.2,'#d4d8de',10.5,0,0);box(1,14,1.8,'#2a2420',3.6,0,0);pop();
  con(2.4,4,2.4,'#5a5550',0,-41.5,0);push();T(0,-34,0);lvlLook('axe',12);pop();}
function daggerW(){cyl(1.4,6,1.4,'#2a1c14',0,1,0);sph(5.6,1.4,2,'#d4ac50',0,-2.4,0);push();T(0,-3,0);box(2.4,12,.7,'#d8dde6',0,-6,0);box(.5,12,.9,'#ffffff',-1,-6,0);RZ(PI);con(1.7,4,.6,'#d8dde6',0,-12-2,0);pop();sph(1.6,1.6,1.6,'#b3232f',0,4.4,0);
  push();T(0,-3,0);lvlLook('dagger',12);pop();}
function glaiveW(){cyl(1.7,54,1.7,'#4a3236',0,-4,0);sph(2.8,2.8,2.8,HC.gold,0,23,0);cyl(2.8,2.4,2.8,HC.gold,0,-27,0);
  push();T(1.6,-36,0);sph(7.5,19,1,'#e0cfc4',0,0,0);push();T(1.2,-8,0);RZ(-.5);sph(4.5,11,.9,'#e0cfc4',0,-3,0);pop();pop();}
function limbArm(side,p,item){
  push();T(p.tx||0,40.5,side*7.6);sph(5.8,5.8,5.8,HC.cloth,0,0,0);RY(-side*(p.y||0));RZ(p.t);if(p.r)RY(-side*p.r);
  sph(4.8,10.8,4.8,HC.cloth,0,-4.6,0);T(0,-9,0);RZ(p.e==null?.3:p.e);
  sph(4.1,9.6,4.1,HC.vest,0,-4.2,0);cyl(4.3,1.2,4.3,HC.gold,0,-7.4,0);sph(3.6,3.9,3.6,HC.skin,0,-9.4,0);
  if(item){T(0,-9.6,0);RZ(p.w);item();
    if(CAP===side&&!SHPASS){const L=BLEN[stats.wpn]||[-8,-35,3],w=(x,y)=>[cur[0]*x+cur[4]*y+cur[12],cur[1]*x+cur[5]*y+cur[13],cur[2]*x+cur[6]*y+cur[14]];
      TRAIL.push({a:w(0,L[0]),b:w(L[2],L[1]),tm:time});}}
  pop();
}
/* ---------- the swing: keyframed poses, each light cut flowing into the next along a figure eight ---------- */
let CAP=0,SHPASS=false,TRAIL=[],TRL=.17;
const BLEN={shield:[-8,-35,3],dual:[-8,-35,3],glaive:[-34,-58,2],axe:[-26,-40,6],dagger:[-4,-17,1]};
// key: [u, shoulder pitch t, yaw y (+ out to the weapon side, - across the body), elbow e, wrist w, roll r, lean]
// u: 0..1 wind-up, 1..2 the cut, 2..3 follow-through
const K={
  A:[[0,.35,.25,.6,.6,0,0],[.55,1.6,.75,1.2,.9,-.3,.04],[1,2.45,.95,.95,.55,-.55,.12],[1.5,1.65,.15,.3,.25,-.25,-.12],[2,.95,-1.15,.2,.35,.2,-.22],[2.5,.75,-1.05,.35,.5,.15,-.16],[3,.6,-.7,.45,.6,0,-.08]],
  B:[[0,.6,-.7,.45,.6,0,-.08],[.5,1.5,-1.05,1.5,.9,.4,.02],[1,2.45,-.95,1.1,.55,.6,.12],[1.5,1.65,-.1,.3,.25,.25,-.12],[2,.95,1.15,.2,.35,-.2,-.22],[2.5,.75,1.0,.35,.5,-.15,-.16],[3,.5,.5,.5,.65,0,-.06]],
  C:[[0,.5,.5,.5,.65,0,-.06],[.5,2.1,.3,1.4,.6,0,.1],[1,3.05,.1,1.0,.25,0,.2],[1.45,1.9,0,.15,.15,0,-.18],[2,.6,-.1,.1,.1,0,-.34],[2.6,.55,-.05,.2,.3,0,-.22],[3,.4,0,.45,.6,0,-.06]],
  H:[[0,.35,.2,.6,.6,0,0],[.6,2.7,.35,1.5,.5,0,.22],[1,3.15,.15,1.15,.25,0,.28],[1.4,1.9,0,.1,.12,0,-.2],[2,.55,-.1,.08,.08,0,-.4],[2.6,.5,-.05,.2,.3,0,-.26],[3,.35,0,.5,.6,0,-.05]],
  S:[[0,.5,.2,1.0,.4,0,0],[1,1.2,.35,2.0,.9,0,.08],[1.4,1.5,.05,.2,.25,0,-.22],[2,1.5,0,.05,.2,0,-.26],[3,.5,.2,.6,.6,0,-.04]],
  X:[[0,.4,.3,.9,.6,0,0],[1,1.6,1.35,1.0,.45,-.3,.04],[1.5,1.45,.2,.2,.25,-.1,-.12],[2,1.4,-1.3,.2,.3,.2,-.2],[3,.5,-.4,.5,.6,0,-.06]],
  R:[[0,.5,0,.6,.5,0,0],[1,1.15,-.1,2.1,1.0,0,.06],[1.3,1.55,0,.05,.2,0,-.3],[2,1.5,0,0,.2,0,-.3],[3,.5,0,.6,.6,0,-.05]]};
function keyPose(key,u){const L=K[key]||K.A;let i=0;while(i<L.length-2&&u>L[i+1][0])i++;const a=L[i],b=L[i+1],k0=clamp((u-a[0])/((b[0]-a[0])||1),0,1),k=k0*k0*(3-2*k0);
  const f=j=>a[j]+(b[j]-a[j])*k;return {t:f(1),y:f(2),e:f(3),w:f(4),r:f(5),ln:f(6)};}
function atkU(a,t){return t<a.wind?t/a.wind:t<a.wind+a.act?1+(t-a.wind)/a.act:Math.min(3,2+(t-a.wind-a.act)/a.rec);}
function drawTrail(){
  for(let i=TRAIL.length-1;i>=0;i--)if(time-TRAIL[i].tm>TRL)TRAIL.splice(i,1);
  if(TRAIL.length<2)return;const q=[],col=stats.wpn==='glaive'?[1,.62,.55]:[.9,.94,1];
  for(let i=1;i<TRAIL.length;i++){const p=TRAIL[i-1],n=TRAIL[i],k0=1-(time-p.tm)/TRL,k1=1-(time-n.tm)/TRL;
    // blend from the guard toward the tip: a thin bright edge near the tip, fading toward the hilt
    const mid=(s,f)=>[s.a[0]+(s.b[0]-s.a[0])*f,s.a[1]+(s.b[1]-s.a[1])*f,s.a[2]+(s.b[2]-s.a[2])*f];
    const pa=mid(p,.6),na=mid(n,.6);
    const v=(P,k)=>[P[0],P[1],P[2],0,1,0,col[0]*k,col[1]*k,col[2]*k,1];
    q.push(v(pa,0),v(p.b,k0),v(n.b,k1),v(pa,0),v(n.b,k1),v(na,0));}
  ribbon(q,.6);
}

function drawHero(){dressHero();
  const wpn=stats.wpn,st=P.state;
  gAlpha=1;gFlash=false;
  if(cam.d<44)gAlpha=.3;
  if(st==='dead')gAlpha=Math.max(0,1-P.t/2);
  else if(P.inv>0&&st==='idle'&&Math.floor(time*20)%2)gAlpha=.45;
  const moving=st==='idle'&&P.spd>20&&!P.air;P.mvk=lerp(P.mvk||0,moving?Math.min(1,P.spd/110):0,.2);const mk=P.mvk;
  // m = right (weapon) arm, o = left arm
  let m={t:.25,y:0,e:.5,w:.7},o={t:.25,y:0,e:.5,w:.7},lean=0,twist=0,guardUp=false;
  const sw=Math.sin(P.walk),ws=sw*.42*mk,br=Math.sin(time*1.7)*.45*(1-mk);
  m.t-=ws;m.e+=.25*mk;o.t+=ws;o.e+=.25*mk;twist=sw*.16*mk;lean=-.1*mk;
  if(wpn==='glaive'){m={t:.7,y:.2,e:.5,w:.3};o={t:.5,y:-.5,e:.8,w:0};}
  if(wpn==='shield'){o={t:.35+ws,y:0,e:.9,w:0};if(P.guard){guardUp=true;o={t:.95,y:-.55,e:.9,w:0};lean=-.06;}}
  let tilt=0;
  if(st==='atk'){const a=P.atk,hd=a.hand||0,p=keyPose(a.key||'A',atkU(a,P.t));lean=p.ln;
    twist=-p.y*.36*(hd===1?-1:1);if(hd===2)twist=0;
    const cw=P.t>a.wind*.8&&P.t<a.wind+a.act+.07;
    if(hd===1){o=p;CAP=cw?-1:0;}else{m=p;CAP=cw?1:0;if(hd===2)o={t:p.t,y:p.y,e:p.e,w:p.w,r:p.r};}
    if(wpn==='glaive'||wpn==='axe'){o={t:p.t-.25,y:p.y-.35,e:p.e+.45,w:0,r:p.r};}
  }else if(st==='heal'){const u=P.t/HEALT,hk=[[0,.35,0,.9,0],[.18,.2,-.35,.6,0],[.36,1.2,-.62,2.15,-.2],[.78,1.38,-.62,2.3,-.45],[.92,.9,-.4,1.4,0],[1,.35,0,.9,0]];
    let i=0;while(i<hk.length-2&&u>hk[i+1][0])i++;const a=hk[i],b=hk[i+1],k0=clamp((u-a[0])/(b[0]-a[0]),0,1),k=k0*k0*(3-2*k0),f=j=>a[j]+(b[j]-a[j])*k;
    o={t:f(1),y:f(2),e:f(3),w:f(4)};tilt=u>.36&&u<.84?-.45*Math.sin(clamp((u-.36)/.48,0,1)*PI):0;lean=-tilt*.15;
    if(u>.15&&u<.95)o.item=flask;}
  else if(st==='hurt'){lean=.32;twist=.25;}
  else if(st==='cast'){o={t:1.75,y:-.25,e:.15,w:0};lean=-.1;}
  else if(st==='aim'){const k=clamp(P.t/.3,0,1),rel=P.t>.38;o={t:1.55,y:-.15,e:.05,w:-.2,item:bowHeld};m={t:1.45,y:lerp(-.1,.25,k),e:rel?.4:lerp(.4,2.4,k),w:.5};twist=.35;lean=-.05;}
  else if(st==='grabbed'){lean=.25+Math.sin(time*14)*.08;m={t:1.2+Math.sin(time*12)*.3,y:.4,e:.8,w:.6};o={t:1.4,y:-.6,e:1.2,w:0};}
  else if(st==='air'){m={t:2.6,y:0,e:.3,w:.3};if(wpn==='dual')o={t:2.6,y:0,e:.3,w:.3};lean=-.05;}
  else if(st==='plunge'||st==='land'){m={t:.5,y:0,e:.1,w:-.25};if(wpn==='dual')o={t:.5,y:0,e:.1,w:-.25};lean=-.3;}
  else if(st==='dead')lean=Math.min(1,P.t/.4)*1.5;
  let rootLean=(st==='dead'||st==='hurt')?lean:0,upLean=(st==='dead'||st==='hurt')?0:lean;
  if(st==='sleep'){rootLean=1.5;upLean=Math.sin(time*1.4)*.015;twist=0;m={t:.2,y:.3,e:.2,w:.5};o={t:.2,y:.3,e:.3,w:0};}
  else if(st==='wake'){const t=P.t,a=clamp(t/.2,0,1),b=clamp((t-1)/.85,0,1),sb=b*b*(3-2*b);   // jolt upright, catch a breath, stand
    rootLean=1.5*(1-sb);upLean=-(1.2*(1-Math.pow(1-a,3))+(a>=1?Math.sin(t*10)*.035:0))*(1-sb);twist=0;
    m={t:lerp(.9,.25,sb),y:.2,e:lerp(.9,.5,sb),w:.7};o={t:lerp(1.1,.35,sb),y:-.3,e:.9,w:0};}
  push();T(P.x,P.ry,P.z);RY(-P.a);
  if(st==='roll'){T(0,20,0);RZ(-6.283*P.t/.42);Sc(.8,.8,.8);T(0,-25,0);}
  if(rootLean){T(0,9.5*Math.min(1,Math.abs(rootLean)),0);RZ(rootLean);}
  const crouch=st==='land'?3.5*(1-P.t/.26):st==='atk'?1.6:0,bob=Math.abs(Math.cos(P.walk))*1.5*mk-crouch;T(0,bob,0);
  // legs with knees (drawn from the hips so the feet stay planted)
  for(const sd of[-1,1]){const ph=P.walk+(sd>0?0:PI);let hip,knee;
    if(P.air){hip=sd>0?.75:-.3;knee=sd>0?1.1:.5;}
    else if(st==='atk'||st==='land'){hip=sd>0?.5:-.42;knee=sd>0?.5:.2;}
    else if(st==='sleep'||st==='wake'){hip=.04*sd;knee=.05;}
    else if(guardUp&&mk<.2){hip=sd>0?-.25:.3;knee=.3;}
    else{hip=Math.sin(ph)*.66*mk+.04*sd*(1-mk);knee=.06+.85*Math.max(0,Math.cos(ph))*mk;}
    push();T(0,24.4,sd*3.6);RZ(hip);sph(6.2,13.8,6.4,HC.pants,0,-6,0);T(0,-11.5,0);RZ(-knee);
    sph(5.1,12.6,5.3,HC.pants,0,-5,0);sph(5.2,6,5.4,HC.boot,0,-8.4,0);T(0,-10.3,0);RZ(knee-hip);sph(8.4,4.4,5.6,HC.boot,1.5,0,0);pop();}
  fru(13.4,17,15.6,HC.cloth,0,25.5,0);
  // upper body: leans, twists with the stride and the swing, breathes when standing
  push();T(0,30,0);RY(twist);RZ(upLean);RX(Math.sin(time*.8)*.02*(1-mk));T(0,-30+br,0);
  push();T(-5.6,43.5,0);RZ(-.16-Math.sin(time*3)*.03-.2*mk);
  sph(3.6,35,19,HC.bisht,-.5,-17,0);sph(3.2,7,20,HC.bisht,-.2,-1.5,0);
  sph(1.3,34,1.5,HC.gold,.7,-17,8.6);sph(1.3,34,1.5,HC.gold,.7,-17,-8.6);pop();
  sph(11.2,17.5,14.8,HC.vest,0,37.5,0);sph(4.2,11.5,7.2,HC.cloth,3.7,38.6,0);
  cyl(11.8,3.4,14.8,HC.red,0,31,0);sph(2.6,3.8,3.6,HC.gold,5.6,31,0);
  push();T(5.2,29.6,-3.4);RZ(.18+.14*mk+sw*.1*mk);sph(1.7,9.5,3.2,HC.red,0,-4.2,0);pop();
  // head (turns a little against the torso so the eyes stay forward)
  push();T(.3,45,0);RY(-twist*.6);RZ(-upLean*.5+tilt);T(-.3,-45,0);
  const hy=50.4;
  cyl(4.6,4.4,4.6,HC.skin,.3,45.2,0);
  sph(8.8,10,8.4,HC.skin,.7,hy,0);
  sph(2.3,3,2.1,HC.skin2,5.2,hy-.5,0);
  sph(7.6,6.4,8.3,HC.beard,2.1,hy-3.4,0);sph(4.6,4.8,5.2,HC.beard,3.7,hy-5.3,0);sph(1.7,1.4,5.4,HC.beard,5,hy-2,0);
  for(const sd of[-1,1]){sph(1.3,1.5,2.2,'#f4efe6',4.6,hy+1,sd*1.95);sph(.9,1.2,1.3,'#120c0a',5,hy+1,sd*1.95);box(.8,.7,2.6,'#120c0a',4.8,hy+2.4,sd*1.95);}
  sph(10.8,7.8,10.4,HC.white,-.3,hy+3,0);
  fru(12.8,14,12.6,HC.white,-2.7,hy-3.4,0);
  for(const d of[[1.25,-1],[1.9,-5],[2.6,-2],[3.14,-6],[3.7,-2],[4.4,-5],[5.05,-1],[2.2,-8.5],[4.1,-8.5],[3.14,-10]]){const r=5.2-d[1]*.15;
    sph(1.5,1.5,1.5,HC.red,-2.7+Math.cos(d[0])*r,hy+d[1],Math.sin(d[0])*r);}
  cyl(11.5,1.3,11.1,'#0b090b',-.3,hy+3.4,0);cyl(10.5,1.3,10.1,'#0b090b',-.3,hy+4.8,0);
  pop();
  sph(10.5,4.4,16.5,HC.white,-3.6,42.8,0);
  // arms: weapon in the right hand (+z side), shield or second sword in the left
  const armed=!(st==='sleep'||(st==='wake'&&P.t<1.3));
  const RW_={glaive:glaiveW,axe:axeW,dagger:daggerW}[wpn]||saif;
  limbArm(-1,o,o.item||(armed&&wpn==='dual'?saif:null));
  limbArm(1,m,armed&&st!=='aim'?RW_:null);CAP=0;
  if(wpn==='shield'&&armed){push();if(st==='heal'||st==='aim'){T(-3,26,-14);RX(PI/2);RZ(.2);}else 
    if(guardUp){T(12,35,-2.5);RZ(PI/2);}else{T(2,31,-12);RX(PI/2);}
    const SL=wl('shield');if(SL>=7){cyl(23.5,.6,23.5,SL>=10?'#ffd27a':'#e8c87a',0,-.9,0,1,.5);}
    cyl(22.5,1.2,22.5,HC.gold,0,-.5,0);cyl(20.4,1.9,20.4,'#7a4a2a',0,0,0);sph(7.5,5,7.5,HC.gold,0,guardUp?-1:-1,0);pop();}
  pop();
  pop();
  if(!armed){   // his sword and shield lie on the ground beside him while he sleeps
    push();T(P.x+14,P.ry+1.6,P.z+30);RY(.5);RX(PI/2);RZ(PI/2);saif();pop();
    push();T(P.x+44,P.ry+1.4,P.z+22);cyl(22.5,1.2,22.5,HC.gold,0,-.5,0);cyl(20.4,1.9,20.4,'#7a4a2a',0,0,0);sph(7.5,5,7.5,HC.gold,0,.6,0);pop();}
  gAlpha=1;
}
function drawSwoosh(){drawTrail();}
function drawSwooshOld(){
  if(P.state!=='atk')return;const a=P.atk,t=P.t-a.wind;if(t<0||t>a.act+.12)return;
  const al=(1-t/(a.act+.12))*.75,R=a.reach*.78,k=clamp(t/a.act,0,1),col=stats.wpn==='glaive'?'#ff9a8c':'#e6ecff',dir=a.rev?-1:1,n=8,sg=a.hand===1?1:-1;
  push();T(P.x,P.ry,P.z);RY(-P.a);
  for(let i=0;i<=n;i++){const q=i/n*k,fa=al*(.3+.7*i/n);
    if(a.v){push();T(0,40,-sg*7);RZ(lerp(2.95,.75,q));box(R*.2,1.4,1.2,col,0,-R,0,1,fa);pop();}
    else{push();RY(sg*lerp(1.35,-1.35,q)*dir);box(1.4,1.2,R*.22,col,R,34,0,1,fa);pop();}}
  pop();
}
/* ---------- mobs ---------- */
function mobArm(side,cb,th,e,w,yaw,tx,item){
  push();T(tx||0,35.5,side*6.6);sph(4.8,4.8,4.8,cb,0,0,0);RY(-side*(yaw||0));RZ(th);
  sph(3.9,9.8,3.9,cb,0,-4,0);T(0,-8,0);RZ(e);sph(3.4,9.2,3.4,cb,0,-4,0);sph(3.2,3.4,3.2,cb,0,-8.4,0);
  if(item){T(0,-8.6,0);RZ(w);item();}pop();
}
function drawMob(e){
  if(e.k==='gmini'){const mk=MINI_GUARDS[REG]&&MINI_GUARDS[REG].k;if(mk==='scorpK')drawScorp(e);else if(mk==='crocK')drawCroc(e);else if(mk==='ghostK')drawGhost(e);else drawWolf(e);return;}if(e.k==='wolf'){drawWolf(e);return;}if(e.k==='anzu'){drawAnzu(e);return;}if(e.k==='boss'){drawNamtar(e);return;}if(e.k==='erra'){drawErra(e);return;}if(e.k==='gb'){drawWarrior(e);return;}if(e.k==='scorp'||e.k==='scorpK'){drawScorp(e);return;}if(e.k==='croc'||e.k==='crocK'){drawCroc(e);return;}if(e.k==='ghost'||e.k==='ghostK'){drawGhost(e);return;}if(e.k==='wolfK'){drawWolf(e);return;}
  const T_=e.T,k=e.k==='guard'?'brute':(e.k==='sling'||e.k==='archer')?'husk':e.k,s=T_.h/46,wf=k==='brute'?1.25:k==='boss'?1.12:1,c=MC[e.k],st=e.state;
  let fall=0;gAlpha=1;
  if(e.dead){if(e.t>1.2)return;fall=Math.min(1,e.t/.35)*1.5;gAlpha=Math.max(0,1-e.t/1.2);}
  if(Math.hypot(e.x-P.x,e.z-P.z)>DR)return;
  gFlash=e.flash>0;
  let th=.35,el=.45,w=.6,yaw=0,tx=0,tele=false,lean=0;
  if(e.boss){
    if(st==='sweepW'){th=1.4;el=.15;w=.25;yaw=1.3*Math.min(1,e.t/.25);tele=true;}
    else if(st==='sweep1'){th=1.4;el=.15;w=.25;yaw=lerp(1.3,-1.3,Math.min(1,e.t/.16));}
    else if(st==='sweepW2'){th=1.4;el=.15;w=.25;yaw=-1.3;tele=true;}
    else if(st==='sweep2'){th=1.4;el=.15;w=.25;yaw=lerp(-1.3,1.3,Math.min(1,e.t/.16));}
    else if(st==='slamW'){th=lerp(.4,2.9,Math.min(1,e.t/.25));el=.3;w=.25;tele=true;}
    else if(st==='leap'){th=2.9;el=.2;w=.25;}
    else if(st==='dashW'){th=1.45;el=.1;w=.05;tx=-8;tele=true;}
    else if(st==='dash'){th=1.45;el=.1;w=.05;tx=8;lean=-.3;}
    else if(st==='rec'){th=.7;el=.2;w=.3;}
    else if(st==='stag')lean=.35;
    else{th=.6;el=.6;w=.5;}
  }else if(k==='spear'){th=.6;el=.7;w=.5;
    if(st==='wind'){tx=-9*Math.min(1,e.t/(T_.wind*.5));th=1.4;el=.15;w=0;tele=true;}
    else if(st==='strike'){tx=12;th=1.45;el=.08;w=0;}
    else if(st==='rec'){th=1.2;el=.2;w=.15;}
  }else{
    if(st==='wind'){th=lerp(.4,2.9,Math.min(1,e.t/(T_.wind*.5)));el=.5;w=.25;tele=true;}
    else if(st==='strike'){th=lerp(2.9,.65,Math.min(1,e.t/T_.act));el=.12;w=.25;}
    else if(st==='rec'){th=.65;el=.12;w=.25;}
  }
  if(st==='stun')lean=.3;
  const wk=Math.sin(e.walk),chasing=st==='chase'||st==='dash';
  if(chasing)lean-=k==='husk'?.06:.12;
  if(!e.boss){if(st==='wind')lean+=.16*Math.min(1,e.t/(T_.wind*.6));else if(st==='strike')lean-=.24;else if(st==='rec')lean-=.1*(1-Math.min(1,e.t/T_.rec));}
  const sway=k==='husk'?Math.sin(e.walk*.5)*.11+Math.sin(time*1.3+e.hx)*.03:Math.sin(time*1.1+e.hx)*.02,
    bobY=(st==='chase'||st==='idle')?Math.abs(Math.cos(e.walk))*(k==='brute'?2:1.1):0;
  const awake=st!=='idle'&&st!=='sleep',eyeC=e.boss?'#ff3434':awake?'#ffb040':'#5a4a3a',eyeE=awake||e.boss?1:0;
  const mv=(st==='chase'||st==='idle'||st==='strike'||st==='dash')?1:0;
  push();T(e.x,e.y+e.j,e.z);RY(-e.a);if(fall)RZ(fall);Sc(s*wf,s,s*wf);T(0,bobY,0);
  for(const sd of[-1,1]){const ph=e.walk+(sd>0?0:PI);
    const lw=k==='boss'?1:k==='brute'?1.15:.74;
    push();T(0,20.5,sd*3.3);RZ(Math.sin(ph)*.6);sph(5.2*lw,11.8,5.4*lw,c.dark,0,-5,0);T(0,-9.5,0);RZ(-(.12+.6*Math.max(0,Math.cos(ph))*mv));
    sph(4.3*lw,11.2,4.5*lw,k==='boss'?c.dark:'#3a342e',0,-4.6,0);sph(7*lw,3.4,4.8*lw,c.dark,1.1,-9,0);pop();}
  T(0,20,0);RY(wk*.14*mv+(e.boss?0:yaw*-.3));RZ(lean-(k==='husk'?.36:k==='boss'?0:.16));RX(sway);T(0,-20,0);
  if(k==='boss')fru(17,31,19,c.dark,0,17.5,0);
  else fru(11.5,10,13.4,c.dark,0,21.5,0);
  const bone='#b9ad94',soc='#0b0909',jaw=(st==='wind'||st==='strike')?2.2:.4+Math.sin(time*2+e.hx)*.3;
  if(k!=='boss'){   // rags hanging from the waist, stirred by every step
    for(let q=0;q<7;q++){const a=q/7*6.283+e.hx,l=7+hash(q+e.hx)*8;push();T(Math.cos(a)*5.2,19,Math.sin(a)*6.2);RZ(Math.cos(a)*.18+wk*.1*mv);RX(Math.sin(a)*.18);box(1,l,3.6,q%2?c.dark:c.body,0,-l/2,0);pop();}}
  if(k==='brute'){   // bloated executioner: stitched belly, sack over the head, rope and chain
    sph(17,20,18.5,c.body,1,28,0);sph(12,11,14,c.dark,2.5,23.5,0);
    for(let q=0;q<5;q++)box(.8,2.6,.8,'#1a1412',8.6-Math.abs(q-2)*.5,22+q*3.2,(q%2?1:-1)*.8);
    box(1.2,1.2,19,'#3a2e22',6.5,33,0);
    for(const sd of[-1,1]){sph(9,7,9,'#3c3632',0,37.5,sd*8.6);push();T(0,40.5,sd*8.6);con(3,5.5,3,'#6a625c',0,2,0);pop();
      for(let q=0;q<4;q++)sph(2.2,2.8,1.2,'#57504a',5-q*.4,34-q*3,sd*(7-q*.6));}
    sph(7.4,8.6,7.4,'#7a6a54',.9,42,0);fru(9,6,9,'#6e5f4a',.6,37.6,0);cyl(8.4,1.4,8.4,'#2e241a',.6,38.4,0);
    for(const sd of[-1,1])sph(2,2.6,2.2,soc,5.2,42.8,sd*2.3);
  }else if(k!=='boss'){   // a body long past dying: ribs, a bare skull, a hanging jaw
    sph(8.4,16.5,11.2,c.body,0,29,0);
    for(let q=0;q<4;q++)sph(8.9,1.1,11.6-q*.5,'#6f675a',.5,24.5+q*2.9,0);
    sph(6.9,8.2,6.6,bone,1.3,41.6,0);sph(5,2.8,5,'#a39880',2.6,37.6-jaw,0);
    for(const sd of[-1,1])sph(2.3,2.6,2.3,soc,4.4,42.3,sd*1.9);sph(1.2,1.8,1.3,soc,5.1,40.2,0);
    if(k==='husk'){sph(9.6,10,9.8,c.dark,-1.6,42.8,0);fru(11.5,9,12,c.dark,-2.2,37,0);sph(7.5,23,12.2,c.dark,-4.8,26.5,0);}
    else{sph(9,6.6,8.6,'#55483c',.3,44.8,0);cyl(10.4,1.3,10,'#463a30',.3,43.2,0);box(1.2,5.4,1.6,'#463a30',5.4,42.4,0);
      sph(9.6,12.5,12.2,'#4a4038',.7,30.8,0);box(1,15,8,'#5a1c1c',5.6,26,0);sph(5.4,24,13,c.dark,-4.6,26.5,0);}
  }else if(k==='boss'){sph(12,19.5,14.5,c.body,0,31,0);sph(1.6,7.5,5,'#ff3434',5.2,29.5,0,e.hp<T_.hp*.5?1:.3);
    sph(9,5,17,c.dark,-1,38.5,0);
    for(const sd of[-1,1]){push();T(0,40,sd*8.6);RX(-sd*.5);con(3.4,8,3.4,'#cdbfa6',0,4,0);pop();
      push();T(.6,45,sd*2.6);RX(-sd*.45);fru(3,7,3,'#3a2a2c',0,3.2,0);T(0,6.4,0);RX(-sd*.6);RZ(-.3);con(2.2,8,2.2,'#3a2a2c',0,3.6,0);pop();
      push();T(-5.4,38,sd*4.5);RX(-sd*(.42+Math.sin(time*2.2)*.13+(chasing?.15:0)));sph(2,38,21,c.dark,0,8,sd*10.5);sph(1.4,31,13,'#5a1622',.5,5,sd*18);cyl(1.6,40,1.6,'#2a1418',.8,10,sd*2);pop();}
    sph(7.8,8.8,7.4,'#cdbfa6',.9,42,0);sph(5.2,3.8,5.2,'#b3a388',2.2,38.6,0);
    for(const sd of[-1,1])sph(2,2.4,2.4,'#120a0c',4,42.6,sd*1.9);
  }
  if(k==='boss')for(const sd of[-1,1])sph(1.3,1.6,1.9,eyeC,4.5,42.4,sd*1.85,eyeE);
  else for(const sd of[-1,1])sph(.95,.95,.95,awake?'#ff5a1e':'#2a1c16',k==='brute'?5.9:5.2,k==='brute'?42.8:42.3,sd*(k==='brute'?2.3:1.9),awake?1:0);

  // off arm
  mobArm(-1,c.body,(k==='husk'?.55:.25)+wk*.35,k==='husk'?.7:.4,0,0,0,e.k==='archer'?()=>{push();RX(PI/2);RZ(PI/2);bowW();pop();}:k==='spear'?()=>{push();T(1.5,2,-3);RX(PI/2);cyl(14.5,1.6,14.5,'#8a7a52',0,0,0);sph(5,3.4,5,'#b8a86a',0,1,0);pop();}:null);
  // weapon arm
  mobArm(1,c.body,th-(chasing&&!tele?wk*.25:0),el,w,yaw,tx,()=>{
    if(e.k==='archer'){}
    else if(e.k==='sling'){cyl(.7,15,.7,'#3a2c20',0,-7,0);sph(4.5,4.5,4.5,'#8a8478',0,-15,0);}
    else if(k==='husk'){cyl(1.5,7,1.5,'#2a201a',0,-1,0);box(6.5,1.3,1.6,'#4a3c30',0,-4.6,0);box(3.3,17,.9,'#6b5a4c',0,-13.5,0);box(2.4,6,.9,'#74685c',.4,-24.5,0);}
    else if(k==='spear'){cyl(1.4,64,1.4,'#7a6650',0,-15,0);push();T(0,-51,0);RZ(PI);con(3.2,10,1.3,'#dadee6',0,0,0);pop();}
    else if(k==='brute'){cyl(2.6,23,2.6,'#5a4a3c',0,-10.5,0);sph(9.5,9.5,9.5,'#4a4440',0,-24,0);sph(13.5,3.2,3.2,'#6a625c',0,-24,0);sph(3.2,3.2,13.5,'#6a625c',0,-24,0);sph(3.2,13.5,3.2,'#6a625c',0,-24,0);}
    else glaiveW();});
  if(tele&&!e.dead){const q=1+.3*Math.sin(e.t*18);push();T(0,57,0);RY(time*6);con(4.5*q,6*q,4.5*q,'#ff3030',0,3*q,0,1);RZ(PI);con(4.5*q,6*q,4.5*q,'#ff3030',0,3*q,0,1);pop();}
  pop();gFlash=false;gAlpha=1;
}

/* ---------- mountain wolf ---------- */
function mobFx(e,life){gAlpha=1;if(e.dead){if(e.t>life)return -1;gAlpha=Math.max(0,1-e.t/life);gFlash=false;return Math.min(1,e.t/.35)*1.45;}gFlash=e.flash>0;return 0;}
function drawWolf(e){
  if(Math.hypot(e.x-P.x,e.z-P.z)>DR)return;const fall=mobFx(e,1.1);if(fall<0)return;
  const st=e.state,run=(st==='chase'||st==='strike'||st==='idle')?1:0,wk=e.walk*1.7,c='#47474e',d='#232327',awake=st!=='idle';
  const leap=st==='strike'?11*Math.sin(Math.min(1,e.t/e.T.act)*PI):0,crouch=st==='wind'?-4*Math.min(1,e.t/.2):0;
  push();T(e.x,e.y+e.j,e.z);RY(-e.a);if(fall)RX(fall);if(e.T.sc)Sc(e.T.sc,e.T.sc,e.T.sc);
  T(0,leap+crouch+Math.abs(Math.sin(wk))*1.2*run,0);RZ(st==='wind'?.16:st==='strike'?-.22:st==='stun'?.3:0);
  sph(36,14,12.5,c,0,19,0);sph(20,18,14.5,d,10,21.5,0);sph(16,12,11.5,c,-13,18.5,0);
  for(let q=0;q<7;q++){push();T(15-q*5,27-q*.7,0);RZ(.5);con(4,7.5-q*.5,3,d,0,3,0);pop();}
  for(let q=0;q<3;q++)sph(13,.9,13.2,'#6a5f58',-4+q*4,17,0);
  push();T(20,26,0);RZ(st==='wind'?-.35:Math.sin(time*1.5+e.hx)*.06);
  sph(13,11,11,c,0,0,0);sph(11.5,6,6.5,d,8,-1.6,0);sph(2.6,2.6,2.6,'#111114',13.8,-1,0);
  for(const sd of[-1,1]){push();T(-1,5,sd*3.8);con(4.2,8,3.2,d,0,3.5,0);pop();sph(1.7,1.7,1.7,awake?'#ff3a20':'#3a2420',5.4,2,sd*3.7,awake?1:0);}
  if(st==='wind'||st==='strike'){sph(9.5,2.6,5.4,'#7a1c1c',7,-5.2,0);for(const sd of[-1,1])con(1.3,3,1.3,'#f0ead8',10,-3.4,sd*1.8);}
  pop();
  for(const fx of[11,-11])for(const sd of[-1,1]){const ph=wk+(fx>0?0:PI)+(sd>0?0:1.5);
    push();T(fx,15,sd*4.6);RZ(Math.sin(ph)*.75*run+(st==='strike'?(fx>0?.9:-.9):0));sph(4.2,10,4.2,d,0,-4.5,0);T(0,-8,0);RZ(-.3-.4*Math.max(0,Math.cos(ph))*run);sph(3.4,9,3.4,d,0,-4,0);pop();}
  push();T(-18,22,0);RZ(-.55+Math.sin(time*6+e.hx)*.16*(awake?1:.3));sph(17,4.6,4.6,d,-8,0,0);pop();
  if(st==='wind'){const q=1+.3*Math.sin(e.t*18);push();T(8,44,0);RY(time*6);con(4*q,5.5*q,4*q,'#ff3030',0,2.7*q,0,1);RZ(PI);con(4*q,5.5*q,4*q,'#ff3030',0,2.7*q,0,1);pop();}
  pop();gFlash=false;gAlpha=1;
}
/* ---------- Anzu: lion head, eagle body ---------- */
function drawAnzu(e){
  const fall=mobFx(e,1.6);if(fall<0)return;
  const st=e.state,fly=e.j>4||st==='fly'||st==='dive',ph2=e.hp<e.T.hp*.5;
  const flap=fly?Math.sin(time*9)*.75:st==='gustW'?.95*Math.min(1,e.t/.5):st==='gust'?lerp(.95,-.7,Math.min(1,e.t/.18)):st==='intro'||st==='storm'?.55+Math.sin(time*5)*.1:Math.sin(time*2)*.06,
    open=fly||st==='gustW'||st==='gust'||st==='intro'||st==='storm',wl=open?1:.5,
    pitch=st==='dive'?-.95:st==='claw'?-.32:st==='clawW'?.25:st==='stag'?.3:st==='fly'?-.15:0,roar=st==='intro'||st==='gustW'||st==='storm'||st==='clawW';
  const bodyC='#3a3432',chest='#4d443c',dark='#1f1b1c',mane='#141012',wingC='#2f2d36',tip='#14131a',bone='#cfc6b0',hide='#4a3f38',glow=ph2?'#9ad8ff':'#ffb040';
  push();T(e.x,e.y+e.j,e.z);RY(-e.a);if(fall)RX(fall);Sc(1.55,1.55,1.55);
  // feathered thighs, scaled shanks, hooked talons
  for(const sd of[-1,1]){const sw=fly?.9:Math.sin(e.walk+(sd>0?0:PI))*.5;push();T(2,31,sd*11);RZ(sw);sph(12,22,12,dark,0,-7,0);T(0,-16,0);RZ(fly?-1.2:-sw*.6);cyl(4.6,15,4.6,'#7d6f48',0,-7,0);
    for(const tz of[-4.5,0,4.5]){push();T(3,-14,tz);RZ(-1.15);con(3,12,3,'#0e0c0c',0,5,0);pop();}push();T(-2,-14,0);RZ(1.3);con(2.6,8,2.6,'#0e0c0c',0,3.5,0);pop();pop();}
  T(0,36,0);RZ(pitch);
  sph(52,30,31,bodyC,0,0,0);sph(31,26,27,dark,-18,4,0);sph(30,34,32,chest,16,4,0);
  for(let q=0;q<7;q++)sph(13,3,31-q*1.6,q%2?dark:'#2a2526',-26+q*8,14-q*.5,0);
  for(let q=0;q<5;q++)sph(9,2.6,24,'#5a5046',22+q*1.2,-8+q*4.2,0);
  if(ph2){sph(9,11,13,'#9ad8ff',23.5,2,0,1);for(let q=0;q<5;q++){const a=time*7+q*2.1;box(1,1,9+6*Math.sin(a*1.7),'#dff2ff',Math.cos(a)*30,10+Math.sin(a*1.3)*14,Math.sin(a)*24,1,.85);}}
  // tail: long dark feathers with pale bars
  for(const tz of[-2,-1,0,1,2]){push();T(-32,5,tz*5);RY(tz*.13);RZ(.2+Math.sin(time*2+tz)*.03);sph(46,3,8.6,tip,-22,0,0);sph(9,3.3,9,'#62606a',-36,0,0);pop();}
  // wings: bone, coverts, secondaries, splayed primaries
  for(const sd of[-1,1]){push();T(-4,14,sd*13);RX(-sd*flap);if(!open)RY(sd*.95);
    sph(12,7.5,62*wl,chest,5,1,sd*30*wl);sph(36,5.2,76*wl,wingC,-4,0,sd*40*wl);sph(32,3.6,72*wl,tip,-17,-1.6,sd*54*wl);
    for(let q=0;q<5;q++)sph(9,2,30*wl,'#3a3842',-6-q*6,1.2,sd*(22+q*9)*wl);
    for(let q=0;q<7;q++){push();T(-1-q*5.2,-2,sd*(84+q*2)*wl);RY(sd*(.08+q*.075));sph(8.6,2.2,48*wl,q%2?tip:wingC,0,0,sd*19*wl);if(ph2)sph(2.6,2.6,2.6,'#9ad8ff',0,0,sd*42*wl,1);pop();}
    pop();}
  // lion head: spiked mane, heavy brow, fangs, horns
  push();T(33,22,0);RZ(roar?.42:Math.sin(time*1.3)*.05);
  sph(34,32,33,mane,-7,1,0);
  for(let q=0;q<11;q++){const a=q/11*6.283;push();T(-9,1,0);RX(a);T(0,13.5,0);RZ(.35);con(8.5,19,8.5,q%3?'#1e181a':'#3a3230',0,7,0);pop();}
  sph(22,21,20,hide,2,0,0);sph(8,5.4,19.5,dark,8.4,6.4,0);sph(14,10,13.5,'#5a4c42',11,-3,0);sph(4.6,3.6,5.6,'#0e0c0c',17.4,-1.2,0);
  sph(12,roar?8.5:3,10.6,'#5a1010',10,-9-(roar?3:0),0);
  for(const sd of[-1,1]){push();T(14.6,-4.6,sd*3.3);RZ(PI);con(2.3,8.5,2.3,bone,0,4,0);pop();if(roar)con(1.9,5.5,1.9,bone,13,-13.5,sd*2.7);
    sph(4,2.6,4,glow,9.4,4.4,sd*6.5,1);sph(1.4,2.2,1.6,'#0a0808',10.9,4.4,sd*6.6);
    push();T(-2,12,sd*8.5);RX(-sd*.55);RZ(.55);con(5.6,19,5.6,bone,0,9,0);T(0,16,0);RZ(.75);con(3.6,13,3.6,'#a89e88',0,5.5,0);pop();}
  pop();
  pop();gFlash=false;gAlpha=1;
}
/* ---------- people of the road ---------- */
function drawNPC(n){
  if(Math.hypot(n.x-P.x,n.z-P.z)>DR)return;gAlpha=1;gFlash=false;
  const L=n.look,br=Math.sin(time*1.6+n.x)*.4;
  push();T(n.x,n.y,n.z);RY(-n.a);
  fru(16,32,18,L.robe,0,16,0);sph(8,4,5,'#2c1e17',3,2,-4);sph(8,4,5,'#2c1e17',3,2,4);
  T(0,br,0);
  sph(11.5,18,14.5,L.vest,0,37,0);cyl(12,3.2,15,L.sash,0,30,0);
  for(const sd of[-1,1]){const hold=sd>0&&L.staff;push();T(0,41,sd*7.6);sph(5.4,5.4,5.4,L.vest,0,0,0);RZ(hold?.85:.12+Math.sin(time*1.1+sd)*.03);
    sph(4.5,10.5,4.5,L.vest,0,-4.6,0);T(0,-9,0);RZ(hold?.5:.3);sph(3.9,9.4,3.9,L.vest,0,-4.2,0);sph(3.5,3.8,3.5,L.skin,0,-9.2,0);
    if(hold){T(0,-9.4,0);RZ(-1.3);cyl(2,64,2,'#5a3e28',0,5,0);sph(4,4,4,'#4a3018',0,37,0);}pop();}
  cyl(4.6,4.4,4.6,L.skin,.3,46,0);
  const hy=51;sph(8.8,10,8.4,L.skin,.7,hy,0);sph(2.3,3,2.1,L.skin,5.2,hy-.5,0);
  if(L.beard){sph(7.6,8.4,8.2,L.beard,2.2,hy-4.2,0);sph(1.7,1.4,5.6,L.beard,5,hy-2,0);}
  if(L.blind)box(1,2.6,9.4,'#d8d0c0',4.6,hy+1.2,0);
  else for(const sd of[-1,1]){sph(1.2,1.4,2,'#f4efe6',4.6,hy+1,sd*1.95);sph(.9,1.1,1.2,'#120c0a',5,hy+1,sd*1.95);push();T(4.7,hy+2.7,sd*1.95);RX(sd*.18);box(.9,.8,2.8,L.beard||'#2a1c14',0,0,0);pop();}
  sph(2.6,.8,3,'#6a3a30',5,hy-3.6,0);
  if(L.hat==='turban'){sph(12.6,5.4,12.2,L.hatC,-.3,hy+3.2,0);sph(11,6.5,10.6,L.hatC,-.3,hy+5,0);}
  else if(L.hat==='scarf'){sph(11,11,10.8,L.hatC,-1,hy+1,0);fru(13,15,13,L.hatC,-2.8,hy-4.5,0);}
  else{sph(11.4,11.4,11,L.hatC,-1.2,hy+1.4,0);fru(14,16,14,L.hatC,-3,hy-4.5,0);}
  pop();
}
