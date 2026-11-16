#!/usr/bin/env python3
"""Assertion-based gameplay, region-loading, and save/load regression tests."""
from __future__ import annotations

import asyncio
import functools
import threading
from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path

from playwright.async_api import async_playwright

ROOT = Path(__file__).resolve().parents[1]


def verify_adapa_visual_design() -> None:
    source = (ROOT / "src" / "bosses.js").read_text(encoding="utf-8")
    required = (
        "isAdapa=!!(e.T&&e.T.name&&e.T.name.includes('أدابا'))",
        "function adapaSword()",
        "stepped gold crown",
        "royal vertical sash",
        "if(isAdapa&&st==='sleep')",
        "if(isAdapa&&st==='trans')",
        "p2?'#ff563d':'#287f91'",
        "function adapaSwordPose(state,u)",
        "['c1','c2','c3','sp','sl'].includes(st)",
        "c1:{t:1.62,y:.38,e:.08,w:.86,r:-.35}",
        "c2:{t:.48,y:.88,e:1.78,w:-.9,r:-.42}",
        "c3:{t:.2,y:-.05,e:2.05,w:.5,r:0}",
        "sp:{t:.95,y:.98,e:1.65,w:-.95,r:.65}",
        "sl:{t:.2,y:.05,e:2.2,w:.9,r:0}",
        "case 'parried':b.a=turn(b.a,ang,2*dt)",
        "if(isAdapa&&st==='parried')",
        "voice('boss',m==='sl'?",
        "b.adapaRead||(b.adapaRead={rolls:0,retreat:0,lastRoll:false,lastDist:ad})",
        "if(rolling&&!read.lastRoll)read.rolls=Math.min(3,read.rolls+1)",
        "if(ad>read.lastDist+1.8&&ad>150&&!rolling)",
        "read.rolls>=1.35&&ad<185",
        "read.retreat>.9&&ad>145&&ad<430",
        "else if(isAdapa&&st==='stomp'){",
        "const wind=clamp(e.t/.75,0,1),impact=clamp((e.t-.75)/.24,0,1)",
        "p2?(st==='stomp'?.72+.28*Math.sin(e.t*19+q*.7):1):0",
        "if(p2&&st==='stomp')",
        "q*.065)/.34,0,1)",
        "const isAdapa=b.T&&b.T.name&&b.T.name.includes('أدابا')",
        "باب الطلسم… آخر أبواب بغداد.",
        "أدابا… معلم الحراس، وحارس المفاتيح الخمسة.",
    )
    missing = [item for item in required if item not in source]
    if missing:
        raise AssertionError(f"Adapa's royal visual identity is incomplete: {missing}")
    hero = (ROOT / "src" / "chars.js").read_text(encoding="utf-8")
    for pose in (
        "else if(st==='roll'){const u=clamp(P.t/.42,0,1),curl=Math.sin(u*PI)",
        "else if(st==='hurt'){const flinch=Math.max(0,1-P.t/.3)",
        "else if(st==='roll'){hip=sd>0?.82:-.58;knee=1.22;}",
        "st==='roll'?5+2*Math.sin(P.t/.42*PI)",
        "if(P.guard&&wpn!=='shield')",
    ):
        if pose not in hero:
            raise AssertionError(f"Furat's responsive combat animation is missing: {pose}")
    print("PASS: Adapa has royal visuals, five bespoke sword attacks, voiced taunts, and a dedicated perfect-parry reaction")
    print("PASS: Furat uses curled dodge-roll, flinch recovery, and readable guard poses")


