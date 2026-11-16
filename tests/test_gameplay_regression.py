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
                    const perfectParry = {hp: p.hp, enemyState: e.state, stamina: p.st};

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
            if defense["perfectParry"]["hp"] != defense["hpBeforeParry"] or defense["perfectParry"]["enemyState"] != "stun":
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
                            boss: !!g.boss
                        };
                    });
                }"""
            )
            for result in region_results:
                if result["actual"] != result["id"]:
                    raise AssertionError(f"Travel loaded the wrong region: {result}")
                if result["blocks"] <= 0 or result["chunks"] <= 0:
                    raise AssertionError(f"Region did not build world geometry: {result}")
                if not result["boss"]:
                    raise AssertionError(f"Expected region boss was not spawned: {result}")
            print("PASS: Duhok, Muthanna, Nineveh, Anbar, and Baghdad load with geometry and bosses")

            await page.evaluate(
                """() => {
                    const g = window.__g;
                    g.stats.embers = 321;
                    g.stats.vig = 2;
                    g.travel("muthanna");
                }"""
            )
            await page.reload(wait_until="load")
            await page.wait_for_function(
                "window.__g && window.__g.ok() && window.__g.chunks > 0",
                timeout=15000,
            )
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
            await page.reload(wait_until="load")
            await page.wait_for_function("window.__g && window.__g.ok() && window.__g.chunks > 0", timeout=15000)
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
            await page.reload(wait_until="load")
            await page.wait_for_function("window.__g && window.__g.ok() && window.__g.chunks > 0", timeout=15000)
            recovered = await page.evaluate(
                """() => ({
                    level: __g.stats.lvl,
                    backup: localStorage.getItem("ashes-rafidain-v1.backup-corrupt")
                })"""
            )
            if recovered["level"] != 1 or recovered["backup"] != partial_raw:
                raise AssertionError(f"Partial save recovery/backup failed: {recovered}")
            print("PASS: partial save rejected, preserved, and fresh game booted safely")

            if page_errors:
                raise AssertionError("Uncaught browser errors: " + " | ".join(page_errors))
            print("PASS: no uncaught browser page errors")
        finally:
            await browser.close()


def main() -> int:
    handler = functools.partial(SimpleHTTPRequestHandler, directory=str(ROOT))
    server = ThreadingHTTPServer(("127.0.0.1", 0), handler)
    thread = threading.Thread(target=server.serve_forever, daemon=True)
    thread.start()
    try:
        asyncio.run(run_suite(f"http://127.0.0.1:{server.server_port}"))
    finally:
        server.shutdown()
        server.server_close()
        thread.join(timeout=2)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
