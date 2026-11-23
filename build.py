"""Builds the game into one self-contained page: dist/index.html.
   python3 build.py          -> dist/index.html
   python3 tools/site_split.py -> site/index.html + site/game.js (for the website, whose CSP blocks inline scripts)"""
import os
H=os.path.dirname(os.path.abspath(__file__));S=os.path.join(H,'src')
T=open(os.path.join(S,'template.html'),encoding='utf-8').read()
def rep(s,a,b):
    assert s.count(a)==1,(s.count(a),a);return s.replace(a,b)
for k,*fs in [('/*@WORLD@*/','regions.js'),('/*@LOGO@*/','logo.b64'),('/*@AUDIO@*/','audio.js'),('/*@GL@*/','gl.js'),('/*@HERO@*/','chars.js'),('/*@SYS@*/','systems.js','bosses.js','gen.js','depth.js','travel.js','link.js')]:
    T=rep(T,k,'\n'.join(open(os.path.join(S,f),encoding='utf-8').read() for f in fs))
os.makedirs(os.path.join(H,'dist'),exist_ok=True)
open(os.path.join(H,'dist','index.html'),'w',encoding='utf-8').write(T);print('dist/index.html',len(T),'bytes')