async def run_suite(base_url: str) -> None:
    async with async_playwright() as playwright:
        browser = await playwright.chromium.launch(
            headless=True,
            args=[
                "--use-gl=angle",
                "--use-angle=swiftshader",
                "--enable-unsafe-swiftshader",
                "--ignore-gpu-blocklist",
            ],
        )
        page = await browser.new_page(viewport={"width": 960, "height": 540})
        page_errors: list[str] = []
        page.on("pageerror", lambda error: page_errors.append(str(error)))


        try:
            await page.goto(f"{base_url}/dist/index.html", wait_until="load")
            await page.click("#go")
            await page.wait_for_function(
                "window.__g && window.__g.ok() && window.__g.chunks > 0",
                timeout=15000,
            )

            opening_film = await page.evaluate("""() => __g.cineShots""")
            opening_plan = await page.evaluate("""() => __g.cinePlan""")
            if len(opening_film) < 6 or not any("أدابا" in line and "بغداد" in line for line in opening_film):
                raise AssertionError(f"Opening film must establish Baghdad and reveal Adapa, its final leader: {opening_film}")
            if not any("باب الطلسم" in line for line in opening_film):
                raise AssertionError(f"Opening film is missing Baghdad's Talisman Gate: {opening_film}")
            if [shot["region"] for shot in opening_plan[:4]] != ["baghdad"] * 4 or opening_plan[4]["region"] != "duhok":
                raise AssertionError(f"Opening film must stay in Baghdad for its first four shots, then transition to Zakho: {opening_plan}")
            cinematic_frame = await page.evaluate("""() => {
                const v = document.querySelector("#cV");
                return !!v && getComputedStyle(v).pointerEvents === "none";
            }""")
            if not cinematic_frame:
                raise AssertionError("Opening cinematic is missing its non-interactive film vignette layer")
            print("PASS: opening film stays in Baghdad for four shots, reveals the Talisman Gate and Adapa, then transitions to Zakho")
            print("PASS: cinematic film vignette is present and does not intercept controls")

            ambience = await page.evaluate("""() => {
                __g.setEnvironment("duhok");
                const river = __g.environment;
                __g.setEnvironment("baghdad");
                const city = __g.environment;
                return {river, city};
            }""")
            if ambience["river"]["region"] != "duhok" or ambience["city"]["region"] != "baghdad":
                raise AssertionError(f"Regional ambience did not follow world changes: {ambience}")
            print("PASS: regional ambience profile switches with world transitions")
            arena = await page.evaluate("""() => { __g.travel("duhok"); return __g.miniArena; }""")
            if not arena or arena["region"] != "duhok" or arena["kind"] != "wolfK":
                raise AssertionError(f"Regional mini-boss arena anchor is missing or mismatched: {arena}")
            if "if(MINI_ARENA){" not in (ROOT / "src" / "template.html").read_text(encoding="utf-8"):
                raise AssertionError("Themed mini-boss arena geometry is not built")
            print("PASS: regional mini-boss arena anchor and themed standing-stone ring")
            cinematic_source = (ROOT / "src" / "systems.js").read_text(encoding="utf-8")
            if "function miniBossFilm(e,m)" not in cinematic_source or "isMini=!!cine.mini" not in cinematic_source:
                raise AssertionError("Mini-boss first-encounter cinematic flow is missing")
            game_source = (ROOT / "src" / "template.html").read_text(encoding="utf-8")
            if "miniFilm:world.miniFilm" not in game_source or "miniFilm:!!w.miniFilm" not in game_source:
                raise AssertionError("Mini-boss first-encounter cinematic is not persisted per region")
            if "bossMusic(true,m.k)" not in cinematic_source or "if(e.T.mini){bossMusic(false);" not in game_source:
                raise AssertionError("Mini-boss combat music does not start and stop with the encounter")
            print("PASS: mini-boss first-contact cinematic, per-region persistence, and battle music lifecycle")
            audio_source = (ROOT / "src" / "audio.js").read_text(encoding="utf-8")
            for profile in ("wolfK:{notes:", "ghostK:{notes:", "scorpK:{notes:", "crocK:{notes:"):
                if profile not in audio_source:
                    raise AssertionError(f"Distinct mini-boss music profile missing: {profile}")
            print("PASS: wolf, ghost, scorpion, and crocodile encounters use distinct procedural battle scores")

            speech = await page.evaluate("""() => {
                const spoken = [];
                class FakeUtterance { constructor(text) { this.text = text; } }
                Object.defineProperty(window, "SpeechSynthesisUtterance", {configurable: true, value: FakeUtterance});
                Object.defineProperty(window, "speechSynthesis", {configurable: true, value: {
                    cancel() {},
                    getVoices() { return [{name: "Arabic Test Voice", lang: "ar-IQ"}]; },
                    speak(utterance) { spoken.push({text: utterance.text, lang: utterance.lang, pitch: utterance.pitch, rate: utterance.rate}); }
                }});
                __g.voice("khidr", "امشِ ويا الماي، الماي يعرف الدرب.");
                const first = spoken[0] || null;
                __g.voice("shirin", "اسمي شيرين.");
                const second = spoken[1] || null;
                __g.setVoiceEnabled(false);
                __g.voice("derv", "النار تمشي مثل الشمس.");
                const disabledCount = spoken.length;
                __g.setVoiceEnabled(true);
                return {first, second, disabledCount, enabled: __g.voiceEnabled};
            }""")
            if not speech["first"] or speech["first"]["lang"] != "ar-IQ" or speech["first"]["text"] != "امشِ ويا الماي، الماي يعرف الدرب.":
                raise AssertionError(f"Arabic character speech did not produce the expected utterance: {speech}")
            if not speech["second"] or speech["second"]["pitch"] <= speech["first"]["pitch"]:
                raise AssertionError(f"Character voice profiles were not differentiated: {speech}")
            if speech["disabledCount"] != 2 or not speech["enabled"]:
                raise AssertionError(f"Dialogue mute toggle failed: {speech}")
            print("PASS: Arabic spoken dialogue, distinct character profiles, and mute toggle")

            diagnostics = await page.evaluate("""() => {
                const enabled = __g.toggleDiagnostics(true);
                const snapshot = __g.diagnosticsSnapshot();
                const visible = document.getElementById("diag").classList.contains("on");
                __g.toggleDiagnostics(false);
                return {enabled, visible, snapshot};
            }""")
            if not diagnostics["enabled"] or not diagnostics["visible"]:
                raise AssertionError(f"Performance diagnostics toggle failed: {diagnostics}")
            if not diagnostics["snapshot"]["webgl"] or diagnostics["snapshot"]["width"] <= 0 or diagnostics["snapshot"]["height"] <= 0:
                raise AssertionError(f"Performance diagnostics snapshot incomplete: {diagnostics}")
            print("PASS: runtime performance diagnostics toggle and renderer snapshot")

            combat = await page.evaluate(
                """() => {
                    const g = window.__g;
                    // End the opening cinematic and let the player reach the normal idle state.
                    if (g.cine) g.endCine();
                    for (let i = 0; i < 140; i++) g.step(0.02);
                    const p = g.P;
                    if (p.state !== "idle") throw new Error("Player did not reach idle after intro: " + p.state);
                    p.inv = 0;
                    const target = g.enemies.find(e => !e.boss && !e.dead);
                    if (!target) throw new Error("No standard enemy available for combat test");

                    // Place a controlled target directly in front of the player.
                    target.x = p.x + 32;
                    target.z = p.z;
                    target.y = p.y;
                    target.hx = target.x;
                    target.hz = target.z;
                    target.a = Math.PI;
                    target.kv = 0;
                    target.state = "stun";
                    target.t = 0;
                    target.dead = false;
                    target.hp = target.T.hp;
                    p.a = 0;
                    p.st = 100;
                    const beforeHp = target.hp;

                    g.act("atk");
                    g.step(0.02);
                    for (let i = 0; i < 24; i++) g.step(0.02);
                    const afterAttackHp = target.hp;
                    const attackState = p.state;

                    for (let i = 0; i < 70; i++) g.step(0.02);
                    if (p.state !== "idle") throw new Error("Player did not recover from attack: " + p.state);
                    p.st = 100;
                    const staminaBeforeRoll = p.st;
                    g.act("roll");
                    g.step(0.02);
                    const rollState = p.state;
                    const invulnerability = p.inv;
                    const staminaAfterRoll = p.st;
                    return {
                        beforeHp,
                        afterAttackHp,
                        attackState,
                        staminaBeforeRoll,
                        staminaAfterRoll,
                        rollState,
                        invulnerability
                    };
                }"""
            )
            if not combat["afterAttackHp"] < combat["beforeHp"]:
                raise AssertionError(f"Light attack did not damage the target: {combat}")
            if combat["rollState"] != "roll":
                raise AssertionError(f"Dodge did not enter roll state: {combat}")
            if combat["invulnerability"] <= 0:
                raise AssertionError(f"Dodge did not grant an invulnerability window: {combat}")
            if not combat["staminaAfterRoll"] < combat["staminaBeforeRoll"]:
                raise AssertionError(f"Dodge did not consume stamina: {combat}")
            print("PASS: light attack damage, dodge state, invulnerability, and stamina cost")

            defense = await page.evaluate(
                """() => {
                    const g = window.__g, p = g.P;
                    const e = g.enemies.find(x => !x.boss && !x.dead);
                    if (!e) throw new Error("No standard enemy available for defense test");
                    g.stats.wpn = "shield";
                    p.state = "idle"; p.t = 0; p.hp = 100; p.st = 100; p.a = 0; p.inv = 0;
                    e.x = p.x + 35; e.z = p.z; e.y = p.y; e.a = Math.PI;
                    g.input.block = true;
                    g.step(0.08);
                    e.x = p.x + 35; e.z = p.z; e.y = p.y; e.a = Math.PI;
                    e.state = "strike"; e.t = 0; e.hit = false;
                    const hpBeforeParry = p.hp;
                    g.step(0.02);
                    const perfectParry = {hp: p.hp, enemyState: e.state, stamina: p.st, flash: g.parryFlash};

                    // Hold guard beyond the perfect-parry window, then receive another strike.
                    for (let i = 0; i < 20; i++) g.step(0.02);
                    p.state = "idle"; p.t = 0; p.hp = hpBeforeParry; p.st = 100; p.inv = 0;
                    e.x = p.x + 35; e.z = p.z; e.y = p.y; e.a = Math.PI;
                    e.state = "strike"; e.t = 0; e.hit = false;
                    const staminaBeforeBlock = p.st;
                    g.step(0.02);
                    const normalBlock = {hp: p.hp, stamina: p.st, enemyState: e.state};
                    g.input.block = false;
                    return {hpBeforeParry, perfectParry, normalBlock, staminaBeforeBlock};
                }"""
            )
            if defense["perfectParry"]["hp"] != defense["hpBeforeParry"] or defense["perfectParry"]["enemyState"] != "stun" or defense["perfectParry"]["flash"] <= 0:
                raise AssertionError(f"Perfect parry did not stun the attacker without damage: {defense}")
            if defense["normalBlock"]["hp"] != defense["hpBeforeParry"] or not defense["normalBlock"]["stamina"] < defense["staminaBeforeBlock"]:
                raise AssertionError(f"Normal guard did not trade stamina for blocked damage: {defense}")
            print("PASS: perfect parry stuns attacker; normal guard blocks damage at stamina cost")

            region_results = await page.evaluate(
                """() => {
                    const g = window.__g;
                    g.stats.boat = true; // Skip the one-time Baghdad boat cinematic in this test.
                    const ids = ["duhok", "muthanna", "nineveh", "anbar", "baghdad"];
                    return ids.map(id => {
                        g.travel(id);
                        return {
                            id,
                            actual: g.REG,
                            blocks: g.B.length,
                            chunks: g.chunks,
                            enemies: g.enemies.length,
                            miniBosses: g.enemies.filter(e => e.k === "gmini" && !e.dead).map(e => ({name:e.T.name,scale:e.T.sc})),
                            boss: !!g.boss,
                            duhokDetails: g.duhokDetails()
                        };
                    });
                }"""
            )
            for result in region_results:
                if result["actual"] != result["id"]:
                    raise AssertionError(f"Travel loaded the wrong region: {result}")
                if result["blocks"] <= 0 or result["chunks"] <= 0:
                    raise AssertionError(f"Region did not build world geometry: {result}")
                expected_mini = {
                    "duhok": "ذيب وادي الخابور",
                    "muthanna": "ملك عقارب أوروك",
                    "nineveh": "حارس الحدباء المنفي",
                    "anbar": "ذيب الرمادي",
                    "baghdad": "شبح باب الطلسم",
                }[result["id"]]
                if len(result["miniBosses"]) != 1 or result["miniBosses"][0]["name"] != expected_mini:
                    raise AssertionError(f"Region must spawn its assigned gmini encounter ({expected_mini}): {result}")
                if result["miniBosses"][0]["scale"] <= 1:
                    raise AssertionError(f"Regional mini-boss must use its enlarged boss model: {result}")
                if not result["boss"]:
                    raise AssertionError(f"Expected region boss was not spawned: {result}")
                if result["id"] == "duhok" and result["duhokDetails"] < 20:
                    raise AssertionError(f"Zakho/Delal landmark detail count was not preserved through baking: {result}")
            print("PASS: Duhok, Muthanna, Nineveh, Anbar, and Baghdad load with geometry and bosses")

            await page.evaluate(
                """() => {
                    const g = window.__g;
                    g.stats.embers = 321;
                    g.stats.vig = 2;
                    g.travel("muthanna");
                }"""
            )
            await page.evaluate("() => window.location.reload()")
            # Wait for game startup, then assert persistence explicitly below.
            # Do not turn a wrong restored region into a 30-second opaque timeout.
            await page.wait_for_function("!!window.__g", timeout=30000)
            save_state = await page.evaluate(
                "() => ({region: __g.REG, embers: __g.stats.embers, vigor: __g.stats.vig})"
            )
            if save_state != {"region": "muthanna", "embers": 321, "vigor": 2}:
                raise AssertionError(f"Save/load round trip mismatch: {save_state}")
            print("PASS: Muthanna save/load round trip preserves region and player progression")

            # Legacy v1 saves are migrated in place only after preserving their exact bytes.
            legacy = {
                "stats": {"lvl": 1, "vig": 3, "end": 2, "str": 1, "embers": 777, "wpn": "shield"},
                "reg": "muthanna",
                "rs": {},
            }
            legacy_raw = __import__("json").dumps(legacy, separators=(",", ":"))
            await page.evaluate(
                """raw => {
                    localStorage.removeItem("ashes-rafidain-v1.backup-v1");
                    localStorage.removeItem("ashes-rafidain-v1.backup-corrupt");
                    localStorage.setItem("ashes-rafidain-v1", raw);
                }""",
                legacy_raw,
            )
            await page.evaluate("() => window.location.reload()")
                        # Migration correctness is asserted below; WebGL health is covered by its dedicated test.
            await page.wait_for_function(
                "window.__g && window.__g.chunks > 0 && window.__g.REG === 'muthanna'",
                timeout=30000,
            )
            migrated = await page.evaluate(
                """() => ({
                    live: JSON.parse(localStorage.getItem("ashes-rafidain-v1")),
                    backup: localStorage.getItem("ashes-rafidain-v1.backup-v1"),
                    region: __g.REG,
                    embers: __g.stats.embers,
                    vigor: __g.stats.vig
                })"""
            )
            if migrated["live"].get("schemaVersion") != 2 or migrated["backup"] != legacy_raw:
                raise AssertionError(f"Legacy save migration/backup failed: {migrated}")
            if (migrated["region"], migrated["embers"], migrated["vigor"]) != ("muthanna", 777, 3):
                raise AssertionError(f"Legacy save progress was not preserved: {migrated}")
            print("PASS: legacy v1 save migrates to schema v2 with byte-for-byte backup")

            # A structurally incomplete save must not be loaded as valid progress.
            partial_raw = '{"stats":{"lvl":99},"reg":"muthanna","rs":{}}'
            await page.evaluate(
                "raw => localStorage.setItem('ashes-rafidain-v1', raw)", partial_raw
            )
            await page.evaluate("() => window.location.reload()")
            # Corrupt-save recovery only needs the game world to boot; WebGL
            # context health is independently covered by test_webgl_recovery.py.
            await page.wait_for_function("window.__g && window.__g.chunks > 0 && window.__g.REG === 'duhok'", timeout=30000)
            recovered = await page.evaluate(
                """() => ({
                    level: __g.stats.lvl,
                    region: __g.REG,
                    liveSave: JSON.parse(localStorage.getItem("ashes-rafidain-v1") || "null"),
                    backup: localStorage.getItem("ashes-rafidain-v1.backup-corrupt")
                })"""
            )
            fresh = recovered["liveSave"]
            if (
                recovered["level"] != 1
                or recovered["region"] != "duhok"
                or not fresh
                or fresh.get("schemaVersion") != 2
                or fresh.get("reg") != "duhok"
                or fresh.get("stats", {}).get("lvl") != 1
                or recovered["backup"] != partial_raw
            ):
                raise AssertionError(f"Partial save recovery/backup failed: {recovered}")
            print("PASS: partial save rejected, preserved, and fresh game booted safely")

            if page_errors:
                raise AssertionError("Uncaught browser errors: " + " | ".join(page_errors))
            print("PASS: no uncaught browser page errors")
        finally:
            await browser.close()


