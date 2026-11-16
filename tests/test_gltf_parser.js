const fs = require('node:fs');
const vm = require('node:vm');
const assert = require('node:assert/strict');

const source = fs.readFileSync('src/gltf.js', 'utf8') + '\n globalThis.__createPlaybackState = gltfCreatePlaybackState; globalThis.__setPlaybackAnimation = gltfSetPlaybackAnimation; globalThis.__advancePlayback = gltfAdvancePlayback; globalThis.__resolveAnimationClip = gltfResolveAnimationClip; globalThis.__resolveEnemyAction = gltfResolveEnemyAction; globalThis.__blendAnimationPoses = gltfBlendAnimationPoses; globalThis.__animationLocalTime = gltfAnimationLocalTime; globalThis.__parseGLB = parseGLB; globalThis.__transformNormal = gltfTransformNormal; globalThis.__sampleAnimation = gltfSampleAnimationSampler; globalThis.__evaluateAnimation = gltfEvaluateAnimation; globalThis.__jointMatrices = gltfBuildJointMatrices; globalThis.__nodeWorldMatrices = gltfNodeWorldMatrices; globalThis.__skinCPU = gltfSkinPrimitiveCPU; globalThis.__transformExpanded = gltfTransformExpanded; globalThis.__cpuSkin = gltfSkinPrimitiveCPU; globalThis.__createAnimatedEntity=gltfCreateAnimatedEntity; globalThis.__setEntityAction=gltfSetEntityAction; globalThis.__updateAnimatedEntity=gltfUpdateAnimatedEntity; globalThis.__skinCPU = gltfSkinPrimitiveCPU; globalThis.__skinCPU = gltfSkinPrimitiveCPU;';
const context = {TextDecoder, DataView, Uint8Array, Uint16Array, Uint32Array, Int8Array, Int16Array, Float32Array, ArrayBuffer, Math, Error, JSON, console};
vm.createContext(context);
vm.runInContext(source, context);

function makeGLB(document, bin) {
  document.buffers = [{byteLength: bin.length}];
  const json = Buffer.from(JSON.stringify(document));
  const jsonChunk = Buffer.concat([json, Buffer.alloc((4 - json.length % 4) % 4, 0x20)]);
  const binChunk = Buffer.from(bin);
  const header = Buffer.alloc(12);
  header.writeUInt32LE(0x46546c67, 0);
  header.writeUInt32LE(2, 4);
  header.writeUInt32LE(12 + 8 + jsonChunk.length + 8 + binChunk.length, 8);
  const jh = Buffer.alloc(8);
  jh.writeUInt32LE(jsonChunk.length, 0);
  jh.writeUInt32LE(0x4e4f534a, 4);
  const bh = Buffer.alloc(8);
  bh.writeUInt32LE(binChunk.length, 0);
  bh.writeUInt32LE(0x004e4942, 4);
  return Buffer.concat([header, jh, jsonChunk, bh, binChunk]);
}

function baseDocument() {
  return {
    asset: {version: '2.0'},
    bufferViews: [
      {buffer: 0, byteOffset: 0, byteLength: 36},
      {buffer: 0, byteOffset: 36, byteLength: 6}
    ],
    accessors: [
      {bufferView: 0, componentType: 5126, count: 3, type: 'VEC3'},
      {bufferView: 1, componentType: 5123, count: 3, type: 'SCALAR'}
    ],
    meshes: [{primitives: [{attributes: {POSITION: 0}, indices: 1}]}]
  };
}
function baseBin() {
  const bin = Buffer.alloc(44);
  [0,0,0, 1,0,0, 0,1,0].forEach((v, i) => bin.writeFloatLE(v, i * 4));
  [0,1,2].forEach((v, i) => bin.writeUInt16LE(v, 36 + i * 2));
  return bin;
}
function parse(buffer) {
  return context.__parseGLB(buffer.buffer.slice(buffer.byteOffset, buffer.byteOffset + buffer.byteLength));
}
function expectInvalid(document, bin, pattern) {
  assert.throws(() => parse(makeGLB(document, bin)), pattern);
}

