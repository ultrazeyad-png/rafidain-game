/* ================= the Next Level account: your journey on your profile =================
   Only on the website (or the app showing the website): the game asks who is signed in, shows it on the
   title card, and after a guardian falls or a road is taken sends how far you got to /api/game/progress.
   Played from a file or offline, nothing here runs and the game is exactly as before. */
const LINK={on:false,user:null,sig:'',busy:false,t:0};
function linkSite(){try{return /^https?:$/.test(location.protocol)&&/\/game\/?/.test(location.pathname);}catch(_){return false;}}
function linkPayload(){return {done:Object.keys(stats.done||{}).filter(k=>stats.done[k]),keys:stats.keys||0,level:stats.lvl||1,region:REG,ending:stats.ended||0,playSeconds:Math.floor(stats.playSec||0)};}
function linkSig(p){return [p.done.slice().sort().join(','),p.keys,p.level,p.region,p.ending].join('|');}
function linkShow(){const el=$('acct');if(!el)return;el.textContent='';
  if(LINK.user){el.textContent='رحلتك تنحفظ ببروفايلك: '+LINK.user;return;}
  const a=document.createElement('a');a.href='/login?next=/game/';a.textContent='سجّل دخول بحسابك بنيكست لفل';el.append(a,' حتى تطلع رحلتك ببروفايلك.');}
function linkStart(){if(!linkSite())return;LINK.on=true;
  fetch('/api/game/me',{credentials:'same-origin',cache:'no-store'}).then(r=>r.json()).then(d=>{
    if(!d||!d.success)return;LINK.user=d.loggedIn?(d.name||'حسابك'):null;linkShow();if(LINK.user&&linkPayload().done.length)linkSync(true);}).catch(()=>{});
  setInterval(()=>{if(!paused&&!cine&&document.visibilityState==='visible')stats.playSec=(stats.playSec||0)+10;},10000);}
function linkSync(force){
  if(!LINK.on||!LINK.user||LINK.busy)return;const p=linkPayload(),s=linkSig(p);if(!force&&s===LINK.sig)return;
  const wait=5000-(Date.now()-LINK.t);if(wait>0){clearTimeout(LINK.tm);LINK.tm=setTimeout(()=>linkSync(force),wait+50);return;}
  LINK.busy=true;LINK.t=Date.now();
  fetch('/api/game/progress',{method:'POST',credentials:'same-origin',headers:{'content-type':'application/json'},body:JSON.stringify(p)})
    .then(r=>{if(r.ok)LINK.sig=s;else if(r.status===401){LINK.user=null;linkShow();}}).catch(()=>{}).finally(()=>{LINK.busy=false;});}
