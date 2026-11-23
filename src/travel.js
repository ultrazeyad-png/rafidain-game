/* ================= the road between governorates =================
   After a guardian falls, a gate of fire opens at the far side of his square: walk through it to the next land.
   The same road card opens from the win card and from the Iraq map at any fire, then a short film of the road. */
function nextReg(id){const i=regIdx(id);return i>=0&&i<ORDER.length-1?ORDER[i+1]:null;}
function govName(id){return (GOVM[id]&&GOVM[id].n||'').split(' · ').pop();}
function toName(id){const n=govName(id);return n.startsWith('ال')?'لل'+n.slice(2):'لـ'+n;}   // للسليمانية، لـكركوك
let EXC={reg:null,p:null};
function exitPt(){
  if(EXC.reg===REG)return EXC.p;EXC={reg:REG,p:null};
  if(!AC||REG==='baghdad'||!nextReg(REG))return null;
  const y0=AC.y!=null?AC.y:P.y;
  for(const off of[AC.r-70,AC.r-120,150,90]){const x=AC.x+off,z=AC.z,y=floorAt(x,z,y0+60);
    if(y>-900&&!blockedPt(x,z,y)){EXC.p={x,y,z};break;}}
  return EXC.p;}
function exitOpen(){const nx=nextReg(REG);return !!(world.bossDead&&nx&&canGo(nx)&&exitPt());}
function nearExit(){if(!exitOpen())return false;const e=exitPt();return Math.hypot(P.x-e.x,P.z-e.z)<70&&Math.abs(P.y-e.y)<40;}
function drawExit(){
  if(!exitOpen())return;const e=exitPt(),f=.8+.2*Math.sin(time*7)+.08*Math.sin(time*13);
  push();T(e.x,e.y,e.z);
  for(const s of[-1,1]){box(16,118,16,'#6a5a48',0,59,s*44);box(22,10,22,'#4a3e32',0,5,s*44);box(20,8,20,'#4a3e32',0,116,s*44);}
  box(18,14,110,'#5a4a3a',0,124,0);for(let q=0;q<5;q++)box(19,3,7,'#e0c050',0,124,-36+q*18,1);
  cyl(11,5,11,'#3a3028',0,134,0);con(10*f,26*f,10*f,'#ff8a20',0,148,0,1);con(5,15*f,5,'#ffe08a',0,143,0,1);
  for(let q=0;q<7;q++)sph(6,2,6,'#ffb050',-20+q*24,1.5,0,1);
  pop();}
/* ---------- the card that asks before you leave ---------- */
let roadTo=null,roadFrom='';
function askTravel(id,from){
  if(!id||!canGo(id)||id===REG)return;roadTo=id;roadFrom=from||'';paused=true;iqOpen=false;$('iraq').classList.remove('on');$('shrine').classList.remove('on');
  const done=stats.done&&stats.done[id],boat=id==='baghdad'&&REG==='basra';
  $('rH').textContent=boat?'القارب لبغداد':'الدرب '+toName(id);
  $('rP').textContent=(boat?'عمو سلمان جهّز القارب. الماي ياخذك ضد التيار لبغداد.':
    done?'هالأرض خلصتها قبل. ترجعلها حتى تجمع اللي فاتك، والحارس ما يرجع.':'أرض جديدة. حارسها بعده واگف، والجمر اللي بإيدك ما ينضاع إذا مشيت.')+
    ' المفاتيح: '+(stats.keys||0)+' من 5.';
  $('rGo').textContent=boat?'اركب القارب':'امشِ';$('road').classList.add('on');}
function roadCancel(){$('road').classList.remove('on');roadTo=null;if(roadFrom==='map'){openIraq();return;}
  if(roadFrom==='shrine'&&nearShrine>=0){renderShrine();$('shrine').classList.add('on');return;}paused=false;}
function roadGo(){const id=roadTo;$('road').classList.remove('on');$('shrine').classList.remove('on');$('iraq').classList.remove('on');iqOpen=false;roadTo=null;if(!id)return;
  if(id==='baghdad'&&REG==='basra'&&!stats.boat){paused=false;travel(id);return;}   // the boat film tells this road
  roadFilm(REG,id,()=>{paused=false;travel(id);});}
/* ---------- the road film: the map of Iraq, a line of fire from here to there ---------- */
function roadFilm(a,b,done){
  const ov=$('roadv'),cv=$('rvc');ov.classList.add('on');$('rvT').textContent='من '+govName(a)+' '+toName(b);snd('rest');
  const t0=performance.now(),D=2600;let fin=false;
  const end=()=>{if(fin)return;fin=true;ov.classList.remove('on');done();};$('rvSkip').onclick=end;
  const tick=now=>{if(fin)return;const k=Math.min(1,(now-t0)/D);drawRoad(cv,a,b,k);if(k<1)requestAnimationFrame(tick);else setTimeout(end,350);};
  requestAnimationFrame(tick);}
function drawRoad(cv,a,b,k){
  const w=RW,h=RH;cv.width=Math.round(w*DPR);cv.height=Math.round(h*DPR);const m=cv.getContext('2d');m.setTransform(DPR,0,0,DPR,0,0);
  m.fillStyle='#17120e';m.fillRect(0,0,w,h);
  const top=36,bot=40,lo0=38.6,lo1=48.8,la0=29,la1=37.6,sc=Math.min((w-40)/(lo1-lo0),(h-top-bot)/(la1-la0)),ox=(w-(lo1-lo0)*sc)/2,oy=top+((h-top-bot)-(la1-la0)*sc)/2;
  const X=lo=>ox+(lo-lo0)*sc,Y=la=>oy+(la1-la)*sc;
  m.fillStyle='#d8c896';m.strokeStyle='#5a4428';m.lineWidth=2;m.beginPath();IRQ.forEach((p,i)=>i?m.lineTo(X(p[0]),Y(p[1])):m.moveTo(X(p[0]),Y(p[1])));m.closePath();m.fill();m.stroke();
  m.strokeStyle='#4a8ab4';m.lineWidth=3;for(const R of[TIG,EUP]){m.beginPath();R.forEach((p,i)=>i?m.lineTo(X(p[0]),Y(p[1])):m.moveTo(X(p[0]),Y(p[1])));m.stroke();}
  for(const id of ORDER){const g=GOVM[id],dn=stats.done&&stats.done[id];m.beginPath();m.arc(X(g.lon),Y(g.lat),5,0,7);m.fillStyle=dn?'#e8701c':'#8a7a62';m.fill();}
  const A=GOVM[a],B=GOVM[b],ax=X(A.lon),ay=Y(A.lat),bx=X(B.lon),by=Y(B.lat),e=k*k*(3-2*k),px=lerp(ax,bx,e),py=lerp(ay,by,e);
  m.setLineDash([6,5]);m.strokeStyle='#c81e28';m.lineWidth=3;m.beginPath();m.moveTo(ax,ay);m.lineTo(px,py);m.stroke();m.setLineDash([]);
  const gr=m.createRadialGradient(px,py,1,px,py,16);gr.addColorStop(0,'rgba(255,200,90,1)');gr.addColorStop(1,'rgba(255,120,30,0)');m.fillStyle=gr;m.beginPath();m.arc(px,py,16,0,7);m.fill();
  m.font='700 13px '+FONT;m.textAlign='center';m.textBaseline='middle';m.lineWidth=3;m.strokeStyle='rgba(216,200,150,.95)';
  for(const [x,y,t] of[[ax,ay-18,govName(a)],[bx,by-24,govName(b)]]){m.strokeText(t,x,y);m.fillStyle='#2a1c10';m.fillText(t,x,y);}}