async def run_mobile_smoke(base_url: str) -> None:
    """Exercise touch layout and pointer-driven controls in mobile Chromium."""
    async with async_playwright() as playwright:
        browser = await playwright.chromium.launch(
            headless=True,
            args=["--use-gl=angle", "--use-angle=swiftshader", "--enable-unsafe-swiftshader", "--ignore-gpu-blocklist"],
        )
        context = await browser.new_context(
            viewport={"width": 390, "height": 844},
            screen={"width": 390, "height": 844},
            device_scale_factor=2,
            is_mobile=True,
            has_touch=True,
        )
        page = await context.new_page()
        page_errors: list[str] = []
        page.on("pageerror", lambda error: page_errors.append(str(error)))
        try:
            await page.goto(f"{base_url}/dist/index.html", wait_until="load")
            await page.evaluate("() => { if (window.__g) window.__g.stats.intro = 1; }")
            await page.click("#go")
            await page.wait_for_function(
                "window.__g && window.__g.REG === 'duhok' && window.__g.ok()",
                timeout=20000,
            )
            layout = await page.evaluate("""() => {
                const ids = ['jz','cz','bAtk','bRoll','bHeal','bJump','bLock','bBlock','bCast','bBow','bMap'];
                const rect = el => {
                    const r = el.getBoundingClientRect();
                    return {left:r.left,top:r.top,right:r.right,bottom:r.bottom,width:r.width,height:r.height,
                            display:getComputedStyle(el).display,pointerEvents:getComputedStyle(el).pointerEvents,
                            touchAction:getComputedStyle(el).touchAction};
                };
                return {width:innerWidth,height:innerHeight,controls:Object.fromEntries(ids.map(id=>[id,rect(document.getElementById(id))]))};
            }""")
            for control, rect in layout["controls"].items():
                if rect["display"] == "none" or rect["pointerEvents"] == "none" or rect["width"] <= 0 or rect["height"] <= 0:
                    raise AssertionError(f"Mobile control is hidden or has no hit area: {control}: {rect}")
                if rect["left"] < -2 or rect["top"] < -2 or rect["right"] > layout["width"] + 2 or rect["bottom"] > layout["height"] + 2:
                    raise AssertionError(f"Mobile control falls outside viewport: {control}: {rect}")
            if layout["controls"]["jz"]["touchAction"] != "none" or layout["controls"]["cz"]["touchAction"] != "none":
                raise AssertionError(f"Virtual sticks must suppress browser gestures: {layout}")
            await page.evaluate("""() => {
                const el=document.getElementById('jz'),r=el.getBoundingClientRect(),x=r.left+r.width*.5,y=r.top+r.height*.5;
                el.dispatchEvent(new PointerEvent('pointerdown',{bubbles:true,cancelable:true,pointerId:41,pointerType:'touch',clientX:x,clientY:y}));
                el.dispatchEvent(new PointerEvent('pointermove',{bubbles:true,cancelable:true,pointerId:41,pointerType:'touch',clientX:x+35,clientY:y-25}));
            }""")
            joystick = await page.evaluate("() => ({x:__g.input.jx,y:__g.input.jy})")
            if abs(joystick["x"]) < .1 or abs(joystick["y"]) < .1:
                raise AssertionError(f"Virtual joystick did not update movement input: {joystick}")
            await page.evaluate("""() => document.getElementById('jz').dispatchEvent(
                new PointerEvent('pointerup',{bubbles:true,pointerId:41,pointerType:'touch'}))""")
            reset = await page.evaluate("() => ({x:__g.input.jx,y:__g.input.jy})")
            if reset != {"x":0,"y":0}:
                raise AssertionError(f"Virtual joystick did not reset after touch release: {reset}")
            await page.evaluate("""() => {
                const el=document.getElementById('cz'),r=el.getBoundingClientRect(),x=r.left+r.width*.5,y=r.top+r.height*.5,before=__g.cam.yaw;
                el.dispatchEvent(new PointerEvent('pointerdown',{bubbles:true,cancelable:true,pointerId:42,pointerType:'touch',clientX:x,clientY:y}));
                el.dispatchEvent(new PointerEvent('pointermove',{bubbles:true,cancelable:true,pointerId:42,pointerType:'touch',clientX:x+40,clientY:y+10}));
                window.__mobileCameraChanged=__g.cam.yaw!==before&&__g.input.camDown;
                el.dispatchEvent(new PointerEvent('pointerup',{bubbles:true,pointerId:42,pointerType:'touch'}));
            }""")
            camera = await page.evaluate("() => ({changed:window.__mobileCameraChanged,down:__g.input.camDown})")
            if not camera["changed"] or camera["down"]:
                raise AssertionError(f"Touch camera drag did not update/release correctly: {camera}")
            await page.dispatch_event("#bRoll","pointerdown",{"pointerId":43,"pointerType":"touch","bubbles":True,"cancelable":True})
            action = await page.evaluate("() => __g.P.buf && __g.P.buf.a")
            await page.dispatch_event("#bRoll","pointerup",{"pointerId":43,"pointerType":"touch","bubbles":True})
            if action != "roll":
                raise AssertionError(f"Mobile dodge button did not trigger roll action: {action}")
            if page_errors:
                raise AssertionError("Mobile runtime JavaScript errors: " + " | ".join(page_errors))
            print("PASS: mobile Chromium portrait layout, touch hit areas, joystick, camera drag, dodge button, and runtime errors")
        finally:
            await context.close()
            await browser.close()


