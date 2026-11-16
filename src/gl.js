/* ================= WebGL ================= */
let uMode=null,uSP=null,uLVP=null,uShOn=null,uTime=null,uSk0=null,uSk1=null,uSd=null,uDc=null,uStar=null,shFB=null,shTex=null,dumTex=null,SHON=false,glLost=false;const SHN=1024,LVP=new Float32Array(16),Lp=new Float32Array(16),Lv=new Float32Array(16);
function ortho(o,R,n,f){o.fill(0);o[0]=1/R;o[5]=1/R;o[10]=-2/(f-n);o[14]=-(f+n)/(f-n);o[15]=1;}
let prog,uVP,uM,uC,uE,uF,uA,uNS,uCam,uFog,uPL,uTx=null,uUV=null,uSun=null,uSunC=null,uAT=null,uAB=null,uFg=null,aP=0,aN=0,aC=0,meshVB=null,meshIB=null,CYLN=0,glOK=!!gl;
const MS={},MV=[],MI=[];                 // unit meshes: index ranges, plus CPU copies used to bake the static world
const VP=new Float32Array(16),Pm=new Float32Array(16),Vm=new Float32Array(16),IDM=new Float32Array([1,0,0,0,0,1,0,0,0,0,1,0,0,0,0,1]);
let cur=new Float32Array(16);const stk=[];let spx=0;for(let i=0;i<32;i++)stk.push(new Float32Array(16));
let gAlpha=1,gFlash=false;
const eye=[0,0,0],VE=[0,0,0],VT=[0,0,0];   // VE/VT: the eye and target actually used this frame
function ident(){cur.fill(0);cur[0]=cur[5]=cur[10]=cur[15]=1;}
function push(){stk[spx++].set(cur);}
function pop(){cur.set(stk[--spx]);}
function T(x,y,z){const m=cur;m[12]+=m[0]*x+m[4]*y+m[8]*z;m[13]+=m[1]*x+m[5]*y+m[9]*z;m[14]+=m[2]*x+m[6]*y+m[10]*z;m[15]+=m[3]*x+m[7]*y+m[11]*z;}
function Sc(x,y,z){const m=cur;for(let i=0;i<4;i++){m[i]*=x;m[4+i]*=y;m[8+i]*=z;}}
function RZ(a){const m=cur,s=Math.sin(a),c=Math.cos(a);for(let i=0;i<4;i++){const x=m[i],y=m[4+i];m[i]=x*c+y*s;m[4+i]=y*c-x*s;}}
function RY(a){const m=cur,s=Math.sin(a),c=Math.cos(a);for(let i=0;i<4;i++){const x=m[i],z=m[8+i];m[i]=x*c-z*s;m[8+i]=x*s+z*c;}}
function RX(a){const m=cur,s=Math.sin(a),c=Math.cos(a);for(let i=0;i<4;i++){const y=m[4+i],z=m[8+i];m[4+i]=y*c+z*s;m[8+i]=z*c-y*s;}}
function mul(o,a,b){for(let c=0;c<4;c++)for(let r=0;r<4;r++)o[c*4+r]=a[r]*b[c*4]+a[4+r]*b[c*4+1]+a[8+r]*b[c*4+2]+a[12+r]*b[c*4+3];}
function persp(o,fovy,asp,n,f){const t=1/Math.tan(fovy/2);o.fill(0);o[0]=t/asp;o[5]=t;o[10]=(f+n)/(n-f);o[11]=-1;o[14]=2*f*n/(n-f);}
function lookAt(o,e,c){
  let zx=e[0]-c[0],zy=e[1]-c[1],zz=e[2]-c[2],l=Math.hypot(zx,zy,zz)||1;zx/=l;zy/=l;zz/=l;
  let xx=zz,xy=0,xz=-zx;l=Math.hypot(xx,xz)||1;xx/=l;xz/=l;
  const yx=zy*xz-zz*xy,yy=zz*xx-zx*xz,yz=zx*xy-zy*xx;
  o[0]=xx;o[1]=yx;o[2]=zx;o[3]=0;o[4]=xy;o[5]=yy;o[6]=zy;o[7]=0;o[8]=xz;o[9]=yz;o[10]=zz;o[11]=0;
  o[12]=-(xx*e[0]+xy*e[1]+xz*e[2]);o[13]=-(yx*e[0]+yy*e[1]+yz*e[2]);o[14]=-(zx*e[0]+zy*e[1]+zz*e[2]);o[15]=1;
}
const HEXC={};
function hex(h){let c=HEXC[h];if(!c){const n=parseInt(h.slice(1),16);c=HEXC[h]=[(n>>16&255)/255,(n>>8&255)/255,(n&255)/255];}return c;}
function initGL(){
  const vs='attribute vec3 p;attribute vec3 n;attribute vec4 c;attribute vec2 uv;varying vec2 vGLBUV;uniform mat4 uVP,uM,uLVP;uniform vec3 uNS;uniform float uTime;varying vec3 vN,vW,vP;varying float vZ;varying vec4 vC,vL;varying vec2 vGLBUV;'+
    'void main(){vP=p;vZ=n.z;vC=c;vGLBUV=uv;vec4 w=uM*vec4(p,1.);if(c.a>.13&&c.a<.19){float k=(c.a-.14)*25.,ph=uTime*1.7+w.x*.021+w.z*.017;w.x+=(sin(ph)*3.2+sin(ph*2.3)*1.)*k;w.z+=cos(ph*.8)*2.2*k;}vW=w.xyz;vN=mat3(uM)*(n*uNS);vL=uLVP*w;gl_Position=uVP*w;}';
  const fs=`precision highp float;
varying vec3 vN,vW,vP;varying float vZ;varying vec4 vC,vL;varying vec2 vGLBUV;
uniform sampler2D uT,uSh,uGLBTex;uniform float uGLBTexture;uniform vec3 uSun,uSunC,uAT,uAB,uSk0,uSk1,uSd,uDc,uCam,uFog;uniform float uTx,uMode,uSP,uShOn,uTime,uStar,uE,uF,uA;uniform vec4 uUV,uC;uniform vec2 uFg;uniform vec4 uPL[3];
float h1(vec2 p){return fract(sin(dot(p,vec2(127.1,311.7)))*43758.5453);}
float ns(vec2 p){vec2 i=floor(p),f=fract(p);f=f*f*(3.-2.*f);return mix(mix(h1(i),h1(i+vec2(1.,0.)),f.x),mix(h1(i+vec2(0.,1.)),h1(i+vec2(1.,1.)),f.x),f.y);}
float upk(vec4 c){return dot(c.rgb,vec3(1.,1./255.,1./65025.));}
void main(){
  if(uSP>.5){   // depth from the sun, packed into colour
    if(uC.a<.6||uE>.5||vC.a>.75)discard;vec3 e=fract(gl_FragCoord.z*vec3(1.,255.,65025.));e.xy-=e.yz/255.;gl_FragColor=vec4(e,1.);return;}
  float dist=distance(vW,uCam);
  if(uMode>.5&&uMode<1.5){   // sky: gradient, drifting cloud, sun or moon, stars
    vec3 d=normalize(vW-uCam);float el=d.y;vec3 c=mix(uSk0,uSk1,pow(clamp(el,0.,1.),.55));
    vec2 q=d.xz/(abs(el)+.22)*1.6+uTime*vec2(.006,.003);float cl=ns(q)*.55+ns(q*2.3)*.3+ns(q*5.1)*.15;
    c=mix(c,mix(uSk0,uAT+uSunC*.45,.7),smoothstep(.5,.78,cl)*smoothstep(.02,.3,el)*.85);
    float s=max(dot(d,normalize(uSd)),0.);c+=uDc*(pow(s,900.)*4.+pow(s,40.)*.3);
    c+=vec3(step(.9975,h1(floor(d.xz/(abs(el)+.3)*260.))))*uStar*smoothstep(.05,.3,el);
    gl_FragColor=vec4(mix(uFog,c,smoothstep(-.03,.1,el)),1.);return;}
  vec3 n=normalize(vN);
  if(uMode>2.5){float g=pow(max(dot(n,normalize(uCam-vW)),0.),2.6);gl_FragColor=vec4(uC.rgb*g*uC.a,1.);return;}   // soft glow, added on top
  if(uMode>1.5){   // water: moving ripples, sky on the surface, sun glitter
    vec2 p=vW.xz*.035;float w1=ns(p+uTime*vec2(.25,.1)),w2=ns(p*2.7-uTime*vec2(.2,.3));n=normalize(vec3((w1-.5)*.55,1.,(w2-.5)*.55));
    vec3 v=normalize(uCam-vW);float fr=pow(1.-max(dot(n,v),0.),3.);vec3 c=mix(vec3(.06,.22,.28)*(uA*.6+.4),uSk1*.85+.08,fr*.8+.12);
    c+=uSunC*pow(max(dot(n,normalize(v+normalize(uSun))),0.),90.)*.9;
    float f=clamp((dist-uFg.x)/uFg.y,0.,1.)*uF;gl_FragColor=vec4(mix(c,uFog,f),.88);return;}
  vec4 bc=vec4(uC.rgb*vC.rgb,uC.a);if(uGLBTexture>.5)bc*=texture2D(uGLBTex,vGLBUV);float mt=vC.a;
  if(uTx>.5&&abs(vZ)>.5){float uu=(vP.x+.5)*uUV.z+uUV.x;if(vZ<0.)uu=1.-uu;bc=vec4(texture2D(uT,vec2(uu,(.5-vP.y)*uUV.w+uUV.y)).rgb,uC.a);}
  else if(mt>.2&&mt<.75){   // surface grain for land and stone; coursed blocks on walls
    float g=ns(vW.xz*.31+vW.y*.17)*.55+ns(vW.xz*.07+vW.y*.05)*.45;bc.rgb*=.82+.36*g;
    if(mt>.45&&abs(n.y)<.5){float u=vW.x*abs(n.z)+vW.z*abs(n.x),row=floor(vW.y/15.),uu=u/31.+mod(row,2.)*.5;vec2 f2=vec2(fract(uu),fract(vW.y/15.));
      float mo=smoothstep(0.,.05,f2.x)*smoothstep(1.,.95,f2.x)*smoothstep(0.,.1,f2.y)*smoothstep(1.,.9,f2.y);
      bc.rgb*=mix(1.,(.68+.32*mo)*(.86+.28*h1(vec2(floor(uu),row))),clamp(1.-dist/700.,0.,1.));}}
  float d=max(dot(n,normalize(uSun)),0.),sh=1.;
  if(uShOn>.5){vec3 q=vL.xyz/vL.w*.5+.5;
    if(q.x>0.&&q.x<1.&&q.y>0.&&q.y<1.&&q.z<1.){float b=.0016+.0045*(1.-d),s=0.,o=.0006;
      s+=step(q.z-b,upk(texture2D(uSh,q.xy+vec2(-o,-o))));s+=step(q.z-b,upk(texture2D(uSh,q.xy+vec2(o,-o))));
      s+=step(q.z-b,upk(texture2D(uSh,q.xy+vec2(-o,o))));s+=step(q.z-b,upk(texture2D(uSh,q.xy+vec2(o,o))));
      vec2 ed=abs(q.xy-.5)*2.;sh=mix(s*.25,1.,smoothstep(.85,1.,max(ed.x,ed.y)));}}
  vec3 lit=(mix(uAB,uAT,n.y*.5+.5)*(.72+.28*sh)+uSunC*d*sh)*uA;float rim=pow(1.-max(dot(n,normalize(uCam-vW)),0.),3.);lit+=uAT*.5*rim*uA;
  for(int i=0;i<3;i++){vec3 l=uPL[i].xyz-vW;float dd=dot(l,l);lit+=vec3(1.,.5,.18)*uPL[i].w*max(dot(n,normalize(l)),.3)/(1.+dd*.0004);}
  vec3 c=mix(bc.rgb*lit,bc.rgb,max(uE,step(.75,mt)));float f=clamp((dist-uFg.x)/uFg.y,0.,1.)*uF;
  c=mix(c,uFog,f);
  // Cinematic Mesopotamian grade: restrained saturation, cool shadow bias, warm ember highlights.
  c=clamp(c,0.,1.);float lum=dot(c,vec3(.299,.587,.114));
  vec3 shadowTint=vec3(.72,.82,1.04),emberTint=vec3(1.12,.88,.66);
  c*=mix(shadowTint,vec3(1.),smoothstep(.08,.62,lum));
  c=mix(c,c*emberTint,smoothstep(.48,.96,lum)*.22);
  float grey=dot(c,vec3(.299,.587,.114));c=mix(vec3(grey),c,.91);
  // Soft filmic shoulder keeps fire, sky and pale stone from clipping on mobile displays.
  c=(c*(2.15*c+.035))/(c*(2.05*c+.52)+.12);
  c=clamp(c,0.,1.);
  gl_FragColor=vec4(c,bc.a);}`;
  const sh=(t,s)=>{const o=gl.createShader(t);gl.shaderSource(o,s);gl.compileShader(o);if(!gl.getShaderParameter(o,gl.COMPILE_STATUS))throw new Error(gl.getShaderInfoLog(o));return o;};
  prog=gl.createProgram();gl.attachShader(prog,sh(gl.VERTEX_SHADER,vs));gl.attachShader(prog,sh(gl.FRAGMENT_SHADER,fs));gl.linkProgram(prog);
  if(!gl.getProgramParameter(prog,gl.LINK_STATUS))throw new Error(gl.getProgramInfoLog(prog));
  gl.useProgram(prog);
  const U=n=>gl.getUniformLocation(prog,n);
  uVP=U('uVP');uM=U('uM');uC=U('uC');uE=U('uE');uF=U('uF');uA=U('uA');uNS=U('uNS');uCam=U('uCam');uFog=U('uFog');uPL=U('uPL');
  uTx=U('uTx');uUV=U('uUV');uSun=U('uSun');uSunC=U('uSunC');uAT=U('uAT');uAB=U('uAB');uFg=U('uFg');
  uMode=U('uMode');uSP=U('uSP');uLVP=U('uLVP');uShOn=U('uShOn');uTime=U('uTime');uSk0=U('uSk0');uSk1=U('uSk1');uSd=U('uSd');uDc=U('uDc');uStar=U('uStar');gl.uniform1i(U('uSh'),1);
  gl.uniform1i(U('uT'),0);gl.uniform1f(uTx,0);gl.uniform4f(uUV,0,0,1,1);gl.uniform2f(uFg,240,640);
  const v=MV,ix=MI,mark=(key,i0,v0)=>{MS[key]=[i0,ix.length-i0,v0,v.length/6-v0];};
  let i0=0,v0=0;
  const face=(nx,ny,nz,ux,uy,uz,wx,wy,wz)=>{const b=v.length/6;
    for(const [s,t] of [[-1,-1],[1,-1],[1,1],[-1,1]])v.push(nx*.5+ux*s*.5+wx*t*.5,ny*.5+uy*s*.5+wy*t*.5,nz*.5+uz*s*.5+wz*t*.5,nx,ny,nz);
    ix.push(b,b+1,b+2,b,b+2,b+3);};
  face(1,0,0, 0,0,-1, 0,1,0);face(-1,0,0, 0,0,1, 0,1,0);
  face(0,1,0, 1,0,0, 0,0,-1);face(0,-1,0, 1,0,0, 0,0,1);
  face(0,0,1, 1,0,0, 0,1,0);face(0,0,-1, -1,0,0, 0,1,0);
  mark('box',0,0);
  i0=ix.length;v0=v.length/6;
  for(let i=0;i<14;i++){const a0=i/14*2*PI,a1=(i+1)/14*2*PI,b=v.length/6;
    const x0=Math.cos(a0)*.5,z0=Math.sin(a0)*.5,x1=Math.cos(a1)*.5,z1=Math.sin(a1)*.5;
    v.push(x0,-.5,z0,x0*2,0,z0*2, x1,-.5,z1,x1*2,0,z1*2, x1,.5,z1,x1*2,0,z1*2, x0,.5,z0,x0*2,0,z0*2);
    ix.push(b,b+2,b+1,b,b+3,b+2);
    v.push(0,.5,0,0,1,0, x0,.5,z0,0,1,0, x1,.5,z1,0,1,0);ix.push(b+4,b+6,b+5);
    v.push(0,-.5,0,0,-1,0, x0,-.5,z0,0,-1,0, x1,-.5,z1,0,-1,0);ix.push(b+7,b+8,b+9);}
  mark('cyl',i0,v0);CYLN=MS.cyl[1];
  const ring=(rt,rb,key)=>{const j0=ix.length,w0=v.length/6,M=16,sl=rb-rt,nl=Math.hypot(1,sl);
    for(let i=0;i<M;i++){const a0=i/M*2*PI,a1=(i+1)/M*2*PI,b=v.length/6,ca=Math.cos(a0),sa=Math.sin(a0),cb=Math.cos(a1),sb_=Math.sin(a1);
      v.push(rb*ca,-.5,rb*sa,ca/nl,sl/nl,sa/nl, rb*cb,-.5,rb*sb_,cb/nl,sl/nl,sb_/nl, rt*cb,.5,rt*sb_,cb/nl,sl/nl,sb_/nl, rt*ca,.5,rt*sa,ca/nl,sl/nl,sa/nl);
      ix.push(b,b+2,b+1,b,b+3,b+2);
      v.push(0,-.5,0,0,-1,0, rb*ca,-.5,rb*sa,0,-1,0, rb*cb,-.5,rb*sb_,0,-1,0);ix.push(b+4,b+5,b+6);
      if(rt>.01){v.push(0,.5,0,0,1,0, rt*ca,.5,rt*sa,0,1,0, rt*cb,.5,rt*sb_,0,1,0);ix.push(b+7,b+9,b+8);}}
    mark(key,j0,w0);};
  ring(.33,.5,'fru');ring(0,.5,'con');
  {const j0=ix.length,w0=v.length/6,SK=10,SL=16;
    for(let i=0;i<=SK;i++){const ph=i/SK*PI,y=Math.cos(ph),r=Math.sin(ph);for(let j=0;j<=SL;j++){const t=j/SL*2*PI,x=r*Math.cos(t),z=r*Math.sin(t);v.push(x*.5,y*.5,z*.5,x,y,z);}}
    for(let i=0;i<SK;i++)for(let j=0;j<SL;j++){const a=w0+i*(SL+1)+j,b=a+SL+1;ix.push(a,b,a+1,a+1,b,b+1);}
    mark('sph',j0,w0);}
  meshVB=gl.createBuffer();gl.bindBuffer(gl.ARRAY_BUFFER,meshVB);gl.bufferData(gl.ARRAY_BUFFER,new Float32Array(v),gl.STATIC_DRAW);
  meshIB=gl.createBuffer();gl.bindBuffer(gl.ELEMENT_ARRAY_BUFFER,meshIB);gl.bufferData(gl.ELEMENT_ARRAY_BUFFER,new Uint16Array(ix),gl.STATIC_DRAW);
  aP=gl.getAttribLocation(prog,'p');aN=gl.getAttribLocation(prog,'n');aC=gl.getAttribLocation(prog,'c');
  gl.enableVertexAttribArray(aP);gl.enableVertexAttribArray(aN);
  bindMesh();
  gl.enable(gl.DEPTH_TEST);gl.depthFunc(gl.LEQUAL);gl.disable(gl.CULL_FACE);
  gl.enable(gl.BLEND);gl.blendFuncSeparate(gl.SRC_ALPHA,gl.ONE_MINUS_SRC_ALPHA,gl.ONE,gl.ONE_MINUS_SRC_ALPHA);
  // logo texture
  const tx=gl.createTexture();gl.activeTexture(gl.TEXTURE0);gl.bindTexture(gl.TEXTURE_2D,tx);
  gl.texImage2D(gl.TEXTURE_2D,0,gl.RGBA,1,1,0,gl.RGBA,gl.UNSIGNED_BYTE,new Uint8Array([255,255,255,255]));
  const tp=()=>{gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_MIN_FILTER,gl.LINEAR);gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_MAG_FILTER,gl.LINEAR);gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_WRAP_S,gl.CLAMP_TO_EDGE);gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_WRAP_T,gl.CLAMP_TO_EDGE);};
  tp();const im=new Image();im.onload=()=>{try{gl.bindTexture(gl.TEXTURE_2D,tx);gl.texImage2D(gl.TEXTURE_2D,0,gl.RGBA,gl.RGBA,gl.UNSIGNED_BYTE,im);tp();}catch(_){}};im.src=LOGO;
  // shadow map: the scene's depth as the sun sees it
  try{gl.activeTexture(gl.TEXTURE1);
    dumTex=gl.createTexture();gl.bindTexture(gl.TEXTURE_2D,dumTex);gl.texImage2D(gl.TEXTURE_2D,0,gl.RGBA,1,1,0,gl.RGBA,gl.UNSIGNED_BYTE,new Uint8Array([255,255,255,255]));
    shTex=gl.createTexture();gl.bindTexture(gl.TEXTURE_2D,shTex);gl.texImage2D(gl.TEXTURE_2D,0,gl.RGBA,SHN,SHN,0,gl.RGBA,gl.UNSIGNED_BYTE,null);
    gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_MIN_FILTER,gl.NEAREST);gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_MAG_FILTER,gl.NEAREST);gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_WRAP_S,gl.CLAMP_TO_EDGE);gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_WRAP_T,gl.CLAMP_TO_EDGE);
    const rb=gl.createRenderbuffer();gl.bindRenderbuffer(gl.RENDERBUFFER,rb);gl.renderbufferStorage(gl.RENDERBUFFER,gl.DEPTH_COMPONENT16,SHN,SHN);
    shFB=gl.createFramebuffer();gl.bindFramebuffer(gl.FRAMEBUFFER,shFB);gl.framebufferTexture2D(gl.FRAMEBUFFER,gl.COLOR_ATTACHMENT0,gl.TEXTURE_2D,shTex,0);gl.framebufferRenderbuffer(gl.FRAMEBUFFER,gl.DEPTH_ATTACHMENT,gl.RENDERBUFFER,rb);
    if(gl.checkFramebufferStatus(gl.FRAMEBUFFER)!==gl.FRAMEBUFFER_COMPLETE)shFB=null;
    gl.bindFramebuffer(gl.FRAMEBUFFER,null);gl.bindTexture(gl.TEXTURE_2D,dumTex);
  }catch(_){shFB=null;}
  gl.activeTexture(gl.TEXTURE0);
}
function bindMesh(){   // unit meshes: constant white vertex colour, no baked emissive
  gl.bindBuffer(gl.ARRAY_BUFFER,meshVB);gl.vertexAttribPointer(aP,3,gl.FLOAT,false,24,0);gl.vertexAttribPointer(aN,3,gl.FLOAT,false,24,12);
  gl.disableVertexAttribArray(aC);gl.vertexAttrib4f(aC,1,1,1,0);gl.bindBuffer(gl.ELEMENT_ARRAY_BUFFER,meshIB);
}
function emit(col,em,al,off,cnt){
  gl.uniformMatrix4fv(uM,false,cur);
  if(gFlash){gl.uniform4f(uC,1,1,1,gAlpha);gl.uniform1f(uE,1);}
  else{const c=hex(col);gl.uniform4f(uC,c[0],c[1],c[2],gAlpha*(al==null?1:al));gl.uniform1f(uE,em||0);}
  gl.drawElements(gl.TRIANGLES,cnt,gl.UNSIGNED_SHORT,off*2);
}
function prim(m,sx,sy,sz,col,cx,cy,cz,em,al){push();T(cx||0,cy||0,cz||0);Sc(sx,sy,sz);gl.uniform3f(uNS,1/(sx*sx),1/(sy*sy),1/(sz*sz));emit(col,em,al,m[0],m[1]);pop();}
function box(sx,sy,sz,col,cx,cy,cz,em,al){prim(MS.box,sx,sy,sz,col,cx,cy,cz,em,al);}
function cyl(sx,sy,sz,col,cx,cy,cz,em,al){prim(MS.cyl,sx,sy,sz,col,cx,cy,cz,em,al);}
function sph(sx,sy,sz,col,cx,cy,cz,em,al){prim(MS.sph,sx,sy,sz,col,cx,cy,cz,em,al);}
function fru(sx,sy,sz,col,cx,cy,cz,em,al){prim(MS.fru,sx,sy,sz,col,cx,cy,cz,em,al);}
function con(sx,sy,sz,col,cx,cy,cz,em,al){prim(MS.con,sx,sy,sz,col,cx,cy,cz,em,al);}
/* ---------- baked static world: everything that never moves goes into a few big buffers ---------- */
let CHK=[];const CHS=760;let chkOpen={};
function chunkFor(x,z,nv){const k=Math.floor(x/CHS)+','+Math.floor(z/CHS);let c=chkOpen[k];
  if(!c||c.n+nv>65000){if(c)closeChunk(c);c=chkOpen[k]={v:[],i:[],n:0,cx:(Math.floor(x/CHS)+.5)*CHS,cz:(Math.floor(z/CHS)+.5)*CHS};}return c;}
