#!/usr/bin/env python3
"""Browser regression test for WebGL context loss and restoration."""
from __future__ import annotations

import asyncio
import functools
import threading
from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path

from playwright.async_api import async_playwright

ROOT = Path(__file__).resolve().parents[1]


async def run_browser(base_url: str) -> None:
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
            await page.wait_for_function(
                "window.__g && window.__g.ok() && window.__g.chunks > 0",
                timeout=15000,
            )

            supported = await page.evaluate(
                """() => {
                    const context = document.getElementById('gl').getContext('webgl');
                    const extension = context && context.getExtension('WEBGL_lose_context');
                    if (!extension) return false;
                    window.__webglLossExtension = extension;
                    extension.loseContext();
                    return true;
                }"""
            )
            if not supported:
                raise AssertionError("WEBGL_lose_context is unavailable in the CI browser")

            await page.wait_for_function(
                "window.__g && !window.__g.ok()",
                timeout=5000,
            )
            await page.evaluate("window.__webglLossExtension.restoreContext()")
            await page.wait_for_function(
                "window.__g && window.__g.ok() && window.__g.chunks > 0",
                timeout=15000,
            )
            await page.wait_for_timeout(300)

            if page_errors:
                raise AssertionError("Browser runtime errors: " + " | ".join(page_errors))

            print("PASS: WebGL context loss pauses rendering and restoration rebuilds the world")
        finally:
            await browser.close()


def main() -> int:
    handler = functools.partial(SimpleHTTPRequestHandler, directory=str(ROOT))
    server = ThreadingHTTPServer(("127.0.0.1", 0), handler)
    thread = threading.Thread(target=server.serve_forever, daemon=True)
    thread.start()
    try:
        asyncio.run(run_browser(f"http://127.0.0.1:{server.server_port}"))
    finally:
        server.shutdown()
        server.server_close()
        thread.join(timeout=2)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
