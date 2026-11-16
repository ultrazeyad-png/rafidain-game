/* ================= bosses: warrior lords in the souls manner ================= */
/* Namtar, herald of the underworld: a giant in bronze with a great axe.
   Phase one reads clean: diagonal cuts in a figure eight, a slam held back to bait your roll, a spin when you sit behind him.
   Phase two (half health): he roars, his cape catches black fire, and everything gets faster, longer and meaner:
   a third cut, a grab, a stomp that sends rings of force, a shoulder charge. */
const NM={c1:{w:.62,a:.2,r:.32,key:'A',reach:96,arc:1.25,dmg:28,step:120},
          c2:{w:.42,a:.2,r:.55,key:'B',reach:96,arc:1.25,dmg:30,step:140},
          c3:{w:.75,a:.22,r:.8,key:'C',reach:104,arc:.8,dmg:40,step:150},
          sl:{w:.6,a:.16,r:1.0,key:'H',reach:108,arc:.75,dmg:44,step:90},
          sp:{w:.62,a:.42,r:.9,key:'X',reach:118,arc:3.2,dmg:30,step:40}};
function arY(){return REG==='muthanna'?AY:AC.y;}
function arClamp(x,z,m){if(REG==='muthanna')return [clamp(x,AX0+m,AX1-m),clamp(z,AZ0+m,AZ1-m)];return inArena(x,z,m);}
function bossHalf(b){return b.hp<b.T.hp*.5;}
function upNamtar(b,dt){
  const dx=P.x-b.x,dz=P.z-b.z,ad=Math.hypot(dx,dz),ang=Math.atan2(dz,dx),ph2=b.p2,sp=ph2?1.18:1,alive=P.state!=='dead',behind=adiff(ang,b.a)>1.9,isAdapa=!!(b.T&&b.T.name&&b.T.name.includes('أدابا'));
  b.cd-=dt;let mvs=0;
  if(!b.p2&&bossHalf(b)&&b.state!=='stag'&&b.state!=='grab'&&b.state!=='parried'&&!b.j){b.p2=true;setE(b,'trans');snd('boss');snd('roar');if(isAdapa){voice('boss','المفاتيح الخمسة ما تنطيك بغداد!');say('المفاتيح الخمسة ما تنطيك بغداد!','#c0242e',2.6);}return;}
  const atk=(m,next)=>{const A=NM[m];if(isAdapa&&!b.vsaid&&['c3','sp','sl'].includes(m)){b.vsaid=true;voice('boss',m==='sl'?'اركع أمام باب الطلسم!':m==='sp'?'لا تدير ظهرك إليّ!':'هذا حدّك يا فرات!');}if(b.t<A.w/sp){b.a=turn(b.a,ang,(b.t<A.w*.6?5:1.5)*dt);mvs=b.t>A.w*.7/sp?A.step*.25:0;return;}
    const k=b.t-A.w/sp;if(k<A.a){mvs=A.step;if(!b.hit&&hitP(b,A.reach,A.arc,A.dmg))b.hit=true;if(k<dt*1.5)snd('swingH');return;}
    if(k>=A.a+A.r/sp){if(next){setE(b,next);}else bRec(b,ph2?.3:.55);}};
  switch(b.state){
    case 'sleep':if(world.locked)setE(b,'intro');break;
    case 'intro':b.a=turn(b.a,ang,3*dt);if(b.t>1.4){if(isAdapa)voice('boss','أنا أدابا… وحارس هذا الباب.');setE(b,'chase');b.cd=.5;}break;
    case 'trans':b.a=turn(b.a,ang,2*dt);shake=Math.max(shake,b.t<1.6?4:0);
      if(Math.random()<dt*40)parts.push({x:b.x+(Math.random()-.5)*60,y:b.y+Math.random()*150,z:b.z+(Math.random()-.5)*60,vx:(Math.random()-.5)*60,vz:(Math.random()-.5)*60,vy:60+Math.random()*80,l:.9,m:.9,col:Math.random()<.5?'#2a0c14':'#c0242e',s:2.6,g:-30,f:-999});
      if(b.t>.9&&!b.hit){b.hit=true;elemHit(b,5);waves.push({x:b.x,z:b.z,y:arY(),r:30,l:1.2});if(Math.hypot(dx,dz)<120)hurtP(Math.round(18*(b.dm||1)),b.x,b.z);}
      if(b.t>2.3){setE(b,'chase');b.cd=.2;say(b.k==='erra'?'العاصفة تنزل ويا إرّا':b.k==='boss'?'نمتار… يغضب':b.T.name.split('،')[0]+' يغضب',ELC[b.elem]||'#c0242e',2.4);}break;
    case 'chase':
      if(!alive)break;
      b.a=turn(b.a,ang,(ad<160?4.5:6)*dt);
      if(ad>110)mvs=ph2?110:78;else if(ad<70)mvs=-50;else{b.sx=(b.sx||1);mvs=0;const sa=b.a+PI/2*b.sx;moveBossBy(b,Math.cos(sa)*44*dt,Math.sin(sa)*44*dt);if(Math.random()<dt*.6)b.sx=-b.sx;}
      if(b.cd<=0){const r=Math.random();
        if(behind&&ad<130)setE(b,'sp');
        else if(ad<130){if(ph2&&r<.18)setE(b,'grab');else if(r<.55)setE(b,'c1');else if(r<.82)setE(b,'sl');else setE(b,'sp');b.hold=.25+Math.random()*(ph2?.85:.55);}
        else if(ad<360){if(ph2&&r<.35)setE(b,'stomp');else if(ph2&&r<.6)setE(b,'charge');else if(r<.6){setE(b,'leapW');}else b.cd=.25;}
        else if(r<.5)setE(b,'leapW');else b.cd=.3;}
      break;
    case 'c1':atk('c1','c2');break;
    case 'c2':atk('c2',ph2?'c3':null);break;
    case 'c3':atk('c3',null);break;
    case 'sp':atk('sp',null);break;
    case 'sl':{   // overhead slam: the axe hangs at the top for a moment that changes every time
      const A=NM.sl;if(b.t<A.w/sp){b.a=turn(b.a,ang,5*dt);break;}
      if(b.t<A.w/sp+b.hold){b.a=turn(b.a,ang,1.2*dt);break;}
      const k=b.t-A.w/sp-b.hold;if(k<A.a){mvs=A.step;if(k<dt*1.5)snd('swingH');
        if(!b.hit&&k>A.a*.7){b.hit=true;const hx=b.x+Math.cos(b.a)*80,hz=b.z+Math.sin(b.a)*80;shake=10;snd('land');burst(hx,b.y+3,hz,'#8a7a70',16,160,b.y);puff(hx,b.y,hz,8,12,80);
          if(Math.hypot(P.x-hx,P.z-hz)<64&&P.y-b.y<24)hurtP(Math.round(A.dmg*(b.dm||1)),b.x,b.z);if(ph2)waves.push({x:hx,z:hz,y:arY(),r:20,l:1.1});}break;}
      if(k>=A.a+A.r)bRec(b,ph2?.35:.6);break;}
    case 'leapW':b.a=turn(b.a,ang,6*dt);if(b.t>=.55/sp){setE(b,'leap');b.sx0=b.x;b.sz0=b.z;{const q=arClamp(P.x,P.z,40);b.tx=q[0];b.tz=q[1];}snd('swingH');}break;
    case 'leap':{const k=Math.min(1,b.t/.7);b.x=lerp(b.sx0,b.tx,k);b.z=lerp(b.sz0,b.tz,k);b.j=Math.sin(PI*k)*110;
      if(k>=1){b.j=0;shake=13;snd('boss');burst(b.x,arY()+4,b.z,'#8a7a70',18,170,arY());puff(b.x,arY(),b.z,12,14,110);
        if(Math.hypot(P.x-b.x,P.z-b.z)<100&&P.y-arY()<20)hurtP(Math.round(38*(b.dm||1)),b.x,b.z);if(ph2)elemHit(b,3);if(ph2)waves.push({x:b.x,z:b.z,y:arY(),r:30,l:1.25});bRec(b,ph2?.7:1.05);}
      break;}
    case 'stomp':b.a=turn(b.a,ang,2*dt);
      if(b.elem&&b.elem!=='shadow'){if(b.t>.5&&b.n<1){b.n=1;elemHit(b,6);}if(b.t>1.6){b.n=0;bRec(b,.5);}break;}
      if(b.t>.75&&b.n<1){b.n=1;shake=9;snd('land');waves.push({x:b.x,z:b.z,y:arY(),r:30,l:1.25});puff(b.x,b.y,b.z,10,12,120);}
      if(b.t>1.35&&b.n<2){b.n=2;shake=9;snd('land');waves.push({x:b.x,z:b.z,y:arY(),r:30,l:1.25});}
      if(b.t>1.9){b.n=0;bRec(b,.5);}break;
    case 'charge':if(b.t<.6){b.a=turn(b.a,ang,6*dt);break;}
      if(b.t<.6+dt*1.5)snd('swingH');b.walk+=dt*22;moveBossBy(b,Math.cos(b.a)*520*dt,Math.sin(b.a)*520*dt);
      if(!b.hit&&ad<b.T.r+P.r+16&&hurtP(Math.round(34*(b.dm||1)),b.x,b.z)){b.hit=true;P.kbs=420;}
      if(b.t>=1.05)bRec(b,.9);break;
    case 'grab':   // a short reach with the open hand; if it lands he lifts you and breaks you on the ground
      if(b.t<.5){b.a=turn(b.a,ang,6*dt);break;}
      if(b.t<.72){mvs=170;if(!b.hit&&ad<b.T.r+P.r+30&&adiff(ang,b.a)<.7&&P.state!=='roll'&&P.inv<=0&&P.state!=='dead'){b.hit=true;setE(b,'held');setP('grabbed');P.grab=b;snd('hurt');}break;}
      if(b.t>1.3)bRec(b,.8);break;
    case 'held':if(b.t>1.5){P.inv=0;P.grab=null;setP('idle');hurtP(Math.round(52*(b.dm||1)),b.x,b.z);P.kbs=300;shake=14;snd('land');burst(P.x,P.y+5,P.z,'#8a7a70',16,150,P.y);bRec(b,.9);}break;
    case 'rec':if(b.t>=b.recT){setE(b,'chase');b.cd=ph2?.25:.6;}break;
    case 'stag':if(b.t>=2.4){setE(b,'chase');b.cd=.2;}break;
    case 'parried':b.a=turn(b.a,ang,2*dt);if(b.t>=.72){setE(b,'chase');b.cd=.55;}break;
  }
  if(mvs){const ca=mvs<0?b.a:b.a;moveBossBy(b,Math.cos(ca)*mvs*dt,Math.sin(ca)*mvs*dt);}
  {const q=arClamp(b.x,b.z,30);b.x=q[0];b.z=q[1];}b.y=arY();
  if(ph2&&Math.random()<dt*10)parts.push({x:b.x+(Math.random()-.5)*50,y:arY()+b.j+60+Math.random()*90,z:b.z+(Math.random()-.5)*40,vx:0,vz:0,vy:40,l:.8,m:.8,col:b.elem&&b.elem!=='shadow'?(ELC[b.elem]||'#bfe4ff'):'#1a0a10',s:b.elem&&b.elem!=='shadow'?2:3,g:-20,f:-999});
}
function moveBossBy(b,mx,mz){const nx=b.x+mx,nz=b.z+mz;if(Math.hypot(P.x-nx,P.z-nz)>=P.r+b.T.r-1||P.state==='roll'){b.x=nx;b.z=nz;b.walk+=Math.hypot(mx,mz)*.08;}}

