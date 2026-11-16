/* ================= audio (synthesised) ================= */
let AUC=null,MG=null,NB=null;
function initAudio(){
  try{
    if(!AUC){const Cx=window.AudioContext||window.webkitAudioContext;if(!Cx)return;AUC=new Cx();
      MG=AUC.createGain();MG.gain.value=.9;const cp=AUC.createDynamicsCompressor();MG.connect(cp);cp.connect(AUC.destination);
      NB=AUC.createBuffer(1,AUC.sampleRate*2,AUC.sampleRate);const d=NB.getChannelData(0);for(let i=0;i<d.length;i++)d[i]=Math.random()*2-1;
      // quiet wind bed
      const s=AUC.createBufferSource();s.buffer=NB;s.loop=true;const f=AUC.createBiquadFilter();f.type='lowpass';f.frequency.value=380;
      const g=AUC.createGain();g.gain.value=.03;const l=AUC.createOscillator(),lg=AUC.createGain();l.frequency.value=.11;lg.gain.value=.018;l.connect(lg);lg.connect(g.gain);
      s.connect(f);f.connect(g);g.connect(MG);s.start();l.start();}
    if(AUC.state==='suspended')AUC.resume();
  }catch(_){AUC=null;}
}
function tone(f0,f1,dur,type,vol){
  if(!AUC)return;
  try{const o=AUC.createOscillator(),g=AUC.createGain(),t=AUC.currentTime;
    o.type=type;o.frequency.setValueAtTime(f0,t);o.frequency.exponentialRampToValueAtTime(Math.max(20,f1),t+dur);
    g.gain.setValueAtTime(vol,t);g.gain.exponentialRampToValueAtTime(.0008,t+dur);
    o.connect(g);g.connect(MG);o.start(t);o.stop(t+dur+.02);}catch(_){}
}
function nz(dur,vol,type,f0,f1,q){   // filtered noise burst: whooshes, scrapes, thuds
  if(!AUC)return;
  try{const s=AUC.createBufferSource(),f=AUC.createBiquadFilter(),g=AUC.createGain(),t=AUC.currentTime;
    s.buffer=NB;s.loop=true;f.type=type;f.Q.value=q||1;f.frequency.setValueAtTime(f0,t);f.frequency.exponentialRampToValueAtTime(Math.max(30,f1),t+dur);
    g.gain.setValueAtTime(.0005,t);g.gain.exponentialRampToValueAtTime(vol,t+dur*.3);g.gain.exponentialRampToValueAtTime(.0005,t+dur);
    s.connect(f);f.connect(g);g.connect(MG);s.start(t,Math.random());s.stop(t+dur+.05);}catch(_){}
}
function ring(fr,dur,vol){            // inharmonic partials: struck metal
  if(!AUC)return;
  try{const t=AUC.currentTime,dt=.985+Math.random()*.03;
    fr.forEach((f,i)=>{const o=AUC.createOscillator(),g=AUC.createGain(),d=dur*(1-.11*i);o.type='sine';o.frequency.value=f*dt*(1+(Math.random()-.5)*.012);
      g.gain.setValueAtTime(vol/Math.pow(i+1,.6),t);g.gain.exponentialRampToValueAtTime(.0004,t+d);o.connect(g);g.connect(MG);o.start(t);o.stop(t+d+.02);});}catch(_){}
}
function snd(k){
  if(!AUC)return;
  switch(k){
    case 'swing':nz(.17,.17,'bandpass',480,2700,1.3);nz(.09,.045,'highpass',3200,6500);break;
    case 'miniScorp':nz(.2,.055,'highpass',2400,5200,2.2);tone(1180,620,.16,'sine',.025);break;
    case 'miniCroc':nz(.2,.075,'lowpass',420,105,1.1);tone(96,54,.17,'triangle',.045);break;
    case 'miniGhost':tone(880,1320,.24,'sine',.025);tone(660,990,.28,'sine',.018);break;
    case 'miniWolf':nz(.22,.065,'bandpass',180,760,1.4);tone(138,78,.19,'triangle',.04);break;
    case 'swingH':nz(.27,.22,'bandpass',280,2000,1.1);nz(.12,.05,'highpass',2600,6000);break;
    case 'ewh':nz(.2,.08,'bandpass',260,1300,1);break;
    case 'hit':ring([1740,2630,3910,5230,6850],.34,.065);nz(.05,.24,'highpass',2600,5200);tone(150,48,.14,'sine',.22);nz(.13,.13,'lowpass',950,280);break;
    case 'block':ring([610,1040,1720,2490],.24,.085);nz(.06,.26,'bandpass',1300,650,.8);tone(185,70,.11,'sine',.2);break;
    case 'parry':ring([2880,4340,5810,7420],.75,.07);nz(.04,.2,'highpass',4200,7500);break;
    case 'draw':nz(.32,.09,'bandpass',2300,5200,3.5);ring([3100,4720],.45,.025);break;
    case 'hurt':tone(112,44,.2,'sine',.3);nz(.18,.2,'lowpass',720,190);nz(.08,.1,'bandpass',1900,900,1);break;
    case 'die':tone(92,28,1.4,'sawtooth',.09);nz(1.2,.08,'lowpass',420,80);break;
    case 'rest':ring([523,784,1046,1568],1.5,.045);break;
    case 'boss':tone(72,24,.6,'sine',.4);nz(.55,.26,'lowpass',320,60);break;
    case 'land':tone(84,40,.2,'sine',.26);nz(.16,.16,'lowpass',520,150);break;
    case 'roll':nz(.23,.1,'lowpass',1250,380);break;
    case 'jump':nz(.12,.06,'lowpass',950,480);break;
    case 'step':nz(.065,.05,'lowpass',520,190);tone(92,58,.05,'sine',.045);break;
    case 'doorOpen':tone(1900,900,.03,'square',.05);nz(.75,.085,'bandpass',360,880,11);nz(.6,.04,'bandpass',900,640,8);break;
    case 'doorClose':nz(.5,.07,'bandpass',820,360,11);break;
    case 'doorShut':tone(96,44,.2,'sine',.32);nz(.13,.22,'lowpass',620,150);tone(2300,1200,.025,'square',.05);break;
    case 'doorLock':tone(1500,1100,.03,'square',.06);nz(.09,.1,'bandpass',1800,1200,3);break;
    case 'drink':for(let k=0;k<3;k++)setTimeout(()=>{nz(.12,.05,'bandpass',500,240,4);tone(180,120,.1,'sine',.05);},k*170);break;
    case 'roar':nz(1.4,.35,'lowpass',420,90,1.5);tone(95,55,1.3,'sawtooth',.16);tone(140,70,1.1,'sawtooth',.08);break;
    case 'bow':nz(.12,.12,'bandpass',1800,600,3);tone(260,120,.12,'triangle',.06);break;
    case 'talk':tone(300+Math.random()*90,250,.07,'triangle',.05);break;
    case 'sling':nz(.16,.09,'bandpass',1400,3200,2);break;
    case 'gust':nz(.75,.3,'lowpass',260,1100,.7);nz(.6,.12,'bandpass',900,2200,1);break;
    case 'screech':tone(1500,2600,.45,'sawtooth',.05);tone(1560,2300,.5,'square',.03);nz(.4,.08,'bandpass',2600,3800,4);break;
    case 'thunder':nz(.9,.4,'lowpass',900,70,.8);tone(60,30,.7,'sine',.35);nz(.05,.3,'highpass',3000,6000);break;
    case 'voice':ring([440,554,659],2.2,.03);break;
    case 'heal':ring([660,990,1320],.65,.032);tone(300,620,.4,'sine',.035);break;
  }
}
let musN=null;
function music(on){   // a low drone with a slow Hijaz phrase, only under the opening film
  if(!AUC)return;
  try{if(!on){if(musN){musN.g.gain.linearRampToValueAtTime(0,AUC.currentTime+1.5);const m=musN;setTimeout(()=>{try{m.o.forEach(o=>o.stop());}catch(_){}} ,1700);musN=null;}return;}
    if(musN)return;const g=AUC.createGain(),t=AUC.currentTime;g.gain.setValueAtTime(0,t);g.gain.linearRampToValueAtTime(.09,t+2.5);g.connect(MG);
    const o=[];for(const f of[73.4,110,146.8]){const os=AUC.createOscillator();os.type='sawtooth';os.frequency.value=f*(1+(Math.random()-.5)*.004);const fl=AUC.createBiquadFilter();fl.type='lowpass';fl.frequency.value=420;os.connect(fl);fl.connect(g);os.start();o.push(os);}
    const mel=[293.7,311.1,370,392,370,311.1,293.7,220,293.7,311.1,293.7,261.6,220];
    mel.forEach((f,i)=>{const os=AUC.createOscillator(),gg=AUC.createGain(),s=t+2+i*2.4;os.type='triangle';os.frequency.value=f;gg.gain.setValueAtTime(0,s);gg.gain.linearRampToValueAtTime(.5,s+.5);gg.gain.exponentialRampToValueAtTime(.001,s+2.3);os.connect(gg);gg.connect(g);os.start(s);os.stop(s+2.4);});
    musN={g,o};}catch(_){}
}


