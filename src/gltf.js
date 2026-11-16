/* ================= GLB / glTF 2.0 runtime foundation =================
   Parses binary GLB v2 files without a third-party runtime. The parser is
   intentionally separate from the legacy primitive renderer: scene binding,
   PBR shader integration and skin animation are added in subsequent passes.
*/
const GLB_MAGIC=0x46546c67,GLB_VERSION=2;
const GLTF_COMPONENTS={5120:[1,Int8Array],5121:[1,Uint8Array],5122:[2,Int16Array],5123:[2,Uint16Array],5125:[4,Uint32Array],5126:[4,Float32Array]};
const GLTF_WIDTH={SCALAR:1,VEC2:2,VEC3:3,VEC4:4,MAT2:4,MAT3:9,MAT4:16};
function gltfFail(message){throw new Error('GLB: '+message);}
function gltfMatMul(a,b){
  const o=new Float32Array(16);
  for(let c=0;c<4;c++)for(let r=0;r<4;r++)o[c*4+r]=a[r]*b[c*4]+a[4+r]*b[c*4+1]+a[8+r]*b[c*4+2]+a[12+r]*b[c*4+3];
  return o;
}
function gltfQuatSlerp(a,b,t){
  let dot=a[0]*b[0]+a[1]*b[1]+a[2]*b[2]+a[3]*b[3],bb=b;
  if(dot<0){dot=-dot;bb=[-b[0],-b[1],-b[2],-b[3]];}
  if(dot>.9995){const q=a.map((v,i)=>v+(bb[i]-v)*t),l=Math.hypot(...q)||1;return q.map(v=>v/l);}
  const theta=Math.acos(Math.max(-1,Math.min(1,dot))),sin=Math.sin(theta);
  const x=Math.sin((1-t)*theta)/sin,y=Math.sin(t*theta)/sin;
  return a.map((v,i)=>v*x+bb[i]*y);
}
function gltfSampleAnimationSampler(sampler,time,path){
  const times=sampler.input,values=sampler.output,width=sampler.outputWidth;
  if(!times.length||!Number.isFinite(time))gltfFail('animation sample requires finite time and keyframes');
  const cubic=sampler.interpolation==='CUBICSPLINE',stride=width*(cubic?3:1);
  let hi=0;while(hi<times.length&&times[hi]<time)hi++;
  if(hi===0)hi=0;else if(hi>=times.length)hi=times.length-1;
  let lo=Math.max(0,hi-1),t=0,dt=0;
  if(hi!==lo){dt=times[hi]-times[lo];t=Math.max(0,Math.min(1,(time-times[lo])/dt));}
  const valueAt=(key,part= cubic?1:0)=>Array.from(values.slice(key*stride+part*width,key*stride+(part+1)*width));
  if(lo===hi)return valueAt(hi);
  if(sampler.interpolation==='STEP')return valueAt(lo);
  if(cubic){
    const p0=valueAt(lo,1),m0=valueAt(lo,2),p1=valueAt(hi,1),m1=valueAt(hi,0);
    const t2=t*t,t3=t2*t,h00=2*t3-3*t2+1,h10=t3-2*t2+t,h01=-2*t3+3*t2,h11=t3-t2;
    const out=p0.map((v,i)=>h00*v+h10*dt*m0[i]+h01*p1[i]+h11*dt*m1[i]);
    if(path==='rotation'){const l=Math.hypot(...out)||1;return out.map(v=>v/l);}return out;
  }
  const a=valueAt(lo),b=valueAt(hi);
  if(path==='rotation')return gltfQuatSlerp(a,b,t);
  return a.map((v,i)=>v+(b[i]-v)*t);
}
function gltfAnimationLocalTime(animation,elapsed,loop=true){
  if(!Number.isFinite(elapsed))gltfFail('animation elapsed time must be finite');
  let first=Infinity,last=-Infinity;
  for(const sampler of animation.samplers||[]){
    if(!sampler.input?.length)continue;
    first=Math.min(first,sampler.input[0]);last=Math.max(last,sampler.input[sampler.input.length-1]);
  }
  if(!Number.isFinite(first)||!Number.isFinite(last))gltfFail('animation has no keyframe times');
  const duration=last-first;
  if(duration<=0)return first;
  if(loop)return first+(((elapsed%duration)+duration)%duration);
  return Math.max(first,Math.min(last,first+elapsed));
}
function gltfEvaluateAnimation(animation,time){
  const pose=new Map();
  for(const channel of animation.channels){
    const sampler=animation.samplers[channel.sampler];
    const value=gltfSampleAnimationSampler(sampler,time,channel.path);
    let node=pose.get(channel.node);if(!node){node={};pose.set(channel.node,node);}
    node[channel.path]=value;
  }
  return pose;
}
function gltfBlendAnimationPoses(asset,fromPose,toPose,alpha){
  if(!Number.isFinite(alpha))gltfFail('animation blend alpha must be finite');
  const t=Math.max(0,Math.min(1,alpha)),out=new Map(),nodes=new Set([...fromPose.keys(),...toPose.keys()]);
  for(const index of nodes){
    const base=asset.nodes[index]||{},a=fromPose.get(index)||{},b=toPose.get(index)||{},mixed={};
    for(const path of ['translation','rotation','scale']){
      const av=a[path]||base[path],bv=b[path]||base[path];
      if(!av&&!bv)continue;
      const x=av||bv,y=bv||av;
      mixed[path]=path==='rotation'?gltfQuatSlerp(x,y,t):x.map((v,i)=>v+(y[i]-v)*t);
    }
    if(Object.keys(mixed).length)out.set(index,mixed);
  }
  return out;
}
function gltfResolveAnimationClip(asset,action){
  const patterns={
    idle:/idle|stand|breath/i,walk:/walk|move|locomotion/i,run:/run|sprint/i,
    dodge:/dodge|roll|evade/i,attack:/attack|slash|strike|combat/i,
    heavy:/heavy|strong|power/i,jump:/jump|leap/i,death:/death|die|dead/i
  };
  const pattern=patterns[action];if(!pattern)return -1;
  return (asset.animations||[]).findIndex(clip=>pattern.test(clip.name||''));
}
function setGLBHeroAction(action){
  if(!gltfHero) return {ok:false,reason:'no-model'};
  if(typeof action!=='string'||!action)gltfFail('hero action must be a non-empty string');
  const clip=gltfResolveAnimationClip(gltfHero.asset,action);
  if(clip<0)return {ok:false,reason:'clip-not-found',action};
  if(gltfHeroPlayback.clip===clip)return {ok:true,clip,changed:false};
  const transitionDuration=action==='attack'||action==='heavy'||action==='dodge'?0.10:0.20;
  const playback=setGLBHeroAnimation({clip,transitionDuration});
  return {ok:true,clip,changed:true,playback};
}
function gltfNodeMatrix(n){
  if(n.matrix){
    if(n.matrix.length!==16||!n.matrix.every(Number.isFinite))gltfFail('node matrix must contain 16 finite values');
    return new Float32Array(n.matrix);
  }
  const t=n.translation||[0,0,0],q=n.rotation||[0,0,0,1],s=n.scale||[1,1,1];
  if(t.length!==3||q.length!==4||s.length!==3||
     !t.every(Number.isFinite)||!q.every(Number.isFinite)||!s.every(Number.isFinite))gltfFail('invalid or non-finite node TRS');
  const qlen=Math.hypot(q[0],q[1],q[2],q[3]);
  if(qlen<1e-12)gltfFail('node rotation quaternion must be non-zero');
  const [x0,y0,z0,w0]=q,x=x0/qlen,y=y0/qlen,z=z0/qlen,w=w0/qlen,xx=x*x,yy=y*y,zz=z*z,xy=x*y,xz=x*z,yz=y*z,wx=w*x,wy=w*y,wz=w*z;
  return new Float32Array([
    (1-2*(yy+zz))*s[0],(2*(xy+wz))*s[0],(2*(xz-wy))*s[0],0,
    (2*(xy-wz))*s[1],(1-2*(xx+zz))*s[1],(2*(yz+wx))*s[1],0,
    (2*(xz+wy))*s[2],(2*(yz-wx))*s[2],(1-2*(xx+yy))*s[2],0,
    t[0],t[1],t[2],1
  ]);
}
function gltfInvertMat4(a){
  const out=new Float32Array(16),b00=a[0]*a[5]-a[1]*a[4],b01=a[0]*a[6]-a[2]*a[4],b02=a[0]*a[7]-a[3]*a[4],b03=a[1]*a[6]-a[2]*a[5],b04=a[1]*a[7]-a[3]*a[5],b05=a[2]*a[7]-a[3]*a[6],b06=a[8]*a[13]-a[9]*a[12],b07=a[8]*a[14]-a[10]*a[12],b08=a[8]*a[15]-a[11]*a[12],b09=a[9]*a[14]-a[10]*a[13],b10=a[9]*a[15]-a[11]*a[13],b11=a[10]*a[15]-a[11]*a[14];
  let det=b00*b11-b01*b10+b02*b09+b03*b08-b04*b07+b05*b06;
  if(!Number.isFinite(det)||Math.abs(det)<1e-12)gltfFail('cannot invert a singular node transform');det=1/det;
  out[0]=(a[5]*b11-a[6]*b10+a[7]*b09)*det;out[1]=(a[2]*b10-a[1]*b11-a[3]*b09)*det;out[2]=(a[13]*b05-a[14]*b04+a[15]*b03)*det;out[3]=(a[10]*b04-a[9]*b05-a[11]*b03)*det;
  out[4]=(a[6]*b08-a[4]*b11-a[7]*b07)*det;out[5]=(a[0]*b11-a[2]*b08+a[3]*b07)*det;out[6]=(a[14]*b02-a[12]*b05-a[15]*b01)*det;out[7]=(a[8]*b05-a[10]*b02+a[11]*b01)*det;
  out[8]=(a[4]*b10-a[5]*b08+a[7]*b06)*det;out[9]=(a[1]*b08-a[0]*b10-a[3]*b06)*det;out[10]=(a[12]*b04-a[13]*b02+a[15]*b00)*det;out[11]=(a[9]*b02-a[8]*b04-a[11]*b00)*det;
  out[12]=(a[5]*b07-a[4]*b09-a[6]*b06)*det;out[13]=(a[0]*b09-a[1]*b07+a[2]*b06)*det;out[14]=(a[13]*b01-a[12]*b03-a[14]*b00)*det;out[15]=(a[8]*b03-a[9]*b01+a[10]*b00)*det;
  return out;
}
function gltfSkinPrimitiveCPU(primitive,jointMatrices){
  if(!primitive.joints||!primitive.weights)gltfFail('CPU skinning requires JOINTS_0 and WEIGHTS_0');
  const positions=new Float32Array(primitive.positions.length),normals=primitive.normals?new Float32Array(primitive.normals.length):null;
  for(let v=0;v<primitive.positions.length/3;v++){
    let total=0,px=0,py=0,pz=0,nx=0,ny=0,nz=0;
    for(let k=0;k<4;k++){
      const at=v*4+k,w=primitive.weights[at],ji=primitive.joints[at];
      if(w<=0)continue;
      const m=jointMatrices.subarray(ji*16,ji*16+16),p=v*3,x=primitive.positions[p],y=primitive.positions[p+1],z=primitive.positions[p+2];
      px+=(m[0]*x+m[4]*y+m[8]*z+m[12])*w;py+=(m[1]*x+m[5]*y+m[9]*z+m[13])*w;pz+=(m[2]*x+m[6]*y+m[10]*z+m[14])*w;
      if(normals){const n=gltfTransformNormal(m,primitive.normals[p],primitive.normals[p+1],primitive.normals[p+2]);nx+=n[0]*w;ny+=n[1]*w;nz+=n[2]*w;}
      total+=w;
    }
    const p=v*3;if(total>1e-8){positions[p]=px/total;positions[p+1]=py/total;positions[p+2]=pz/total;if(normals){const l=Math.hypot(nx,ny,nz)||1;normals[p]=nx/l;normals[p+1]=ny/l;normals[p+2]=nz/l;}}
    else{positions[p]=primitive.positions[p];positions[p+1]=primitive.positions[p+1];positions[p+2]=primitive.positions[p+2];if(normals)normals.set(primitive.normals.subarray(p,p+3),p);}
  }
  return {positions,normals};
}
function gltfNodeWorldMatrices(asset,pose=new Map()){
  const nodes=asset.nodes||[],world=new Array(nodes.length),active=new Set();
  const parent=new Array(nodes.length).fill(-1);
  for(let i=0;i<nodes.length;i++)for(const child of nodes[i].children||[]){if(parent[child]!==-1)gltfFail('node has multiple parents');parent[child]=i;}
  const resolve=i=>{
    if(world[i])return world[i];if(active.has(i))gltfFail('node hierarchy contains a cycle');active.add(i);
    const n=nodes[i],p=pose.get(i)||{};
    if(n.matrix&&(p.translation||p.rotation||p.scale))gltfFail('animated nodes must use TRS instead of matrix');
    const local=n.matrix?gltfNodeMatrix(n):gltfNodeMatrix({translation:p.translation||n.translation,rotation:p.rotation||n.rotation,scale:p.scale||n.scale});
    world[i]=parent[i]===-1?local:gltfMatMul(resolve(parent[i]),local);active.delete(i);return world[i];
  };
  for(let i=0;i<nodes.length;i++)resolve(i);
  return world;
}
function gltfSkinPrimitiveCPU(primitive,jointMatrices){
  if(!primitive.joints||!primitive.weights)gltfFail('CPU skinning requires JOINTS_0 and WEIGHTS_0');
  if(primitive.joints.length!==primitive.positions.length/3*4||primitive.weights.length!==primitive.joints.length)gltfFail('skinning attributes do not match vertex count');
  if(jointMatrices.length%16)gltfFail('joint matrix palette must contain MAT4 values');
  const positions=new Float32Array(primitive.positions.length),normals=primitive.normals?new Float32Array(primitive.normals.length):null,tangents=primitive.tangents?new Float32Array(primitive.tangents.length):null;
  for(let v=0;v<primitive.positions.length/3;v++){
    const pi=v*3,ji=v*4,x=primitive.positions[pi],y=primitive.positions[pi+1],z=primitive.positions[pi+2];
    let total=0,px=0,py=0,pz=0,nx=0,ny=0,nz=0,tx=0,ty=0,tz=0;
    for(let k=0;k<4;k++){
      const weight=primitive.weights[ji+k];if(weight<=0)continue;
      const joint=primitive.joints[ji+k],offset=joint*16;if(offset+15>=jointMatrices.length)gltfFail('vertex references missing joint matrix');
      const m=jointMatrices.subarray(offset,offset+16);
      px+=(m[0]*x+m[4]*y+m[8]*z+m[12])*weight;py+=(m[1]*x+m[5]*y+m[9]*z+m[13])*weight;pz+=(m[2]*x+m[6]*y+m[10]*z+m[14])*weight;
      if(normals){const n=gltfTransformNormal(m,primitive.normals[pi],primitive.normals[pi+1],primitive.normals[pi+2]);nx+=n[0]*weight;ny+=n[1]*weight;nz+=n[2]*weight;}
      if(tangents){const ti=v*4,xx=primitive.tangents[ti],yy=primitive.tangents[ti+1],zz=primitive.tangents[ti+2];tx+=(m[0]*xx+m[4]*yy+m[8]*zz)*weight;ty+=(m[1]*xx+m[5]*yy+m[9]*zz)*weight;tz+=(m[2]*xx+m[6]*yy+m[10]*zz)*weight;}
      total+=weight;
    }
    if(total<=1e-8){positions.set([x,y,z],pi);if(normals)normals.set(primitive.normals.subarray(pi,pi+3),pi);if(tangents)tangents.set(primitive.tangents.subarray(v*4,v*4+4),v*4);continue;}
    positions[pi]=px/total;positions[pi+1]=py/total;positions[pi+2]=pz/total;
    if(normals){const l=Math.hypot(nx,ny,nz)||1;normals[pi]=nx/l;normals[pi+1]=ny/l;normals[pi+2]=nz/l;}
    if(tangents){const l=Math.hypot(tx,ty,tz)||1,ti=v*4;tangents[ti]=tx/l;tangents[ti+1]=ty/l;tangents[ti+2]=tz/l;tangents[ti+3]=primitive.tangents[ti+3];}
  }
  return {...primitive,positions,normals,tangents};
}
function gltfBuildJointMatrices(asset,skinIndex,meshNode,pose=new Map()){
  const skin=asset.skins?.[skinIndex];if(!skin)gltfFail('missing skin '+skinIndex);
  if(!Number.isInteger(meshNode)||meshNode<0||meshNode>=asset.nodes.length)gltfFail('invalid skinned mesh node');
  const worlds=gltfNodeWorldMatrices(asset,pose),inverseMesh=gltfInvertMat4(worlds[meshNode]),out=new Float32Array(skin.joints.length*16);
  for(let i=0;i<skin.joints.length;i++){
    const jointWorld=worlds[skin.joints[i]],ibm=skin.inverseBindMatrices.subarray(i*16,(i+1)*16);
    out.set(gltfMatMul(gltfMatMul(inverseMesh,jointWorld),ibm),i*16);
  }
  return out;
}
function gltfSkinPrimitiveCPU(primitive,jointMatrices){
  if(!primitive.joints||!primitive.weights)gltfFail('CPU skinning requires JOINTS_0 and WEIGHTS_0');
  const count=primitive.positions.length/3;
  if(primitive.joints.length!==count*4||primitive.weights.length!==count*4)gltfFail('CPU skin attributes do not match vertex count');
  const positions=new Float32Array(primitive.positions.length),normals=primitive.normals?new Float32Array(primitive.normals.length):null,tangents=primitive.tangents?new Float32Array(primitive.tangents.length):null;
  const transform=(m,x,y,z,w)=>[m[0]*x+m[4]*y+m[8]*z+m[12]*w,m[1]*x+m[5]*y+m[9]*z+m[13]*w,m[2]*x+m[6]*y+m[10]*z+m[14]*w];
  for(let v=0;v<count;v++){
    let total=0,px=0,py=0,pz=0,nx=0,ny=0,nz=0,tx=0,ty=0,tz=0;
    for(let k=0;k<4;k++){
      const a=v*4+k,w=primitive.weights[a],j=primitive.joints[a];
      if(!Number.isFinite(w)||w<0||!Number.isInteger(j)||j<0||j*16+15>=jointMatrices.length)gltfFail('invalid CPU skin joint influence');
      if(w===0)continue;const m=jointMatrices.subarray(j*16,j*16+16),q=v*3;
      const p=transform(m,primitive.positions[q],primitive.positions[q+1],primitive.positions[q+2],1);
      px+=p[0]*w;py+=p[1]*w;pz+=p[2]*w;
      if(normals){const n=gltfTransformNormal(m,primitive.normals[q],primitive.normals[q+1],primitive.normals[q+2]);nx+=n[0]*w;ny+=n[1]*w;nz+=n[2]*w;}
      if(tangents){const t=v*4,r=transform(m,primitive.tangents[t],primitive.tangents[t+1],primitive.tangents[t+2],0);tx+=r[0]*w;ty+=r[1]*w;tz+=r[2]*w;}
      total+=w;
    }
    if(total<=1e-8)gltfFail('vertex has no positive skin weights');
    const q=v*3;positions[q]=px/total;positions[q+1]=py/total;positions[q+2]=pz/total;
    if(normals){const l=Math.hypot(nx,ny,nz)||1;normals[q]=nx/l;normals[q+1]=ny/l;normals[q+2]=nz/l;}
    if(tangents){const t=v*4,l=Math.hypot(tx,ty,tz)||1;tangents[t]=tx/l;tangents[t+1]=ty/l;tangents[t+2]=tz/l;tangents[t+3]=primitive.tangents[t+3];}
  }
  return {positions,normals,tangents};
}
function gltfSceneInstances(json,meshCount){
  const nodes=json.nodes||[],scenes=json.scenes||[],instances=[];
  if(!nodes.length)return Array.from({length:meshCount},(_,mesh)=>({mesh,node:-1,matrix:new Float32Array([1,0,0,0,0,1,0,0,0,0,1,0,0,0,0,1])}));
  const sceneIndex=json.scene??0;
  if(scenes.length&&(!Number.isInteger(sceneIndex)||sceneIndex<0||sceneIndex>=scenes.length))gltfFail('selected scene index is out of range');
  const scene=scenes[sceneIndex];
  const roots=scene?(scene.nodes||[]):(nodes.map((_,i)=>i).filter(i=>!nodes.some(n=>(n.children||[]).includes(i))));
  if(!Array.isArray(roots))gltfFail('scene nodes must be an array');
  const active=new Set(),visited=new Set();
  const walk=(index,parent)=>{
    if(!Number.isInteger(index)||index<0||index>=nodes.length)gltfFail('scene references missing node '+index);
    const n=nodes[index];if(!n)gltfFail('scene references missing node '+index);
    if(active.has(index))gltfFail('node hierarchy contains a cycle');
    if(visited.has(index))gltfFail('node is referenced more than once in the selected scene');
    active.add(index);visited.add(index);
    const world=gltfMatMul(parent,gltfNodeMatrix(n));
    if(n.mesh!=null){if(!Number.isInteger(n.mesh)||n.mesh<0||n.mesh>=meshCount)gltfFail('node references missing mesh '+n.mesh);instances.push({mesh:n.mesh,node:index,skin:n.skin??null,matrix:world});}
    if(n.children!=null&&!Array.isArray(n.children))gltfFail('node children must be an array');
    for(const child of n.children||[])walk(child,world);
    active.delete(index);
  };
  const I=new Float32Array([1,0,0,0,0,1,0,0,0,0,1,0,0,0,0,1]);
  for(const root of roots)walk(root,I);
  if(!instances.length&&meshCount)gltfFail('selected scene contains no mesh instances');
  return instances;
}
function parseGLB(buffer){
  if(!(buffer instanceof ArrayBuffer))gltfFail('expected an ArrayBuffer');
  if(buffer.byteLength<20)gltfFail('file is too small');
  const dv=new DataView(buffer);
  if(dv.getUint32(0,true)!==GLB_MAGIC)gltfFail('invalid magic');
  if(dv.getUint32(4,true)!==GLB_VERSION)gltfFail('only GLB version 2 is supported');
  const declared=dv.getUint32(8,true);if(declared>buffer.byteLength||declared<20)gltfFail('invalid declared length');
  let off=12,json=null,bin=null;
  while(off+8<=declared){
    const len=dv.getUint32(off,true),type=dv.getUint32(off+4,true);off+=8;
    if(len>declared-off)gltfFail('chunk exceeds file bounds');
    if(type===0x4e4f534a){if(json)gltfFail('duplicate JSON chunk');json=JSON.parse(new TextDecoder().decode(new Uint8Array(buffer,off,len)).replace(/\\u0000+$/g,'').trim());}
    else if(type===0x004e4942){if(bin)gltfFail('duplicate BIN chunk');bin=buffer.slice(off,off+len);}
    off+=len;
  }
  if(!json)gltfFail('missing JSON chunk');
  if(json.asset?.version!=='2.0')gltfFail('asset is not glTF 2.0');
  if(json.buffers?.length){
    const b=json.buffers[0];
    if(json.buffers.length!==1||b.uri)gltfFail('only a single embedded GLB buffer is supported');
    if(!bin||bin.byteLength<b.byteLength)gltfFail('missing or truncated BIN chunk');
  }
  const readAccessor=index=>{
    const a=json.accessors?.[index];if(!a)gltfFail('missing accessor '+index);
    if(a.sparse)gltfFail('sparse accessors are not supported yet');
    const view=json.bufferViews?.[a.bufferView];if(!view||view.buffer!==0)gltfFail('invalid bufferView');
    const spec=GLTF_COMPONENTS[a.componentType],width=GLTF_WIDTH[a.type];if(!spec||!width)gltfFail('unsupported accessor type');
    const [bytes,Ctor]=spec,stride=view.byteStride||bytes*width,viewStart=view.byteOffset||0,viewLength=view.byteLength,accessorOffset=a.byteOffset||0,base=viewStart+accessorOffset,count=a.count;
    if(view.byteStride!=null&&(!Number.isInteger(view.byteStride)||view.byteStride<4||view.byteStride>252||view.byteStride%4))gltfFail('invalid bufferView byteStride');
    if(view.byteStride!=null&&view.byteStride<bytes*width)gltfFail('bufferView byteStride is smaller than accessor element');
    if(!Number.isInteger(count)||count<1||!Number.isInteger(viewStart)||viewStart<0||!Number.isInteger(viewLength)||viewLength<0||!Number.isInteger(accessorOffset)||accessorOffset<0||stride<bytes*width||stride%bytes||accessorOffset%bytes||base%bytes)gltfFail('invalid accessor layout');
    const end=base+(count-1)*stride+bytes*width;if(end>bin.byteLength||base<viewStart||end>viewStart+viewLength)gltfFail('accessor exceeds bufferView bounds');
    const out=new Float32Array(count*width),src=new DataView(bin);
    const get=(p)=>a.componentType===5126?src.getFloat32(p,true):a.componentType===5125?src.getUint32(p,true):a.componentType===5123?src.getUint16(p,true):a.componentType===5122?src.getInt16(p,true):a.componentType===5121?src.getUint8(p):src.getInt8(p);
    const signed=a.componentType===5120||a.componentType===5122,unsigned=a.componentType===5121||a.componentType===5123||a.componentType===5125;
    for(let i=0;i<count;i++)for(let k=0;k<width;k++){
      let v=get(base+i*stride+k*bytes);
      if(a.normalized&&a.componentType!==5126){if(signed)v=Math.max(-1,v/(bytes===1?127:32767));else if(unsigned)v/=bytes===1?255:bytes===2?65535:4294967295;}
      if(!Number.isFinite(v))gltfFail('accessor contains a non-finite value');
      out[i*width+k]=v;
    }
    return {data:out,count,width,componentType:a.componentType};
  };
  const meshes=(json.meshes||[]).map((mesh,mi)=>({
    name:mesh.name||'mesh_'+mi,
    primitives:(mesh.primitives||[]).map((p,pi)=>{
      if(p.mode!=null&&p.mode!==4)gltfFail('only TRIANGLES primitives are supported');
      const pos=readAccessor(p.attributes?.POSITION);if(pos.width!==3)gltfFail('POSITION must be VEC3');
      const nor=p.attributes.NORMAL==null?null:readAccessor(p.attributes.NORMAL);
      const tan=p.attributes.TANGENT==null?null:readAccessor(p.attributes.TANGENT);
      const joints=p.attributes.JOINTS_0==null?null:readAccessor(p.attributes.JOINTS_0);
      const weights=p.attributes.WEIGHTS_0==null?null:readAccessor(p.attributes.WEIGHTS_0);
      const uv=p.attributes.TEXCOORD_0==null?null:readAccessor(p.attributes.TEXCOORD_0);
      if(nor&&(nor.width!==3||nor.count!==pos.count))gltfFail('NORMAL must be a matching VEC3 accessor');
      if(tan&&(tan.width!==4||tan.count!==pos.count))gltfFail('TANGENT must be a matching VEC4 accessor');
      if((joints&&!weights)||(!joints&&weights))gltfFail('JOINTS_0 and WEIGHTS_0 must be supplied together');
      if(joints&&(joints.width!==4||joints.count!==pos.count||![5121,5123].includes(joints.componentType)))gltfFail('JOINTS_0 must be a matching unsigned byte or short VEC4 accessor');
      if(weights&&(weights.width!==4||weights.count!==pos.count||![5126,5121,5123].includes(weights.componentType)))gltfFail('WEIGHTS_0 must be a matching float or normalized unsigned VEC4 accessor');
      if(weights&&weights.componentType!==5126&&!json.accessors[p.attributes.WEIGHTS_0].normalized)gltfFail('integer WEIGHTS_0 accessor must be normalized');
      if(uv&&(uv.width!==2||uv.count!==pos.count))gltfFail('TEXCOORD_0 count mismatch');
      let indices;
      if(p.indices!=null){const q=readAccessor(p.indices);if(q.width!==1||![5121,5123,5125].includes(q.componentType))gltfFail('indices must use an unsigned integer SCALAR accessor');indices=q.data;}
      else{indices=new Uint32Array(pos.count);for(let i=0;i<pos.count;i++)indices[i]=i;}
      if(indices.length%3)gltfFail('triangle index count is not divisible by 3');
      for(const n of indices)if(n>=pos.count)gltfFail('index is outside POSITION range');
      if(p.material!=null&&(!Number.isInteger(p.material)||!json.materials?.[p.material]))gltfFail('primitive references missing material '+p.material);
      const mat=json.materials?.[p.material]||{};
      const pm=mat.pbrMetallicRoughness||{};
      const normalTexture=mat.normalTexture||null;
      if(normalTexture&&(!Number.isInteger(normalTexture.index)||normalTexture.index<0))gltfFail('normalTexture has an invalid texture index');
      if(normalTexture&&normalTexture.texCoord!=null&&normalTexture.texCoord!==0)gltfFail('only TEXCOORD_0 is supported for normal maps');
      if(normalTexture&&normalTexture.scale!=null&&!Number.isFinite(normalTexture.scale))gltfFail('normalTexture scale must be finite');
      return {name:mesh.name||'mesh_'+mi,positions:pos.data,normals:nor?.data||null,tangents:tan?.data||null,joints:joints?.data||null,weights:weights?.data||null,uvs:uv?.data||null,indices,
        material:{name:mat.name||'material_'+(p.material??0),baseColorFactor:pm.baseColorFactor||[1,1,1,1],
          metallicFactor:pm.metallicFactor??1,roughnessFactor:pm.roughnessFactor??1,
          baseColorTexture:pm.baseColorTexture?.index??null,
          normalTexture:normalTexture?.index??null,normalScale:normalTexture?.scale??1,
          alphaMode:mat.alphaMode||'OPAQUE',doubleSided:!!mat.doubleSided}};
    })
  }));
  const instances=gltfSceneInstances(json,meshes.length);
  const nodes=json.nodes||[];
  const skins=(json.skins||[]).map((skin,index)=>{
    if(!Array.isArray(skin.joints)||!skin.joints.length)gltfFail('skin '+index+' must contain joints');
    const seen=new Set();
    for(const joint of skin.joints){if(!Number.isInteger(joint)||joint<0||joint>=nodes.length)gltfFail('skin '+index+' references missing joint node');if(seen.has(joint))gltfFail('skin '+index+' contains duplicate joints');seen.add(joint);}
    if(skin.skeleton!=null&&(!Number.isInteger(skin.skeleton)||skin.skeleton<0||skin.skeleton>=nodes.length))gltfFail('skin '+index+' references missing skeleton node');
    let inverseBindMatrices=null;
    if(skin.inverseBindMatrices!=null){const a=readAccessor(skin.inverseBindMatrices);if(a.width!==16||a.componentType!==5126||a.count!==skin.joints.length)gltfFail('skin '+index+' inverseBindMatrices must be a matching FLOAT MAT4 accessor');inverseBindMatrices=a.data;}
    else{inverseBindMatrices=new Float32Array(skin.joints.length*16);for(let j=0;j<skin.joints.length;j++){inverseBindMatrices[j*16]=1;inverseBindMatrices[j*16+5]=1;inverseBindMatrices[j*16+10]=1;inverseBindMatrices[j*16+15]=1;}}
    return {name:skin.name||'skin_'+index,joints:skin.joints.slice(),skeleton:skin.skeleton??null,inverseBindMatrices};
  });
  for(const instance of instances){const node=nodes[instance.node];if(node?.skin!=null&&(!Number.isInteger(node.skin)||!skins[node.skin]))gltfFail('node references missing skin '+node.skin);if(node?.skin!=null){const skin=skins[node.skin],mesh=meshes[instance.mesh];for(const p of mesh.primitives){if(!p.joints||!p.weights)gltfFail('skinned mesh primitive is missing JOINTS_0 or WEIGHTS_0');for(let i=0;i<p.joints.length;i++)if(p.joints[i]>=skin.joints.length)gltfFail('vertex joint index exceeds skin joint count');}}}
  const animations=(json.animations||[]).map((animation,index)=>{
    const samplers=(animation.samplers||[]).map((sampler,si)=>{
      if(!Number.isInteger(sampler.input)||!Number.isInteger(sampler.output))gltfFail('animation sampler has invalid accessor references');
      const input=readAccessor(sampler.input),output=readAccessor(sampler.output),interpolation=sampler.interpolation||'LINEAR';
      if(input.width!==1||input.componentType!==5126)gltfFail('animation input must be a FLOAT SCALAR accessor');
      if(!['LINEAR','STEP','CUBICSPLINE'].includes(interpolation))gltfFail('animation sampler has unsupported interpolation');
      for(let i=1;i<input.data.length;i++)if(input.data[i]<=input.data[i-1])gltfFail('animation keyframe times must be strictly increasing');
      return {input:input.data,output:output.data,outputWidth:output.width,interpolation};
    });
    const channels=(animation.channels||[]).map(channel=>{
      const sampler=channel.sampler,target=channel.target||{},path=target.path,node=target.node;
      if(!Number.isInteger(sampler)||!samplers[sampler])gltfFail('animation channel references missing sampler');
      if(!Number.isInteger(node)||node<0||node>=nodes.length)gltfFail('animation channel references missing node');
      if(!['translation','rotation','scale','weights'].includes(path))gltfFail('animation channel has unsupported target path');
      const sample=samplers[sampler],expected=path==='rotation'?4:path==='weights'?null:3;
      if(expected!=null&&sample.outputWidth!==expected)gltfFail('animation output width does not match target path');
      const multiplier=sample.interpolation==='CUBICSPLINE'?3:1;
      if(sample.output.length!==sample.input.length*(expected||sample.outputWidth)*multiplier)gltfFail('animation sampler input/output counts do not match');
      return {sampler,node,path};
    });
    return {name:animation.name||'animation_'+index,samplers,channels};
  });
  const images=(json.images||[]).map((image,index)=>{
    if(image.uri)gltfFail('external image URIs are not supported yet');
    const view=json.bufferViews?.[image.bufferView];
    if(!view||view.buffer!==0||!['image/png','image/jpeg'].includes(image.mimeType))gltfFail('unsupported or invalid embedded image '+index);
    const start=view.byteOffset||0,end=start+view.byteLength;
    if(!Number.isInteger(start)||!Number.isInteger(view.byteLength)||start<0||view.byteLength<1||end>bin.byteLength)gltfFail('embedded image exceeds BIN bounds');
    return {mimeType:image.mimeType,bytes:new Uint8Array(bin.slice(start,end))};
  });
  const samplers=(json.samplers||[]).map((sampler,index)=>{
    const wrapS=sampler.wrapS??10497,wrapT=sampler.wrapT??10497;
    const minFilter=sampler.minFilter??9987,magFilter=sampler.magFilter??9729;
    if(![33071,33648,10497].includes(wrapS)||![33071,33648,10497].includes(wrapT))gltfFail('sampler '+index+' has an invalid wrap mode');
    if(![9728,9729,9984,9985,9986,9987].includes(minFilter)||![9728,9729].includes(magFilter))gltfFail('sampler '+index+' has an invalid filter');
    return {wrapS,wrapT,minFilter,magFilter};
  });
  const textures=(json.textures||[]).map((texture,index)=>{
    if(!Number.isInteger(texture.source)||!images[texture.source])gltfFail('texture '+index+' references missing image');
    if(texture.sampler!=null&&(!Number.isInteger(texture.sampler)||!samplers[texture.sampler]))gltfFail('texture '+index+' references missing sampler');
    return {source:texture.source,sampler:texture.sampler==null?{wrapS:10497,wrapT:10497,minFilter:9987,magFilter:9729}:samplers[texture.sampler]};
  });
  for(const mesh of meshes)for(const primitive of mesh.primitives){
    const ti=primitive.material.baseColorTexture,ni=primitive.material.normalTexture;
    if(ti!=null&&!textures[ti])gltfFail('material references missing base-colour texture '+ti);
    if(ni!=null&&!textures[ni])gltfFail('material references missing normal texture '+ni);
  }
  return {asset:json.asset,meshes,nodes,scenes:json.scenes||[],scene:json.scene||0,instances,skins,animations,images,textures,
    bounds:meshes.reduce((n,m)=>n+m.primitives.length,0),extensionsUsed:json.extensionsUsed||[]};
}
async function loadGLB(url){
  const response=await fetch(url,{credentials:'same-origin'});
  if(!response.ok)gltfFail('HTTP '+response.status+' while loading '+url);
  return parseGLB(await response.arrayBuffer());
}