/* the giant himself: built on the same skeleton as the hero, at twice the size, in old bronze */
function namArm(side,p,item,col){
  push();T(p.tx||0,40.5,side*9.2);sph(9,8,9.5,'#5a4630',0,1,0);con(5,7,5,'#3a2c1e',0,6,0);RY(-side*(p.y||0));RZ(p.t);if(p.r)RY(-side*p.r);
  sph(5.4,11.5,5.4,col,0,-4.6,0);cyl(6.2,3,6.2,'#5a4630',0,-8.5,0);T(0,-9,0);RZ(p.e==null?.3:p.e);
  sph(5,10.5,5,'#4a3a28',0,-4.2,0);cyl(5.4,4,5.4,'#6a5236',0,-7,0);sph(4.4,4.6,4.4,'#2a201a',0,-9.6,0);
  if(item){T(0,-9.6,0);RZ(p.w);item();}
  pop();}
function namAxe(){cyl(2.1,70,2.1,'#2a1c14',0,-14,0);for(let i=0;i<4;i++)cyl(2.5,1,2.5,'#6a5236',0,12-i*14,0);
  push();T(0,-44,0);box(4.4,10,3.2,'#4a443e',1.6,0,0);
  sph(15,19,2,'#5a5650',8,0,0);sph(13.5,17,1.5,'#8a8680',10,0,0);box(.9,17,2.4,'#c8ccd2',16.4,0,0);
  for(const s of[-1,1]){push();T(8,s*8,0);RZ(s*.4);box(6,2.2,2,'#3a3430',0,0,0);pop();}
  con(3,9,3,'#4a443e',-4,0,0);pop();con(2.4,7,2.4,'#4a443e',0,-50,0);}
