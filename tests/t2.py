import asyncio
from playwright.async_api import async_playwright
NAV="""
window.nav=(pts,max)=>{const g=__g,P=g.P;let i=0,n=0;while(i<pts.length&&n++<(max||1500)){
  const dx=pts[i][0]-P.x,dz=pts[i][1]-P.z,d=Math.hypot(dx,dz);if(d<10){i++;continue;}
  const fx=Math.cos(g.cam.yaw),fz=Math.sin(g.cam.yaw);g.input.jy=(dx*fx+dz*fz)/d;g.input.jx=(-dx*fz+dz*fx)/d;g.step(1/60);}
  g.input.jx=g.input.jy=0;return [P.x|0,P.y|0,P.z|0,i+'/'+pts.length];};
window.wait=(n)=>{for(let i=0;i<n;i++)__g.step(1/60);return [__g.P.x|0,__g.P.y|0,__g.P.z|0];};
window.fight=(n)=>{const g=__g,P=g.P;for(let k=0;k<n;k++){if(k%5===0){let m=null,bd=1e9;for(const e of g.enemies){if(e.dead||Math.abs(e.y-P.y)>60)continue;const d=Math.hypot(e.x-P.x,e.z-P.z);if(d<bd){bd=d;m=e;}}
 if(!m){g.input.jx=g.input.jy=0;}else if(bd<95){g.input.jx=0;g.input.jy=0;const s=m.state;if(s==='wind'||s==='leap'||s==='dashW'||s==='sweepW')g.act('roll');else g.act('atk');if(P.hp<60)g.act('heal');}
 else{const dx=m.x-P.x,dz=m.z-P.z,fx=Math.cos(g.cam.yaw),fz=Math.sin(g.cam.yaw);g.input.jy=(dx*fx+dz*fz)/bd;g.input.jx=(-dx*fz+dz*fx)/bd;}}g.step(1/60);}
 return JSON.stringify({p:[P.x|0,P.y|0,P.z|0],hp:P.hp|0,boss:g.boss&&g.boss.hp,bd:g.world.bossDead,w:g.stats.wpn});};
"""
async def main():
    async with async_playwright() as p:
        b=await p.chromium.launch(executable_path='/usr/bin/google-chrome',args=['--no-sandbox','--use-gl=angle','--use-angle=swiftshader','--enable-unsafe-swiftshader','--ignore-gpu-blocklist'])
        pg=await b.new_page(viewport={'width':844,'height':390},device_scale_factor=1)
        errs=[];pg.on('pageerror',lambda e:errs.append(str(e)));pg.on('console',lambda m:errs.append(m.text) if m.type=='error' else None)
        async def shot(n):
            await pg.wait_for_timeout(500);await pg.screenshot(path=n)
        await pg.goto('file://'+__import__('os').path.abspath(__import__('os').path.join(__import__('os').path.dirname(__file__),'..','dist','index.html'))+'');await pg.click('#go');await pg.evaluate(NAV)
        await pg.evaluate("__g.stats.vig=60;__g.stats.str=6;__g.P.hp=1000")
        P=lambda *a:print(*a,flush=True)
        P('stairs',await pg.evaluate("nav([[345,60],[345,262],[420,255],[420,178]])"));await shot('s2.png')
        P('roofs',await pg.evaluate("nav([[1090,178],[1150,178],[1150,230]])"));await shot('s3.png')
        P('hole',await pg.evaluate("nav([[1242,230]])"),await pg.evaluate("wait(60)"));await shot('s4.png')
        P('door',await pg.evaluate("nav([[1242,345]],200)"),await pg.evaluate("__g.world.door"))
        P('down',await pg.evaluate("nav([[1242,230],[1300,230],[1420,230]])"));await shot('s5.png')
        P('tunnel',await pg.evaluate("nav([[1650,280],[1650,440]])"));await shot('s6.png')
        P('hall',await pg.evaluate("nav([[1650,730]])"),await pg.evaluate("[__g.world.locked,!!__g.lock]"));await pg.evaluate("wait(90)");await shot('s7.png')
        P(await pg.evaluate("fight(2400)"));await shot('s8.png')
        P(await pg.evaluate("fight(3600)"))
        P('back up',await pg.evaluate("nav([[1650,700],[1650,440],[1650,280],[1420,230],[1300,230],[1242,230],[1242,400]],4000)"))
        await pg.evaluate("localStorage.clear()")
        await pg.goto('file://'+__import__('os').path.abspath(__import__('os').path.join(__import__('os').path.dirname(__file__),'..','dist','index.html'))+'');await pg.click('#go');await pg.evaluate(NAV)
        await pg.evaluate("__g.stats.vig=60;__g.P.hp=1000;__g.P.x=1058;__g.P.z=-150;__g.cam.yaw=-1.57")
        P('cellar',await pg.evaluate("nav([[1058,-250],[1058,-420],[1058,-560]])"));await shot('s9.png')
        await pg.evaluate("__g.P.x=760;__g.P.z=-170;__g.P.y=0;__g.cam.yaw=-1.57")
        P('H4 stairs',await pg.evaluate("nav([[760,-350],[700,-340]])"))
        await pg.evaluate("__g.act('jump');wait(14);__g.act('atk');wait(4)");await shot('s10.png')
        P('fall off',await pg.evaluate("nav([[700,-150]])"),await pg.evaluate("wait(60)"))
        P(errs[:6]);await b.close()
asyncio.run(main())