/* ================= regional environmental soundscape =================
   Procedural ambience only: filtered water and wind beds, not field recordings. */
let ENV=null,envRegion='none';
function setEnvironment(region){
  envRegion=String(region||'none');
  if(!AUC||!MG)return envRegion;
  try{
    if(!ENV){
      const master=AUC.createGain();master.gain.value=0;master.connect(MG);
      const makeBed=(kind)=>{
        const src=AUC.createBufferSource();src.buffer=NB;src.loop=true;
        const filter=AUC.createBiquadFilter();filter.type=kind==='water'?'bandpass':'lowpass';
        filter.frequency.value=kind==='water'?620:kind==='city'?165:420;filter.Q.value=kind==='water'?.45:.25;
        const slow=AUC.createOscillator(),depth=AUC.createGain(),gain=AUC.createGain();
        slow.type='sine';slow.frequency.value=kind==='water'?.075:kind==='city'?.045:.035;depth.gain.value=kind==='water'?.012:kind==='city'?.004:.009;
        gain.gain.value=0;slow.connect(depth);depth.connect(gain.gain);
        src.connect(filter);filter.connect(gain);gain.connect(master);src.start();slow.start();
        return {src,slow,gain};
      };
      ENV={master,water:makeBed('water'),wind:makeBed('wind'),city:makeBed('city')};
    }
    const water=envRegion==='duhok'?.055:envRegion==='baghdad'?.008:0;
    const wind=envRegion==='duhok'?.024:envRegion==='muthanna'?.035:envRegion==='baghdad'?.012:.018;
    const city=envRegion==='baghdad'?.018:0;
    const t=AUC.currentTime;
    for(const [bed,value,tc] of [[ENV.water,water,.9],[ENV.wind,wind,1.4],[ENV.city,city,1.8]]){
      bed.gain.gain.cancelScheduledValues(t);
      bed.gain.gain.setTargetAtTime(value,t,tc);
    }
    ENV.master.gain.cancelScheduledValues(t);
    ENV.master.gain.setTargetAtTime(1,t,.8);
  }catch(_){}
  return envRegion;
}
function environmentSnapshot(){return {region:envRegion,active:!!ENV};}