function closeChunk(c){if(!c.n)return;const vb=gl.createBuffer();gl.bindBuffer(gl.ARRAY_BUFFER,vb);gl.bufferData(gl.ARRAY_BUFFER,new Float32Array(c.v),gl.STATIC_DRAW);
  const ib=gl.createBuffer();gl.bindBuffer(gl.ELEMENT_ARRAY_BUFFER,ib);gl.bufferData(gl.ELEMENT_ARRAY_BUFFER,new Uint16Array(c.i),gl.STATIC_DRAW);
  CHK.push({vb,ib,cnt:c.i.length,cx:c.cx,cz:c.cz});c.v=null;c.i=null;}
function bakeMesh(m,mat,ns,col,mv){   // mv: 0 plain, .3 grained, .6 masonry, 1 glowing   // m = MS entry; transforms a unit mesh into world space
  const c=chunkFor(mat[12],mat[14],m[3]),v=c.v,b=c.n,e=mv;
  for(let k=0;k<m[3];k++){const o=(m[2]+k)*6,x=MV[o],y=MV[o+1],z=MV[o+2],nx=MV[o+3]*ns[0],ny=MV[o+4]*ns[1],nz=MV[o+5]*ns[2];
    let ax=mat[0]*nx+mat[4]*ny+mat[8]*nz,ay=mat[1]*nx+mat[5]*ny+mat[9]*nz,az=mat[2]*nx+mat[6]*ny+mat[10]*nz;const l=Math.hypot(ax,ay,az)||1;
    v.push(mat[0]*x+mat[4]*y+mat[8]*z+mat[12],mat[1]*x+mat[5]*y+mat[9]*z+mat[13],mat[2]*x+mat[6]*y+mat[10]*z+mat[14],ax/l,ay/l,az/l,col[0],col[1],col[2],e);}
  for(let k=0;k<m[1];k++)c.i.push(b+MI[m[0]+k]-m[2]);
  c.n+=m[3];
}
function bakeTri(ax,ay,az,bx,by,bz,cx,cy,cz,col){   // flat-shaded terrain triangle
  const c=chunkFor((ax+bx+cx)/3,(az+bz+cz)/3,3),v=c.v,b=c.n;
  let nx=(by-ay)*(cz-az)-(bz-az)*(cy-ay),ny=(bz-az)*(cx-ax)-(bx-ax)*(cz-az),nz=(bx-ax)*(cy-ay)-(by-ay)*(cx-ax);const l=Math.hypot(nx,ny,nz)||1;nx/=l;ny/=l;nz/=l;
  if(ny<0){nx=-nx;ny=-ny;nz=-nz;}
  v.push(ax,ay,az,nx,ny,nz,col[0],col[1],col[2],0, bx,by,bz,nx,ny,nz,col[0],col[1],col[2],0, cx,cy,cz,nx,ny,nz,col[0],col[1],col[2],0);
  c.i.push(b,b+1,b+2);c.n+=3;
}
function bakeTri3(p,q,r){   // smooth terrain triangle: each corner is [x,y,z,nx,ny,nz,r,g,b]
  const c=chunkFor((p[0]+q[0]+r[0])/3,(p[2]+q[2]+r[2])/3,3),v=c.v,b=c.n;
  for(const a of[p,q,r])v.push(a[0],a[1],a[2],a[3],a[4],a[5],a[6],a[7],a[8],.3);
  c.i.push(b,b+1,b+2);c.n+=3;
}
function bakeRaw(q){   // q: flat list of [x,y,z,nx,ny,nz,r,g,b,a] per vertex, three per triangle
  const c=chunkFor(q[0][0],q[0][2],q.length),v=c.v,b=c.n;for(const a of q)v.push(a[0],a[1],a[2],a[3],a[4],a[5],a[6],a[7],a[8],a[9]);
  for(let k=0;k<q.length;k++)c.i.push(b+k);c.n+=q.length;}
