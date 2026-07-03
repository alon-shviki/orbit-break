# Engine

Layout of the game code in `OrbitBreak.Client/` and where the tuning knobs live.

## Files

| File | Owns |
|------|------|
| `Game/Engine.cs` | Whole simulation: ball physics, gravity wells, block collision, combo/scoring, hazard advance, run-end conditions. Headless — no rendering or interop, so it's directly testable. |
| `Game/Constellation.cs` | Procedural generator: wells (rejection-sampled, min separation) + block grid, seeded `Random`, difficulty scales with tier. |
| `Pages/Game.razor` | UI states (menu / playing / game over / leaderboard), canvas rendering, aim input, portal score submission. |
| `wwwroot/js/gameInterop.js` | rAF loop → C# `Tick`, viewport size, pointer drag state. |
| `../OrbitBreak.Tests/EngineTests.cs` | Generator determinism, gravity pull, block break, ball-loss → game over, hazard breach. |

## Mechanics as implemented

- **Launch**: direct aim — drag in the direction you want to fire, release to launch (fixed from an initial slingshot-pull-back build that confused players, see issue #6). Must aim upward; speed clamped 380–1000 px/s.
- **Wells**: inverse-square pull inside `Influence` radius (accel floor stops it exploding near center); solid `Core` acts as an elastic bumper.
- **Combo**: +1 per *distinct well entered* during a flight; multiplier `1 + 0.5 × combo`; resets on catch. This approximates the design doc's "per well-assisted deflection" — refine after feel-testing (marked with a `ponytail:` comment in code).
- **Catch**: bottom edge within `CatchHalf` (110 px) of the launcher = caught; outside = ball lost (`StartingBalls` = 3). Flights over 25 s are recalled as caught (anti-softlock for stable orbits).
- **Blocks**: standard (1 hit / 10 pts), armored (3 hits / 30), explosive (20, chains within 80 px), hazard (50, descends 30 px per launch; reaching the launcher line ends the run).
- **Tier shift**: constellation regenerates one tier harder every 5 launches or on full clear.
- **Seeding**: `Engine.Reset(seed)` is fully deterministic — a future daily-seed mode needs no new infra.

## Tuning knobs

All constants sit at the top of `Engine.cs` (ball radius, catch width, launches per tier, hazard step, explosion radius, flight timeout) and in `Constellation.Generate` (well strength/influence, block density, kind chances). The gravity falloff curve and generator algorithm are open questions in [[Design/Core Loop]] — grid placement is deliberate laziness, switch to Poisson-disc if constellations feel too regular.

## Skipped for now

- **Ball variants** (heavy / split / phase) — own task in [[Tasks]]; design open question (picked between launches vs. mid-flight) still unresolved.
- **Moving blocks** — "occasional moving blocks" from the design doc; add once base feel is tuned.
- **Juice extras** — particles, trail, and screen shake are in; sound and bigger effects are a later pass.
