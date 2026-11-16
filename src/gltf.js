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
function gltfNodeMatrix(n){
  if(n.matrix){if(n.matrix.length!==16)gltfFail('node matrix must contain 16 values');return new Float32Array(n.matrix);}
  const t=n.translation||[0,0,0],q=n.rotation||[0,0,0,1],s=n.scale||[1,1,1];
  if(t.length!==3||q.length!==4||s.length!==3)gltfFail('invalid node TRS');
  const [x,y,z,w]=q,xx=x*x,yy=y*y,zz=z*z,xy=x*y,xz=x*z,yz=y*z,wx=w*x,wy=w*y,wz=w*z;
  return new Float32Array([
    (1-2*(yy+zz))*s[0],(2*(xy+wz))*s[0],(2*(xz-wy))*s[0],0,
    (2*(xy-wz))*s[1],(1-2*(xx+zz))*s[1],(2*(yz+wx))*s[1],0,
    (2*(xz+wy))*s[2],(2*(yz-wx))*s[2],(1-2*(xx+yy))*s[2],0,
    t[0],t[1],t[2],1
  ]);
}
function gltfSceneInstances(json,meshCount){
  const nodes=json.nodes||[],scenes=json.scenes||[],instances=[];
  if(!nodes.length)return Array.from({length:meshCount},(_,mesh)=>({mesh,node:-1,matrix:new Float32Array([1,0,0,0,0,1,0,0,0,0,1,0,0,0,0,1])}));
  const scene=scenes[json.scene||0];
  const roots=scene?.nodes||nodes.map((_,i)=>i).filter(i=>!nodes.some(n=>(n.children||[]).includes(i)));
  const active=new Set(),visited=new Set();
  const walk=(index,parent)=>{
    const n=nodes[index];if(!n)gltfFail('scene references missing node '+index);
    if(active.has(index))gltfFail('node hierarchy contains a cycle');
    active.add(index);visited.add(index);
    const world=gltfMatMul(parent,gltfNodeMatrix(n));
    if(n.mesh!=null){if(!Number.isInteger(n.mesh)||n.mesh<0||n.mesh>=meshCount)gltfFail('node references missing mesh '+n.mesh);instances.push({mesh:n.mesh,node:index,matrix:world});}
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
    if(!Number.isInteger(count)||count<1||!Number.isInteger(viewStart)||viewStart<0||!Number.isInteger(viewLength)||viewLength<0||!Number.isInteger(accessorOffset)||accessorOffset<0||stride<bytes*width||stride%bytes||accessorOffset%bytes||base%bytes)gltfFail('invalid accessor layout');
    if(view.byteStride!=null&&(!Number.isInteger(view.byteStride)||view.byteStride<4||view.byteStride>252||view.byteStride%4))gltfFail('invalid bufferView byteStride');
    if(view.byteStride!=null&&view.byteStride<bytes*width)gltfFail('bufferView byteStride is smaller than accessor element');
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
      const uv=p.attributes.TEXCOORD_0==null?null:readAccessor(p.attributes.TEXCOORD_0);
      if(nor&&(nor.width!==3||nor.count!==pos.count))gltfFail('NORMAL must be a matching VEC3 accessor');
      if(uv&&(uv.width!==2||uv.count!==pos.count))gltfFail('TEXCOORD_0 count mismatch');
      let indices;
      if(p.indices!=null){const q=readAccessor(p.indices);if(q.width!==1||![5121,5123,5125].includes(q.componentType))gltfFail('indices must use an unsigned integer SCALAR accessor');indices=q.data;}
      else{indices=new Uint32Array(pos.count);for(let i=0;i<pos.count;i++)indices[i]=i;}
      if(indices.length%3)gltfFail('triangle index count is not divisible by 3');
      for(const n of indices)if(n>=pos.count)gltfFail('index is outside POSITION range');
      const mat=json.materials?.[p.material]||{};
      const pm=mat.pbrMetallicRoughness||{};
      return {name:mesh.name||'mesh_'+mi,positions:pos.data,normals:nor?.data||null,uvs:uv?.data||null,indices,
        material:{name:mat.name||'material_'+(p.material??0),baseColorFactor:pm.baseColorFactor||[1,1,1,1],
          metallicFactor:pm.metallicFactor??1,roughnessFactor:pm.roughnessFactor??1,
          baseColorTexture:pm.baseColorTexture?.index??null,alphaMode:mat.alphaMode||'OPAQUE',doubleSided:!!mat.doubleSided}};
    })
  }));
  const instances=gltfSceneInstances(json,meshes.length);
  const images=(json.images||[]).map((image,index)=>{
    if(image.uri)gltfFail('external image URIs are not supported yet');
    const view=json.bufferViews?.[image.bufferView];
    if(!view||view.buffer!==0||!['image/png','image/jpeg'].includes(image.mimeType))gltfFail('unsupported or invalid embedded image '+index);
    const start=view.byteOffset||0,end=start+view.byteLength;
    if(!Number.isInteger(start)||!Number.isInteger(view.byteLength)||start<0||view.byteLength<1||end>bin.byteLength)gltfFail('embedded image exceeds BIN bounds');
    return {mimeType:image.mimeType,bytes:new Uint8Array(bin.slice(start,end))};
  });
  const textures=(json.textures||[]).map((texture,index)=>{
    if(!Number.isInteger(texture.source)||!images[texture.source])gltfFail('texture '+index+' references missing image');
    return {source:texture.source};
  });
  for(const mesh of meshes)for(const primitive of mesh.primitives){
    const ti=primitive.material.baseColorTexture;
    if(ti!=null&&!textures[ti])gltfFail('material references missing base-colour texture '+ti);
  }
  return {asset:json.asset,meshes,nodes:json.nodes||[],scenes:json.scenes||[],scene:json.scene||0,instances,images,textures,
    bounds:meshes.reduce((n,m)=>n+m.primitives.length,0),extensionsUsed:json.extensionsUsed||[]};
}
async function loadGLB(url){
  const response=await fetch(url,{credentials:'same-origin'});
  if(!response.ok)gltfFail('HTTP '+response.status+' while loading '+url);
  return parseGLB(await response.arrayBuffer());
}


