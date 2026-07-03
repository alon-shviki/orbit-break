# Engine

Layout of the game code in `OrbitBreak.Client/` and where the tuning knobs live.

## Files

| File | Owns |
|------|------|
| `Game/Engine.cs` | Whole simulation: ball physics, gravity wells, block collision, combo/scoring, hazard advance, run-end conditions. Headless — no rendering or interop, so it's directly testable. |
| `Game/Constellation.cs` | Procedural generator: wells (rejection-sampled, min separation) + block grid, seeded `Random`, difficulty scales with tier. |
| `Pages/Game.razor` | UI states (menu / playing / game over / leaderboard), canvas rendering, aim input, paddle input, portal score submission. |
| `wwwroot/js/gameInterop.js` | rAF loop → C# `Tick`, viewport size, pointer drag state, paddle key state (A/D, ←/→). |
| `../OrbitBreak.Tests/EngineTests.cs` | Generator determinism, gravity pull, block break, paddle bounce, ball-miss → game over, hazard breach. |

## Mechanics as implemented

- **Launch**: direct aim — drag in the direction you want to fire, release to launch (fixed from an initial slingshot-pull-back build that confused players, see issue #6). Must aim upward; speed clamped 380–1000 px/s.
- **Paddle**: horizontal rect, `PaddleHalfWidth` (55) either side of `PaddleX`, moved by A/D or ←/→ at `PaddleSpeed` (640 px/s). While no ball is in flight, the ball rests on the paddle and slides with it, ready to launch. Added in issue #8 to replace an invisible auto-catch zone that gave the player nothing to actually do.
- **Wells**: inverse-square pull inside `Influence` radius (accel floor stops it exploding near center); solid `Core` acts as an elastic bumper.
- **Combo**: +1 per *distinct well entered* during a flight; multiplier `1 + 0.5 × combo`; resets when the flight ends. This approximates the design doc's "per well-assisted deflection" — refine after feel-testing (marked with a `ponytail:` comment in code).
- **Paddle bounce**: hitting the paddle reflects the ball back up into play (Arkanoid-style — hit offset from paddle center steers the bounce angle) and the flight continues; missing it entirely (ball passes below the paddle line) ends the flight and costs a ball (`StartingBalls` = 3). Every paddle bounce resets the 25 s orbit-trap recall timer, so recall (no ball lost) only fires for a ball genuinely stuck orbiting a well, never during active play (issue #2).
- **Blocks**: standard (1 hit / 10 pts), armored (3 hits / 30), explosive (20, chains within 80 px), hazard (50, descends 30 px per launch; reaching the paddle line ends the run).
- **Tier shift**: full clear advances the tier and regenerates *immediately mid-flight* (Block Breaker style — no waiting out the flight in an empty arena); every 5 launches the constellation also reshuffles one tier harder as a pity-shift for stuck players (issue #2).
- **Seeding**: `Engine.Reset(seed)` is fully deterministic — a future daily-seed mode needs no new infra.

## Tuning knobs

All constants sit at the top of `Engine.cs` (ball radius, paddle width/speed, launches per tier, hazard step, explosion radius, flight timeout) and in `Constellation.Generate` (well strength/influence, block density, kind chances). Current values are simulation-tuned — see [[Tuning]] for the method, the numbers, and why (issue #2). Generator algorithm remains an open question in [[Design/Core Loop]] — grid placement is deliberate laziness, switch to Poisson-disc if constellations feel too regular.

## Known issues

_None currently tracked here — check the GitHub issue list._

## Fixed

- **Frame pacing / stutter** (issue #10) — three contributors addressed: (1) runaway ball speed capped (see #11 below); (2) the two per-frame JS interop input calls merged into one `getInputState` round-trip; (3) the rAF loop now has a busy guard — if C# is still processing the previous tick it skips the frame instead of queueing overlapping `Tick` calls, and the skipped time is absorbed by dt. The dt clamp (1/30s) still means sustained sub-30fps plays in slow motion rather than skipping — acceptable; revisit only if reports persist.

- **Ball speed cap + paddle tunneling** (issue #11) — speed is now clamped to `MaxBallSpeed` (1200 px/s) every tick, and the paddle check is *swept*: it tests the path the ball travelled this frame against the paddle band and bounces at the crossing point, so no speed inside the cap can tunnel through. Block collision remains a discrete check (missing a block costs nothing, unlike missing the paddle).

## Skipped for now

- **Ball variants** (heavy / split / phase) — own task in [[Tasks]]; design open question (picked between launches vs. mid-flight) still unresolved.
- **Power-ups** (transient pickups: wider paddle, extra ball, slower ball, sticky paddle) — issue #12; distinct from ball variants above, which are a persistent per-run pick rather than a dropped-from-a-block temporary effect.
- **Moving blocks** — "occasional moving blocks" from the design doc; add once base feel is tuned.
- **Juice extras** — particles, trail, and screen shake are in; sound and bigger effects are a later pass.