/* ================= spoken character dialogue =================
   Prefer native Arabic speech; keep a short synthesized signature as fallback. */
let VN=[],voiceEnabled=true;
try{voiceEnabled=localStorage.getItem('ashes-rafidain-voice')!=='0';}catch(_){}
function voiceStop(){try{if(window.speechSynthesis)window.speechSynthesis.cancel();}catch(_){}for(const o of VN){try{o.stop();}catch(_){}}VN=[];}
function setVoiceEnabled(on){voiceEnabled=!!on;try{localStorage.setItem('ashes-rafidain-voice',voiceEnabled?'1':'0');}catch(_){}if(!voiceEnabled)voiceStop();return voiceEnabled;}
function voice(id,text){
  voiceStop();if(!voiceEnabled)return;const phrase=String(text||'').trim();if(!phrase)return;
  const profiles={khidr:{pitch:.72,rate:.88,volume:.96},shirin:{pitch:1.08,rate:.98,volume:1},derv:{pitch:.62,rate:.78,volume:.94},narrator:{pitch:.82,rate:.9,volume:.96},erra:{pitch:.55,rate:.76,volume:1},anzu:{pitch:.48,rate:.72,volume:1},boss:{pitch:.52,rate:.78,volume:1}},profile=profiles[id]||profiles.narrator;
  try{const synth=window.speechSynthesis,Utterance=window.SpeechSynthesisUtterance;if(synth&&typeof Utterance==='function'){
    const u=new Utterance(phrase);u.lang='ar-IQ';u.pitch=profile.pitch;u.rate=profile.rate;u.volume=profile.volume;
    const voices=synth.getVoices?synth.getVoices():[],arabic=voices.filter(v=>/^ar(?:-|$)/i.test(v.lang||''));
    if(arabic.length){let chosen=arabic[0];if(id==='shirin')chosen=arabic.find(v=>/female|woman|zira|laila|mona/i.test((v.name||'')+' '+(v.voiceURI||'')))||arabic[0];else chosen=arabic.find(v=>!/female|woman|zira|laila|mona/i.test((v.name||'')+' '+(v.voiceURI||'')))||arabic[0];u.voice=chosen;}
    else if(voices.length)throw new Error('No Arabic speech voice installed');
    synth.speak(u);return;
  }}catch(_){}
  if(!AUC)return;
  try{const V={khidr:[108,'sawtooth',.9],shirin:[225,'triangle',1.25],derv:[84,'sawtooth',.75],narrator:[118,'sawtooth',.9]}[id]||[140,'triangle',1],n=Math.max(5,Math.min(18,Math.round(phrase.length/4))),FM=[700,1050,520,880,1300,620];let t=AUC.currentTime+.03;
    for(let k=0;k<n;k++){const o=AUC.createOscillator(),f=AUC.createBiquadFilter(),g=AUC.createGain(),d=(.075+Math.random()*.07)/V[2]*1.1,f0=V[0]*(1+(Math.random()-.5)*.3)*(k===n-1?.82:1);o.type=V[1];o.frequency.setValueAtTime(f0,t);o.frequency.linearRampToValueAtTime(f0*(.9+Math.random()*.18),t+d);f.type='bandpass';f.Q.value=3.5;f.frequency.setValueAtTime(FM[Math.floor(Math.random()*FM.length)],t);f.frequency.linearRampToValueAtTime(FM[Math.floor(Math.random()*FM.length)],t+d);g.gain.setValueAtTime(.0005,t);g.gain.linearRampToValueAtTime(V[1]==='triangle'?.5:.3,t+.02);g.gain.exponentialRampToValueAtTime(.0008,t+d);o.connect(f);f.connect(g);g.connect(MG);o.start(t);o.stop(t+d+.03);VN.push(o);t+=d*.9+(Math.random()<.18?.11:.012);}
  }catch(_){}
}

