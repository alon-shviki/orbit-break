# Orbit Break

Blazor WASM (.NET) physics arena/breakout game. Canvas via `Blazor.Extensions.Canvas`; loop = `requestAnimationFrame` → JS interop → C#. No game engine. **Client-only in production** — auth, scores, and leaderboard are owned by the portal auth server. See [[Notes/Design/Core Loop]] for the concept.

Status: concept only — no project scaffold yet. This file will gain real `## Commands` once `dotnet new blazorwasm` is run.

## Portal integration
- Auth: user logs in at the portal (`localhost:3000`); JWT passed via URL hash (`#portal_token=...`) on game launch and stored in `localStorage["jwt"]`
- Scores: game's `nginx.conf` must proxy `POST /api/scores` → `portal-auth:5001/api/scores/orbit-break`
- Leaderboard: game's `nginx.conf` must proxy `GET /api/leaderboard` → `portal-auth:5001/api/leaderboard/orbit-break`
- CI: none yet — add `.github/workflows/docker.yml` pushing `ghcr.io/alon-shviki/orbit-break-client:latest` on push to `main` once there's a client to build

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

Scripts live in the portal repo at `~/Desktop/game/.claude/scripts/` — no machine setup needed. From inside this repo the slug is auto-detected.

```bash
# Issue work (run from this repo's root or any directory inside it)
bash ~/Desktop/game/.claude/scripts/start-issue <number>   # auto-detects orbit-break, no slug needed
# Then from inside the worktree:
bash ~/Desktop/game/.claude/scripts/finish-issue

# Non-issue work (docs, config)
bash ~/Desktop/game/.claude/scripts/start-task <name>      # auto-detects orbit-break
# Then from inside the worktree:
bash ~/Desktop/game/.claude/scripts/auto-pr "description"
```

Cross-repo triage (all games at once) is at the portal hub (`~/Desktop/game`). Full workflow docs: portal's `Tech/Agentic Pipeline.md` and `Tech/Scripts.md`.

New tasks go as GitHub issues, not doc edits:
```bash
gh issue create --repo alon-shviki/orbit-break --title "..." --body "..." --label "enhancement,priority:medium"
```

Spot a bug outside the current task → open a GitHub issue immediately, continue with the task. Use `bug` · `question` · `enhancement` labels. Always set a priority.

Never commit directly to `main`.

## Not Done Yet

This repo is a docs-only scaffold. Before real work starts, someone still needs to (see portal's `.claude/rules/adding-a-game.md`):
- Create the GitHub repo, push this scaffold, make it public
- Add `orbit-break` to `REPOS`/`ROOTS` in the portal's `start-issue`/`start-task` scripts
- Set up issue labels, issue templates, CI workflow, branch protection
- Scaffold the actual Blazor WASM project (`dotnet new blazorwasm`) and wire it into `docker-compose.yml`
