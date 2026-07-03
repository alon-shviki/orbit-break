# Tasks

Concept → playable. Roughly in order.

- [ ] Create GitHub repo, push scaffold, make public
- [ ] Add `orbit-break` to `REPOS`/`ROOTS` in portal's `start-issue`/`start-task` scripts
- [ ] Issue labels, issue templates, branch protection (CI workflow now exists at `.github/workflows/docker.yml`; see portal's `.claude/rules/adding-a-game.md`)
- [x] Blazor WASM project scaffold + Canvas render loop bring-up (reused BH's JS interop bridge pattern)
- [x] Gravity-well physics (inverse-square pull + core bumper — feel-tuning still open, see [[Tech/Engine]])
- [x] Procedural constellation generator (grid-based, seeded, tier scaling)
- [x] Block variety: standard, armored, explosive, hazard
- [x] Combo/scoring system + multiplier stacking (well-entry approximation — refine to deflection detection)
- [ ] Ball variants: heavy, split, phase (design open question: picked between launches vs. mid-flight)
- [x] Juice pass: particles, screen shake, trails (sound + bigger effects later)
- [ ] Playtest & tune: gravity falloff, launch power, tier curve, catch width
- [ ] Wire into `docker-compose.yml`, nginx proxy for scores/leaderboard
- [ ] `Games/Orbit Break.md` hub page status update once playable