function adapaSword(){
  // ceremonial straight blade with a gold crossguard and lapis pommel
  cyl(2.1,17,2.1,'#4a3020',0,0,0);
  for(let i=0;i<3;i++)cyl(2.6,1,2.6,'#d8ad54',0,-5+i*4,0);
  box(18,3.2,3.2,'#d8ad54',0,-9,0);
  box(7.2,38,2.4,'#cfd5d5',0,-29,0);
  box(1.4,34,1.1,'#fff4d8',-1.6,-29,1.5);
  box(5.2,29,1.4,'#8f9b9d',0,-28,-1.4);
  con(3.7,10,2.2,'#e4e0d1',0,-53,0);
  sph(3.2,3.2,3.2,'#287f91',0,10,0);
}
function adapaArmBlend(a,b,k){return {t:lerp(a.t,b.t,k),y:lerp(a.y,b.y,k),e:lerp(a.e,b.e,k),w:lerp(a.w,b.w,k),r:lerp(a.r||0,b.r||0,k)};}
function adapaSwordPose(state,u){
  // Three readable beats: measure, commit, then recover into the royal guard.
  const guard={t:.82,y:-.32,e:.98,w:-.2,r:0};
  const wind={c1:{t:1.62,y:.38,e:.08,w:.86,r:-.35},c2:{t:1.55,y:-.62,e:.12,w:.78,r:.3},c3:{t:1.82,y:-.08,e:.02,w:.9,r:0},sp:{t:1.45,y:-.9,e:.22,w:.75,r:-.45},sl:{t:1.95,y:.05,e:.02,w:1.05,r:0}};
  const strike={c1:{t:.42,y:-.82,e:1.82,w:.92,r:.42},c2:{t:.48,y:.88,e:1.78,w:-.9,r:-.42},c3:{t:.2,y:-.05,e:2.05,w:.5,r:0},sp:{t:.95,y:.98,e:1.65,w:-.95,r:.65},sl:{t:.2,y:.05,e:2.2,w:.9,r:0}};
  const recover={c1:{t:1.05,y:-.22,e:.62,w:.25,r:.1},c2:{t:1.02,y:.22,e:.65,w:-.22,r:-.1},c3:{t:1.12,y:-.12,e:.55,w:.2,r:0},sp:{t:.9,y:-.18,e:.78,w:.2,r:0},sl:{t:1.18,y:0,e:.48,w:.1,r:0}};
  const k=clamp(u-1,0,1);
  if(u<1)return adapaArmBlend(guard,wind[state]||wind.c1,u);
  if(u<2)return adapaArmBlend(wind[state]||wind.c1,strike[state]||strike.c1,k);
  return adapaArmBlend(strike[state]||strike.c1,recover[state]||guard,clamp((u-2),0,1));
}
function drawNamtar(e){
  const fall=mobFx(e,2.2);if(fall<0)return;
  const st=e.state,p2=e.p2,isAdapa=!!(e.T&&e.T.name&&e.T.name.includes('أدابا'));
  const body=isAdapa?'#d9d0bd':'#4e3e2c',dark=isAdapa?'#30251f':'#241a12',
    cape=isAdapa?(p2?'#351018':'#641d25'):(p2?'#120708':'#3a1218'),
    trim=isAdapa?'#d8ad54':'#8a7045',plate=isAdapa?'#eee5d2':'#5e4a32';
  let key=null,u=0,lean=0,crouch=0;
  const A=NM[st];
  if(A){const sp=p2?1.18:1;key=A.key;
    if(st==='sl'){const w=A.w/sp,h=e.hold||0,k=e.t-w-h;u=e.t<w?e.t/w:e.t<w+h?1:k<A.a?1+k/A.a:Math.min(3,2+(k-A.a)/A.r);}
    else{const w=A.w/sp;u=e.t<w?e.t/w:e.t<w+A.a?1+(e.t-w)/A.a:Math.min(3,2+(e.t-w-A.a)/(A.r/sp));}}
  else if(st==='leapW'){key='H';u=Math.min(1,e.t/.55)*.9;crouch=-6*Math.min(1,e.t/.4);}
  else if(st==='leap'){key='H';u=e.t<.55?1:1+Math.min(1,(e.t-.55)/.15);}
  else if(st==='stomp'){key=null;lean=-.1;}
  else if(st==='charge'){lean=e.t<.6?.15:-.4;}
  else if(st==='stag'){lean=.6;crouch=-14;}
  else if(st==='parried'){lean=.62;crouch=-10;}
  else if(st==='trans'){lean=-.35+Math.sin(e.t*9)*.04;}
  else if(st==='grab'||st==='held'){lean=st==='grab'?-.25:-.05;}
  const p=key?keyPose(key,u):{t:.45,y:.25,e:.4,w:-.25,r:0,ln:0};if(key)lean+=p.ln;
  let m=p,o={t:p.t-.25,y:p.y-.35,e:p.e+.45,w:0,r:p.r};
  if(st==='grab'){const k=clamp(e.t/.5,0,1);o={t:lerp(.4,1.55,k),y:-.2,e:lerp(.8,.1,k),w:0};m={t:.5,y:.4,e:.5,w:.6};}
  if(st==='held'){o={t:2.4,y:-.35,e:.25,w:0};m={t:.5,y:.4,e:.5,w:.6};}
  if(st==='trans'){m={t:2.7,y:.6,e:.4,w:.4};o={t:2.6,y:.6,e:.4,w:0};}
  if(st==='stag'){m={t:.2,y:.3,e:.3,w:.9};o={t:.6,y:0,e:.4,w:0};}
  // Adapa has a measured royal guard stance, not Namtar's heavy axe posture.
  if(isAdapa&&st==='sleep'){
    const breath=Math.sin(time*1.35);
    lean=breath*.018;
    m={t:.92+breath*.025,y:-.18,e:.72,w:-.12};
    o={t:.38-breath*.018,y:.16,e:.78,w:.12};
  }else if(isAdapa&&st==='intro'){
    const rise=clamp(e.t/1.4,0,1),breath=Math.sin(time*1.5);
    lean=-.025+breath*.012;
    m={t:lerp(.92,1.65,rise),y:lerp(-.18,.12,rise),e:lerp(.72,.12,rise),w:lerp(-.12,.55,rise)};
    o={t:lerp(.38,1.05,rise),y:lerp(.16,-.28,rise),e:lerp(.78,.3,rise),w:.18};
  }else if(isAdapa&&st==='chase'){
    const breath=Math.sin(time*2.1);
    lean=-.035+breath*.012;
    m={t:.78+breath*.025,y:-.32,e:.98,w:-.2};
    o={t:.62,y:.3,e:.82,w:.18};
  }else if(isAdapa&&st==='trans'){
    const rise=clamp(e.t/2.3,0,1),pulse=Math.sin(time*12);
    lean=-.12-rise*.16;
    m={t:lerp(1.3,2.9,rise),y:lerp(-.1,.25,rise),e:lerp(.5,.08,rise),w:lerp(.2,.85,rise)};
    o={t:lerp(.8,2.35,rise),y:lerp(.1,-.45,rise),e:lerp(.6,.18,rise),w:.2};
    crouch=-pulse*rise*1.2;
  }else if(isAdapa&&st==='parried'){
    const recoil=1-clamp(e.t/.72,0,1);
    m={t:lerp(.82,1.42,recoil),y:lerp(-.32,.82,recoil),e:lerp(.98,.16,recoil),w:lerp(-.2,.95,recoil)};
    o={t:lerp(.62,1.05,recoil),y:-.48,e:lerp(.82,.22,recoil),w:.1};
    lean=.48*recoil;
  }else if(isAdapa&&st==='rec'){
    const settle=clamp(e.t/.65,0,1);
    m={t:lerp(1.3,.82,settle),y:-.24,e:lerp(.3,.9,settle),w:lerp(.4,-.18,settle)};
    o={t:.55,y:.22,e:.8,w:.1};
  }else if(isAdapa&&['c1','c2','c3','sp','sl'].includes(st)){
    const attack=adapaSwordPose(st,u),counter=clamp((u-.55)/.8,0,1);
    m=attack;
    o=adapaArmBlend({t:.62,y:.3,e:.82,w:.18,r:0},
      {t:.98,y:st==='c2'?.5:-.52,e:1.15,w:st==='c2'?.45:-.45,r:0},counter);
    lean=lerp(-.035,st==='sl'?.22:st==='c3'?.12:-.08,clamp(u/2,0,1));
  }
  const mvk=(st==='chase'||st==='charge')?1:0,wk=e.walk;
  push();T(e.x,e.y+e.j,e.z);RY(-e.a);if(fall)RZ(fall);Sc(2.15,2.15,2.15);T(0,crouch*.4,0);
  // legs: greaves and sabatons
  for(const sd of[-1,1]){const ph=wk+(sd>0?0:PI);let hip=Math.sin(ph)*.5*mvk+(st==='stag'&&sd>0?1.4:0),knee=.1+.7*Math.max(0,Math.cos(ph))*mvk+(st==='stag'&&sd>0?1.7:0);
    if(st==='stomp'&&sd>0){const s=e.t%(.6);hip=s<.45?1.1*s/.45:0;knee=s<.45?.9:0;}
    push();T(0,24.4,sd*4.6);RZ(hip);sph(7,14,7.4,dark,0,-6,0);T(0,-11.5,0);RZ(-knee);
    sph(6,12.8,6.4,body,0,-5,0);sph(6.8,4,7,'#6a5236',1,0,0);T(0,-10.3,0);RZ(knee-hip);sph(10,5,7,dark,1.8,0,0);pop();}
  // skirt of plates
  for(let q=0;q<(isAdapa?10:8);q++){const a=q/(isAdapa?10:8)*6.283;push();T(Math.cos(a)*7,22,Math.sin(a)*8.6);RX(Math.sin(a)*.25);RZ(-Math.cos(a)*.25);box(isAdapa?1.8:1.4,isAdapa?18:15,isAdapa?7:6.4,q%2?body:(isAdapa?trim:'#5a4630'),0,-6,0);pop();}
  push();T(0,30,0);RZ(lean);T(0,-30,0);
  // cape: tattered, burning black in the second phase
  push();T(-8,46,0);RZ(-.22-Math.sin(time*2.2)*.05-mvk*.2);
  for(let q=0;q<5;q++){const l=38+hash(q*3.1)*14;box(2,l,7.4,cape,0,-l/2,(q-2)*7);if(p2)box(2.2,6,7.6,'#c0242e',.2,-l+3,(q-2)*7,1,.8);}pop();
  // breastplate, belly plates, pauldrons
  sph(14,18,17.5,body,0,36.5,0);sph(9.6,12,14,plate,3.6,38,0);
  for(let q=0;q<3;q++)cyl(14.4-q,.9,17.6-q,isAdapa?trim:'#2a201a',0,27.5+q*3.4,0);
  cyl(15,4,18,isAdapa?trim:'#3a2c1e',0,24,0);sph(4,4.4,4,isAdapa?'#f7d77c':'#a88a4a',6.6,24,0);
  if(isAdapa){
    // layered lamellar plates: five rows of individually raised bronze-and-ivory scales
    for(let row=0;row<3;row++)for(let col=-2;col<=2;col++){
      const x=col*4.15,y=42-row*5.1,z=15.1-Math.abs(col)*.22;
      sph(3.35,2.65,1.05,row===1?'#d9c8a2':plate,x,y,z);
      sph(.48,.48,.34,trim,x,y,z+1.02);
    }
    // three engraved breastplate ribs and the royal vertical sash
    for(let q=0;q<3;q++){push();T(0,38-q*5,16);box(2.2,3,1.2,trim,0,0,0);pop();}
    // five ancient keys hang from the belt; they flare with the second phase
    for(let q=0;q<5;q++){
      const x=(q-2)*5.1,glow=p2?1:0;
      push();T(x,20.5,14.2);RZ((q-2)*.09);
      cyl(1.05,7.5,1.05,trim,0,-2.2,0,glow,.72);
      sph(2.15,2.15,.85,p2?'#ff6747':'#287f91',0,2.5,0,glow,.9);
      box(3.8,1.1,1.1,trim,0,-5.5,0,glow,.8);
      box(1.1,2.8,1.1,trim,1.5,-7,0,glow,.8);
      pop();
    }
    // Phase two fractures the ivory cuirass: five hot seams echo the awakened keys.
    if(p2){
      const pulse=.72+.28*Math.sin(time*11);
      for(let q=0;q<5;q++){
        const x=(q-2)*5.2;
        push();T(x,39-(q%2)*4,16.25);
        box(1.05,9+((q%2)*3),.8,'#ff6848',0,0,0,pulse,.92);
        sph(1.15,1.15,1,'#ffd18a',0,-4.5,0,pulse,.9);
        pop();
      }
      // ember motes circle the crown and shoulders only after the oath breaks
      for(let q=0;q<5;q++){
        const a=time*1.35+q*6.283/5;
        push();T(Math.cos(a)*19,45+Math.sin(a*1.7)*5,Math.sin(a)*17);
        sph(1.25,1.25,1.25,q%2?'#ff6a45':'#ffd08a',0,0,0,.8,.9);
        pop();
      }
    }
    // royal vertical sash and a hanging seal at the waist
    push();T(4,34,14);box(5,27,2.2,'#7b2028',0,-2,0);box(1.1,27,2.5,trim,0,-2,1.3);pop();
    push();T(0,20,15);sph(4.2,5.2,2.2,trim,0,0,0);box(1.2,5,1.2,'#fff0c8',0,0,2);pop();}
  for(const sd of[-1,1]){push();T(0,44,sd*10.6);for(let q=0;q<3;q++)sph(isAdapa?12-q*1.5:11-q*1.5,4.6,isAdapa?12-q*1.5:11-q*1.5,q?(isAdapa?body:'#5a4630'):(isAdapa?trim:'#6a5236'),0,-q*3.6,0);if(!isAdapa)con(2.4,7,2.4,'#b9ad94',0,4.4,0);else{for(let q=0;q<3;q++)con(2.2,6,2.2,trim,0,4+q*3,0);}pop();}
  // Adapa wears a royal crown and a pale ceremonial mask; Namtar keeps his horned war-helm
  push();T(.6,47,0);sph(7.4,4,7.4,dark,0,0,0);
  sph(9.4,11,9,isAdapa?'#d9c9a7':'#3a2e22',.6,6,0);
  box(1.2,1.4,8,'#0a0606',8.6,6.6,0);
  for(const sd of[-1,1])sph(1.3,1.1,1.6,isAdapa?(p2?'#fff0a8':'#fff8d8'):(p2?'#ff3a20':'#ff9a40'),8.4,6.6,sd*2.2,1);
  box(1.6,7,1.6,isAdapa?trim:'#2a201a',9,2,0);cyl(10,1.6,10,isAdapa?trim:'#6a5236',.6,10.6,0);
  if(isAdapa){
    // stepped gold crown, central lapis jewel and five upright points
    cyl(10.5,2.4,10.5,trim,.6,12,0);cyl(8.8,2.2,8.8,'#fff0c8',.6,14,0);
    for(let q=0;q<5;q++){
      const x=(q-2)*4.1;
      box(1.7,8,1.7,trim,x,18,0);
      con(2.2,5.5,2.2,trim,x,24,0);
      if(q===0||q===4){push();T(x,21,0);RZ(q===0?-.22:.22);con(1.5,7,1.5,trim,0,3.2,0);pop();}
    }
    // stepped diadem, raised side wings and the lapis centrepiece
    box(18,1.6,2.4,trim,.6,17,0);
    for(const sd of[-1,1]){push();T(.6,14,sd*7.4);RX(sd*.35);con(2.2,8,2.2,trim,0,4,0);pop();}
    sph(2.8,3.4,1.8,p2?'#ff563d':'#287f91',.6,15,9.4,p2?1:.82);
    if(p2){for(let q=0;q<3;q++){push();RY(time*1.8+q*2.094);T(0,15,12+Math.sin(time*3+q)*1.2);sph(1.4,1.4,1.4,'#ffb05a',0,0,0,.85);pop();}}
    for(const sd of[-1,1]){push();T(0,10,sd*8);box(2,8,2,trim,0,2,0);pop();}
    // fine ceremonial grooves across the pale mask; the central fracture wakes in phase two
    box(.7,12,.7,p2?'#ff624b':'#9d8150',8.7,5.5,0,p2?1:0,.85);
    for(const sd of[-1,1]){
      box(2.8,.7,.7,trim,8.4,7.7,sd*2.3);
      box(1.8,.65,.7,p2?'#ff684e':'#9d8150',8.6,2.7,sd*2.1,p2?1:0,.8);
    }
  }else{
    for(const sd of[-1,1]){push();T(-1,11,sd*7.5);RX(-sd*.9);RZ(.35);con(3.2,15,3.2,'#cfc6b0',0,7,0);T(0,13,0);RZ(.6);con(2,9,2,'#b9ad94',0,4,0);pop();}
  }
  pop();
  const ax=st==='held'?null:(isAdapa?adapaSword:namAxe);namArm(-1,o,null,body);namArm(1,m,ax,body);
  if(st==='stag'){}   // he stays down; the axe hangs from his hand
  pop();pop();
  if(st==='held'||st==='grab')gFlash=false;
  gAlpha=1;gFlash=false;
}
/* a short film the first time each boss is met */
function bossFilm(b){
  if(world.bf)return false;world.bf=true;
  const fx=Math.cos(b.a),fz=Math.sin(b.a),h=b.k==='erra'?200:240,M=b.k==='erra'?.85:1;
  const isAdapa=b.T&&b.T.name&&b.T.name.includes('أدابا');
  const shots=isAdapa?[
    {d:2.5,a:[b.x+(fx*310-fz*130),b.y+155,b.z+(fz*310+fx*130),b.x,b.y+90,b.z],
      b:[b.x+(fx*245-fz*95),b.y+135,b.z+(fz*245+fx*95),b.x,b.y+112,b.z],t:'باب الطلسم… آخر أبواب بغداد.',v:1},
    {d:2.1,a:[b.x+(fx*125-fz*28),b.y+92,b.z+(fz*125+fx*28),b.x,b.y+158,b.z],
      b:[b.x+(fx*92-fz*18),b.y+82,b.z+(fz*92+fx*18),b.x,b.y+164,b.z],t:'أدابا… معلم الحراس، وحارس المفاتيح الخمسة.'},
    {d:1.8,a:[P.x-Math.cos(ang2(b))*175,P.y+105,P.z-Math.sin(ang2(b))*135,b.x,b.y+125,b.z],
      b:[P.x-Math.cos(ang2(b))*135,P.y+90,P.z-Math.sin(ang2(b))*105,b.x,b.y+132,b.z],t:'هنا ينتهي الطريق.'}
  ]:[
    {d:2.8,a:[b.x+(fx*150-fz*60)*M,b.y+25*M,b.z+(fz*150+fx*60)*M,b.x,b.y+h*.7,b.z],b:[b.x+(fx*240-fz*110)*M,b.y+45*M,b.z+(fz*240+fx*110)*M,b.x,b.y+h*.6,b.z],t:b.T.name,v:1},
    {d:1.7,a:[P.x-Math.cos(ang2(b))*150,P.y+90,P.z-Math.sin(ang2(b))*120,b.x,b.y+h*.5,b.z],b:[P.x-Math.cos(ang2(b))*120,P.y+80,P.z-Math.sin(ang2(b))*95,b.x,b.y+h*.5,b.z],t:''}
  ];
  cine={i:0,t:0,boss:b,shots};
  paused=true;root.classList.add('cine');cineShot();snd('roar');return true;}
