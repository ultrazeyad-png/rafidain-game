import asyncio,sys,time
from playwright.async_api import async_playwright
from PIL import Image
src=open(__import__('os').path.join(__import__('os').path.dirname(__file__),'t2.py')).read();NAV=src[src.index('NAV="""')+7:src.index('"""',src.index('NAV="""')+7)]
IDS=sys.argv[1].split(',')
async def main():
    async with async_playwright() as p:
        b=await p.chromium.launch(executable_path='/usr/bin/google-chrome',args=['--no-sandbox','--use-gl=angle','--use-angle=swiftshader','--enable-unsafe-swiftshader','--ignore-gpu-blocklist'])
        pg=await b.new_page(viewport={'width':844,'height':390},device_scale_factor=1)
        errs=[];pg.on('pageerror',lambda e:errs.append(str(e)))
        ev=pg.evaluate;P=lambda *a:print(*a,flush=True)
        await pg.goto('file://'+__import__('os').path.abspath(__import__('os').path.join(__import__('os').path.dirname(__file__),'..','dist','index.html'))+'');await pg.click('#go');await ev(NAV)
        await ev("__g.endCine();wait(140);__g.P.hp=99999;__g.stats.prog=99")
        await ev("""window.pnav=(tx,tz,max)=>{const g=__g,P=g.P,C=92,J0=-12,GI=40,GJ=28,H=12;  // breadth-first over a half-cell grid, then walk it
  const ok=(i,j)=>{const x=(i+.5)*C/2,z=(j+.5)*C/2+J0*C;if(i<0||j<0||i>=GI*2||j>=GJ*2)return false;const y=g.floorAt(x,z,P.y+60);if(y<-900)return false;
    for(const [a,b] of [[0,0],[14,0],[-14,0],[0,14],[0,-14]])if(g.blockedPt(x+a,z+b,y))return false;return true;};
  const ci=x=>Math.floor(x/(C/2)),cj=z=>Math.floor((z-J0*C)/(C/2));const si=ci(P.x),sj=cj(P.z),ti=ci(tx),tj=cj(tz),W=GI*2;
  const prev=new Int32Array(W*GJ*2).fill(-2);prev[sj*W+si]=-1;const q=[sj*W+si];let f=-1;
  while(q.length){const c=q.shift(),i=c%W,j=(c/W)|0;if(i===ti&&j===tj){f=c;break;}
    for(const [a,b] of [[1,0],[-1,0],[0,1],[0,-1],[1,1],[1,-1],[-1,1],[-1,-1]]){const ni=i+a,nj=j+b,n=nj*W+ni;if(ni<0||nj<0||ni>=W||nj>=GJ*2||prev[n]!==-2)continue;if(!ok(ni,nj))continue;prev[n]=c;q.push(n);}}
  if(f<0)return 'nopath';const pts=[];for(let c=f;c>=0;c=prev[c])pts.unshift([((c%W)+.5)*C/2,(((c/W)|0)+.5)*C/2+J0*C]);return nav(pts.filter((p,k)=>k%2===0||k===pts.length-1),max||9000);};""")
        ims=[]
        for id in IDS:
            t=time.time();await ev(f"__g.travel('{id}');wait(30)")
            info=await ev("JSON.stringify({reg:__g.REG,blocks:__g.B.length,ch:__g.chunks,en:__g.enemies.length,shr:__g.SHRs.map(s=>[s.x|0,s.z|0]),p:[__g.P.x|0,__g.P.y|0,__g.P.z|0],boss:__g.boss&&__g.boss.T.name})")
            P(id,round(time.time()-t,1),info)
            await ev("__g.cam.yaw=0;__g.cam.pitch=.3;wait(20)");await pg.wait_for_timeout(1300);await pg.screenshot(path='x.png');ims.append(Image.open('x.png').copy())
            # walk the main path from fire to fire to the arena
            r=await ev("(()=>{const S=__g.SHRs,A=__g.AC;for(const e of __g.enemies)if(!e.boss)e.dead=true;let r='';for(const d of [S[1],S[2],{x:A.x-150,z:A.z}]){r+=pnav(d.x,d.z,9000)+' ';}return r;})()")
            P(' walk',r,await ev("[__g.world.locked]"))
            await ev("document.getElementById('cSkip').click()") if await ev("!!__g.cine") else None
        P(errs[:6])
        W,H=ims[0].size;cols=2;rows=(len(ims)+1)//2;o=Image.new('RGB',(W*cols,H*rows))
        for n,im in enumerate(ims):o.paste(im,((n%cols)*W,(n//cols)*H))
        o.resize((o.size[0]//2,o.size[1]//2)).save('sheet.png');await b.close()
asyncio.run(main())