let ribVB=null;
function ribbon(q,al){   // dynamic triangles (sword trails): q = vertices of [x,y,z,nx,ny,nz,r,g,b,a]
  if(!ribVB)ribVB=gl.createBuffer();const f=new Float32Array(q.length*10);for(let i=0;i<q.length;i++)f.set(q[i],i*10);
  gl.bindBuffer(gl.ARRAY_BUFFER,ribVB);gl.bufferData(gl.ARRAY_BUFFER,f,gl.STREAM_DRAW);
  gl.enableVertexAttribArray(aC);gl.vertexAttribPointer(aP,3,gl.FLOAT,false,40,0);gl.vertexAttribPointer(aN,3,gl.FLOAT,false,40,12);gl.vertexAttribPointer(aC,4,gl.FLOAT,false,40,24);
  gl.uniformMatrix4fv(uM,false,IDM);gl.uniform3f(uNS,1,1,1);gl.uniform4f(uC,1,1,1,al);gl.uniform1f(uE,1);gl.uniform1f(uF,0);
  gl.blendFunc(gl.SRC_ALPHA,gl.ONE);gl.drawArrays(gl.TRIANGLES,0,q.length);
  gl.blendFuncSeparate(gl.SRC_ALPHA,gl.ONE_MINUS_SRC_ALPHA,gl.ONE,gl.ONE_MINUS_SRC_ALPHA);gl.uniform1f(uF,1);bindMesh();}