const valid = parse(makeGLB(baseDocument(), baseBin()));
assert.equal(valid.meshes.length, 1);
assert.equal(valid.meshes[0].primitives[0].indices.length, 3);
assert.equal(valid.meshes[0].primitives[0].positions[3], 1);

// Header and container validation.
const good = makeGLB(baseDocument(), baseBin());
const badMagic = Buffer.from(good);
badMagic.writeUInt32LE(0, 0);
assert.throws(() => parse(badMagic), /invalid magic/);
const badVersion = Buffer.from(good);
badVersion.writeUInt32LE(1, 4);
assert.throws(() => parse(badVersion), /only GLB version 2/);

// Accessors must remain inside their declared bufferView, not merely inside BIN.
{
  const d = baseDocument();
  d.accessors[0].count = 4;
  expectInvalid(d, baseBin(), /exceeds bufferView bounds/);
}
{
  const d = baseDocument();
  d.accessors[1].componentType = 5122; // signed indices are forbidden
  expectInvalid(d, baseBin(), /unsigned integer SCALAR/);
}
{
  const d = baseDocument();
  d.accessors.push({bufferView: 1, componentType: 5126, count: 3, type: 'VEC3'});
  d.meshes[0].primitives[0].attributes.NORMAL = 2;
  expectInvalid(d, baseBin(), /invalid accessor layout|exceeds bufferView bounds/);
}
{
  const d = baseDocument();
  d.meshes[0].primitives[0].attributes.NORMAL = 1; // SCALAR cannot be NORMAL
  expectInvalid(d, baseBin(), /NORMAL must be a matching VEC3/);
}

// The active scene's node transforms must be composed and applied to mesh instances.
{
  const d = baseDocument();
  d.nodes = [
    {translation:[2,0,0], children:[1]},
    {mesh:0, translation:[0,3,0], scale:[2,2,2]}
  ];
  d.scenes = [{nodes:[0]}];
  d.scene = 0;
  const parsed = parse(makeGLB(d, baseBin()));
  assert.equal(parsed.instances.length, 1);
  assert.deepEqual(Array.from(parsed.instances[0].matrix.slice(12,15)), [2,3,0]);
  assert.equal(parsed.instances[0].matrix[0], 2);
}
{
  const d = baseDocument();
  d.nodes = [{mesh:0, children:[0]}];
  d.scenes = [{nodes:[0]}];
  assert.throws(() => parse(makeGLB(d, baseBin())), /node hierarchy contains a cycle/);
}

{
  const d=baseDocument();
  d.nodes=[{mesh:0,translation:[NaN,0,0]}];
  d.scenes=[{nodes:[0]}];
  assert.throws(()=>parse(makeGLB(d,baseBin())),/invalid or non-finite node TRS/);
}
{
  const d=baseDocument();
  d.nodes=[{mesh:0,rotation:[0,0,0,0]}];
  d.scenes=[{nodes:[0]}];
  assert.throws(()=>parse(makeGLB(d,baseBin())),/rotation quaternion must be non-zero/);
}
{
  const d=baseDocument();
  d.nodes=[{mesh:0}];
  d.scenes=[{nodes:[0]}];
  d.scene=2;
  assert.throws(()=>parse(makeGLB(d,baseBin())),/selected scene index is out of range/);
}
{
  const d=baseDocument();
  d.nodes=[{children:[1]},{mesh:0}];
  d.scenes=[{nodes:[0,1]}];
  assert.throws(()=>parse(makeGLB(d,baseBin())),/referenced more than once/);
}

{
  const m = new Float32Array([2,0,0,0, 0,1,0,0, 0,0,0.5,0, 0,0,0,1]);
  const n = context.__transformNormal(m, 1, 1, 1);
  const len = Math.hypot(0.5, 1, 2);
  assert.ok(Math.abs(n[0] - 0.5/len) < 1e-6);
  assert.ok(Math.abs(n[1] - 1/len) < 1e-6);
  assert.ok(Math.abs(n[2] - 2/len) < 1e-6);
  assert.throws(() => context.__transformNormal(new Float32Array(16), 0, 1, 0), /singular normal matrix/);
}