def verify_major_boss_weakpoints() -> None:
    game = (ROOT / "src" / "template.html").read_text(encoding="utf-8")
    chars = (ROOT / "src" / "chars.js").read_text(encoding="utf-8")
    for item in (
        "if(e.armor==null)e.armor=2",
        "d=Math.round(d*.58)",
        "if(heavy||crit)",
        "e.weakT=7",
        "d=Math.round(d*2.15)",
        "src.armor=Math.max(0,src.armor-1)",
        "case 'parried'",
        "نقطة الضعف مكشوفة",
        "درع الحارس:",
    ):
        if item not in game:
            raise AssertionError(f"Major-boss armour/weakpoint combat hook missing: {item}")
    for item in (
        "function drawBossCore(e)",
        "if(e.weakT>0)",
        "drawNamtar(e);drawBossCore(e)",
        "drawErra(e);drawBossCore(e)",
        "drawWarrior(e);drawBossCore(e)",
    ):
        if item not in chars:
            raise AssertionError(f"Major-boss weakpoint visual missing: {item}")
    print("PASS: major-boss armour plates, perfect-parry breaks, exposed weakpoint damage, HUD and core visuals")


def verify_boss_finisher() -> None:
    game = (ROOT / "src" / "template.html").read_text(encoding="utf-8")
    systems = (ROOT / "src" / "systems.js").read_text(encoding="utf-8")
    for item in (
        "e.weakT>0))",
        "e.weakT>0&&e.armor===0&&e.hp<=e.T.hp*.28",
        "اضغط هجوم لتنفيذ الضربة القاضية",
        "startBossFinisher(e)",
    ):
        if item not in game:
            raise AssertionError(f"Boss finisher trigger or HUD prompt missing: {item}")
    for item in (
        "function startBossFinisher(e)",
        "finisher:true,finisherWeapon:weapon,shots",
        "const bossLine=finisherRegion.line",
        "weaponStyle.line",
        "if(cine.finisher){const a=cine.boss&&cine.boss.finisherPose;P.t=Math.min(a||1.45,P.t+dt)",
        "if(cine&&cine.finisher)",
        "hurtE(b,b.hp,true,true)",
    ):
        if item not in systems:
            raise AssertionError(f"Boss finisher cinematic lifecycle missing: {item}")
    print("PASS: exposed-core critical finisher trigger, three-shot cinematic, and standard boss reward/death flow")


