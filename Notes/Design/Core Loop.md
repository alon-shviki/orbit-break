# Core Loop

> Ported from the portal's `Design/Orbit Break.md` (the Game 2 brainstorm pick). This is the canonical, evolving copy going forward — update here, not in the portal vault.

## Pitch

A single ball launches from a player-controlled paddle at the bottom of the arena — classic Breakout's core defense — but the arena above also contains **gravity wells**: orbs that pull the ball into curved, slingshot trajectories. Blocks are arranged in procedurally generated "constellations" around and between the wells. Chaining a single launch through multiple wells to clear blocks in one pass is the core skill — each well-assisted hit stacks a combo multiplier, while the paddle keeps the ball (and the run) alive between chains.

No lives in the traditional sense: the run ends when the ball gets past the paddle with no balls remaining, or a "collapse" hazard reaches the paddle line. Difficulty escalates by generating denser, faster-moving constellations forever — so score is uncapped and purely a high-score chase, which maps directly onto the portal's leaderboard (best score, no other state needed).

## Loop

1. Aim + launch ball (drag in the direction you want to fire, release); steer the paddle left/right (A/D or ←/→) while the ball is in flight.
2. Ball travels, curving around any gravity well within its influence radius, breaking blocks on contact, and bouncing off the paddle (angle set by hit offset) instead of ending the flight.
3. Combo multiplier increases per well-assisted deflection in a single flight; resets when the flight ends (ball missed, or recalled after a long stable orbit).
4. Every N launches, the constellation regenerates one tier harder (more wells, tighter block clusters, occasional moving/hazard blocks).
5. Run ends on: ball gets past the paddle with no balls remaining, or a hazard block reaches the paddle line.
6. Score submitted to portal leaderboard on run end.

## Systems

- **Physics**: gravity-well influence (inverse-square pull within radius), ball-block collision, wall bounces outside well influence.
- **Procedural constellation generator**: tiered difficulty curve, seeded per run (enables a future "daily seed" mode with no new portal infra — pure client-side determinism).
- **Combo/scoring system**: multiplier stacking, well-chain bonuses, block-type value differences.
- **Block variety**: standard (1 hit), armored (multi-hit), explosive (area clear + chain reaction), hazard (advances toward launcher if untouched).
- **Ball variants** (unlocked via in-run pickups, not persisted): heavy ball, split ball, phase ball.
- **Juice**: particle bursts on block break, screen shake on big combos, trail rendering on curved flight.

## Scope

Matches Bullet Heaven's scale: physics/orbit engine comparable to BH's quadtree + projectile system; block/ball variety comparable to BH's enemy roster + upgrade catalogue; juice/combo UI is direct pattern reuse from BH.

## Open Questions

- ~~Exact gravity-well falloff curve and tuning~~ — answered by simulation (issue #2): inverse-square kept, `Strength` lowered so escape speed from a core (~700 px/s) sits below typical ball speed — wells must deflect, never permanently capture. Method and numbers in [[Tech/Tuning]].
- Constellation generation algorithm (grid-based vs. Poisson-disc scatter)
- Whether ball variants are picked between launches or found mid-flight

See also [[Breakout Research]] for what this game borrows from the classics vs. keeps unique.
