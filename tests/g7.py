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
        await pg.goto('file://'+__import__('os').path.abspath(__import__('os').path.join(__import__('os').path.dirname(__file__),'..','dist','index.html'))+'');await pg.click('#go');await ev(NAV);await ev(eval(PN.replace('await ev(','',1)[:-1]))
        await ev("__g.endCine();wait(140);__g.stats.vig=40;__g.stats.str=6;__g.stats.prog=99")
        ims=[]
        async def shot():
            await pg.wait_for_timeout(1200);await pg.screenshot(path='x.png');ims.append(Image.open('x.png').copy())
        for rid in ['nineveh','erbil','sulay']:
            await ev(f"__g.travel('{rid}');wait(20);__g.cine&&__g.endCine();__g.P.hp=__g.P.hp;for(const e of __g.enemies)if(!e.boss&&!e.T.mini)e.dead=true;__g.setTod(0);")
            info=await ev("JSON.stringify({sec:__g.SEC&&__g.SEC.st,off:__g.SEC&&__g.SEC.off,items:__g.ITEMS.map(i=>i.k+(i.t?':'+i.t:'')),brz:__g.BRZ.length,mini:__g.enemies.filter(e=>e.T.mini).map(e=>e.k)})");P(rid,info)
            S=await ev("[(__g.SEC.x0+__g.SEC.x1)/2,(__g.SEC.z0+__g.SEC.z1)/2]")
            D4=await ev("[__g.RC.D[4][0],__g.RC.D[4][1],__g.RC.D[4][3]]")
            await ev(f"__g.P.x={D4[0]};__g.P.z={D4[1]};__g.P.y=__g.floorAt({D4[0]},{D4[1]},{D4[2]}+30);__g.P.ry=__g.P.y;wait(5)")
            r=await ev(f"(()=>{{const dx=({D4[0]})-({S[0]}),dz=({D4[1]})-({S[1]}),l=Math.hypot(dx,dz);return pnav(({S[0]})+dx/l*60,({S[1]})+dz/l*60,3000)}})()");P(' to secret',r,await ev("__g.SEC.off"))
            st=await ev("__g.SEC.st")
            if st=='wall':
                await ev(f"__g.P.a=Math.atan2({S[1]}-__g.P.z,{S[0]}-__g.P.x);__g.act('atk');wait(40)");P(' after hit',await ev("__g.SEC.off"))
            elif st=='night':
                await ev("__g.setTod(2);wait(5)");P(' at night',await ev("__g.SEC.off"));await shot();await ev("__g.setTod(0)")
            else:
                await ev("__g.lightBrz(0);__g.lightBrz(1);__g.lightBrz(2);wait(5)");P(' fires',await ev("__g.SEC.off"))
            b2=await ev("const it=__g.ITEMS.find(i=>i.k==='letter');[it.x,it.z]")
            r=await ev(f"pnav({b2[0]},{b2[1]},4000)");P(' to letter',r,await ev("!!__g.dlg"));await shot()
            await ev("for(let i=0;i<5;i++)__g.nextDlg()")
            other=await ev("const it=__g.ITEMS.find(i=>i.k==='tab'||i.k==='armor');it?[it.x,it.z]:null")
            if other:
                await ev(f"pnav({other[0]},{other[1]},2000)");P(' tabs/arms',await ev("JSON.stringify([__g.stats.tabs,__g.stats.arms,__g.stats.spell])"))
            if await ev("__g.enemies.some(e=>e.T.mini&&!e.dead)"):
                for i in range(6):
                    r=await ev("(()=>{for(let k=0;k<60;k++)fight(10);const m=__g.enemies.find(e=>e.T.mini);return JSON.stringify({hp:__g.P.hp|0,mini:m.hp|0,dead:m.dead})})()");P(' mini',r)
                    if '"dead":true' in r:break
                await shot()
        # spells: cast fire at an enemy, then ward
        await ev("__g.stats.tabs={fire:1,bolt:1,ward:1,drain:1};__g.stats.spell='fire';__g.P.fp=200;const e=__g.enemies.find(q=>!q.dead&&!q.boss)||null;window.__e=e;")
        await ev("__g.travel('anbar');wait(10);__g.stats.tabs={fire:1,bolt:1,ward:1,drain:1};__g.stats.spell='fire';__g.P.fp=60")
        P('cast fire',await ev("(()=>{const e=__g.enemies.find(q=>!q.boss&&q.k==='husk')||__g.enemies.find(q=>!q.boss);e.dead=false;e.hp=e.T.hp;e.state='idle';__g.P.x=e.x-150;__g.P.z=e.z;__g.P.y=e.y;__g.P.a=0;wait(5);const h0=e.hp;__g.cast();wait(70);return [h0,e.hp,__g.P.fp|0];})()"))
        await ev("__g.stats.spell='bolt';__g.toggleLock()");P('cast bolt',await ev("(()=>{const e=__g.enemies.find(q=>!q.boss&&!q.dead);const h0=e?e.hp:0;__g.cast();wait(70);return [h0,e&&e.hp];})()"))
        await ev("__g.stats.arm='zig';wait(2)");await shot()
        # a boss falls: last words then the card
        await ev("__g.travel('kirkuk');wait(20);__g.cine&&__g.endCine();__g.stats.gk=['kirkuk'];const A=__g.AC;__g.P.x=A.x-200;__g.P.z=A.z;__g.P.y=A.y;wait(5)")
        await ev("pnav(__g.AC.x-100,__g.AC.z,2000)");await ev("__g.cine&&__g.endCine();__g.boss.hp=30;fight(400);wait(300)")
        P('last words',await ev("[!!__g.dlg,__g.dlg&&__g.dlg.lines.length]"));await shot()
        await ev("for(let i=0;i<8;i++)__g.nextDlg()");P('win card',await ev("document.getElementById('win').classList.contains('on')"))
        P(errs[:5])
        W,H=ims[0].size;o=Image.new('RGB',(W*2,H*((len(ims)+1)//2)))
        for n,im in enumerate(ims):o.paste(im,((n%2)*W,(n//2)*H))
        o.resize((o.size[0]//2,o.size[1]//2)).save('sheet.png');await b.close()
asyncio.run(main())
