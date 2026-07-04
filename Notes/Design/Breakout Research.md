# Breakout Research

What the classics (Breakout, Arkanoid, Block Breaker) teach, and how it maps onto this game's unique gravity-well twist. Gathered for the issue #2 tuning pass; also feeds power-ups (#12) and ball variants (#3). Sources: [Breaking Down Breakout — Game Developer](https://www.gamedeveloper.com/design/breaking-down-breakout-system-and-level-design-for-breakout-style-games), [GameDev.net Arkanoid physics threads](https://gamedev.net/forums/topic/372965-arkanoid-physics/).

## Lessons taken

- **Ball speed should be player-controlled, not random.** Speed = difficulty. Our launch power (drag length, 380–1000 px/s) already does this — and because well pull is conservative (energy gained falling in is lost climbing out), the ball returns to the paddle at roughly the speed it was launched. Launch soft = easy mode. This is a real, discovered property of our physics worth protecting.
- **Paddle catch difficulty is the difficulty knob for casual players.** Wider paddle / faster paddle, not weaker enemies. Tuned via simulation (see [[Tech/Tuning]]).
- **Power-ups fall from blocks and must be caught by the paddle** — the tension between chasing the ball and chasing the pickup is the mechanic. Never auto-collect. (For #12.)
- **Positive and negative pickups both exist** in Arkanoid-likes (wide/narrow, slow/fast). Start positive-only; negatives are a later spice. (For #12.)
- **Stair-step difficulty**: introduce one new object per tier plateau, don't scale everything at once. Our tier system already regenerates harder — keep new block/well types gated to tier thresholds. (For #4.)
- **"A boring success is less fun than a glorious failure"** — the run should end in a flurry, not a stall. This drove the biggest #2 finding: runs were ending by *timeout stalemate* (ball trapped orbiting an inescapable well), not by dramatic misses. Fixed by making wells escapable.
- **Timely ball return; no idle waiting.** Two changes: full clear now advances the tier *mid-flight* (no bouncing in an empty arena), and the 25s orbit-trap recall timer resets on every paddle touch so it only fires for genuinely trapped balls.

## Fun audit — July 2026

Player verdict: "game isn't really fun." Second research pass against Block Blast / Block Blaster–style games ([TechRadar on Block Breaker's pull](https://www.techradar.com/computing/dont-play-this-new-block-breaker-game-in-google-search-im-already-hopelessly-addicted-to-the-nostalgia), [Fieldston News on Block Blast addictiveness](https://fieldstonnews.com/home/2025/02/why-is-block-blast-so-addictive/)) plus a re-read of the Game Developer breakdown. What the addictive ones share that we lack:

- **Sound.** Every source calls out satisfying sound as the #1 reinforcement channel. **Decision: no sound effects in this game** (owner's call, July 2026 — original issue deleted). Compensate through the visual channels below.
- **Constant positive reinforcement.** Block Blast pops "Good! / Amazing!" and rainbow sparkles on every clear. We give particles only; score changes silently in a corner. Floating score popups + combo callouts → issue filed.
- **Paddle feel.** The Game Developer piece: friction/momentum transfer from paddle to ball makes the paddle feel like a bat, not a wall — and gives the player something to do while waiting. We only steer by hit offset → issue filed.
- **Pickups must read as rewards.** Our power-ups were flat squares with a letter — they read as UI debris, not loot. Redrawn as pulsing glowing orbs with shape icons (bar-with-arrows = wide, hourglass = slow, two balls = extra, fork = split, kettlebell = heavy, hollow ring = phase, ball-on-paddle = sticky).
- **Ball trail removed** by player preference. (The Game Developer article suggests trails aid visibility at speed; if fast balls become hard to track, revisit with a subtler/shorter trail — not the 24-segment smear we had.)

## What stays unique (not Breakout)

- Gravity wells that curve flight and reward chaining — combo = distinct wells entered per flight.
- Aimed launches (drag direction + power) instead of a fixed serve.
- Procedural constellations + endless tiers instead of authored levels; score chase, not completion.
- The orbit-trap recall (a mechanic Breakout never needed — only exists because wells can capture).

Related: [[Core Loop]] · [[Tech/Engine]] · [[Tech/Tuning]]
