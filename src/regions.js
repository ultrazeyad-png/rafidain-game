const LOGO='/*@LOGO@*/';
let ITEMS=[],B=[],DOOR=null,GATE=null,MGATE=null,DOORS=[],FLAGS=[],SHR=[],FRAGS=[],LIGHTS=[],SPAWN=[],NPCS=[],BRZ=[],WATER=[],DECO=[],REG='duhok',RNAME='';
const HT=130,WC='#86664a',RC='#7a5c40',PL='#6b4a2c',OWN=new Uint8Array(GI*GJ);
function blk(x0,z0,x1,z1,b,t,col,kind){const o={x0,z0,x1,z1,b,t,col:col||'#8a6a4a',kind:kind||'',off:false,ax:null,h0:0,h1:0};B.push(o);return o;}
function ramp(x0,z0,x1,z1,ax,h0,h1,b,col){const o=blk(x0,z0,x1,z1,b,Math.max(h0,h1),col||'#6e5a4a','ramp');o.ax=ax;o.h0=h0;o.h1=h1;return o;}
function houseXZ(x0,z0,x1,z1,col,ft,y0,fr){y0=y0||0;const b=blk(x0,z0,x1,z1,y0,y0+HT,col,'house');b.fr=fr==null?(z0>0?-1:1):fr;
  for(const f of ft||[]){const x=x0+(x1-x0)*f[1],z=z0+(z1-z0)*f[2];
    if(f[0]==='dome'){const r=f[3]||30;blk(x-r*.8,z-r*.8,x+r*.8,z+r*.8,y0+HT,y0+HT+r*.8,col,'dome');}
    else blk(x-15,z-15,x+15,z+15,y0+HT,y0+HT+52,col,'tower');}
  return b;}
function house(i0,j0,w,d,col,ft){return houseXZ(i0*C,j0*C,(i0+w)*C,(j0+d)*C,col,ft,0,null);}
function hollow(x0,z0,x1,z1,side,dx,y0,col){const t=14;y0=y0||0;col=col||WC;
  for(const sd of['s','n']){const za=sd==='s'?z0:z1-t,zb=sd==='s'?z0+t:z1;
    if(side===sd){blk(x0,za,dx-32,zb,y0,y0+HT,col,'wall');blk(dx+32,za,x1,zb,y0,y0+HT,col,'wall');blk(dx-32,za,dx+32,zb,y0+72,y0+HT,col,'wall');}
    else blk(x0,za,x1,zb,y0,y0+HT,col,'wall');}
  blk(x0,z0+t,x0+t,z1-t,y0,y0+HT,col,'wall');blk(x1-t,z0+t,x1,z1-t,y0,y0+HT,col,'wall');}
function mkDoor(hx,hz,y,dir){return {hx,hz,y,dir,blk:blk(hx,hz-4,hx+64,hz+4,y,y+72,'#5a3a22','leaf'),o:0,t:0};}
function hhouse(x0,z0,x1,z1,side,dx,y0,col){   // enterable house with a roof and a hinged door ('s' = the z0 wall)
  hollow(x0,z0,x1,z1,side,dx,y0,col);blk(x0,z0,x1,z1,y0+HT-8,y0+HT,RC,'roof');DOORS.push(mkDoor(dx-32,side==='s'?z0+7:z1-7,y0,side==='s'?-1:1));}
function mosque(x0,z0,x1,z1,face,mx,mz){const b=blk(x0,z0,x1,z1,0,170,'#cdb48a','mosque');b.face=face;const cx=(x0+x1)/2,cz=(z0+z1)/2;
  blk(cx-76,cz-76,cx+76,cz+76,170,250,'#2f8f98','mdome');blk(mx-18,mz-18,mx+18,mz+18,0,360,'#cdb48a','minaret');}