/* ---------- GLB -> existing WebGL renderer bridge ---------- */
let gltfHero=null,gltfHeroGPU=null,gltfHeroTextures=[],gltfHeroError='';
let gltfHeroPlayback={clip:0,speed:1,paused:false,elapsed:0,lastTime:null,loop:true,transition:null,currentPose:new Map()};
function gltfNormals(positions,indices){
  const n=new Float32Array(positions.length);
  for(let i=0;i<indices.length;i+=3){
    const a=indices[i]*3,b=indices[i+1]*3,c=indices[i+2]*3;
    const abx=positions[b]-positions[a],aby=positions[b+1]-positions[a+1],abz=positions[b+2]-positions[a+2];
    const acx=positions[c]-positions[a],acy=positions[c+1]-positions[a+1],acz=positions[c+2]-positions[a+2];
    const nx=aby*acz-abz*acy,ny=abz*acx-abx*acz,nz=abx*acy-aby*acx;
    for(const q of[a,b,c]){n[q]+=nx;n[q+1]+=ny;n[q+2]+=nz;}
  }
  for(let i=0;i<n.length;i+=3){const l=Math.hypot(n[i],n[i+1],n[i+2])||1;n[i]/=l;n[i+1]/=l;n[i+2]/=l;}
  return n;
}
function gltfTransformNormal(m,x,y,z){
  // Inverse-transpose of the upper-left 3x3; required for non-uniform scale.
  const a=m[0],b=m[4],c=m[8],d=m[1],e=m[5],f=m[9],g=m[2],h=m[6],i=m[10];
  const c00=e*i-f*h,c01=f*g-d*i,c02=d*h-e*g;
  const c10=c*h-b*i,c11=a*i-c*g,c12=b*g-a*h;
  const c20=b*f-c*e,c21=c*d-a*f,c22=a*e-b*d;
  const det=a*c00+b*c01+c*c02;
  if(!Number.isFinite(det)||Math.abs(det)<1e-12)gltfFail('node transform has a singular normal matrix');
  const invDet=1/det,nx=(c00*x+c01*y+c02*z)*invDet,ny=(c10*x+c11*y+c12*z)*invDet,nz=(c20*x+c21*y+c22*z)*invDet;
  const len=Math.hypot(nx,ny,nz)||1;
  return [nx/len,ny/len,nz/len];
}
function gltfExpandPrimitive(p){
  const srcN=p.normals||gltfNormals(p.positions,p.indices),out=new Float32Array(p.indices.length*12);
  for(let i=0;i<p.indices.length;i++){
    const j=p.indices[i],k=i*12,q=j*3,u=p.uvs?j*2:-1,t=j*4;
    out[k]=p.positions[q];out[k+1]=p.positions[q+1];out[k+2]=p.positions[q+2];
    out[k+3]=srcN[q];out[k+4]=srcN[q+1];out[k+5]=srcN[q+2];
    out[k+6]=p.uvs?p.uvs[u]:0;out[k+7]=p.uvs?p.uvs[u+1]:0;
    if(p.tangents){out[k+8]=p.tangents[t];out[k+9]=p.tangents[t+1];out[k+10]=p.tangents[t+2];out[k+11]=p.tangents[t+3];}
    else{out[k+8]=1;out[k+9]=0;out[k+10]=0;out[k+11]=1;}
  }
  return out;
}
function gltfTransformExpanded(data,m){
  for(let i=0;i<data.length;i+=12){
    const x=data[i],y=data[i+1],z=data[i+2],nx=data[i+3],ny=data[i+4],nz=data[i+5];
    data[i]=m[0]*x+m[4]*y+m[8]*z+m[12];data[i+1]=m[1]*x+m[5]*y+m[9]*z+m[13];data[i+2]=m[2]*x+m[6]*y+m[10]*z+m[14];
    const normal=gltfTransformNormal(m,nx,ny,nz);data[i+3]=normal[0];data[i+4]=normal[1];data[i+5]=normal[2];
    const tx=data[i+8],ty=data[i+9],tz=data[i+10],tl=Math.hypot(m[0]*tx+m[4]*ty+m[8]*tz,m[1]*tx+m[5]*ty+m[9]*tz,m[2]*tx+m[6]*ty+m[10]*tz)||1;
    data[i+8]=(m[0]*tx+m[4]*ty+m[8]*tz)/tl;data[i+9]=(m[1]*tx+m[5]*ty+m[9]*tz)/tl;data[i+10]=(m[2]*tx+m[6]*ty+m[10]*tz)/tl;
  }
  return data;
}
function prepareGLBHero(asset){
  const entries=[],instances=asset.instances||asset.meshes.map((_,mesh)=>({mesh,node:-1,skin:null,matrix:new Float32Array([1,0,0,0,0,1,0,0,0,0,1,0,0,0,0,1])}));
  for(const instance of instances){
    const mesh=asset.meshes[instance.mesh];
    for(const p of mesh.primitives){
      let data=gltfExpandPrimitive(p);
      if(instance.skin!=null){const palette=gltfBuildJointMatrices(asset,instance.skin,instance.node);data=gltfExpandPrimitive(gltfSkinPrimitiveCPU(p,palette));}
      gltfTransformExpanded(data,instance.matrix);
      const buffer=gl.createBuffer();gl.bindBuffer(gl.ARRAY_BUFFER,buffer);gl.bufferData(gl.ARRAY_BUFFER,data,instance.skin!=null?gl.DYNAMIC_DRAW:gl.STATIC_DRAW);
      entries.push({buffer,count:p.indices.length,material:p.material,primitive:p,node:instance.node,skin:instance.skin,matrix:instance.matrix,stride:48,texture:p.material.baseColorTexture,normalTexture:p.material.normalTexture,hasTangents:!!p.tangents,hasUVs:!!p.uvs});
    }
  }
  return entries;
}
function setGLBHeroAnimation(options={}){
  if(!gltfHero)gltfFail('no GLB hero is loaded');
  const next={...gltfHeroPlayback};
  if(options.clip!=null){
    if(!Number.isInteger(options.clip)||options.clip<0||options.clip>=gltfHero.asset.animations.length)gltfFail('animation clip index is out of range');
    if(options.clip!==next.clip){
      const duration=options.transitionDuration??0;
      if(!Number.isFinite(duration)||duration<0||duration>10)gltfFail('transitionDuration must be between 0 and 10 seconds');
      if(duration>0&&next.currentPose?.size)next.transition={fromPose:next.currentPose,duration,elapsed:0};
      else next.transition=null;
      next.clip=options.clip;next.elapsed=0;next.lastTime=null;
    }
  }
  if(options.speed!=null){if(!Number.isFinite(options.speed)||options.speed<0||options.speed>4)gltfFail('animation speed must be between 0 and 4');next.speed=options.speed;}
  if(options.paused!=null){if(typeof options.paused!=='boolean')gltfFail('animation paused must be boolean');next.paused=options.paused;}
  if(options.loop!=null){if(typeof options.loop!=='boolean')gltfFail('animation loop must be boolean');next.loop=options.loop;}
  if(options.position!=null){if(!Number.isFinite(options.position)||options.position<0)gltfFail('animation position must be a non-negative finite number');next.elapsed=options.position;next.lastTime=null;}
  gltfHeroPlayback=next;
  return {clip:next.clip,speed:next.speed,paused:next.paused,loop:next.loop,position:next.elapsed,transition:next.transition?next.transition.duration:0};
}
function updateGLBHeroSkinning(now){
  if(!gltfHero||!gltfHeroGPU||!gltfHero.asset.animations?.length)return;
  const asset=gltfHero.asset,playback=gltfHeroPlayback,animation=asset.animations[playback.clip];
  if(!animation?.channels.length)return;
  const clock=Number.isFinite(now)?now:0;
  if(playback.lastTime==null)playback.lastTime=clock;
  else{const dt=Math.max(0,clock-playback.lastTime);if(!playback.paused){playback.elapsed+=dt*playback.speed;if(playback.transition)playback.transition.elapsed+=dt;}playback.lastTime=clock;}
  const clipTime=gltfAnimationLocalTime(animation,playback.elapsed,playback.loop);
  let pose=gltfEvaluateAnimation(animation,clipTime);
  if(playback.transition){
    const alpha=playback.transition.elapsed/playback.transition.duration;
    pose=gltfBlendAnimationPoses(asset,playback.transition.fromPose,pose,alpha);
    if(alpha>=1)playback.transition=null;
  }
  playback.currentPose=pose;
  const palettes=new Map();
  for(const item of gltfHeroGPU){
    if(item.skin==null)continue;
    let palette=palettes.get(item.skin+':'+item.node);
    if(!palette){palette=gltfBuildJointMatrices(asset,item.skin,item.node,pose);palettes.set(item.skin+':'+item.node,palette);}
    const deformed=gltfSkinPrimitiveCPU(item.primitive,palette),data=gltfTransformExpanded(gltfExpandPrimitive(deformed),item.matrix);
    gl.bindBuffer(gl.ARRAY_BUFFER,item.buffer);gl.bufferData(gl.ARRAY_BUFFER,data,gl.DYNAMIC_DRAW);
  }
}
async function prepareGLBTextures(asset){
  const out=[];
  for(const spec of asset.textures||[]){
    const image=asset.images[spec.source],sampler=spec.sampler;
    const blob=new Blob([image.bytes],{type:image.mimeType});
    let bitmap=null;
    if(typeof createImageBitmap==='function')bitmap=await createImageBitmap(blob);
    else bitmap=await new Promise((resolve,reject)=>{
      const objectUrl=URL.createObjectURL(blob),img=new Image();
      img.onload=()=>{URL.revokeObjectURL(objectUrl);resolve(img);};
      img.onerror=()=>{URL.revokeObjectURL(objectUrl);reject(new Error('GLB: embedded image decode failed'));};
      img.src=objectUrl;
    });
    const texture=gl.createTexture();
    gl.bindTexture(gl.TEXTURE_2D,texture);
    gl.pixelStorei(gl.UNPACK_FLIP_Y_WEBGL,true);
    gl.texImage2D(gl.TEXTURE_2D,0,gl.RGBA,gl.RGBA,gl.UNSIGNED_BYTE,bitmap);
    const pot=(n)=>(n&(n-1))===0;
    const isPOT=pot(bitmap.width)&&pot(bitmap.height);
    const wrap=(v)=>v===33071?gl.CLAMP_TO_EDGE:v===33648?gl.MIRRORED_REPEAT:gl.REPEAT;
    const filter=(v)=>v===9728?gl.NEAREST:v===9729?gl.LINEAR:v===9984?gl.NEAREST_MIPMAP_NEAREST:v===9985?gl.LINEAR_MIPMAP_NEAREST:v===9986?gl.NEAREST_MIPMAP_LINEAR:gl.LINEAR_MIPMAP_LINEAR;
    const wantsMip=sampler.minFilter>=9984;
    gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_MIN_FILTER,wantsMip&&!isPOT?gl.LINEAR:filter(sampler.minFilter));
    gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_MAG_FILTER,sampler.magFilter===9728?gl.NEAREST:gl.LINEAR);
    gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_WRAP_S,isPOT?wrap(sampler.wrapS):gl.CLAMP_TO_EDGE);
    gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_WRAP_T,isPOT?wrap(sampler.wrapT):gl.CLAMP_TO_EDGE);
    if(wantsMip&&isPOT)gl.generateMipmap(gl.TEXTURE_2D);
    bitmap.close?.();
    out.push(texture);
  }
  gl.pixelStorei(gl.UNPACK_FLIP_Y_WEBGL,false);
  gl.activeTexture(gl.TEXTURE0);
  return out;
}
async function setGLBHero(url,scale=60){
  gltfHero=null;gltfHeroGPU=null;gltfHeroTextures=[];gltfHeroError='';
  try{
    const asset=await loadGLB(url);
    if(!asset.meshes.length)gltfFail('model contains no meshes');
    gltfHero={asset,scale};
    if(glOK){gltfHeroTextures=await prepareGLBTextures(asset);gltfHeroGPU=prepareGLBHero(asset);}
    return {ok:true,meshes:asset.meshes.length,primitives:asset.bounds};
  }catch(e){gltfHeroError=String(e&&e.message||e);return {ok:false,error:gltfHeroError};}
}
if(typeof glc!=='undefined')glc.addEventListener('webglcontextrestored',()=>{
  if(!gltfHero)return;
  gltfHeroGPU=null;gltfHeroTextures=[];
  prepareGLBTextures(gltfHero.asset).then(textures=>{
    gltfHeroTextures=textures;
    if(glOK)gltfHeroGPU=prepareGLBHero(gltfHero.asset);
  }).catch(error=>{gltfHeroError=String(error&&error.message||error);});
});
function drawGLBHero(){
  if(!gltfHero||!glOK)return false;
  if(!gltfHeroGPU){try{gltfHeroGPU=prepareGLBHero(gltfHero.asset);}catch(e){gltfHeroError=String(e);return false;}}
  try{updateGLBHeroSkinning(typeof time==='number'?time:0);}catch(e){gltfHeroError=String(e&&e.message||e);}
  push();T(P.x,P.ry,P.z);RY(-P.a);Sc(gltfHero.scale,gltfHero.scale,gltfHero.scale);
  gl.uniformMatrix4fv(uM,false,cur);gl.uniform3f(uNS,1/(gltfHero.scale*gltfHero.scale),1/(gltfHero.scale*gltfHero.scale),1/(gltfHero.scale*gltfHero.scale));
  gl.uniform1f(uTx,0);gl.uniform1f(uE,0);
  for(const item of gltfHeroGPU){
    const m=item.material,f=m.baseColorFactor;
    gl.uniform4f(uC,f[0],f[1],f[2],f[3]);
    gl.bindBuffer(gl.ARRAY_BUFFER,item.buffer);
    gl.enableVertexAttribArray(aP);gl.enableVertexAttribArray(aN);
    gl.vertexAttribPointer(aP,3,gl.FLOAT,false,48,0);gl.vertexAttribPointer(aN,3,gl.FLOAT,false,48,12);
    const uvLoc=gl.getAttribLocation(prog,'uv');
    if(uvLoc>=0){gl.enableVertexAttribArray(uvLoc);gl.vertexAttribPointer(uvLoc,2,gl.FLOAT,false,48,24);}
    gl.disableVertexAttribArray(aC);gl.vertexAttrib4f(aC,1,1,1,0);
    const tangentLoc=gl.getAttribLocation(prog,'tangent');if(tangentLoc>=0){gl.enableVertexAttribArray(tangentLoc);gl.vertexAttribPointer(tangentLoc,4,gl.FLOAT,false,48,32);}
    const texIndex=item.texture==null?-1:gltfHero.asset.textures[item.texture]?.source??-1;
    const tex=texIndex>=0?gltfHeroTextures[texIndex]:null;
    gl.uniform1f(gl.getUniformLocation(prog,'uGLBTexture'),tex?1:0);
    gl.uniform1f(gl.getUniformLocation(prog,'uGLBMaterial'),1);
    gl.uniform1f(gl.getUniformLocation(prog,'uGLBMetallic'),m.metallicFactor??1);
    gl.uniform1f(gl.getUniformLocation(prog,'uGLBRoughness'),m.roughnessFactor??1);
    if(tex){gl.activeTexture(gl.TEXTURE2);gl.bindTexture(gl.TEXTURE_2D,tex);gl.uniform1i(gl.getUniformLocation(prog,'uGLBTex'),2);}
    const normalTex=item.normalTexture==null?null:gltfHeroTextures[item.normalTexture];const useNormal=!!(normalTex&&item.hasTangents&&item.hasUVs);
    gl.uniform1f(gl.getUniformLocation(prog,'uGLBNormalMap'),useNormal?1:0);gl.uniform1f(gl.getUniformLocation(prog,'uGLBNormalScale'),m.normalScale??1);
    if(useNormal){gl.activeTexture(gl.TEXTURE3);gl.bindTexture(gl.TEXTURE_2D,normalTex);gl.uniform1i(gl.getUniformLocation(prog,'uGLBNormalTex'),3);}
    gl.drawArrays(gl.TRIANGLES,0,item.count);
  }
  gl.uniform1f(gl.getUniformLocation(prog,'uGLBTexture'),0);gl.uniform1f(gl.getUniformLocation(prog,'uGLBMaterial'),0);gl.uniform1f(gl.getUniformLocation(prog,'uGLBNormalMap'),0);gl.activeTexture(gl.TEXTURE0);
  const uvLoc=gl.getAttribLocation(prog,'uv');if(uvLoc>=0)gl.disableVertexAttribArray(uvLoc);
  pop();bindMesh();gAlpha=1;return true;
}