{
  const d = baseDocument();
  d.accessors[0].byteOffset = 2; // FLOAT data must be four-byte aligned
  expectInvalid(d, baseBin(), /invalid accessor layout/);
}
{
  const d = baseDocument();
  d.bufferViews[0].byteStride = 6; // glTF vertex strides are four-byte aligned
  expectInvalid(d, baseBin(), /invalid bufferView byteStride/);
}
{
  const d = baseDocument();
  const bin = baseBin();
  bin.writeFloatLE(Number.NaN, 0);
  expectInvalid(d, bin, /non-finite value/);
}


{
  const d = baseDocument();
  const bin = Buffer.alloc(72);
  baseBin().copy(bin, 0);
  d.bufferViews.push(
    {buffer:0,byteOffset:44,byteLength:24},
    {buffer:0,byteOffset:68,byteLength:4}
  );
  d.accessors.push({bufferView:2,componentType:5126,count:3,type:'VEC2'});
  d.meshes[0].primitives[0].attributes.TEXCOORD_0=2;
  d.materials=[{pbrMetallicRoughness:{baseColorTexture:{index:0}}}];
  d.meshes[0].primitives[0].material=0;
  d.images=[{bufferView:3,mimeType:'image/png'}];
  d.textures=[{source:0}];
  const parsed=parse(makeGLB(d,bin));
  assert.equal(parsed.meshes[0].primitives[0].uvs.length,6);
  assert.equal(parsed.meshes[0].primitives[0].material.baseColorTexture,0);
  assert.equal(parsed.images.length,1);
  assert.equal(parsed.images[0].bytes.length,4);
  assert.equal(parsed.textures[0].source,0);
  assert.deepEqual({...parsed.textures[0].sampler},{wrapS:10497,wrapT:10497,minFilter:9987,magFilter:9729});
}
{
  const d=baseDocument();
  d.images=[{bufferView:2,mimeType:'image/png'}];
  d.bufferViews.push({buffer:0,byteOffset:44,byteLength:4});
  d.textures=[{source:0,sampler:0}];
  d.samplers=[{wrapS:33071,wrapT:33648,minFilter:9728,magFilter:9728}];
  const bin=Buffer.alloc(48);baseBin().copy(bin);
  const parsed=parse(makeGLB(d,bin));
  assert.deepEqual({...parsed.textures[0].sampler},{wrapS:33071,wrapT:33648,minFilter:9728,magFilter:9728});
}
{
  const d=baseDocument();
  d.images=[{bufferView:2,mimeType:'image/png'}];
  d.bufferViews.push({buffer:0,byteOffset:44,byteLength:4});
  d.textures=[{source:0,sampler:4}];
  const bin=Buffer.alloc(48);baseBin().copy(bin);
  assert.throws(()=>parse(makeGLB(d,bin)),/references missing sampler/);
}
{
  const d=baseDocument();
  d.samplers=[{wrapS:123}];
  expectInvalid(d,baseBin(),/invalid wrap mode/);
}
{
  const d = baseDocument();
  d.images=[{bufferView:99,mimeType:'image/png'}];
  assert.throws(()=>parse(makeGLB(d,baseBin())),/unsupported or invalid embedded image/);
}


{
  const d=baseDocument();
  d.materials=[{pbrMetallicRoughness:{metallicFactor:0.25,roughnessFactor:0.7}}];
  d.meshes[0].primitives[0].material=0;
  const parsed=parse(makeGLB(d,baseBin()));
  assert.equal(parsed.meshes[0].primitives[0].material.metallicFactor,0.25);
  assert.equal(parsed.meshes[0].primitives[0].material.roughnessFactor,0.7);
}