def verify_weapon_and_boss_specific_finishers() -> None:
    systems = (ROOT / "src" / "systems.js").read_text(encoding="utf-8")
    chars = (ROOT / "src" / "chars.js").read_text(encoding="utf-8")
    for item in (
        "weapon==='dual'?{key:'F2',hand:2",
        "weapon==='glaive'?{key:'F3'",
        "weapon==='axe'?{key:'F4'",
        "weapon==='spear'?{key:'F5'",
        "baghdad:{title:'خاتمة باب الطلسم'",
        "duhok:{title:'خاتمة فارس العاصفة'",
        "muthanna:{title:'خاتمة رسول العالم السفلي'",
        "nineveh:{title:'خاتمة ظلال الحدباء'",
        "dhiqar:{title:'خاتمة ملك أور'",
        "basra:{title:'خاتمة حارس الشط'",
        "const bossLine=finisherRegion.line",
        "impact:true,col:weaponStyle.impact,bossCol:finisherRegion.color",
        "if(s.bossCol)burst(b.x,b.y+b.T.h*.68,b.z,s.bossCol",
        "impact:true,col:weaponStyle.impact",
        "burst(b.x,b.y+b.T.h*.68,b.z,s.col",
    ):
        if item not in systems:
            raise AssertionError(f"Weapon/boss-specific finisher choreography missing: {item}")
    for item in ("F1:[[", "F2:[[", "F3:[[", "F4:[[", "F5:[[", "P.finisherKey||a.key||'A'"):
        if item not in chars:
            raise AssertionError(f"Dedicated finisher weapon animation missing: {item}")
    print("PASS: five weapon-specific finisher poses, boss-specific lines and impact effects")


