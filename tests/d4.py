import asyncio,sys
from playwright.async_api import async_playwright
src=open(__import__('os').path.join(__import__('os').path.dirname(__file__),'t2.py')).read();NAV=src[src.index('NAV="""')+7:src.index('"""',src.index('NAV="""')+7)]
async def main():
    async with async_playwright() as p:
        b=await p.chromium.launch(executable_path='/usr/bin/google-chrome',args=['--no-sandbox','--use-gl=angle','--use-angle=swiftshader','--enable-unsafe-swiftshader','--ignore-gpu-blocklist'])
        pg=await b.new_page(viewport={'width':844,'height':390},device_scale_factor=1)
        errs=[];pg.on('pageerror',lambda e:errs.append(str(e)));pg.on('console',lambda m:errs.append(m.text) if m.type=='error' else None)
        async def shot(n):
            await pg.wait_for_timeout(700);await pg.screenshot(path=n)
        P=lambda *a:print(*a,flush=True)
        ev=pg.evaluate
        await pg.goto('file://'+__import__('os').path.abspath(__import__('os').path.join(__import__('os').path.dirname(__file__),'..','dist','index.html'))+'');await pg.click('#go');await ev(NAV)
        await ev("__g.endCine();wait(130);__g.stats.vig=80;__g.stats.str=8;__g.P.hp=1500")
        P('bank',await ev("nav([[420,-870],[420,-760]],400)"))
        await ev("for(const e of __g.enemies)if(!e.boss)e.dead=true;__g.P.x=1150;__g.P.z=900;__g.P.y=105")
        P('frag2',await ev("nav([[1090,942]],1500)"),await ev("__g.world.frags"))
        await pg.click('#bMap');await shot('f5.png');await pg.click('#mNotes');await shot('f6.png');await pg.click('#mClose')
        await ev("__g.P.x=2640;__g.P.z=1010;__g.P.y=215;__g.cam.yaw=-.2;wait(20)");await shot('f7.png')
        P('gate shut',await ev("nav([[2800,977],[2900,957]],600)"))
        await ev("__g.lightBrz(0);__g.lightBrz(1);__g.lightBrz(2);__g.P.x=2640;__g.P.z=1010;wait(20)");P(await ev("__g.world.mgate"))
        P('gate open',await ev("nav([[2800,977],[2960,945],[3060,920]],1500)"),await ev("[__g.world.locked,__g.boss.state,!!__g.cine]"));await shot('film.png');await ev("__g.cine&&__g.endCine()");await ev("wait(150)");await shot('f8.png')
        P(await ev("fight(1200)"));await shot('f9.png')
        for i in range(7):
            r=await ev("fight(2400)");P(r,await ev("__g.boss.state"))
            if '"bd":true' in r:break
        await shot('f11.png');await pg.wait_for_timeout(5500);await ev("wait(300)");await shot('f10.png')
        P('unl',await ev("__g.stats.unl"))
        await ev("for(let i=0;i<8;i++)__g.nextDlg()");await pg.click('#cont');await ev("__g.travel('muthanna');wait(60)");P(await ev("[__g.REG,__g.P.x|0,__g.P.z|0,__g.B.length,__g.chunks,__g.enemies.length]"));await shot('f12.png')
        P('uruk walk',await ev("nav([[345,60],[345,262],[420,255],[420,178]])"));await shot('f13.png')
        await ev("__g.travel('duhok');wait(30)");P(await ev("[__g.REG,__g.world.bossDead,__g.world.frags,__g.world.mgate]"))
        await ev("__g.world.stain={x:300,y:0,z:-200,amt:50};wait(5)");await shot('st.png');await pg.click('#bMap');await shot('st2.png')
        P('errs',errs)
        await b.close()
asyncio.run(main())