/* boss fight: war drums over a low drone */
let BM=null;
function bossMusic(on,profile){
  profile=profile||'default';
  if(!AUC)return;
  try{
    if(!on){if(BM){clearInterval(BM.iv);BM.g.gain.linearRampToValueAtTime(0,AUC.currentTime+1.2);const m=BM;setTimeout(()=>{try{m.o.forEach(o=>o.stop());}catch(_){}} ,1400);BM=null;}return;}
    if(BM&&BM.profile===profile)return;
    if(BM){clearInterval(BM.iv);BM.g.gain.linearRampToValueAtTime(0,AUC.currentTime+.35);const old=BM;setTimeout(()=>{try{old.o.forEach(o=>o.stop());}catch(_){}} ,500);BM=null;}
    const cfg={
      wolfK:{notes:[65.4,98,130.8,196],filter:720,pulse:'wind'},
      ghostK:{notes:[55,82.4,123.5,164.8],filter:520,pulse:'echo'},
      scorpK:{notes:[49,73.4,98,146.8],filter:360,pulse:'metal'},
      crocK:{notes:[41.2,61.7,82.4,110],filter:260,pulse:'water'},
      default:{notes:[55,82.4,110],filter:380,pulse:'heavy'}
    }[profile]||{notes:[55,82.4,110],filter:380,pulse:'heavy'};
    const g=AUC.createGain(),t=AUC.currentTime;g.gain.setValueAtTime(0,t);g.gain.linearRampToValueAtTime(.12,t+1.5);g.connect(MG);
    const o=[];cfg.notes.slice(0,3).forEach((f)=>{const os=AUC.createOscillator(),fl=AUC.createBiquadFilter();os.type='sawtooth';os.frequency.value=f;fl.type='lowpass';fl.frequency.value=cfg.filter;os.connect(fl);fl.connect(g);os.start();o.push(os);});
    let n=0;const iv=setInterval(()=>{if(!AUC)return;const k=n%8;n++;
      if(cfg.pulse==='wind'){
        if(k===0||k===4){tone(130,55,.34,'sine',.27);nz(.28,.1,'bandpass',500,1800,.8);}
        if(k===2||k===6)nz(.12,.07,'highpass',1900,4200,1);
      }else if(cfg.pulse==='echo'){
        if(k===0||k===4)ring([440,659,988,1480],.7,.025);
        if(k===2||k===6){tone(220,110,.48,'sine',.16);nz(.2,.07,'bandpass',1200,420,.7);}
      }else if(cfg.pulse==='metal'){
        if(k===0||k===3||k===6)ring([174,263,392,587],.42,.04);
        if(k===2||k===5)nz(.11,.1,'highpass',2600,1100,2);
      }else if(cfg.pulse==='water'){
        if(k===0||k===4){tone(88,34,.55,'sine',.3);nz(.42,.09,'lowpass',420,95,.7);}
        if(k===2||k===6)nz(.24,.06,'bandpass',700,260,.5);
      }else{
        if(k===0||k===3||k===4||k===6){tone(k===0?70:90,38,.38,'sine',.32);nz(.18,.12,'lowpass',260,80,1);}
        if(k===2||k===7)nz(.09,.06,'highpass',2400,1600,1);
      }
    },235);
    BM={g,o,iv,profile};
  }catch(_){}
}