function ang2(b){return Math.atan2(b.z-P.z,b.x-P.x);}

/* lightning that lands where you stand, and around him */
function stormAround(b,n){for(let k=0;k<n;k++){const q=arClamp(P.x+(Math.random()-.5)*(k?160:30),P.z+(Math.random()-.5)*(k?160:30),20);waves.push({k:'b',x:q[0],z:q[1],y:arY(),t:-k*.18,d:.8});}}
/* Erra, the storm knight of the mountains: a man, once a guardian, in scale and feathers, with a great curved sword */
function errSword(){cyl(2.2,14,2.2,'#2a1c14',0,1,0);for(let i=0;i<3;i++)cyl(2.5,.8,2.5,'#c9a24a',0,-3+i*4,0);sph(14,2.4,3.4,'#c9a24a',0,-6.5,0);
  push();T(0,-7,0);const L=8.2;for(let i=0;i<6;i++){const w=5-i*.3;box(w,L+.6,1,'#cfd6e0',0,-L/2,0);box(1.3,L+.6,1.6,'#8a94a8',w/2-.6,-L/2,0);box(.7,L+.6,.5,'#ffffff',-(w/2-.2),-L/2,0);T(0,-L,0);RZ(.1);}
  RZ(PI);con(3,9,1,'#cfd6e0',0,4.5,0);pop();}