{
  const d=baseDocument();
  const bin=Buffer.alloc(92);baseBin().copy(bin,0);
  for(let i=0;i<3;i++){bin.writeFloatLE(1,44+i*16);bin.writeFloatLE(0,48+i*16);bin.writeFloatLE(0,52+i*16);bin.writeFloatLE(1,56+i*16);}
  d.bufferViews.push({buffer:0,byteOffset:44,byteLength:48});
  d.accessors.push({bufferView:2,componentType:5126,count:3,type:'VEC4'});
  d.meshes[0].primitives[0].attributes.TANGENT=2;
  const parsed=parse(makeGLB(d,bin));
  assert.equal(parsed.meshes[0].primitives[0].tangents.length,12);
  assert.equal(parsed.meshes[0].primitives[0].tangents[3],1);
}
{
  const d=baseDocument();
  d.accessors.push({bufferView:0,componentType:5126,count:3,type:'VEC3'});
  d.meshes[0].primitives[0].attributes.TANGENT=2;
  expectInvalid(d,baseBin(),/TANGENT must be a matching VEC4/);
}
{
  const d=baseDocument();
  d.materials=[{normalTexture:{index:0,scale:0.65}}];
  d.meshes[0].primitives[0].material=0;
  d.images=[{bufferView:2,mimeType:'image/png'}];
  d.bufferViews.push({buffer:0,byteOffset:44,byteLength:4});
  d.textures=[{source:0}];
  const bin=Buffer.alloc(48);baseBin().copy(bin,0);
  const parsed=parse(makeGLB(d,bin));
  assert.equal(parsed.meshes[0].primitives[0].material.normalTexture,0);
  assert.equal(parsed.meshes[0].primitives[0].material.normalScale,0.65);
}
{
  const d=baseDocument();
  d.materials=[{normalTexture:{index:2}}];
  d.meshes[0].primitives[0].material=0;
  assert.throws(()=>parse(makeGLB(d,baseBin())),/missing normal texture/);
}


{
  const d=baseDocument(),bin=Buffer.alloc(200);baseBin().copy(bin,0);
  d.bufferViews.push(
    {buffer:0,byteOffset:44,byteLength:12},
    {buffer:0,byteOffset:56,byteLength:48},
    {buffer:0,byteOffset:104,byteLength:64},
    {buffer:0,byteOffset:168,byteLength:8},
    {buffer:0,byteOffset:176,byteLength:24}
  );
  d.accessors.push(
    {bufferView:2,componentType:5121,count:3,type:'VEC4'},
    {bufferView:3,componentType:5126,count:3,type:'VEC4'},
    {bufferView:4,componentType:5126,count:1,type:'MAT4'},
    {bufferView:5,componentType:5126,count:2,type:'SCALAR'},
    {bufferView:6,componentType:5126,count:2,type:'VEC3'}
  );
  for(let i=0;i<3;i++){bin.writeUInt8(0,44+i*4);bin.writeFloatLE(1,56+i*16);}
  for(let i=0;i<16;i++)bin.writeFloatLE(i%5===0?1:0,104+i*4);
  bin.writeFloatLE(0,168);bin.writeFloatLE(1,172);
  [0,0,0, 1,0,0].forEach((v,i)=>bin.writeFloatLE(v,176+i*4));
  d.meshes[0].primitives[0].attributes.JOINTS_0=2;
  d.meshes[0].primitives[0].attributes.WEIGHTS_0=3;
  d.nodes=[{mesh:0,skin:0},{name:'root_joint'}];d.scenes=[{nodes:[0,1]}];
  d.skins=[{joints:[1],inverseBindMatrices:4}];
  d.animations=[{name:'idle',samplers:[{input:5,output:6}],channels:[{sampler:0,target:{node:1,path:'translation'}}]}];
  const parsed=parse(makeGLB(d,bin));
  const primitive=parsed.meshes[0].primitives[0];
  assert.equal(primitive.joints.length,12);assert.equal(primitive.weights[0],1);
  assert.equal(parsed.skins[0].joints[0],1);assert.equal(parsed.skins[0].inverseBindMatrices[0],1);
  assert.equal(parsed.animations[0].samplers[0].input.length,2);
  assert.equal(parsed.animations[0].channels[0].path,'translation');
}
{
  const d=baseDocument(),bin=Buffer.alloc(200);baseBin().copy(bin,0);
  d.bufferViews.push({buffer:0,byteOffset:44,byteLength:12},{buffer:0,byteOffset:56,byteLength:48});
  d.accessors.push({bufferView:2,componentType:5121,count:3,type:'VEC4'},{bufferView:3,componentType:5126,count:3,type:'VEC4'});
  d.meshes[0].primitives[0].attributes.JOINTS_0=2;d.meshes[0].primitives[0].attributes.WEIGHTS_0=3;
  d.nodes=[{mesh:0,skin:0},{name:'joint'}];d.scenes=[{nodes:[0,1]}];d.skins=[{joints:[1]}];
  bin.writeUInt8(1,44);
  assert.throws(()=>parse(makeGLB(d,bin)),/vertex joint index exceeds skin joint count/);
}
{
  const d=baseDocument(),bin=Buffer.alloc(104);baseBin().copy(bin,0);
  d.bufferViews.push({buffer:0,byteOffset:44,byteLength:8},{buffer:0,byteOffset:52,byteLength:24});
  d.accessors.push({bufferView:2,componentType:5126,count:2,type:'SCALAR'},{bufferView:3,componentType:5126,count:2,type:'VEC3'});
  d.nodes=[{mesh:0}];d.scenes=[{nodes:[0]}];
  bin.writeFloatLE(0,44);bin.writeFloatLE(1,48);
  d.animations=[{samplers:[{input:2,output:3}],channels:[{sampler:0,target:{node:0,path:'rotation'}}]}];
  assert.throws(()=>parse(makeGLB(d,bin)),/animation output width does not match target path/);
}