function vn(x,z){const xi=Math.floor(x),zi=Math.floor(z),fx=x-xi,fz=z-zi,s=t=>t*t*(3-2*t),h=(a,b)=>hash(a*12.989+b*78.233+.5);
  return lerp(lerp(h(xi,zi),h(xi+1,zi),s(fx)),lerp(h(xi,zi+1),h(xi+1,zi+1),s(fx)),s(fz));}
function resetWorld(){SEC=null;WTH.drops.length=0;CW=null;REGC=null;RIVZ=null;LABELS=[];REGS={hs:1,ds:1};ITEMS=[];B=[];depth.fill(0);cover.fill(0);nogo.fill(0);HG=null;DOOR=GATE=MGATE=null;DOORS=[];FLAGS=[];SHR=[];FRAGS=[];LIGHTS=[];SPAWN=[];NPCS=[];BRZ=[];WATER=[];DECO=[];AC=null;}
function buildOwner(){   // which map piece uncovers which cell: two ragged territories, not straight halves
  for(let j=J0;j<J1;j++)for(let i=0;i<GI;i++){const x=(i+.5)*C,z=(j+.5)*C,
    s0=Math.hypot(x-FRAGS[0].x,z-FRAGS[0].z)*(.55+.9*vn(i*.31+3,j*.31+7)),s1=Math.hypot(x-FRAGS[1].x,z-FRAGS[1].z)*(.55+.9*vn(i*.31+40,j*.31+19));
    OWN[cidx(i,j)]=s0<s1?0:1;}}
function loadRegion(id){if(!GOVM[id])id='duhok';REG=id;resetWorld();if(id==='muthanna')regMuthanna();else if(id==='duhok')regDuhok();else regGen(GOVM[id]);if(id==='duhok'||id==='muthanna')depthFixed(id);buildOwner();if(glOK)buildWorld();}

