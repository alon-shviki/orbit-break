# Tasks

Concept → playable. Roughly in order.

- [x] Create GitHub repo, push scaffold — **public**, branch protection on `main` (required `build` check)
- [x] Add `orbit-break` to `REPOS`/`ROOTS` in portal's `start-issue`/`start-task` scripts (slug `ob`, portal PR #23, merged) — plus local copies of the same scripts vendored into this repo's `.claude/scripts/` so issue work here doesn't depend on the portal repo
- [x] Issue labels, issue templates, CI workflow (`.github/workflows/docker.yml`), branch protection
- [x] Blazor WASM project scaffold + Canvas render loop bring-up (reused BH's JS interop bridge pattern)
- [x] Gravity-well physics (inverse-square pull + core bumper — feel-tuning still open, see [[Tech/Engine]])
- [x] Procedural constellation generator (grid-based, seeded, tier scaling)
- [x] Block variety: standard, armored, explosive, hazard
- [x] Combo/scoring system + multiplier stacking (well-entry approximation — refine to deflection detection)
- [x] Direct-aim launch controls (issue #6 — initial build was a reversed slingshot pull-back, confusing)
- [x] Controllable paddle (A/D or ←/→) that bounces the ball back instead of an invisible auto-catch zone (issue #8 — user feedback: the drag-only build didn't feel like an active game)
- [ ] Ball variants: heavy, split, phase (issue #3; design open question: picked between launches vs. mid-flight)
- [ ] Power-ups: transient pickups dropped from blocks — wider paddle, extra ball, slower ball, sticky paddle (issue #12; distinct from #3's persistent variants, see [[Tech/Engine]])
- [x] Juice pass: particles, screen shake, trails (sound + bigger effects later)
- [ ] Playtest & tune: gravity falloff, launch power, paddle speed/width, tier curve (issue #2)
- [x] Fix ball-speed cap + paddle tunneling at high speed (issues #10, #11 — speed cap + swept paddle collision + single-round-trip input interop + rAF busy guard)
- [x] Wire into `docker-compose.yml` (`orbit-break-client` :8081, portal PR #23) + nginx proxy for scores/leaderboard (`nginx.conf`)
- [x] `Games/Orbit Break.md` hub page status update (portal PR #23)
