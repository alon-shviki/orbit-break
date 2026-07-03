# Orbit Break — Vault Home

Physics arena/breakout hybrid. Launch balls that curve around gravity wells to shatter procedurally generated block constellations. Infinite difficulty ramp, pure high-score chase.

## Quick Start

```bash
dotnet run --project OrbitBreak.Client   # client-only dev server (no portal — scores stay local)
dotnet test OrbitBreak.Tests             # headless engine tests
```

Full portal stack (auth + leaderboard) comes via the portal repo's `docker compose up` once this game is wired into its `docker-compose.yml`. Auth and scores live in the portal. See [[Tech/Architecture]].

## Vault Map

### Design
- [[Design/Core Loop]] — launch/orbit/combo loop, systems, block & ball variety, scope
- [[Design/Breakout Research]] — what the classics teach, what this game borrows vs. keeps unique

### Tech
- [[Tech/Architecture]] — stack, portal integration contract, build & run
- [[Tech/Engine]] — game code layout, mechanics as implemented, tuning knobs
- [[Tech/Tuning]] — headless-bot playtest method, tuned numbers and why

### Work
- [[Tasks]] — pending tasks to go from concept to playable
