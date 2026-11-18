#!/usr/bin/env python3
"""Portable build and embedded-JavaScript smoke checks for CI."""
from __future__ import annotations

import re
import subprocess
import sys
import tempfile
from html.parser import HTMLParser
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
DIST = ROOT / "dist" / "index.html"


class ContractParser(HTMLParser):
    def __init__(self) -> None:
        super().__init__()
        self.ids: set[str] = set()
        self.title_parts: list[str] = []
        self.in_title = False
        self.canvas_ids: set[str] = set()

    def handle_starttag(self, tag: str, attrs: list[tuple[str, str | None]]) -> None:
        values = dict(attrs)
        element_id = values.get("id")
        if element_id:
            self.ids.add(element_id)
            if tag == "canvas":
                self.canvas_ids.add(element_id)
        if tag == "title":
            self.in_title = True

    def handle_endtag(self, tag: str) -> None:
        if tag == "title":
            self.in_title = False

    def handle_data(self, data: str) -> None:
        if self.in_title:
            self.title_parts.append(data)


def main() -> int:
    subprocess.run([sys.executable, str(ROOT / "build.py")], cwd=ROOT, check=True)
    if not DIST.is_file():
        raise AssertionError("Build did not create dist/index.html")

    html = DIST.read_text(encoding="utf-8")
    if len(html.encode("utf-8")) < 100_000:
        raise AssertionError("Generated game page is unexpectedly small")

    unresolved = re.findall(r"/\*@[^*]+@\*/", html)
    if unresolved:
        raise AssertionError(f"Unresolved source placeholders: {unresolved}")

    parser = ContractParser()
    parser.feed(html)
    if "رماد الرافدين" not in "".join(parser.title_parts):
        raise AssertionError("Arabic game title is missing")
    if not {"gl", "hud"}.issubset(parser.canvas_ids):
        raise AssertionError(f"Expected game canvases gl/hud, got {sorted(parser.canvas_ids)}")
    required_ids = {"root", "go", "bAtk", "bRoll", "bHeal"}
    missing = required_ids - parser.ids
    if missing:
        raise AssertionError(f"Required UI contract IDs are missing: {sorted(missing)}")

    scripts = re.findall(r"<script\b[^>]*>([\s\S]*?)</script\s*>", html, re.IGNORECASE)
    if len(scripts) != 1 or not scripts[0].strip():
        raise AssertionError(f"Expected one non-empty inline game script; found {len(scripts)}")

    with tempfile.NamedTemporaryFile("w", suffix=".js", encoding="utf-8") as js_file:
        js_file.write(scripts[0])
        js_file.flush()
        subprocess.run(["node", "--check", js_file.name], cwd=ROOT, check=True)

    print(f"PASS: build generated {DIST.relative_to(ROOT)} ({DIST.stat().st_size:,} bytes)")
    print("PASS: HTML contract, source placeholders, and embedded JavaScript syntax")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
