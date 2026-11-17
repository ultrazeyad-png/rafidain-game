#!/usr/bin/env python3
"""Validate the Capacitor Android wrapper before attempting a Gradle build."""
from __future__ import annotations

import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def main() -> int:
    package = json.loads((ROOT / "package.json").read_text(encoding="utf-8"))
    config = json.loads((ROOT / "capacitor.config.json").read_text(encoding="utf-8"))

    if package.get("name") != "ashes-of-mesopotamia":
        raise AssertionError("Unexpected npm package name")
    deps = package.get("dependencies", {})
    dev_deps = package.get("devDependencies", {})
    for name in ("@capacitor/core", "@capacitor/android"):
        if not deps.get(name, "").startswith("^8."):
            raise AssertionError(f"{name} must use Capacitor 8")
    if not dev_deps.get("@capacitor/cli", "").startswith("^8."):
        raise AssertionError("@capacitor/cli must use Capacitor 8")

    scripts = package.get("scripts", {})
    if "npm run build" not in scripts.get("android:init", ""):
        raise AssertionError("android:init must build the web game before adding Android")
    if "cap add android" not in scripts.get("android:init", ""):
        raise AssertionError("android:init must generate the native Android project")

    if config.get("appId") != "com.nextlevel.ashesofmesopotamia":
        raise AssertionError("Android application ID changed unexpectedly")
    if config.get("appName") != "رماد الرافدين":
        raise AssertionError("Arabic application name is missing")
    if config.get("webDir") != "dist":
        raise AssertionError("Capacitor webDir must point to dist")
    if config.get("android", {}).get("allowMixedContent") is not False:
        raise AssertionError("Mixed content must remain disabled")

    print("PASS: Capacitor 8 dependencies, build scripts, app identity, webDir and secure mixed-content setting")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