/* ================= region: Muthanna (the city of Uruk) ================= */
function regMuthanna(){
  RNAME='أوروك · المثنى';
  pit(9,-7,13,-6,-90);pit(11,-5,11,-5,-90);pit(11,-4,11,-4,-90,1);          // cellar under the south house
  pit(14,2,14,2,-90,1);pit(15,2,18,3,-90);pit(17,4,18,4,-90);pit(16,5,23,11,-200); // secret tunnel + Namtar's hall
  AX0=16*C;AX1=24*C;AZ0=7*C;AZ1=12*C;AY=-200;
  // ---- old town ----
  house(4,1,3,2,'#8f6e4c',[['tower',.85,.82]]);ramp(322,92,368,276,'z',0,HT,0);
  house(8,1,3,2,'#96744f',[['dome',.5,.78,30]]);blk(644,160,736,196,HT-7,HT,PL,'plank');blk(1012,160,1104,196,HT-7,HT,PL,'plank');
  // secret hollow house (roof has a hole, back door opens from inside)
  hollow(1104,92,1380,368,'n',1242);DOOR=blk(1210,356,1274,366,0,72,'#5a3a22','door');
  blk(1104,92,1196,368,HT-8,HT,RC,'roof');blk(1288,92,1380,368,HT-8,HT,RC,'roof');blk(1196,92,1288,184,HT-8,HT,RC,'roof');blk(1196,276,1288,368,HT-8,HT,RC,'roof');
  ramp(1288,184,1380,276,'x',0,-90,-90,'#5a4a44');
  GATE=blk(1440,184,1452,368,-90,-8,'#2a2a30','gate');   // sealed until the key is found

  house(5,-4,3,2,'#8a6848',[['tower',.15,.2]]);ramp(736,-368,782,-184,'z',HT,0,0);
  hollow(920,-460,1196,-184,'n',1058);blk(920,-460,1196,-184,HT-8,HT,RC,'roof');ramp(1012,-368,1104,-276,'z',-90,0,-90,'#5a4a44');
  house(15,-4,3,2,'#94714e',[['dome',.5,.8,26]]);ramp(1334,-368,1380,-184,'z',HT,0,0);
  house(19,-4,3,2,'#8a6848',[['tower',.85,.8]]);blk(1656,-294,1748,-258,HT-7,HT,PL,'plank');
  hollow(1840,92,2116,276,'s',1978);blk(1840,92,2116,276,HT-8,HT,RC,'roof');
  house(1,-6,3,2,'#96744f',[['dome',.5,.5,42]]);house(15,-8,4,2,'#8a6848',[['dome',.3,.5,40],['tower',.8,.5]]);
  house(2,5,4,3,'#8f6e4c',[['dome',.3,.75,34]]);ramp(552,460,598,736,'z',0,HT,0);
  house(8,6,3,3,'#94714e',[['dome',.5,.5,48]]);house(23,-7,3,3,'#8a6848',[['dome',.5,.5,48]]);house(24,2,3,2,'#96744f',[['tower',.5,.5]]);
  // hinged doors: the two map-piece houses and the secret back door
  DOORS=[{hx:1026,hz:-191,y:0,dir:1,blk:blk(1026,-195,1090,-187,0,72,'#5a3a22','leaf'),o:0,t:0},
    {hx:1946,hz:99,y:0,dir:-1,blk:blk(1946,95,2010,103,0,72,'#5a3a22','leaf'),o:0,t:0},
    {hx:1210,hz:361,y:0,dir:1,blk:DOOR,o:0,t:0,secret:true}];
  // ---- east quarter ----
  house(29,1,3,2,'#8f6e4c',[['tower',.8,.8]]);ramp(2622,92,2668,276,'z',0,HT,0);
  house(33,1,3,2,'#96744f',[['dome',.5,.78,30]]);blk(2944,160,3036,196,HT-7,HT,PL,'plank');
  house(29,-4,3,2,'#94714e',[['dome',.5,.25,28]]);blk(2944,-294,3036,-258,HT-7,HT,PL,'plank');
  house(33,-4,3,2,'#8a6848',[['tower',.2,.2]]);ramp(3312,-368,3358,-184,'z',HT,0,0);
  house(37,-1,2,3,'#8f6e4c',[['dome',.5,.5,36]]);
  // ---- north and south rows ----
  house(10,13,3,2,'#94714e',[['dome',.5,.5,40]]);house(14,13,3,2,'#8a6848',[['tower',.5,.5]]);house(20,13,4,2,'#96744f',[['dome',.3,.5,40]]);house(26,13,3,2,'#8f6e4c',[['tower',.7,.5]]);
  house(12,-11,3,2,'#96744f',[['dome',.5,.5,40]]);house(17,-11,3,2,'#8f6e4c',[['tower',.5,.5]]);house(22,-11,3,2,'#94714e',[['dome',.5,.5,40]]);house(26,-10,3,2,'#8a6848');
  // ---- mosques (one per district) ----
  mosque(276,828,644,1104,'s',700,860);mosque(2852,-1012,3220,-736,'n',3270,-770);
  // ---- props ----
  for(const q of[[120,60,84],[150,-120,70],[1300,-150,88],[1185,-165,74],[1000,420,80],[1250,480,92],[700,420,76],[2300,420,84],[300,-250,78],[1500,-480,86],[2200,-250,80]])blk(q[0]-5,q[1]-5,q[0]+5,q[1]+5,0,q[2],'#5c4630','palm');
  blk(1325,-75,1359,-41,0,17,'#6a5a58','well');blk(2313,-167,2347,-133,0,17,'#6a5a58','well');blk(443,743,477,777,0,17,'#6a5a58','well');blk(3019,-667,3053,-633,0,17,'#6a5a58','well');
  ramp(17*C,460,19*C,644,'z',-90,-200,-200,'#4a3c3a');
  for(const c of[[1500,560,110],[1700,520,80],[1950,600,130],[2150,760,95],[1600,900,120],[2000,980,70],[1400,820,100],[2300,560,85]])blk(c[0]-9,c[1]-9,c[0]+9,c[1]+9,0,c[2],'#5a4a50','col');
  for(const s of[[2440,125],[2540,130],[2640,125],[2420,-215],[2520,-250],[2620,-215]])blk(s[0]-30,s[1]-13,s[0]+30,s[1]+13,0,20,'#6b4a2c','stall');
  for(let i=0;i<5;i++)for(let j=0;j<3;j++){const x=190+i*130+hash(i*3.1+j)*40,z=-1040+j*85+hash(i+j*7.7)*30;blk(x-8,z-3,x+8,z+3,0,24,'#77777f','tomb');}

  FLAGS=[{x:1160,y:0,z:-62,ry:-PI/2},{x:250,y:0,z:72,ry:-PI/2},{x:2650,y:0,z:-45,ry:-PI/2}];
  for(const f of FLAGS)blk(f.x-4,f.z-4,f.x+4,f.z+4,0,230,'#8a8a92','flag');
  ITEMS=[{k:'shard',x:700,y:0,z:40},{k:'shard',x:2000,y:0,z:-40,n:2},{k:'arrows',x:1500,y:0,z:30,n:6},{k:'dshard',x:2540,y:0,z:-60}];
  SHR=[{x:200,y:0,z:-30},{x:1242,y:0,z:-125},{x:1500,y:-90,z:276},{x:2540,y:0,z:-130}];
  FRAGS=[{x:966,y:0,z:-325,n:'الجانب الغربي'},{x:1978,y:0,z:215,n:'الجانب الشرقي'}];
  LIGHTS=[{x:1978,y:40,z:240,w:1.3},{x:966,y:40,z:-300,w:1.2},{x:1010,y:-45,z:-575,w:1.5},{x:1560,y:-45,z:330,w:1.4},{x:1650,y:-45,z:420,w:1.3},
    {x:1560,y:-120,z:700,w:2.2},{x:2120,y:-120,z:700,w:2.2},{x:1560,y:-120,z:1050,w:2.2},{x:2120,y:-120,z:1050,w:2.2},{x:1242,y:40,z:240,w:1.2},
    {x:460,y:50,z:800,w:1.6},{x:3036,y:50,z:-705,w:1.6},{x:2540,y:45,z:60,w:1.4},{x:450,y:40,z:-900,w:1.2}];
  for(let i=0;i<SHR.length;i++)LIGHTS.push({x:SHR[i].x,y:SHR[i].y+40,z:SHR[i].z,w:1.8,shr:i});
  SPAWN=[['husk',620,-20,0],['husk',800,40,0],['spear',1060,-10,0],['husk',880,190,HT],['spear',1150,230,HT],
    ['husk',1500,-120,0],['brute',1700,-30,0],['spear',1950,10,0],['husk',2100,-60,0],['brute',2350,0,0],
    ['husk',600,-450,0],['spear',1300,-500,0],['husk',900,-580,-90],['spear',1180,-600,-90],
    ['husk',1600,300,-90],['husk',1660,240,-90],['spear',1630,400,-90],['brute',1700,330,-90],['spear',1560,230,-90],
    ['husk',2040,190,0],['husk',900,500,0],['brute',1300,620,0],['spear',700,700,0],
    ['husk',2700,-30,0],['spear',2850,20,0],['brute',3050,-10,0],['husk',3250,40,0],['spear',3380,-20,0],['husk',2800,180,HT],['spear',3150,180,HT],
    ['husk',2480,60,0],['husk',2600,-150,0],
    ['husk',2800,700,0],['spear',3000,850,0],['brute',3200,950,0],['husk',3350,700,0],['spear',3450,1150,0],
    ['spear',1000,1150,0],['husk',1400,1160,0],['brute',2000,1160,0],['husk',2500,1150,0],
    ['husk',300,-880,0],['husk',560,-990,0],['spear',700,-860,0],['guard',450,-915,0],
    ['husk',1200,-780,0],['spear',1700,-800,0],['brute',2300,-780,0],['husk',2700,-700,0],
    ['spear',480,760,0],['husk',660,790,0],['spear',3000,-690,0],['husk',3160,-680,0],['gmini',3000,-300,0]];
  // trees and palms scattered by a fixed pattern, kept off buildings, pits, the street and spawn points
  function veg(x0,x1,z0,z1,n,pr,seed,kind){for(let k=0;k<n;k++){const x=x0+hash(seed+k*1.7)*(x1-x0),z=z0+hash(seed+k*2.9+5)*(z1-z0);
    if(Math.abs(z)<84)continue;const c=cellOf(x,z);if(c<0||(depth[c]!==0&&!cover[c])||blocked(x,z,0,30))continue;
    if(SHR.some(s=>Math.hypot(s.x-x,s.z-z)<80)||FRAGS.some(s=>Math.hypot(s.x-x,s.z-z)<60)||SPAWN.some(s=>Math.hypot(s[1]-x,s[2]-z)<46))continue;
    const isP=!kind&&hash(seed+k*4.3)<pr,h=kind?58:isP?70+hash(k+seed)*30:42+hash(k+seed)*16;
    blk(x-5,z-5,x+5,z+5,0,h,'#5c4630',kind||(isP?'palm':'tree'));}}
  veg(2650,3620,470,1420,26,.6,11);veg(90,1000,760,1420,14,.6,23);veg(1000,2620,1150,1440,12,.5,37);veg(900,3620,-1080,-520,18,.5,41);
  veg(40,2600,-520,520,9,.7,53);veg(2650,3640,-560,460,7,.7,67);veg(120,860,-1070,-790,6,0,71,'dtree');
}

