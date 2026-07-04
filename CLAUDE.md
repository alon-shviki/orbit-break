# Orbit Break

Blazor WASM (.NET) physics arena/breakout game. Canvas via `Blazor.Extensions.Canvas`; loop = `requestAnimationFrame` → JS interop → C#. No game engine. **Client-only in production** — auth, scores, and leaderboard are owned by the portal auth server. See [[Notes/Design/Core Loop]] for the concept.

Status: playable core scaffolded — `OrbitBreak.Client` (game) + `OrbitBreak.Tests` (engine tests). See `Notes/Tech/Architecture.md`.

## Commands

```bash
dotnet run --project OrbitBreak.Client            # dev server (client-only, no portal)
dotnet build OrbitBreak.Client -c Release
dotnet test OrbitBreak.Tests -c Release           # headless engine tests
docker build -f OrbitBreak.Client/Dockerfile .    # nginx-served production image
```

## Portal integration
- Auth: user logs in at the portal (`localhost:3000`); JWT passed via URL hash (`#portal_token=...`) on game launch and stored in `localStorage["jwt"]`
- Scores: game's `nginx.conf` must proxy `POST /api/scores` → `portal-auth:5001/api/scores/orbit-break`
- Leaderboard: game's `nginx.conf` must proxy `GET /api/leaderboard` → `portal-auth:5001/api/leaderboard/orbit-break`
- CI: `.github/workflows/docker.yml` builds + tests, pushes `ghcr.io/alon-shviki/orbit-break-client:latest` on push to `main`

## Hard rules (always)
- Do **not** add auth endpoints to this game — login is portal-only.
- Do **not** add a scores/leaderboard DB to this game — portal owns that data.
- No game-specific backend/API server — client-only, same as Bullet Heaven.

## Read Before Working

| Task touches | Read first |
|---|---|
| Concept, core loop, systems | `Notes/Design/Core Loop.md` |
| Planned stack, portal integration | `Notes/Tech/Architecture.md` |

## Documentation Rule

**After completing any task or finding any problem: write or update a `.md` in `Notes/`.**

- Feature done → update or create in `Notes/Tech/` or `Notes/Design/`
- Bug or concern outside current task → open a GitHub issue AND note it in the relevant doc
- Use `[[Wiki Links]]` to connect related notes
- `Notes/` is visible to Obsidian — never put docs in hidden folders

## Workflow

Scripts live in this repo at `.claude/scripts/` (copied from the portal; slug `ob`, auto-detected from the git remote when run inside this repo).

```bash
# Issue work (run from this repo's root or any directory inside it)
bash .claude/scripts/start-issue <number>   # auto-detects orbit-break, no slug needed
# Then from inside the worktree:
bash .claude/scripts/finish-issue           # tests → push → PR → wait for CI → merge

# Non-issue work (docs, config)
bash .claude/scripts/start-task <name>      # auto-detects orbit-break
# Then from inside the worktree:
bash .claude/scripts/auto-pr "description"
```

Cross-repo triage (all games at once) is at the portal hub (`~/Desktop/game`). Full workflow docs: portal's `Tech/Agentic Pipeline.md` and `Tech/Scripts.md`.

New tasks go as GitHub issues, not doc edits:
```bash
gh issue create --repo alon-shviki/orbit-break --title "..." --body "..." --label "enhancement,priority:medium"
```

Spot a bug outside the current task → open a GitHub issue immediately, continue with the task. Use `bug` · `question` · `enhancement` labels. Always set a priority.

Never commit directly to `main`.

## Setup status

Fully set up (see portal's `.claude/rules/adding-a-game.md`): public GitHub repo, portal scripts + `docker-compose.yml` wired, issue labels/templates, and a `main` ruleset (PR required, `build` status check, no force-push/deletion — matches portal & Bullet Heaven).

## Product decisions
- **No sound effects** — owner's call (July 2026). Don't re-propose audio.