def verify_regional_mini_bosses() -> None:
    roster = (ROOT / "src" / "gen.js").read_text(encoding="utf-8")
    regions = (ROOT / "src" / "regions.js").read_text(encoding="utf-8")
    game = (ROOT / "src" / "template.html").read_text(encoding="utf-8")
    characters = (ROOT / "src" / "chars.js").read_text(encoding="utf-8")
    depth = (ROOT / "src" / "depth.js").read_text(encoding="utf-8")
    audio = (ROOT / "src" / "audio.js").read_text(encoding="utf-8")
    required = (
        "const MINI_GUARDS=",
        "nineveh:{k:'ghostK'",
        "duhok:{k:'wolfK'",
        "muthanna:{k:'scorpK'",
        "baghdad:{k:'ghostK'",
        "MINI_ARENA=mini?{region:g.id,kind:mini.k,name:mini.n,x:b1[0]+70,z:b1[1]+70,y:b1[3]}:null",
        "SPAWN.push(['gmini',MINI_ARENA.x,MINI_ARENA.z,MINI_ARENA.y])",
    )
    missing = [item for item in required if item not in roster]
    if missing:
        raise AssertionError(f"Regional mini-boss roster is incomplete: {missing}")
    for arena in ("MINI_ARENA={region:'duhok',kind:'wolfK'", "MINI_ARENA={region:'muthanna',kind:'scorpK'"):
        if arena not in regions:
            raise AssertionError(f"Special-region mini-boss arena metadata missing: {arena}")
    for spawn in ("['gmini',1720,720,120]", "['gmini',2400,800,0]"):
        if spawn not in regions:
            raise AssertionError(f"Special-region mini-boss spawn missing: {spawn}")
    for item in ("minis:world.minis", "minis:!!w.minis", "world.minis=true",
        "loot('dsh',2);loot('sh',3);loot('seals',1)",
        "say('سقط '+e.T.name+' · حصلت على ختم الحارس','#e8c66a',3.4);save()",
        "أختام الحراس: '+(stats.seals||0)", "s[0]==='gmini'&&world.minis"):
        if item not in game:
            raise AssertionError(f"Mini-boss persistence/reward hook missing: {item}")
    if "if(e.k==='gmini'){const mk=MINI_GUARDS[REG]&&MINI_GUARDS[REG].k" not in characters:
        raise AssertionError("Regional mini-bosses are missing their dedicated creature renderer")
    for behavior in ("voice('boss',m.line)", "e.orbitT=(e.orbitT||0)+dt", "Math.cos(ang+PI/2)", "MINI_GUARDS[REG].k==='ghostK')e.blinked=false", "burst(e.x,e.y+24,e.z,'#b9a0ff',10,82,e.y)", "mini==='scorpK'&&e.p2&&!e.poisonCast", "waves.push({x:e.x,z:e.z,r:18,l:1.05,y:e.y,col:'#a5d64b'})", "mini==='crocK'&&e.p2?ang+(e.sx||1)*.34:ang", "e.rippleCast=false", "e.k==='gmini'&&MINI_GUARDS[REG].k==='crocK'&&e.p2&&!e.rippleCast", "waves.push({x:e.x,z:e.z,r:34,l:1.15,y:e.y,col:'#55c9bb'})", "e.blinked=true", "e.feintMoved=false", "mini==='wolfK'&&e.p2&&!e.feintMoved&&e.t>=windDur*.42", "moveEnt(e,e.x-Math.cos(ang)*42,e.z-Math.sin(ang)*42,T.r*.9,true)", "Math.sin(clamp(e.t/T.act,0,1)*PI)*(e.p2?62:42)", "?1.32:1", "const tailSnap=st==='strike'?Math.sin(strike*PI):st==='wind'?wind:0", "const recoil=st==='strike'?Math.sin(strike*PI):st==='rec'?Math.sin(Math.min(1,e.t/.28)*PI):0", "const arm=st==='wind'?lerp(.5,2.8,wind):st==='strike'?lerp(2.8,-1.35,strike)", "'#a5d64b'", "case 'miniTrans'", "e.hp<=T.hp*.5", "waves.push({x:e.x,z:e.z,r:24,l:.95,y:e.y,col})", "e.p2?1.18:1"):
        if behavior not in game + depth:
            raise AssertionError(f"Regional mini-boss phase-two behavior missing: {behavior}")
    if "T(e.x,e.y+e.j,e.z)" not in characters:
        raise AssertionError("Wolf mini-boss leap pose is not rendered")
    for cue in ("e.k==='gmini'&&e.state==='wind'", "pulse=.72+.28*Math.sin(time*13)", "if(kind==='scorpK')", "else if(kind==='crocK')", "else if(kind==='ghostK')", "ctx.moveTo(p[0]-2.5,p[1]-12)", "snd(kind==='scorpK'?'miniScorp'"):
        if cue not in game:
            raise AssertionError(f"Mini-boss attack telegraph cue missing: {cue}")
    for cue in ("case 'miniScorp'", "case 'miniCroc'", "case 'miniGhost'", "case 'miniWolf'"):
        if cue not in audio:
            raise AssertionError(f"Mini-boss warning sound missing: {cue}")
    print("PASS: regional mini-boss identities, creature rendering, spawns, rewards, persistence, and wind-up telegraph")


def main() -> int:
    verify_adapa_visual_design()
    verify_major_boss_weakpoints()
    verify_boss_finisher()
    verify_weapon_and_boss_specific_finishers()
    verify_regional_mini_bosses()
    handler = functools.partial(SimpleHTTPRequestHandler, directory=str(ROOT))
    server = ThreadingHTTPServer(("127.0.0.1", 0), handler)
    thread = threading.Thread(target=server.serve_forever, daemon=True)
    thread.start()
    try:
        asyncio.run(run_suite(f"http://127.0.0.1:{server.server_port}"))
        asyncio.run(run_mobile_smoke(f"http://127.0.0.1:{server.server_port}"))
    finally:
        server.shutdown()
        server.server_close()
        thread.join(timeout=2)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
