# Architecture (Planned)

No code exists yet — this documents the intended shape, matching Bullet Heaven's proven pattern.

## Stack

| Layer | Technology |
|-------|-----------|
| Game client | Blazor WASM (.NET), same as Bullet Heaven |
| Rendering | HTML5 Canvas via `Blazor.Extensions.Canvas` |
| Game loop | `requestAnimationFrame` → JS interop → C# tick |
| Auth / Scores / Leaderboard | Portal auth server — no game-specific backend or DB |

## Portal Integration Contract

Same contract every game follows — see the portal's `Tech/Architecture.md` and `.claude/rules/architecture.md`:

- No `/register`/`/login` of its own — JWT via portal, validated with the shared signing key.
- `nginx.conf` proxies `POST /api/scores` → `portal-auth:5001/api/scores/orbit-break` and `GET /api/leaderboard` → `portal-auth:5001/api/leaderboard/orbit-break`.
- No persistent save state needed: the entire loop is a single-run score chase (see [[Design/Core Loop]]), so the portal's one-score-per-game + top-10-leaderboard model is a complete fit.
- Any per-run unlocks (ball variants, etc.) are transient within a run — no `localStorage` workaround required.

## Not Yet Done

- `dotnet new blazorwasm` project scaffold
- `docker-compose.yml` wiring in the portal repo
- CI workflow, GHCR image push
- GitHub repo creation, issue labels/templates, branch protection

See the portal's `.claude/rules/adding-a-game.md` for the full checklist.
