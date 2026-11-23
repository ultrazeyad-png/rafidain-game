"""The account link: served like the website (/game/ + /api/game/*, both faked here), the title card names the
   signed-in member, a fallen guardian sends progress, and played from a file nothing is sent."""
import asyncio,os,json,threading,http.server,subprocess,sys
from playwright.async_api import async_playwright
H=os.path.dirname(os.path.abspath(__file__));R=os.path.join(H,'..')
subprocess.run([sys.executable,os.path.join(R,'tools','site_split.py'),'t'],check=True,capture_output=True)
SITE=os.path.join(R,'site');GOT=[];ME={'success':True,'loggedIn':True,'name':'زياد'}
class Hd(http.server.BaseHTTPRequestHandler):
    def log_message(self,*a):pass
    def _send(self,code,body,ct):
        self.send_response(code);self.send_header('content-type',ct);self.end_headers();self.wfile.write(body)
    def do_GET(self):
        p=self.path.split('?')[0]
        if p=='/api/game/me':return self._send(200,json.dumps(ME).encode(),'application/json')
        if p in('/game/','/game/index.html'):return self._send(200,open(os.path.join(SITE,'index.html'),'rb').read(),'text/html; charset=utf-8')
        if p=='/game/game.js':return self._send(200,open(os.path.join(SITE,'game.js'),'rb').read(),'text/javascript; charset=utf-8')
        self._send(404,b'{}','application/json')
    def do_POST(self):
        n=int(self.headers.get('content-length',0));GOT.append(json.loads(self.rfile.read(n)));self._send(200,b'{"success":true}','application/json')
srv=http.server.ThreadingHTTPServer(('127.0.0.1',0),Hd);threading.Thread(target=srv.serve_forever,daemon=True).start()
URL='http://127.0.0.1:%d/game/'%srv.server_address[1]
async def main():
    async with async_playwright() as p:
        b=await p.chromium.launch(executable_path='/usr/bin/google-chrome',args=['--no-sandbox','--use-gl=angle','--use-angle=swiftshader','--enable-unsafe-swiftshader','--ignore-gpu-blocklist'])
        pg=await b.new_page(viewport={'width':844,'height':390});ev=pg.evaluate;P=lambda *a:print(*a,flush=True)
        errs=[];pg.on('pageerror',lambda e:errs.append(str(e)))
        await pg.goto(URL);await pg.wait_for_timeout(1500)
        P('title',await ev("document.getElementById('acct').textContent"))
        await pg.screenshot(path=os.path.join(H,'link_title.png'))
        P('first sync',GOT[-1] if GOT else None)
        await pg.click('#go');await ev("__g.endCine();__g.stats.prog=1;__g.travel('nineveh')");await ev("__g.cine&&__g.endCine()")
        await pg.wait_for_timeout(5600)
        await ev("(()=>{const g=__g,b=g.boss||g.enemies.find(e=>e.boss);b.armor=0;g.hurtE(b,b.hp*3+50,false);})()")
        await pg.wait_for_timeout(6500)
        P('after guardian',GOT[-1],'posts',len(GOT))
        # signed out: a sign-in link, and nothing is sent
        ME.clear();ME.update({'success':True,'loggedIn':False});n=len(GOT)
        await pg.goto(URL);await pg.wait_for_timeout(1500)
        P('signed out',await ev("document.getElementById('acct').innerHTML"))
        await ev("__g.travel('erbil')");await pg.wait_for_timeout(5500);P('posts while signed out',len(GOT)-n)
        # from a file: no account line at all
        await pg.goto('file://'+os.path.join(R,'dist','index.html'));await pg.wait_for_timeout(800)
        P('file',repr(await ev("document.getElementById('acct').textContent")),await ev("__g.LINK.on"))
        P('errors',errs[:4]);await b.close()
asyncio.run(main());srv.shutdown()
