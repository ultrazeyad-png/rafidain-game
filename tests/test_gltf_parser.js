const fs = require('node:fs');
const vm = require('node:vm');
const assert = require('node:assert/strict');

const source = fs.readFileSync('src/gltf.js', 'utf8') + '\n globalThis.__parseGLB = parseGLB; globalThis.__transformNormal = gltfTransformNormal;';
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

console.log('GLB parser fixtures passed: valid mesh, header checks, accessor bounds, index types, normals, scene transforms and cycle rejection');
