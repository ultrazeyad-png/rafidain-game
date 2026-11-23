"""Walks every governorate's main route for real: first fire -> second -> third -> the guardian's square.
   A breadth-first search that carries the height along (so mountain roads work), then the real walking code.
   python3 tests/route.py [ids]   (default: all)"""
import asyncio,sys,os,json
from playwright.async_api import async_playwright
H=os.path.dirname(os.path.abspath(__file__))
src=open(os.path.join(H,'t2.py')).read();NAV=src[src.index('NAV="""')+7:src.index('"""',src.index('NAV="""')+7)]
ORDER=['duhok','nineveh','erbil','sulay','halabja','kirkuk','saladin','diyala','anbar','karbala','babil','najaf','qadis','wasit','maysan','dhiqar','basra','baghdad']
IDS=sys.argv[1].split(',') if len(sys.argv)>1 else ORDER
PNAV="""window.pnav2=(tx,tz,max)=>{const lerp=(a,b,t)=>a+(b-a)*t,g=__g,P=g.P,C=92,J0=-12,GI=40,GJ=28,W=GI*2,N=W*GJ*2;
  const X=i=>(i+.5)*C/2,Z=j=>(j+.5)*C/2+J0*C,ci=x=>Math.floor(x/(C/2)),cj=z=>Math.floor((z-J0*C)/(C/2));
  const Y=new Float32Array(N).fill(-1e9),prev=new Int32Array(N).fill(-2),s=cj(P.z)*W+ci(P.x);prev[s]=-1;Y[s]=P.y;const q=[s];let f=-1,qi=0;const ti=ci(tx),tj=cj(tz);
  while(qi<q.length){const c=q[qi++],i=c%W,j=(c/W)|0;if(Math.abs(i-ti)<=1&&Math.abs(j-tj)<=1){f=c;break;}
    for(const [a,b] of [[1,0],[-1,0],[0,1],[0,-1],[1,1],[1,-1],[-1,1],[-1,-1]]){const ni=i+a,nj=j+b,n=nj*W+ni;if(ni<0||nj<0||ni>=W||nj>=GJ*2||prev[n]!==-2)continue;
      const x=X(ni),z=Z(nj);let y=Y[c],ok=true;   // walk there in small steps, as the hero would
      for(let k=1;k<=8&&ok;k++){const px=lerp(X(i),x,k/8),pz=lerp(Z(j),z,k/8),fy=g.floorAt(px,pz,y+2);if(fy<-900||fy<y-60){ok=false;break;}y=fy;
        for(const [u,v] of [[0,0],[17,0],[-17,0],[0,17],[0,-17],[12,12],[-12,12],[12,-12],[-12,-12]])if(g.blockedPt(px+u,pz+v,y)){ok=false;break;}}
      if(!ok)continue;prev[n]=c;Y[n]=y;q.push(n);}}
  if(f<0){let best=s,bd=1e18;for(let c=0;c<N;c++)if(prev[c]!==-2){const d=Math.hypot(X(c%W)-tx,Z((c/W)|0)-tz);if(d<bd){bd=d;best=c;}}return 'nopath(closest '+(bd|0)+' at '+(X(best%W)|0)+','+(Z((best/W)|0)|0)+')';}
  const pts=[];for(let c=f;c>=0;c=prev[c])pts.unshift([X(c%W),Z((c/W)|0)]);return nav(pts.filter((p,k)=>k%2===0||k===pts.length-1),max||12000);};"""
async def main():
    async with async_playwright() as p:
        b=await p.chromium.launch(executable_path='/usr/bin/google-chrome',args=['--no-sandbox','--use-gl=angle','--use-angle=swiftshader','--enable-unsafe-swiftshader','--ignore-gpu-blocklist'])
        pg=await b.new_page(viewport={'width':640,'height':300},device_scale_factor=1)
        errs=[];pg.on('pageerror',lambda e:errs.append(str(e)))
        ev=pg.evaluate
        await pg.goto('file://'+os.path.join(H,'..','dist','index.html'));await pg.click('#go');await ev(NAV);await ev(PNAV)
        await ev("__g.endCine();wait(140);__g.P.hp=99999;__g.stats.prog=99;__g.stats.done={basra:1}")
        bad=[]
        for id in IDS:
            await ev(f"__g.travel('{id}');wait(30)")
            if await ev("!!__g.cine"):await ev("__g.endCine&&__g.endCine();wait(10)")
            r=await ev("""(()=>{const g=__g,S=g.SHRs,A=g.AC;g.P.hp=99999;for(const e of g.enemies)if(!e.boss)e.dead=true;const out=[];
              const legs=[['fire2',S[1]],['fire3',S[2]]];if(A&&g.REG!=='duhok')legs.push(['arena',{x:A.x,z:A.z}]);   // Zakho's square sits behind the fire gate: tests/d4.py walks it
              for(const [n,d] of legs){g.P.hp=99999;let r=pnav2(d.x,d.z);for(let t=0;t<3&&typeof r!=='string'&&Math.hypot(g.P.x-d.x,g.P.z-d.z)>=(n==='arena'?A.r:140);t++){nav([[g.P.x+(t%2?-40:40),g.P.z+40]],120);r=pnav2(d.x,d.z);}const ok=typeof r!=='string'&&Math.hypot(g.P.x-d.x,g.P.z-d.z)<(n==='arena'?A.r:140);out.push([n,ok,r]);if(!ok)break;}
              return JSON.stringify(out);})()""")
            r=json.loads(r);ok=all(x[1] for x in r)and len(r)>=2
            if not ok:bad.append(id)
            print(('OK  ' if ok else 'BAD ')+id,' | '.join(f"{n}:{'ok' if o else 'X'} {x if not o else ''}" for n,o,x in r),flush=True)
        print('errors',errs[:5]);print('BAD',bad);await b.close()
asyncio.run(main())