{
  const linear={input:new Float32Array([0,2]),output:new Float32Array([0,0,0, 10,4,2]),outputWidth:3,interpolation:'LINEAR'};
  assert.deepEqual(Array.from(context.__sampleAnimation(linear,1,'translation')),[5,2,1]);
  assert.deepEqual(Array.from(context.__sampleAnimation(linear,-1,'translation')),[0,0,0]);
  assert.deepEqual(Array.from(context.__sampleAnimation(linear,3,'translation')),[10,4,2]);
  const step={...linear,interpolation:'STEP'};
  assert.deepEqual(Array.from(context.__sampleAnimation(step,1.9,'translation')),[0,0,0]);
}
{
  const rotation={input:new Float32Array([0,1]),output:new Float32Array([0,0,0,1, 0,1,0,0]),outputWidth:4,interpolation:'LINEAR'};
  const q=context.__sampleAnimation(rotation,.5,'rotation');
  assert.ok(Math.abs(Math.hypot(...q)-1)<1e-6);
  assert.ok(Math.abs(q[1]-Math.SQRT1_2)<1e-5);
}
{
  const cubic={input:new Float32Array([0,1]),output:new Float32Array([0,0,0, 2,0,0, 0,0,0, 0,0,0, 4,0,0, 0,0,0]),outputWidth:3,interpolation:'CUBICSPLINE'};
  assert.deepEqual(Array.from(context.__sampleAnimation(cubic,.5,'translation')),[3,0,0]);
}
{
  const animation={samplers:[{input:new Float32Array([0,1]),output:new Float32Array([0,0,0, 2,0,0]),outputWidth:3,interpolation:'LINEAR'}],channels:[{sampler:0,node:4,path:'translation'}]};
  assert.deepEqual(Array.from(context.__evaluateAnimation(animation,.25).get(4).translation),[.5,0,0]);
}


{
  const asset={nodes:[{translation:[5,0,0],children:[1]},{translation:[0,2,0]}],skins:[{joints:[1],inverseBindMatrices:new Float32Array([1,0,0,0,0,1,0,0,0,0,1,0,0,0,0,1])}]};
  const base=context.__jointMatrices(asset,0,0);
  assert.equal(base[12],0);assert.equal(base[13],2);
  const pose=new Map([[1,{translation:[0,3,0]}]]);
  const moved=context.__jointMatrices(asset,0,0,pose);
  assert.equal(moved[12],0);assert.equal(moved[13],3);
}
{
  const asset={nodes:[{translation:[2,0,0]}]};
  const worlds=context.__nodeWorldMatrices(asset);
  assert.equal(worlds[0][12],2);
}


{
  const primitive={positions:new Float32Array([1,2,3]),normals:new Float32Array([0,1,0]),tangents:new Float32Array([1,0,0,1]),joints:new Uint16Array([0,0,0,0]),weights:new Float32Array([1,0,0,0])};
  const joint=new Float32Array([1,0,0,0,0,1,0,0,0,0,1,0,4,5,6,1]);
  const result=context.__skinCPU(primitive,joint);
  assert.deepEqual(Array.from(result.positions),[5,7,9]);
  assert.deepEqual(Array.from(result.normals),[0,1,0]);
  assert.deepEqual(Array.from(result.tangents),[1,0,0,1]);
}
{
  const primitive={positions:new Float32Array([0,0,0]),joints:new Uint16Array([0,0,0,0]),weights:new Float32Array([0,0,0,0])};
  const joint=new Float32Array([1,0,0,0,0,1,0,0,0,0,1,0,0,0,0,1]);
  assert.throws(()=>context.__skinCPU(primitive,joint),/no positive skin weights/);
}


