import asyncio
from playwright.async_api import async_playwright
src=open(__import__('os').path.join(__import__('os').path.dirname(__file__),'t2.py')).read();NAV=src[src.index('NAV="""')+7:src.index('"""',src.index('NAV="""')+7)]
P0=open(__import__('os').path.join(__import__('os').path.dirname(__file__),'g1.py')).read();PN=P0[P0.index('await ev("""window.pnav'):P0.index('""")',P0.index('await ev("""window.pnav'))+4]
async def main():
    async with async_playwright() as p:
        b=await p.chromium.launch(executable_path='/usr/bin/google-chrome',args=['--no-sandbox','--use-gl=angle','--use-angle=swiftshader','--enable-unsafe-swiftshader','--ignore-gpu-blocklist'])
        pg=await b.new_page(viewport={'width':844,'height':390},device_scale_factor=1)
        errs=[];pg.on('pageerror',lambda e:errs.append(str(e)))
        ev=pg.evaluate;P=lambda *a:print(*a,flush=True)
        await pg.goto('file://'+__import__('os').path.abspath(__import__('os').path.join(__import__('os').path.dirname(__file__),'..','dist','index.html'))+'');await pg.click('#go');await ev(NAV);await ev(eval(PN.replace('await ev(','',1)[:-1]))
        await ev("__g.endCine();wait(140);__g.P.hp=99999;__g.stats.vig=40;__g.stats.str=6;__g.stats.prog=99;__g.stats.done.basra=true;__g.stats.keys=0")
        await ev("__g.travel('baghdad');wait(10);__g.cine&&__g.endCine();for(const e of __g.enemies)if(!e.boss)e.dead=true;")
        P('gate',await ev("JSON.stringify([__g.RC.gate,__g.RC.D[5]])"))
        await ev("const D=__g.RC.D;__g.P.x=D[5][0];__g.P.z=D[5][1];__g.P.y=0;wait(5)")
        r=await ev("(()=>{const G=__g.RC.gate,D=__g.RC.D,dx=G.x-D[5][0],dz=G.z-D[5][1],l=Math.hypot(dx,dz);return pnav(G.x-dx/l*70,G.z-dz/l*70,2000)})()");P('near gate',r,await ev("[__g.P.x|0,__g.P.z|0,__g.world.mgate]"))
        r=await ev("(()=>{const A=__g.AC;return pnav(A.x,A.z,2000)})()");P('through? (should be nopath)',r)
        await ev("__g.stats.keys=5;wait(30)");P('5 keys open',await ev("__g.world.mgate"))
        r=await ev("(()=>{const A=__g.AC;return pnav(A.x-100,A.z,3000)})()");P('to arena',r,await ev("[__g.world.locked]"))
        await ev("__g.cine&&__g.endCine()")
        await ev('__g.boss.hp=60;__g.P.hp=700')
        for i in range(14):
            r=await ev("(()=>{let out='';for(let k=0;k<60;k++){const s=__g.P.state,h=__g.P.hp;fight(10);if(__g.P.state==='dead'&&s!=='dead')out+='DIED from '+s+' hp '+h+' y '+(__g.P.y|0)+' boss '+__g.boss.state+';';}return out+JSON.stringify({hp:__g.P.hp|0,boss:__g.boss.hp|0,p2:!!__g.boss.p2,y:__g.P.y|0})})()");P(r)
            if '"boss":0' in r:break
        await pg.wait_for_timeout(4500);P('end on',await ev("document.getElementById('end').classList.contains('on')"))
        P(errs[:5]);await b.close()
asyncio.run(main())
