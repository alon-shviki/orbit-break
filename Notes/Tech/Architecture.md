# Architecture

Matches Bullet Heaven's proven pattern. The playable core is scaffolded — see [[Tech/Engine]] for the game code layout.

## Stack

| Layer | Technology |
|-------|-----------|
| Game client | Blazor WASM (.NET 10), `OrbitBreak.Client/` |
| Rendering | HTML5 Canvas via `Blazor.Extensions.Canvas` |
| Game loop | `requestAnimationFrame` → JS interop (`wwwroot/js/gameInterop.js`) → C# `Tick` |
| Tests | `OrbitBreak.Tests/` — headless xunit tests against the engine |
| Auth / Scores / Leaderboard | Portal auth server — no game-specific backend or DB |

## Portal Integration Contract

Same contract every game follows — see the portal's `Tech/Architecture.md` and `.claude/rules/architecture.md`:

- No `/register`/`/login` of its own — JWT arrives via `#portal_token=` URL hash (picked up in `index.html`, stored in `localStorage["jwt"]`).
- `nginx.conf` proxies:
  - `POST /api/scores` → `portal-auth:5001/api/scores/orbit-break`
  - `GET /api/scores/me` → `portal-auth:5001/api/leaderboard/orbit-break/me`
  - `GET /api/leaderboard` → `portal-auth:5001/api/leaderboard/orbit-break`
- Score payload maps to the portal's generic `{ Value, Kills, Level }`: Value = score, Kills = blocks broken, Level = tier reached.
- No persistent save state: the entire loop is a single-run score chase (see [[Design/Core Loop]]). Only the local high score lives in `localStorage["ob_highscore"]`.

## Build & Run

See `CLAUDE.md` → Commands. `OrbitBreak.Client/Dockerfile` produces the nginx-served image. CI is `.github/workflows/ci.yml`, a thin caller of the portal's shared reusable workflow (`alon-shviki/game-portal/.github/workflows/dotnet-ci.yml@main`) — it runs cache→format→build→test and, on push to `main`, pushes `ghcr.io/alon-shviki/orbit-break-client:latest`. Required check: `ci / build`.

## Not Yet Done

- GitHub repo creation, issue labels/templates, branch protection
- `docker-compose.yml` wiring in the portal repo
- Registration in the portal's `start-issue`/`start-task` scripts

See the portal's `.claude/rules/adding-a-game.md` for the full checklist.
