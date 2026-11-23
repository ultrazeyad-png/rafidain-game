"""The road between governorates: beat a guardian, walk to the gate of fire, take the road card, watch the road, arrive.
   Also: the Iraq map asks before it moves you, and a reload puts you back in the land you left."""
import asyncio,os,json
from playwright.async_api import async_playwright
H=os.path.dirname(os.path.abspath(__file__))
src=open(os.path.join(H,'t2.py')).read();NAV=src[src.index('NAV="""')+7:src.index('"""',src.index('NAV="""')+7)]
URL='file://'+os.path.join(H,'..','dist','index.html')
async def main():
    async with async_playwright() as p:
        b=await p.chromium.launch(executable_path='/usr/bin/google-chrome',args=['--no-sandbox','--use-gl=angle','--use-angle=swiftshader','--enable-unsafe-swiftshader','--ignore-gpu-blocklist'])
        pg=await b.new_page(viewport={'width':844,'height':390});ev=pg.evaluate;P=lambda *a:print(*a,flush=True)
        errs=[];pg.on('pageerror',lambda e:errs.append(str(e)))
        await pg.goto(URL);await pg.evaluate("localStorage.clear()");await pg.goto(URL);await pg.click('#go');await ev(NAV)
        await ev("__g.endCine();wait(140);__g.stats.prog=2;__g.stats.done={duhok:true,nineveh:true}")
        await ev("__g.travel('erbil');wait(30)");P('in',await ev("__g.REG"))
        # the gate is shut while the guardian stands
        P('gate before boss',await ev("!!__g.nearExit()"))
        # the guardian falls
        await ev("(()=>{const g=__g;for(const e of g.enemies)if(!e.boss)e.dead=true;})()")
        await ev("(()=>{const g=__g,A=g.AC;g.P.x=A.x;g.P.z=A.z;g.P.y=A.y||g.P.y;wait(60);})()")
        await ev("__g.cine&&__g.endCine()")
        await ev("(()=>{const g=__g,b=g.boss||g.enemies.find(e=>e.boss);b.armor=0;g.hurtE(b,b.hp*3+50,false);})()")
        P('boss down',await ev("__g.world.bossDead"))
        await ev("wait(400)");await pg.wait_for_timeout(300)
        for _ in range(12):
            if await ev("document.getElementById('dlg').classList.contains('on')"):await ev("__g.nextDlg&&__g.nextDlg()")
        P('win card',await ev("[document.getElementById('win').classList.contains('on'),document.getElementById('wGo').hidden,document.getElementById('wGo').textContent,document.getElementById('wP').textContent]"))
        await pg.screenshot(path=os.path.join(H,'road_win.png'))
        await pg.click('#cont')
        e=await ev("JSON.stringify(__g.exitPt())");P('exit',e)
        await ev("(()=>{const g=__g,e=g.exitPt();g.P.x=e.x-40;g.P.z=e.z;g.P.y=e.y;wait(20);})()")
        P('near exit',await ev("[__g.nearExit(),__g.nearX,document.getElementById('bRest').textContent]"))
        await ev("__g.cam.yaw=0;__g.cam.pitch=.25;wait(10)");await pg.wait_for_timeout(700);await pg.screenshot(path=os.path.join(H,'road_gate.png'))
        await ev("__g.ctxAct()")
        P('road card',await ev("[document.getElementById('road').classList.contains('on'),document.getElementById('rH').textContent,document.getElementById('rP').textContent]"))
        await pg.screenshot(path=os.path.join(H,'road_card.png'))
        await pg.click('#rGo');await pg.wait_for_timeout(1400);await pg.screenshot(path=os.path.join(H,'road_film.png'))
        await pg.wait_for_timeout(2600);P('arrived',await ev("__g.REG"))
        # the Iraq map asks first
        await ev("__g.openIraq()");await ev("__g.askTravel('nineveh','map')")
        P('map card',await ev("document.getElementById('rH').textContent"));await pg.click('#rNo')
        P('cancel back to map',await ev("document.getElementById('iraq').classList.contains('on')"))
        await pg.click('#iqClose')
        # reload: back in sulaymaniyah, not zakho
        await ev("__g.travel('sulay');wait(10)");await pg.goto(URL);await pg.click('#go');await ev(NAV);await ev("__g.endCine&&__g.endCine();wait(20)")
        P('after reload',await ev("__g.REG"))
        P('errors',errs[:5]);await b.close()
asyncio.run(main())