function drawErra(e){
  const fall=mobFx(e,2.2);if(fall<0)return;
  const st=e.state,p2=e.p2,scale='#3e5560',dark='#1c262c',bronze='#8a6a3a',skin='#a8765a',feather=p2?'#1a2a3a':'#2a2624';
  let key=null,u=0,lean=0,crouch=0;const A=NM[st],sp=(p2?1.18:1)*1.12;
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
  let m=p,o={t:.6+p.t*.15,y:-.1,e:.9,w:0};
  if(st==='grab'){const k=clamp(e.t/.5,0,1);o={t:lerp(.4,1.55,k),y:-.2,e:lerp(.8,.1,k),w:0};}
  if(st==='held')o={t:2.4,y:-.35,e:.25,w:0};
  if(st==='trans'||st==='stomp'){m={t:3.0,y:.1,e:.2,w:.1};o={t:2.6,y:.5,e:.4,w:0};}
  if(st==='stag'){m={t:.2,y:.3,e:.3,w:.9};o={t:.6,y:0,e:.4,w:0};}
  const mvk=(st==='chase'||st==='charge')?1:0,wk=e.walk;
  push();T(e.x,e.y+e.j,e.z);RY(-e.a);if(fall)RZ(fall);Sc(1.9,1.9,1.9);T(0,crouch*.4,0);
  for(const sd of[-1,1]){const ph=wk+(sd>0?0:PI);const hip=Math.sin(ph)*.55*mvk+(st==='stag'&&sd>0?1.4:0),knee=.1+.75*Math.max(0,Math.cos(ph))*mvk+(st==='stag'&&sd>0?1.7:0);
    push();T(0,24.4,sd*4.2);RZ(hip);sph(6.4,14,6.8,'#3a3228',0,-6,0);T(0,-11.5,0);RZ(-knee);sph(5.6,12.8,6,scale,0,-5,0);cyl(6.4,2,6.6,bronze,0,-1,0);T(0,-10.3,0);RZ(knee-hip);sph(9.4,4.6,6.4,'#2a201a',1.6,0,0);pop();}
  // long coat of scale, split for the stride
  for(let q=0;q<10;q++){const a=q/10*6.283;push();T(Math.cos(a)*6.4,23,Math.sin(a)*7.8);RX(Math.sin(a)*.18);RZ(-Math.cos(a)*.18-(Math.cos(a)<0?mvk*.25:0));box(1.2,19,5.4,q%2?scale:'#4a6470',0,-7,0);pop();}
  push();T(0,30,0);RZ(lean);T(0,-30,0);
  // cloak of dark feathers from the shoulders; in the storm it crackles
  push();T(-7.5,46,0);RZ(-.2-Math.sin(time*2.4)*.06-mvk*.22);
  for(let q=0;q<7;q++){const l=34+hash(q*2.7)*16,z=(q-3)*5.2;push();T(0,0,z);RX((q-3)*.05);sph(2.6,l,6,q%2?feather:'#3a3430',0,-l/2,0);if(p2&&q%2)box(.6,l*.8,.6,'#bfe4ff',.4,-l/2,0,1,.6+.4*Math.sin(time*20+q));pop();}pop();
  sph(12.5,17.5,15.5,scale,0,36.5,0);for(let q=0;q<4;q++)cyl(12.8-q*.4,.8,15.8-q*.4,bronze,0,28+q*4.2,0);
  cyl(13,3.6,16,'#2a201a',0,24.5,0);sph(4.4,4.6,3,bronze,6,24.5,0);
  for(const sd of[-1,1]){push();T(0,44.5,sd*9.6);sph(10,5,10,bronze,0,0,0);sph(8.6,4.4,8.6,scale,0,-3,0);for(let q=0;q<4;q++){push();RY(q*.5-.75);T(-2,1,sd*4);RZ(.5);con(2,9,1,feather,0,4,0);pop();}pop();}
  // head: a hard bearded face under an open helm crested with storm-bird feathers
  push();T(.6,47,0);cyl(5,4,5,skin,0,0,0);
  sph(8.2,9.6,8,skin,.8,6,0);sph(2.4,3,2.2,'#946048',5.6,5.4,0);sph(7.6,7.2,8.4,'#1a1412',2.4,2,0);sph(4.6,5.4,5.4,'#1a1412',4.2,-.4,0);
  for(const sd of[-1,1]){sph(1.2,1.2,1.8,p2?'#bfe4ff':'#e8e0d0',5,7.4,sd*2,p2?1:0);sph(.8,.9,1.1,'#0a0808',5.4,7.4,sd*2);box(.9,.9,3,'#1a1412',5.1,9,sd*2);}
  sph(9.4,6.4,9.2,'#3a4a52',.3,10.4,0);cyl(9.6,1.4,9.4,bronze,.3,8.6,0);box(1.4,6,1.6,bronze,6.4,7.6,0);
  for(let q=0;q<6;q++){push();T(-1-q*1.6,13+q*.4,0);RZ(.9+q*.12);con(2.2,12-q,1.4,q%2?'#c8a050':'#2a2624',0,5,0);pop();}
  pop();
  namArm(-1,o,null,scale);namArm(1,m,st==='held'?null:errSword,scale);
  pop();pop();gAlpha=1;gFlash=false;
}

const ELC={storm:'#9ad8ff',fire:'#ff7a30',water:'#6ac8ff',sand:'#e8c880',poison:'#9ae050',shadow:'#b060ff'};
function elemHit(b,n){let el=b.elem;if(el==='all')el=['storm','fire','water','sand','poison'][Math.floor(Math.random()*5)];
  if(el==='storm'){stormAround(b,n);snd('thunder');return;}
  const c=ELC[el]||'#ff4040';for(let k=0;k<Math.max(1,Math.floor(n/3));k++)waves.push({x:b.x,z:b.z,y:arY(),r:30-k*45,l:1.25+k*.2,col:c});
  if(el==='sand'||el==='fire'){const q=arClamp(P.x,P.z,20);waves.push({k:'b',x:q[0],z:q[1],y:arY(),t:-.2,d:.9});}}