/* ================= region: Duhok (Zakho, the gorge, the village, the citadel) ================= */
function regDuhok(){
  RNAME='زاخو · دهوك';REGC=TERC.mount;RIVZ=-736;
  LABELS=[{x:450,z:-340,t:'زاخو'},{x:520,z:-800,t:'جسر دلال'},{x:2110,z:640,t:'القرية'},{x:3180,z:1230,t:'القلعة'},{x:1560,z:760,t:'الوادي'}];
  HG=new Float32Array((GI+1)*(GJ+1));
  // walkable land: flat discs joined by corridors that climb from the river to the citadel
  const D=[[520,-960,200,0],[620,-430,370,0],[1180,-140,115,40],[1580,140,115,90],[2060,380,335,130],[2120,820,125,160],[2640,1010,140,215],[3180,900,335,260],[1560,640,90,120],[1150,900,210,105]];
  const L=[[0,1,150],[1,2,165],[2,3,155],[3,4,165],[4,5,155],[5,6,155],[6,7,140],[4,8,120],[8,9,110]];
  const pathH=(x,z)=>{let best=1e9,bh=0;
    for(const d of D){const q=Math.hypot(x-d[0],z-d[1])-d[2];if(q<best){best=q;bh=d[3];}}
    for(const l of L){const a=D[l[0]],b=D[l[1]],vx=b[0]-a[0],vz=b[1]-a[1],len=Math.hypot(vx,vz),t=clamp(((x-a[0])*vx+(z-a[1])*vz)/(len*len),0,1),
      q=Math.hypot(x-a[0]-vx*t,z-a[1]-vz*t)-l[2]/2;
      if(q<best){best=q;const t0=a[2]/len,t1=1-b[2]/len;bh=lerp(a[3],b[3],clamp((t-t0)/Math.max(.01,t1-t0),0,1));}}
    return [bh,Math.max(0,best)];};
  const mount=(x,z)=>300+250*vn(x/430,z/430)+120*vn(x/170+9,z/170+3);
  const dist=new Float32Array(HG.length);
  for(let j=J0;j<=J1;j++)for(let i=0;i<=GI;i++){const x=i*C,z=j*C,p=pathH(x,z),o=(j-J0)*(GI+1)+i,s=clamp(p[1]/150,0,1);
    dist[o]=p[1];HG[o]=p[1]<=0?p[0]:lerp(p[0],Math.max(mount(x,z),p[0]+175),s*s*(3-2*s));}
  for(let j=J0;j<J1;j++)for(let i=0;i<GI;i++){const o=(j-J0)*(GI+1)+i;if(dist[o]>40&&dist[o+1]>40&&dist[o+GI+1]>40&&dist[o+GI+2]>40)nogo[cidx(i,j)]=1;}
  // the Khabur river: a channel you can see but never reach
  for(let i=0;i<=GI;i++)HG[(-8-J0)*(GI+1)+i]=-70;
  for(let i=0;i<GI;i++){nogo[cidx(i,-9)]=1;nogo[cidx(i,-8)]=1;}
  WATER=[{x0:-900,x1:WX+900,z0:-822,z1:-650,y:-24}];
  const S1='#8c867a',S2='#7e786e',S3='#958e80';
  // Delal bridge
  ramp(470,-852,570,-794,'z',0,34,-90,S1);blk(470,-794,570,-678,22,34,S1,'bridge');ramp(470,-678,570,-620,'z',34,0,-90,S1);
  // Zakho: only one door looks at the water
  hhouse(330,-570,560,-410,'s',445,0,S1);hhouse(700,-600,900,-440,'n',800,0,S2);hhouse(380,-320,600,-170,'s',490,0,S3);
  houseXZ(710,-330,890,-200,S2,null,0,-1);
  // mountain village
  houseXZ(1890,240,2070,360,S1,null,130,1);ramp(1844,240,1890,360,'z',130,260,130,S2);
  hhouse(2140,225,2330,355,'n',2235,130,S3);houseXZ(1930,480,2090,590,S2,null,130,-1);
  // the stone door of the citadel, the arena columns
  MGATE=blk(2792,880,2808,1080,190,410,'#6e6860','mgate');
  AC={x:3180,z:900,r:290,y:260};
  for(let k=0;k<11;k++){const a=k/11*2*PI+.3,x=AC.x+Math.cos(a)*305,z=AC.z+Math.sin(a)*305;if(Math.abs(wrap(a-PI))<.5)continue;blk(x-9,z-9,x+9,z+9,260,260+50+hash(k)*80,'#7a7468','col');}
  // rocks: the wolf-head rock marking the crack, and the overhang that never sees the sun
  blk(1772,548,1812,588,125,170,'#6e6860','rock');
  blk(1168,818,1188,838,105,195,'#6e6860','rock');blk(1232,862,1252,882,105,195,'#6e6860','rock');blk(1160,810,1262,892,195,218,'#6e6860','rock');
  BRZ=[{id:'e',x:2352,y:130,z:412},{id:'w',x:1792,y:130,z:398},{id:'c',x:1210,y:105,z:850}];
  for(const b of BRZ)blk(b.x-8,b.z-8,b.x+8,b.z+8,b.y,b.y+40,'','brz');
  NPCS=[
    {id:'khidr',name:'مام خدر، حارس الجسر',x:592,y:0,z:-905,a:2.6,look:{robe:'#5a4a3a',vest:'#3c3428',sash:'#8a2a2a',skin:'#c49a72',beard:'#e8e4dc',hat:'turban',hatC:'#d8d0c0',staff:true}},
    {id:'shirin',name:'شيرين، الراعية',x:2205,y:130,z:482,a:2.2,look:{robe:'#6a3a5a',vest:'#7a4a3a',sash:'#d4ac50',skin:'#cfa07a',hat:'scarf',hatC:'#8a2f4a',staff:true}},
    {id:'derv',name:'الدرويش الأعمى',x:2562,y:215,z:955,a:.4,look:{robe:'#4a4a44',vest:'#5a5a50',sash:'#3a3a34',skin:'#b08a66',beard:'#bcb8b0',hat:'hood',hatC:'#3a3a34',blind:true,staff:true}}];
  for(const n of NPCS)blk(n.x-9,n.z-9,n.x+9,n.z+9,n.y,n.y+54,'','npc');
  FLAGS=[{x:662,y:0,z:-392,ry:-PI/2}];for(const f of FLAGS)blk(f.x-4,f.z-4,f.x+4,f.z+4,f.y,f.y+230,'#8a8a92','flag');
  SHR=[{x:430,y:0,z:-905,yaw:1.25},{x:2110,y:130,z:430,yaw:.6},{x:2612,y:215,z:1068,yaw:-.25}];
  FRAGS=[{x:445,y:0,z:-482,n:'القطعة الأولى'},{x:1090,y:105,z:942,n:'القطعة الثانية'}];
  ITEMS=[{k:'dagger',x:800,y:0,z:-520},{k:'axe',x:2235,y:130,z:290},{k:'bow',x:1540,y:120,z:660},{k:'arrows',x:2125,y:160,z:845,n:8},
    {k:'shard',x:930,y:0,z:-300},{k:'shard',x:1180,y:105,z:960},{k:'shard',x:2660,y:215,z:1040,n:2},{k:'shard',x:1800,y:130,z:330}];
  for(const it of ITEMS)it.y=floorAt(it.x,it.z,it.y+30);
  LIGHTS=[{x:445,y:48,z:-520,w:1.2},{x:2235,y:178,z:300,w:1.2}];
  for(let i=0;i<SHR.length;i++)LIGHTS.push({x:SHR[i].x,y:SHR[i].y+40,z:SHR[i].z,w:1.8,shr:i});
  SPAWN=[['husk',655,-385,0],['husk',652,-245,0],['husk',630,-505,0],['husk',930,-300,8],
    ['wolf',1060,-205,35],['wolf',1130,-105,40],['wolf',1240,-150,45],['sling',1565,165,90],['husk',1470,70,80],
    ['husk',2260,170,130],['wolf',1520,585,120],['wolf',1190,815,105],['wolf',1075,985,105],['sling',1245,960,105],
    ['archer',1655,215,100],['archer',2350,470,130],['archer',2585,1005,215],
    ['husk',2110,705,150],['husk',2165,865,160],['sling',2130,790,160],['brute',2290,905,180],['wolf',2400,935,190],['sling',2455,975,198],['gmini',1900,500,130]];
  // pines, oaks and boulders: on the land you walk they are solid, on the slopes they are scenery
  mkCW();
  for(let k=0;k<420;k++){const x=40+hash(k*1.37+2)*(WX-80),z=WZ0+40+hash(k*2.91+11)*(WZ1-WZ0-80),c=cellOf(x,z);if(c<0)continue;
    const j=Math.floor(z/C);if(j===-9||j===-8)continue;const y=hv(x,z),pine=hash(k*5.3)<.7,h=pine?70+hash(k*7.1)*45:44+hash(k*7.1)*16;
    if(y>470)continue;
    if(nogo[c]){DECO.push({k:pine?'pine':'tree',x,y,z,h});continue;}
    if(hash(k*9.7)<.45||blocked(x,z,y,34)||Math.hypot(x-AC.x,z-AC.z)<AC.r+20)continue;
    if(SHR.some(s=>Math.hypot(s.x-x,s.z-z)<170)||FRAGS.some(s=>Math.hypot(s.x-x,s.z-z)<60)||SPAWN.some(s=>Math.hypot(s[1]-x,s[2]-z)<50)||NPCS.some(s=>Math.hypot(s.x-x,s.z-z)<70)||BRZ.some(s=>Math.hypot(s.x-x,s.z-z)<60))continue;
    blk(x-5,z-5,x+5,z+5,y,y+h,'#4a3626',pine?'pine':'tree');}
  for(let k=0;k<170;k++){const x=40+hash(k*3.3+70)*(WX-80),z=WZ0+40+hash(k*1.9+90)*(WZ1-WZ0-80),c=cellOf(x,z);if(c<0)continue;const j=Math.floor(z/C);if(j===-9||j===-8)continue;
    if(!nogo[c]&&(blocked(x,z,hgt(x,z),40)||hash(k*4.4)<.6))continue;DECO.push({k:'rock',x,y:hv(x,z)-3,z,h:10+hash(k*6.1)*26});}
}