function bakeDone(){for(const k in chkOpen)closeChunk(chkOpen[k]);chkOpen={};}
function bakeClear(){for(const c of CHK){gl.deleteBuffer(c.vb);gl.deleteBuffer(c.ib);}CHK=[];chkOpen={};}
function resetGLAfterContextRestore(){
  // Handles from the lost context are invalid. Drop them without issuing GL deletes.
  MV.length=0;MI.length=0;
  for(const k of Object.keys(MS))delete MS[k];
  CHK=[];chkOpen={};ribVB=null;meshVB=null;meshIB=null;
  prog=null;shFB=null;shTex=null;dumTex=null;
}
function drawChunks(R){
  const fx=VT[0]-VE[0],fz=VT[2]-VE[2],fl=Math.hypot(fx,fz)||1;
  gl.uniformMatrix4fv(uM,false,IDM);gl.uniform3f(uNS,1,1,1);gl.uniform4f(uC,1,1,1,1);gl.uniform1f(uE,0);gl.uniform1f(uTx,0);
  gl.enableVertexAttribArray(aC);
  for(const c of CHK){const dx=c.cx-VE[0],dz=c.cz-VE[2];if(Math.abs(dx)>R+CHS*.75||Math.abs(dz)>R+CHS*.75||(dx*fx+dz*fz)/fl<-CHS*.8)continue;
    gl.bindBuffer(gl.ARRAY_BUFFER,c.vb);gl.vertexAttribPointer(aP,3,gl.FLOAT,false,40,0);gl.vertexAttribPointer(aN,3,gl.FLOAT,false,40,12);gl.vertexAttribPointer(aC,4,gl.FLOAT,false,40,24);
    gl.bindBuffer(gl.ELEMENT_ARRAY_BUFFER,c.ib);gl.drawElements(gl.TRIANGLES,c.cnt,gl.UNSIGNED_SHORT,0);}
  bindMesh();
}
