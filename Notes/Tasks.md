# Tasks

Concept → playable. Roughly in order.

- [x] Create GitHub repo, push scaffold — **public**, branch protection on `main` (required `build` check)
- [x] Add `orbit-break` to `REPOS`/`ROOTS` in portal's `start-issue`/`start-task` scripts (slug `ob`, portal PR #23, merged). ~~Plus local copies vendored into this repo's `.claude/scripts/`~~ — **later removed** (portal PR #42): scripts are de-duplicated to the portal only; this repo references them by absolute path (`bash ~/Desktop/game/.claude/scripts/…`), like BH
- [x] Issue labels, issue templates, CI workflow (`.github/workflows/docker.yml`), branch protection
- [x] Blazor WASM project scaffold + Canvas render loop bring-up (reused BH's JS interop bridge pattern)
- [x] Gravity-well physics (inverse-square pull + core bumper — feel-tuning still open, see [[Tech/Engine]])
- [x] Procedural constellation generator (grid-based, seeded, tier scaling)
- [x] Block variety: standard, armored, explosive, hazard
- [x] Combo/scoring system + multiplier stacking (well-entry approximation — refine to deflection detection)
- [x] Direct-aim launch controls (issue #6 — initial build was a reversed slingshot pull-back, confusing)
- [x] Controllable paddle (A/D or ←/→) that bounces the ball back instead of an invisible auto-catch zone (issue #8 — user feedback: the drag-only build didn't feel like an active game)
- [x] Ball variants: heavy, split, phase (issue #3 — rare drops in the #12 pickup channel, flight-scoped modes + true multi-ball; see [[Tech/Engine]])
- [x] Power-ups: transient pickups dropped from blocks — wider paddle, extra ball, slower ball, sticky paddle (issue #12; 15% seeded drop, paddle-catch only, see [[Tech/Engine]])
- [x] Juice pass: particles, screen shake, trails (sound + bigger effects later)
- [x] Playtest & tune: gravity falloff, launch power, paddle speed/width, tier curve (issue #2 — headless bot simulation, see [[Tech/Tuning]]; wells made escapable, recall timer resets on paddle touch, mid-flight clear advance, paddle 760/±70)
- [x] Fix ball-speed cap + paddle tunneling at high speed (issues #10, #11 — speed cap + swept paddle collision + single-round-trip input interop + rAF busy guard)
- [x] Moving blocks at higher tiers (issue #4 — tier 4+, stair-stepped mover chance, wall-bouncing drift, hazards never drift)
- [x] Fix `finish-issue` script: CI-watch retry + merge without `--delete-branch` inside worktrees (issue #15) — **upstreamed to the portal** (PR #36): the fix now lives in the shared `lib.sh` (`wait_for_ci`) that both `auto-pr` and `finish-issue` source
- [x] Wire into `docker-compose.yml` (`orbit-break-client` :8081, portal PR #23) + nginx proxy for scores/leaderboard (`nginx.conf`)
- [x] `Games/Orbit Break.md` hub page status update (portal PR #23)