{
  const primitive={positions:new Float32Array([1,2,3]),normals:new Float32Array([0,1,0]),joints:new Uint16Array([0,0,0,0]),weights:new Float32Array([1,0,0,0])};
  const joint=new Float32Array([1,0,0,0,0,1,0,0,0,0,1,0,4,5,6,1]);
  const skinned=context.__skinCPU(primitive,joint);
  assert.deepEqual(Array.from(skinned.positions),[5,7,9]);
  assert.deepEqual(Array.from(skinned.normals),[0,1,0]);
}
{
  const primitive={positions:new Float32Array([2,0,0]),joints:new Uint16Array([0,0,0,0]),weights:new Float32Array([1,0,0,0])};
  const identity=new Float32Array([1,0,0,0,0,1,0,0,0,0,1,0,0,0,0,1]);
  assert.deepEqual(Array.from(context.__skinCPU(primitive,identity).positions),[2,0,0]);
}


{
  const primitive={
    positions:new Float32Array([1,2,3]),
    normals:new Float32Array([0,1,0]),
    tangents:new Float32Array([1,0,0,1]),
    joints:new Float32Array([0,0,0,0]),
    weights:new Float32Array([1,0,0,0])
  };
  const joint=new Float32Array([1,0,0,0,0,1,0,0,0,0,1,0,4,0,0,1]);
  const skinned=context.__cpuSkin(primitive,joint);
  assert.deepEqual(Array.from(skinned.positions),[5,2,3]);
  assert.deepEqual(Array.from(skinned.normals),[0,1,0]);
  assert.deepEqual(Array.from(skinned.tangents),[1,0,0,1]);
}
{
  const primitive={positions:new Float32Array([0,0,0]),joints:new Float32Array([0,0,0,0]),weights:new Float32Array([0,0,0,0])};
  const joint=new Float32Array([1,0,0,0,0,1,0,0,0,0,1,0,0,0,0,1]);
  assert.throws(()=>context.__cpuSkin(primitive,joint),/no positive skin weights/);
}


{
  const primitive={positions:new Float32Array([1,2,3]),normals:new Float32Array([0,1,0]),tangents:new Float32Array([1,0,0,1]),joints:new Float32Array([0,0,0,0]),weights:new Float32Array([1,0,0,0]),indices:new Uint16Array([0])};
  const palette=new Float32Array([1,0,0,0,0,1,0,0,0,0,1,0,2,0,0,1]);
  const result=context.__skinCPU(primitive,palette);
  assert.deepEqual(Array.from(result.positions),[3,2,3]);
  assert.deepEqual(Array.from(result.normals),[0,1,0]);
  assert.deepEqual(Array.from(result.tangents),[1,0,0,1]);
  assert.deepEqual(Array.from(primitive.positions),[1,2,3]);
}
{
  const primitive={positions:new Float32Array([0,0,0]),joints:new Float32Array([1,0,0,0]),weights:new Float32Array([1,0,0,0])};
  assert.throws(()=>context.__skinCPU(primitive,new Float32Array(16)),/invalid CPU skin joint influence/);
}


{
  const data=new Float32Array([1,2,3,0,1,0,0,0,1,0,0,1]);
  const transform=new Float32Array([2,0,0,0,0,3,0,0,0,0,4,0,5,6,7,1]);
  const result=context.__transformExpanded(data,transform);
  assert.deepEqual(Array.from(result.slice(0,3)),[7,12,19]);
  assert.deepEqual(Array.from(result.slice(3,6)),[0,1,0]);
  assert.deepEqual(Array.from(result.slice(8,11)),[1,0,0]);
}


