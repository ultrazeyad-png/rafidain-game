#!/usr/bin/env node
"use strict";

const { spawnSync } = require("node:child_process");

const configuredPython = process.env.PYTHON;
const command = configuredPython || (process.platform === "win32" ? "py" : "python3");
const args = configuredPython
  ? ["build.py"]
  : process.platform === "win32"
    ? ["-3", "build.py"]
    : ["build.py"];

const result = spawnSync(command, args, {
  cwd: process.cwd(),
  stdio: "inherit",
});

if (result.error) {
  console.error(`Could not start Python (${command}): ${result.error.message}`);
  process.exit(1);
}

process.exit(result.status === null ? 1 : result.status);
