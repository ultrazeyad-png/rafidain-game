# Ashes of Mesopotamia — Commercial Production Plan

**Status:** Phase 00 — baseline and production hardening  
**Current branch:** `production/phase-00-audit`  
**Primary product direction:** a polished, browser-delivered third-person Soulslike, preserving the existing Iraqi/Mesopotamian identity.

## 1. Product definition

Ashes of Mesopotamia follows Furat from Zakho toward Baghdad while he searches for Dijla and uncovers Adaba's role in the catastrophe. The commercial direction should preserve the journey through Iraqi regions, the local dialogue, the river/landscape motifs, and the Mesopotamian mythology.

The product promise is:
- Deliberate, readable, responsive third-person combat.
- Distinct regions with memorable landmarks, routes, secrets, and boss encounters.
- Consistent visual direction rather than isolated graphical effects.
- Reliable save/progression and clear player feedback.
- Stable performance on an explicitly supported device/browser matrix.

This plan does not claim AAA production values or a shipping date. Those require asset, device, performance, and playtest evidence.

## 2. Current technical assessment

### What exists in the repository

- A custom JavaScript/WebGL renderer and a self-contained HTML build.
- Procedural mesh construction and a CPU-side static-world collection.
- A custom shader covering sky, fog, water, directional lighting, a shadow-map path, and up to three local lights.
- Player state-machine movement, weapon-specific attack data, stamina, dodge, guard, lock-on, healing, ranged attacks, and spells.
- Enemy archetypes and region bosses with phase changes and authored attack sequences.
- Region construction, procedural region generation, map/travel, dialogue, notes, secrets, shrines, inventory/progression, audio, and localStorage save data.
- A Python source assembler (`build.py`) and a site splitter (`tools/site_split.py`).
- Browser scenario scripts in `tests/` using Playwright; several rely on a locally installed Google Chrome and SwiftShader.

### Strengths

1. The project already has a recognisable product identity and a broad playable systems foundation.
2. Keeping a custom renderer avoids an immediate rewrite and preserves the work already invested.
3. The game is split into source files by broad domain, even though the build assembles them into one runtime page.
4. Existing scenario scripts encode valuable journeys and regression knowledge.

### Risks and gaps to verify

1. **Runtime architecture:** `src/template.html` still owns many gameplay systems and global state. This raises coupling and regression risk.
2. **Rendering resilience:** explicit WebGL context-loss recovery, GPU resource lifetime handling, and graceful quality fallbacks need verification.
3. **Performance evidence:** there is no committed benchmark report or agreed device matrix yet. FPS, frame-time percentiles, memory, draw calls, and load times must be measured on real target devices.
4. **Test quality:** existing scripts are valuable exploratory scenarios, but most print state and take screenshots rather than asserting pass/fail contracts. They are not yet a reliable CI gate.
5. **Build/deployment:** the build is a custom Python concatenation step, and site deployment currently requires a manual copy/split workflow.
6. **Content pipeline:** the current visual world is largely generated from code. A commercial art pipeline needs documented asset standards, naming, budgets, review, and provenance.
7. **Save compatibility:** localStorage persistence needs versioned schema/migrations and corruption recovery tests before updates can safely ship.

### Engine decision

**Keep the custom WebGL engine for Phase 00 and the first vertical-slice evaluation. Do not migrate to Unreal/Unity yet.**

A migration decision is justified only if measured requirements cannot be met with a bounded renderer/architecture upgrade, or if the product scope requires capabilities whose implementation cost in the current engine is disproportionate (for example, advanced skeletal animation tooling, large authored asset workflows, platform-native builds, or extensive cinematic tooling). Any migration proposal must include a feature gap, cost/risk comparison, asset reuse plan, and playable proof of concept.

## 3. Production phases and exit criteria

### Phase 00 — Baseline, build reliability, and QA foundation

Deliver:
- A repeatable build and JavaScript syntax check.
- CI smoke checks on every pull request.
- A written device/browser support matrix and performance test protocol.
- A baseline defect/performance report from actual browser runs.
- A branch/PR workflow so changes never land directly on `main` without review.

Exit criteria:
- Build completes from a clean checkout.
- Generated HTML contains no unresolved source placeholders.
- Embedded JavaScript parses successfully.
- CI is green.
- Existing browser scenarios have documented commands, dependencies, and known limitations.

### Phase 01 — Runtime architecture and resilience

Work:
- Define clear boundaries for renderer, simulation, input, world/region lifecycle, UI, audio, and persistence.
- Introduce a small runtime diagnostics layer (frame time, FPS, draw-call counts where available, active entities, renderer capability flags).
- Add WebGL context-lost/restored handling and a controlled renderer reinitialization path.
- Make quality settings capability-aware; release GPU resources when regions/assets are unloaded.
- Add a versioned save envelope and migration tests.

Exit criteria:
- Context loss does not silently leave a frozen/broken game.
- Save/load round trips and old-save migration pass automated tests.
- Diagnostics can be enabled without changing gameplay behavior.

### Phase 02 — Combat vertical slice

Work:
- Establish authored timing windows for wind-up, active frames, recovery, hit-stop, invulnerability, and stamina cost.
- Validate hitboxes/hurtboxes and attack direction/height.
- Make input buffering and animation/state transitions predictable.
- Tune one standard enemy, one elite enemy, and one boss encounter.
- Add deterministic combat tests for damage, stamina, dodge invulnerability, guard/parry, death, and boss phase transitions.

