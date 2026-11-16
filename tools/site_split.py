"""Splits dist/index.html into site/index.html + site/game.js. The website's Content-Security-Policy
   (script-src 'self') blocks inline scripts, so the script must be its own file.
   Copy both into nextlevel-v3/frontend/public/game/ and deploy."""
import os,re,sys
H=os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
h=open(os.path.join(H,'dist','index.html'),encoding='utf-8').read()
m=re.search(r'<script>(.*)</script>',h,re.S)
v=sys.argv[1] if len(sys.argv)>1 else '1'
os.makedirs(os.path.join(H,'site'),exist_ok=True)
open(os.path.join(H,'site','game.js'),'w',encoding='utf-8').write(m.group(1))
open(os.path.join(H,'site','index.html'),'w',encoding='utf-8').write(h[:m.start()]+'<script src="/game/game.js?v='+v+'"></script>'+h[m.end():])
print('site/index.html + site/game.js')
