const fs = require('node:fs');
const vm = require('node:vm');
const assert = require('node:assert/strict');
const source = fs.readFileSync('src/gltf.js', 'utf8') + '\n globalThis.__parseGLB = parseGLB;';
const context = {TextDecoder, DataView, Uint8Array, Uint16Array, Uint32Array, Int8Array, Int16Array, Float32Array, ArrayBuffer, Math, Error, JSON, console};
vm.createContext(context);
vm.runInContext(source, context);
function fixture() {
  const bin = Buffer.alloc(44);
  [0,0,0, 1,0,0, 0,1,0].forEach((v,i)=>bin.writeFloatLE(v,i*4));
  [0,1,2].forEach((v,i)=>bin.writeUInt16LE(v,36+i*2));
  const json = Buffer.from(JSON.stringify({
    asset:{version:'2.0'},
    buffers:[{byteLength:44}],
    bufferViews:[{buffer:0,byteOffset:0,byteLength:36},{buffer:0,byteOffset:36,byteLength:6}],
    accessors:[{bufferView:0,componentType:5126,count:3,type:'VEC3'},{bufferView:1,componentType:5123,count:3,type:'SCALAR'}],
    meshes:[{primitives:[{attributes:{POSITION:0},indices:1}]}]
  }));
  const jp = Buffer.concat([json,Buffer.alloc((4-json.length%4)%4,0x20)]);
  const chunks = [Buffer.alloc(12),Buffer.alloc(8),jp,Buffer.alloc(8),bin];
  chunks[0].writeUInt32LE(0x46546c67,0);chunks[0].writeUInt32LE(2,4);
  chunks[0].writeUInt32LE(12+8+jp.length+8+bin.length,8);
  chunks[1].writeUInt32LE(jp.length,0);chunks[1].writeUInt32LE(0x4e4f534a,4);
  chunks[3].writeUInt32LE(bin.length,0);chunks[3].writeUInt32LE(0x004e4942,4);
  return Buffer.concat(chunks);
}
const good = fixture();
const parsed = context.__parseGLB(good.buffer.slice(good.byteOffset,good.byteOffset+good.byteLength));
assert.equal(parsed.meshes.length,1);
assert.equal(parsed.meshes[0].primitives[0].indices.length,3);
assert.equal(parsed.meshes[0].primitives[0].positions[3],1);
const badMagic = Buffer.from(good);badMagic.writeUInt32LE(0,0);
assert.throws(()=>context.__parseGLB(badMagic.buffer.slice(badMagic.byteOffset,badMagic.byteOffset+badMagic.byteLength)),/invalid magic/);
console.log('GLB parser fixtures passed: valid mesh + malformed magic');
