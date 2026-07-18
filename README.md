# Orbit Break

A browser-based Breakout-with-gravity game built with **Blazor WebAssembly** and **.NET 10**. Aim and launch a ball into an arena of gravity wells and procedurally generated block "constellations" — chain a single flight through multiple wells to rack up combo score, then climb the global leaderboard.

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Frontend | Blazor WASM (.NET 10) |
| Rendering | HTML5 Canvas via `Blazor.Extensions.Canvas` |
| Game Loop | `requestAnimationFrame` → JS interop (`gameInterop.js`) → C# `Tick` |
| Physics | Gravity-well simulation, headless in `Engine.cs` (no rendering/interop — directly testable) |
| Auth / Scores / Leaderboard | Portal auth server — **no game-specific backend or database** |
| Tests | xUnit (`OrbitBreak.Tests`) — headless engine tests |
| Deployment | Docker (nginx-served WASM image) |

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Docker (optional, for the production-style image)

### Run the client (game only, no portal auth)

```bash
dotnet run --project OrbitBreak.Client
```

Open the printed localhost URL — do not open `index.html` directly, the game requires an HTTP context.

### Run the full stack (with auth + leaderboard)

```bash
cd ~/Desktop/game && docker compose up --build
```

Visit `http://localhost:3000/orbit-break/` (dev-only direct port: `http://localhost:8081`).

### Run tests

```bash
dotnet test OrbitBreak.Tests -c Release
```

### Build the production image

```bash
docker build -f OrbitBreak.Client/Dockerfile .
```

## Project Structure

```
orbit-break/
├── OrbitBreak.Client/
│   ├── Game/
│   │   ├── Engine.cs          # Whole simulation: physics, wells, blocks, combo, run-end — headless
│   │   └── Constellation.cs   # Procedural generator: wells + block grid, seeded, tier-scaled
│   ├── Pages/
│   │   ├── Game.razor         # Loop glue: canvas rendering, aim/paddle input, score submission
│   │   └── LeaderboardModels.cs
│   ├── Components/            # MainMenuOverlay, LeaderboardOverlay, GameOverOverlay (presentational)
│   └── wwwroot/js/gameInterop.js  # rAF loop, pointer drag, paddle key state
├── OrbitBreak.Tests/          # Headless xUnit engine tests
├── nginx.conf                 # Dev-standalone nginx (proxies scores/leaderboard to portal-auth)
└── Notes/                     # Obsidian vault — Design/ and Tech/ notes
```

## Gameplay

- **Aim + launch** — drag in the direction you want to fire, release to launch (must aim upward; speed clamped 380–1000 px/s)
- **Steer the paddle** — A/D or ←/→ while a ball is in flight
- Ball curves around **gravity wells** (inverse-square pull) and bounces off the paddle (Arkanoid-style angle from hit offset) instead of ending the flight
- **Combo multiplier** stacks per distinct well entered during a flight (`1 + 0.5 × combo`), resets when the flight ends
- Full clear advances the **tier** immediately, mid-flight; every 5 launches the constellation also reshuffles one tier harder
- Run ends when a ball passes the paddle with no balls left (`StartingBalls` = 3), or a hazard block reaches the paddle line
- Score submitted to the portal leaderboard on run end (Normal mode only)

### Game modes

| Mode | Description |
|------|-------------|
| **Normal** | 3 balls, hazards end the run — feeds the portal leaderboard and local high score |
| **Zen** | Nothing ends the run — lost balls cost no life, hazards park instead of breaching |
| **Time Attack** | 60-second clock, lost balls are free, HUD shows blocks-cleared + countdown |

### Blocks

| Type | Hits / Points | Behavior |
|------|--------------|----------|
| Standard | 1 / 10 | — |
| Armored | 3 / 30 | — |
| Explosive | — / 20 | Chains within 80 px |
| Hazard | — / 50 | Descends 30 px per launch; reaching the paddle line ends the run |

From tier 4, non-hazard blocks may roll a chance to become **moving** (drift horizontally, bounce off walls).

### Power-ups & ball variants

Blocks have a seeded 15% chance to drop a pickup (falls straight down, caught with the paddle):

- **W** wide paddle (×1.5 width, 10 s) · **S** slow ball (speed cap halved, 10 s) · **+1** extra ball · **C** sticky (absorbs one ball as a caught return)
- Rarer variants: **H** heavy (3 dmg/hit, plows through, still gravity-bound), **×2** split (forks every flight ball ±20°, cap 6), **P** phase (ignores wells, earns no combo)

## Architecture Notes

- **No game-specific backend.** Portal auth server owns auth, scores, and the leaderboard — see the [portal repo](https://github.com/alon-shviki/game-portal). This client has no `/register` or `/login`.
- JWT arrives via `#portal_token=` URL hash on launch from the portal, stored in `localStorage["jwt"]`.
- `nginx.conf` proxies `/api/scores` and `/api/leaderboard` to `portal-auth:5001/api/{scores,leaderboard}/orbit-break`; score payload maps `Value` = score, `Kills` = blocks broken, `Level` = tier reached.
- No persistent save state — a single-run score chase. Only the local high score lives in `localStorage["ob_highscore"]`.
- Deterministic seeding (`Engine.Reset(seed)`) — a future daily-seed mode needs no new backend infra.
- **No sound effects** — a deliberate product decision, not a gap.

## CI / Deployment

`.github/workflows/ci.yml` is a thin caller of the portal's shared reusable workflow (`alon-shviki/game-portal/.github/workflows/dotnet-ci.yml@main`): cache → format → build → test, and on push to `main`, pushes `ghcr.io/alon-shviki/orbit-break-client:latest` (+ sha tag). Required check: `ci / build`.

## Claude Code Setup

Developed with [Claude Code](https://docs.claude.com/en/docs/claude-code), reusing the portal's shared pipeline for issue and task work.

**`CLAUDE.md`** carries the stack commands, the portal integration contract, the hard rules, and the current setup status for this repo.

**`.claude/skills/`**:

- `ci-cd` — the CI/CD mental model shared across the three repos (.NET/Blazor WASM build → Docker image → GHCR)
- `obsidian-vault` — finds, creates, and organizes notes in this repo's vault using wikilinks

**`.claude/settings.json` hooks** mirror Bullet Heaven's guard-rails: `PreToolUse` blocks hand-edits to `bin/`/`obj/`; `PostToolUse` flags any `.cs`/`.razor` edit; a `Stop` hook compiles `OrbitBreak.Tests` (the client builds transitively) as a build gate, scoped to whichever worktree was actually edited.

## Obsidian

`Notes/` is this repo's own vault. `Home.md` is the quick-start dashboard, `Tasks.md` tracks progress from concept to playable, `Design/` holds the game-design notes (`Core Loop.md`, `Breakout Research.md`), and `Tech/` holds the engineering notes (`Architecture.md`, `Engine.md`, `Tuning.md`).

It's symlinked into the portal vault at `~/Desktop/game/Games/orbit-break`, so the same notes are browsable and editable from either vault — nothing is duplicated between them.

## Contributing

1. Work happens in a worktree via the portal's agentic scripts — never commit directly to `main`:
   ```bash
   bash ~/Desktop/game/.claude/scripts/start-issue <number>   # auto-detects this repo
   bash ~/Desktop/game/.claude/scripts/finish-issue           # tests → push → PR → wait for CI → merge
   ```
2. `dotnet test OrbitBreak.Tests -c Release` — all tests must pass.
3. Open a pull request with a clear description of the change.

See `CLAUDE.md` and `Notes/Tech/Architecture.md` for the full contract and hard rules.
