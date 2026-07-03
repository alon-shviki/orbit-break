# Tasks

Concept → playable. Roughly in order.

- [x] Create GitHub repo, push scaffold (created **private** — still needs `gh repo edit alon-shviki/orbit-break --visibility public --accept-visibility-change-consequences`, then branch protection per adding-a-game.md §7)
- [x] Add `orbit-break` to `REPOS`/`ROOTS` in portal's `start-issue`/`start-task` scripts (slug `ob`, portal PR #23)
- [x] Issue labels, issue templates, CI workflow (`.github/workflows/docker.yml`); branch protection blocked until repo is public
- [x] Blazor WASM project scaffold + Canvas render loop bring-up (reused BH's JS interop bridge pattern)
- [x] Gravity-well physics (inverse-square pull + core bumper — feel-tuning still open, see [[Tech/Engine]])
- [x] Procedural constellation generator (grid-based, seeded, tier scaling)
- [x] Block variety: standard, armored, explosive, hazard
- [x] Combo/scoring system + multiplier stacking (well-entry approximation — refine to deflection detection)
- [ ] Ball variants: heavy, split, phase (issue #3; design open question: picked between launches vs. mid-flight)
- [x] Juice pass: particles, screen shake, trails (sound + bigger effects later)
- [ ] Playtest & tune: gravity falloff, launch power, tier curve, catch width (issue #2)
- [x] Wire into `docker-compose.yml` (`orbit-break-client` :8081, portal PR #23) + nginx proxy for scores/leaderboard (`nginx.conf`)
- [x] `Games/Orbit Break.md` hub page status update (portal PR #23)