Exit criteria:
- No duplicate hit registration in one attack.
- Combat state transitions are deterministic under fixed test inputs.
- A new player can read enemy telegraphs and understand why damage occurred.
- Balance is reviewed through repeatable playtests, not code inspection alone.

### Phase 03 — Art direction and visual-quality slice

Work:
- Create one representative benchmark region with a consistent palette, lighting, fog, materials, landmark silhouette, and environmental storytelling.
- Define material/color standards and modular environment kits.
- Improve character silhouette, weapon readability, impact VFX, and camera framing.
- Add scalable shadow/render resolution and quality presets after profiling.

Exit criteria:
- The benchmark region meets the agreed visual reference and performance budget on the target device matrix.
- Visual improvements do not introduce unacceptable frame-time spikes or memory growth.

### Phase 04 — World, enemies, bosses, and progression

Work:
- Review every region for route readability, shortcuts, landmarks, encounter pacing, and safe rest points.
- Ensure each boss has distinct telegraphs, punish windows, arena readability, and phase logic.
- Review enemy roles and combinations so encounters teach and test skills fairly.
- Add regression coverage for region transitions, keys, gates, secrets, rewards, and endings.

Exit criteria:
- The critical path can be completed without soft locks.
- Every region's main path and progression gates are covered by automated or documented manual tests.
- Bosses pass encounter playtests across more than one weapon/build.

### Phase 05 — UX, accessibility, audio, and save integrity

Work:
- Review menus, HUD hierarchy, inventory, map, subtitles, settings, pause/resume, and controller/touch affordances.
- Add remappable keyboard/controller controls where supported, plus touch layout customization if mobile remains a target.
- Add volume sliders, subtitle controls, and readable UI scaling.
- Version and validate save data; recover safely from malformed data.

Exit criteria:
- New players can understand core controls and progression without external instructions.
- Settings persist and apply consistently.
- Save corruption does not crash startup or erase recoverable progress silently.

### Phase 06 — Optimization, release candidate, and launch

Work:
- Profile representative low/medium/high-end devices.
- Optimize draw calls, geometry, overdraw, shader cost, texture/asset memory, and loading.
- Run long-session, region-transition, save/load, and boss-fight soak tests.
- Produce a release checklist, rollback plan, versioned artifacts, and deployment verification.

Exit criteria:
- No known blocker/critical defects.
- No uncaught runtime errors or WebGL errors in the release test suite.
- Performance budgets pass on the supported device matrix.
- Release build is reproducible and deployment is verified on the live site.

## Current implementation note — 2026-11-18

- The boss weakpoint already has a seven-second exposure period and a 2.15x damage multiplier after armor depletion.
- The HUD now displays the remaining exposure time to make the punish window readable, including during the finisher prompt.
- A regression assertion checks that the countdown is present in the boss HUD.
- This change is isolated on a production-prefixed validation branch; it is not deployed to the live game.
- Automated Quality Gates and Android Debug APK workflows must pass before this update is considered ready for testing.

## 4. Quality gates

Every code change should satisfy the relevant gates:

- **Build:** clean build from source; no unresolved template markers.
- **Static checks:** Python compilation and JavaScript syntax validation.
- **Runtime:** no uncaught page errors; no unexpected WebGL errors.
- **Gameplay:** affected combat/region/save scenario passes.
- **Performance:** compare frame-time and memory against the same device/profile baseline.
- **Review:** inspect the diff and verify the generated `dist/index.html` is reproducible.
- **Release safety:** work on a feature branch; use a pull request; do not push experimental work directly to `main`.

## 5. Initial performance protocol

Targets are provisional until real-device measurements establish a baseline:

- Desktop target: aim for stable 60 FPS on the agreed reference machine.
- Mobile target: provide a stable 30 FPS low preset and aim for 60 FPS on capable devices.
- Record average FPS plus p50/p95 frame time; average FPS alone is insufficient.
- Test at least one low/mid-range Android device, one current iPhone/iPad class device, and one desktop browser/GPU.
- Record region, resolution, quality preset, browser/version, device, duration, and whether the run used hardware acceleration.
- Run a 10-minute soak test and repeated region transitions to detect memory/resource leaks.

No performance claim should be published until measurements are recorded.

## 6. Current QA commands and known limitations

Current documented commands from the repository README:

```bash
python3 build.py
python3 tests/d4.py
python3 tests/g1.py nineveh,anbar,maysan,baghdad
python3 tests/g7.py
```

The browser scripts currently assume Playwright, Google Chrome at `/usr/bin/google-chrome`, and SwiftShader flags. Some scripts also use Pillow for screenshot sheets. They are scenario harnesses, not yet assertion-based CI tests. Phase 00 adds a portable build/syntax smoke test first; browser CI will follow after the browser launcher and test assertions are made portable.

## 7. Change management

- Keep `main` deployable.
- Use short-lived branches named `production/...`, `feature/...`, or `fix/...`.
- Every change gets a focused pull request with purpose, risks, test evidence, and screenshots when visual.
- Never combine a renderer rewrite, combat rebalance, and content redesign in one unreviewable change.
- Update this plan when a phase's scope or acceptance criteria change.