{
  const asset={nodes:[{translation:[0,0,0],rotation:[0,0,0,1],scale:[1,1,1]},{translation:[2,0,0]}]};
  const from=new Map([[0,{translation:[0,0,0],rotation:[0,0,0,1],scale:[1,1,1]}]]);
  const to=new Map([[0,{translation:[10,4,2],rotation:[0,1,0,0],scale:[3,3,3]}],[1,{translation:[8,0,0]}]]);
  const zero=context.__blendAnimationPoses(asset,from,to,0);
  const half=context.__blendAnimationPoses(asset,from,to,.5);
  const one=context.__blendAnimationPoses(asset,from,to,1);
  assert.deepEqual(Array.from(zero.get(0).translation),[0,0,0]);
  assert.deepEqual(Array.from(half.get(0).translation),[5,2,1]);
  assert.deepEqual(Array.from(half.get(0).scale),[2,2,2]);
  assert.ok(Math.abs(half.get(0).rotation[1]-Math.SQRT1_2)<1e-5);
  assert.deepEqual(Array.from(one.get(0).translation),[10,4,2]);
  assert.deepEqual(Array.from(half.get(1).translation),[5,0,0]);
  assert.throws(()=>context.__blendAnimationPoses(asset,from,to,NaN),/blend alpha must be finite/);
}


{
  const asset={animations:[{name:'Idle_Breathing'},{name:'Walk_Forward'},{name:'Run_Fast'},{name:'Dodge_Roll'},{name:'Attack_Slash'},{name:'Death'},{name:'Heavy_Strike'},{name:'Jump_Start'},{name:'Hurt_Reaction'},{name:'Guard_Block'},{name:'Cast_Spell'},{name:'Bow_Aim'},{name:'Heal_Potion'}]};
  assert.equal(context.__resolveAnimationClip(asset,'idle'),0);
  assert.equal(context.__resolveAnimationClip(asset,'walk'),1);
  assert.equal(context.__resolveAnimationClip(asset,'run'),2);
  assert.equal(context.__resolveAnimationClip(asset,'dodge'),3);
  assert.equal(context.__resolveAnimationClip(asset,'attack'),4);
  assert.equal(context.__resolveAnimationClip(asset,'death'),5);
  assert.equal(context.__resolveAnimationClip(asset,'heavy'),6);
  assert.equal(context.__resolveAnimationClip(asset,'jump'),7);
  assert.equal(context.__resolveAnimationClip(asset,'hurt'),8);
  assert.equal(context.__resolveAnimationClip(asset,'block'),9);
  assert.equal(context.__resolveAnimationClip(asset,'cast'),10);
  assert.equal(context.__resolveAnimationClip(asset,'aim'),11);
  assert.equal(context.__resolveAnimationClip(asset,'heal'),12);
  assert.equal(context.__resolveAnimationClip(asset,'unknown'),-1);
  const enemy=(state,extra={})=>context.__resolveEnemyAction({state,...extra});
  assert.equal(enemy('idle'),'idle');
  assert.equal(enemy('chase'),'run');
  assert.equal(enemy('wind'),'heavy');
  assert.equal(enemy('strike'),'attack');
  assert.equal(enemy('stun'),'hurt');
  assert.equal(enemy('rec'),'idle');
  assert.equal(enemy('anything'),'idle');
  assert.equal(enemy('strike',{dead:true}),'death');
  const boss=(state)=>enemy(state,{boss:true});
  assert.equal(boss('sleep'),'idle');
  assert.equal(boss('intro'),'idle');
  assert.equal(boss('chase'),'walk');
  assert.equal(boss('sweepW'),'attack');
  assert.equal(boss('sweep2'),'attack');
  assert.equal(boss('slamW'),'heavy');
  assert.equal(boss('leap'),'heavy');
  assert.equal(boss('dash'),'heavy');
  assert.equal(boss('stag'),'hurt');
  assert.equal(boss('parried'),'hurt');
  assert.equal(boss('rec'),'idle');
  assert.equal(boss('anything'),'idle');
}


