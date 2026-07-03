# Tuning

How the issue #2 "playtest & tune" pass was actually done, and why each knob landed where it did. Method + numbers here so the next pass doesn't start from scratch.

## Method: headless playtest bots

`Engine` is headless, so instead of manual playtesting the tuning ran a simulated player over 40 seeded runs per skill level (bot: launches at a random upward angle, paddle tracks the ball's x; skill = reaction lag of 0s / 0.12s / 0.25s applied to the tracked position). Metrics: run length, tier reached, flights lost vs recalled, blocks per flight, combo, ball-speed distribution. The sim harness is throwaway (scratchpad), but it's ~120 lines around the public `Engine` API — trivial to rebuild for the next pass. Note: a lagged tracker *underestimates* humans (humans predict the landing spot), so treat the 0.12s bot as a floor for a casual player.

## Findings → changes

| Finding (baseline) | Change |
|---|---|
| ~100% of flights ended by the 25s force-recall, not by play; with the #11 speed cap, escape velocity from a well core (~1400 px/s at `Strength` 2.6e7) exceeded the cap — a ball that fell into a well could *never* leave. Wells devoured the ball; perfect play was an infinite stalemate. | `Strength`: `2.6e7 + 2e6·tier` → `6e6 + 5e5·tier`. Escape speed from a core is now ~700 px/s, below typical ball speed — wells deflect and slingshot instead of capturing. |
| Recall timer fired during *active paddle play* (it predates the paddle; it was built for the old auto-catch design). | `FlightTime = 0` on every paddle bounce — recall now only catches genuinely trapped orbits. |
| Full clear did nothing until the flight ended — ball bounced around an empty arena for up to 25s. | Full clear advances the tier and regenerates **mid-flight** (Block Breaker convention). |
| A bot with 0.25s reaction lag died in ~11s; 0.12s lag in ~49s — descent speeds up to the cap vs a 640 px/s paddle were hopeless for anyone imperfect. | `PaddleSpeed` 640 → 760, `PaddleHalfWidth` 55 → 70. |

`MaxBallSpeed` stays 1200 and launch power stays player-chosen (380–1000): because well pull is conservative, the ball returns at ≈ launch speed, so launch power *is* the difficulty dial the player controls (see [[Design/Breakout Research]]).

## Result (median over 40 runs)

| Bot | Baseline score | Tuned score | Tuned run length |
|---|---|---|---|
| perfect tracker | stalemate (never ends) | 5,990 | ~4 min, ends by lost ball |
| 0.12s lag | 725 | 1,075 | ~1 min |
| 0.25s lag | 230 | 245 | ~14s |

Every run now ends by losing the ball — the correct Breakout death — and tier/combo scale with skill (perfect bot reached tier 17 on its best seed, combo p90 = 11).

## Still open

- Hazard-descent pressure is weak: hazards step only on flight end, and flights are now long. Zero hazard deaths in any sim. Revisit when moving blocks (#4) land.
- 0.25s-lag survival is short; power-ups (#12, wider paddle / slow ball) are the intended casual relief valve rather than further base tuning.

Related: [[Engine]] · [[Design/Breakout Research]] · [[Design/Core Loop]]