/* ---------- GLB -> existing WebGL renderer bridge ---------- */
let gltfHero=null,gltfHeroGPU=null,gltfHeroTextures=[],gltfHeroError='';
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
  const srcN=p.normals||gltfNormals(p.positions,p.indices),out=new Float32Array(p.indices.length*8);
  for(let i=0;i<p.indices.length;i++){
    const j=p.indices[i],k=i*8,q=j*3,u=p.uvs?j*2:-1;
    out[k]=p.positions[q];out[k+1]=p.positions[q+1];out[k+2]=p.positions[q+2];
    out[k+3]=srcN[q];out[k+4]=srcN[q+1];out[k+5]=srcN[q+2];
    out[k+6]=p.uvs?p.uvs[u]:0;out[k+7]=p.uvs?p.uvs[u+1]:0;
  }
  return out;
}
function prepareGLBHero(asset){
  const entries=[];
  const instances=asset.instances||asset.meshes.map((_,mesh)=>({mesh,matrix:new Float32Array([1,0,0,0,0,1,0,0,0,0,1,0,0,0,0,1])}));
  for(const instance of instances){
    const mesh=asset.meshes[instance.mesh],m=instance.matrix;
    for(const p of mesh.primitives){
      const data=gltfExpandPrimitive(p);
      for(let i=0;i<data.length;i+=8){
        const x=data[i],y=data[i+1],z=data[i+2],nx=data[i+3],ny=data[i+4],nz=data[i+5];
        data[i]=m[0]*x+m[4]*y+m[8]*z+m[12];
        data[i+1]=m[1]*x+m[5]*y+m[9]*z+m[13];
        data[i+2]=m[2]*x+m[6]*y+m[10]*z+m[14];
        const normal=gltfTransformNormal(m,nx,ny,nz);
        data[i+3]=normal[0];data[i+4]=normal[1];data[i+5]=normal[2];
      }
      const buffer=gl.createBuffer();gl.bindBuffer(gl.ARRAY_BUFFER,buffer);gl.bufferData(gl.ARRAY_BUFFER,data,gl.STATIC_DRAW);
      entries.push({buffer,count:p.indices.length,material:p.material,stride:32,texture:p.material.baseColorTexture});
    }
  }
  return entries;
}
async function prepareGLBTextures(asset){
  const out=[];
  for(const image of asset.images||[]){
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
    gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_MIN_FILTER,gl.LINEAR);
    gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_MAG_FILTER,gl.LINEAR);
    gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_WRAP_S,gl.CLAMP_TO_EDGE);
    gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_WRAP_T,gl.CLAMP_TO_EDGE);
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
function drawGLBHero(){
  if(!gltfHero||!glOK)return false;
  if(!gltfHeroGPU){try{gltfHeroGPU=prepareGLBHero(gltfHero.asset);}catch(e){gltfHeroError=String(e);return false;}}
  push();T(P.x,P.ry,P.z);RY(-P.a);Sc(gltfHero.scale,gltfHero.scale,gltfHero.scale);
  gl.uniformMatrix4fv(uM,false,cur);gl.uniform3f(uNS,1/(gltfHero.scale*gltfHero.scale),1/(gltfHero.scale*gltfHero.scale),1/(gltfHero.scale*gltfHero.scale));
  gl.uniform1f(uTx,0);gl.uniform1f(uE,0);
  for(const item of gltfHeroGPU){
    const m=item.material,f=m.baseColorFactor;
    gl.uniform4f(uC,f[0],f[1],f[2],f[3]);
    gl.bindBuffer(gl.ARRAY_BUFFER,item.buffer);
    gl.enableVertexAttribArray(aP);gl.enableVertexAttribArray(aN);
    gl.vertexAttribPointer(aP,3,gl.FLOAT,false,32,0);gl.vertexAttribPointer(aN,3,gl.FLOAT,false,32,12);
    const uvLoc=gl.getAttribLocation(prog,'uv');
    if(uvLoc>=0){gl.enableVertexAttribArray(uvLoc);gl.vertexAttribPointer(uvLoc,2,gl.FLOAT,false,32,24);}
    gl.disableVertexAttribArray(aC);gl.vertexAttrib4f(aC,1,1,1,0);
    const texIndex=item.texture==null?-1:gltfHero.asset.textures[item.texture]?.source??-1;
    const tex=texIndex>=0?gltfHeroTextures[texIndex]:null;
    gl.uniform1f(gl.getUniformLocation(prog,'uGLBTexture'),tex?1:0);
    if(tex){gl.activeTexture(gl.TEXTURE2);gl.bindTexture(gl.TEXTURE_2D,tex);gl.uniform1i(gl.getUniformLocation(prog,'uGLBTex'),2);}
    gl.drawArrays(gl.TRIANGLES,0,item.count);
  }
  gl.uniform1f(gl.getUniformLocation(prog,'uGLBTexture'),0);gl.activeTexture(gl.TEXTURE0);
  const uvLoc=gl.getAttribLocation(prog,'uv');if(uvLoc>=0)gl.disableVertexAttribArray(uvLoc);
  pop();bindMesh();gAlpha=1;return true;
}