{
  const track=end=>({samplers:[{input:new Float32Array([0,2]),output:new Float32Array([0,0,0,end,0,0]),outputWidth:3,interpolation:'LINEAR'}],channels:[{sampler:0,node:0,path:'translation'}]});
  const asset={nodes:[{translation:[0,0,0]}],meshes:[],instances:[],animations:[{name:'Idle_Breathing',...track(2)},{name:'Attack_Slash',...track(4)}]};
  const a=context.__createAnimatedEntity('enemy-1',asset);
  const b=context.__createAnimatedEntity('boss-1',asset,{speed:.5});
  assert.equal(a.id,'enemy-1');assert.equal(b.id,'boss-1');
  assert.notEqual(a.playback,b.playback);
  context.__updateAnimatedEntity(a,0);context.__updateAnimatedEntity(b,0);
  const pa=context.__updateAnimatedEntity(a,1),pb=context.__updateAnimatedEntity(b,1);
  assert.deepEqual(Array.from(pa.pose.get(0).translation),[1,0,0]);
  assert.deepEqual(Array.from(pb.pose.get(0).translation),[.5,0,0]);
  const switched=context.__setEntityAction(a,'attack');
  assert.equal(switched.ok,true);assert.equal(a.playback.clip,1);
  assert.equal(context.__setEntityAction(b,'jump').reason,'clip-not-found');
  assert.equal(b.playback.clip,0);
  assert.throws(()=>context.__createAnimatedEntity('',asset),/id must be a non-empty string/);
  assert.throws(()=>context.__updateAnimatedEntity(null,0),/invalid animated entity instance/);
}

console.log('GLB parser fixtures passed: skinned mesh world transform application,  CPU vertex skinning and joint palette validation,  CPU skinning weighted translation and identity,  CPU skinning vertex deformation,  animated node world transforms and inverse-bind joint matrices,  linear/step/cubic animation sampling, quaternion interpolation and pose evaluation,  skinning attributes, skin joints, inverse bind matrices, animation samplers and channels,  valid mesh, header checks, accessor bounds, index types, normals, scene transforms and cycle rejection');

{
  const makeTrack=(end)=>({samplers:[{input:new Float32Array([0,4]),output:new Float32Array([0,0,0,end,0,0]),outputWidth:3,interpolation:'LINEAR'}],channels:[{sampler:0,node:0,path:'translation'}]});
  const asset={nodes:[{translation:[0,0,0]}],animations:[makeTrack(4),makeTrack(8)]};
  const one=context.__createPlaybackState(asset),two=context.__createPlaybackState(asset,{speed:.5});
  context.__advancePlayback(asset,one,0);context.__advancePlayback(asset,two,0);
  const poseOne=context.__advancePlayback(asset,one,1),poseTwo=context.__advancePlayback(asset,two,1);
  assert.equal(one.elapsed,1);assert.equal(two.elapsed,.5);
  assert.deepEqual(Array.from(poseOne.get(0).translation),[1,0,0]);
  assert.deepEqual(Array.from(poseTwo.get(0).translation),[.5,0,0]);
  const changed=context.__setPlaybackAnimation(asset,one,{clip:1,transitionDuration:.2});
  assert.equal(changed.clip,1);assert.equal(changed.elapsed,0);
  assert.equal(changed.transition.duration,.2);
  context.__advancePlayback(asset,changed,1);
  context.__advancePlayback(asset,changed,1.1);
  assert.ok(changed.elapsed>0&&changed.elapsed<.11);
  const paused=context.__setPlaybackAnimation(asset,two,{paused:true});
  context.__advancePlayback(asset,paused,2);
  const before=paused.elapsed;
  context.__advancePlayback(asset,paused,3);
  assert.equal(paused.elapsed,before);
  assert.throws(()=>context.__setPlaybackAnimation(asset,one,{speed:9}),/speed must be between 0 and 4/);
}

{
  const animation={samplers:[{input:new Float32Array([2,3,5])}]};
  assert.equal(context.__animationLocalTime(animation,0,true),2);
  assert.equal(context.__animationLocalTime(animation,1,true),3);
  assert.equal(context.__animationLocalTime(animation,4,true),3);
  assert.equal(context.__animationLocalTime(animation,8,true),4);
  assert.equal(context.__animationLocalTime(animation,8,false),5);
  assert.throws(()=>context.__animationLocalTime(animation,NaN,true),/elapsed time must be finite/);
  assert.throws(()=>context.__animationLocalTime({samplers:[]},0,true),/no keyframe times/);
}
