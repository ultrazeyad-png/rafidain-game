import asyncio
from playwright.async_api import async_playwright
from PIL import Image
src=open(__import__('os').path.join(__import__('os').path.dirname(__file__),'t2.py')).read();NAV=src[src.index('NAV="""')+7:src.index('"""',src.index('NAV="""')+7)]
P0=open(__import__('os').path.join(__import__('os').path.dirname(__file__),'g1.py')).read();PN=P0[P0.index('await ev("""window.pnav'):P0.index('""")',P0.index('await ev("""window.pnav'))+4]
async def main():
    async with async_playwright() as p:
        b=await p.chromium.launch(executable_path='/usr/bin/google-chrome',args=['--no-sandbox','--use-gl=angle','--use-angle=swiftshader','--enable-unsafe-swiftshader','--ignore-gpu-blocklist'])
        pg=await b.new_page(viewport={'width':844,'height':390},device_scale_factor=1)
        errs=[];pg.on('pageerror',lambda e:errs.append(str(e)))
        ev=pg.evaluate;P=lambda *a:print(*a,flush=True)
        await pg.goto('file://'+__import__('os').path.abspath(__import__('os').path.join(__import__('os').path.dirname(__file__),'..','dist','index.html'))+'');await pg.click('#go');await ev(NAV)
        exec_=PN.replace('await ev(','',1)[:-1]
        await ev(eval(exec_))
        await ev("__g.endCine();wait(140);__g.P.hp=99999;__g.stats.vig=40;__g.stats.str=6;__g.stats.prog=99;__g.stats.gk=['kirkuk','babil','anbar','sulay']")
        ims=[]
        async def shot():
            await pg.wait_for_timeout(1300);await pg.screenshot(path='x.png');ims.append(Image.open('x.png').copy())
        for rid in ['kirkuk','halabja']:
            await ev(f"__g.travel('{rid}');wait(20);for(const e of __g.enemies)if(!e.boss)e.dead=true;")
            await ev("const A=__g.AC;__g.P.x=A.x-240;__g.P.z=A.z;__g.P.y=A.y;__g.P.ry=A.y;wait(5)")
            P(rid,await ev("pnav(__g.AC.x-120,__g.AC.z,3000)"),await ev("[__g.world.locked,!!__g.cine]"))
            await shot();await ev("__g.cine&&__g.endCine()")
            for i in range(8):
                r=await ev("(()=>{const st={};const h0=__g.P.hp;for(let k=0;k<60;k++){fight(10);st[__g.boss.state]=1;}return JSON.stringify({lost:(h0-__g.P.hp)|0,boss:__g.boss.hp|0,p2:!!__g.boss.p2,st:Object.keys(st)})})()");P(r)
                if i==1:await shot()
                if '"boss":0' in r:break
            await pg.wait_for_timeout(5000);await ev("wait(100)");await shot()
            P('keys',await ev("[__g.stats.keys,JSON.stringify(__g.stats.done)]"))
            await ev("document.getElementById('cont').click()")
        # Baghdad: gate shut with too few keys, open with five, final boss, ending
        await ev("__g.travel('baghdad');wait(10);__g.cine&&__g.endCine();for(const e of __g.enemies)if(!e.boss)e.dead=true;")
        g=await ev("JSON.stringify(__g.RC.gate)");P('gate',g)
        await ev("const G=__g.RC.gate,D=__g.RC.D;__g.P.x=D[5][0];__g.P.z=D[5][1];__g.P.y=0;wait(5)")
        P('to gate',await ev("pnav(__g.RC.gate.x,__g.RC.gate.z,2000)"),await ev("__g.world.mgate"))
        await ev("__g.stats.keys=5;wait(30)");P('5 keys',await ev("__g.world.mgate"))
        await ev("const A=__g.AC;__g.P.x=A.x-200;__g.P.z=A.z;__g.P.y=A.y;wait(5)");P(await ev("pnav(__g.AC.x-100,__g.AC.z,2000)"))
        await shot();await ev("__g.cine&&__g.endCine()")
        for i in range(10):
            r=await ev("(()=>{const h0=__g.P.hp;for(let k=0;k<60;k++)fight(10);return JSON.stringify({lost:(h0-__g.P.hp)|0,boss:__g.boss.hp|0,p2:!!__g.boss.p2})})()");P(r)
            if '"boss":0' in r:break
        await pg.wait_for_timeout(4500);await shot();await pg.click('#end1');await pg.wait_for_timeout(500);await shot()
        P(errs[:5])
        W,H=ims[0].size;o=Image.new('RGB',(W*2,H*((len(ims)+1)//2)))
        for n,im in enumerate(ims):o.paste(im,((n%2)*W,(n//2)*H))
        o.resize((o.size[0]//2,o.size[1]//2)).save('sheet.png');await b.close()
asyncio.run(main())
