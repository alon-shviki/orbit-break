---
name: ci-cd
description: CI/CD for the game-portal stack (.NET / Blazor WASM → Docker image → GHCR). Use when setting up or changing a repo's GitHub Actions pipeline, debugging a CI failure, or reasoning about how images ship.
---

# CI/CD — Game Portal Stack

Every repo here (portal, Bullet Heaven, Orbit Break) follows the **same two-stage pipeline**. This skill is the mental model; the source of truth for any repo is its own `.github/workflows/*.yml`.

## The pipeline

```
Pull request → main            Push (merge) → main
─────────────────────          ─────────────────────
build:                         build:  (same gate re-runs)
  cache NuGet packages           │
  dotnet format --verify         ▼
  dotnet build -c Release      push-image:  (only on push to main)
  dotnet test  -c Release        docker build → push ghcr.io/alon-shviki/<image>:latest
  ← blocks merge if red
```

- **PR gate** (`build`) runs on every PR and every push to `main`. It is the required status check in branch protection — a red `build` blocks merge.
- **`push-image`** runs *only* on push to `main` (`if: github.ref == 'refs/heads/main' && github.event_name == 'push'`) and `needs: build`. PRs never push an image.
- Every repo now runs the **full gate**: NuGet cache → format check → build → test. All three have a test project.

Per-repo images:

| Repo | Build project | Test project | Image |
|------|---------------|--------------|-------|
| `game-portal` | `portal-auth/PortalAuth.csproj` | `PortalAuth.Tests` | `ghcr.io/alon-shviki/portal-auth:latest` |
| `Bullet-Heaven` | `BulletHeaven.Client` | `BulletHeaven.Tests` | `ghcr.io/alon-shviki/bh-client:latest` |
| `orbit-break` | `OrbitBreak.Client` | `OrbitBreak.Tests` | `ghcr.io/alon-shviki/orbit-break-client:latest` |

`docker-compose.yml` in the portal repo pulls those `:latest` images to run the full stack.

## Runner: always `ubuntu-latest`

`runs-on: ubuntu-latest` is the **GitHub-hosted CI VM** — the only Linux flavor GitHub offers for hosted runners. There is no "Alpine runner"; a smaller kernel is not an option and would save nothing (public repos get free, unmetered Linux minutes). Image *size* is a separate concern handled in each `Dockerfile`'s base image (e.g. `nginx:alpine`, `*-alpine` .NET runtimes) — not in the workflow.

## The shape of a workflow

```yaml
name: CI
on:
  pull_request: { branches: [main] }
  push:         { branches: [main] }

permissions:
  contents: read
  packages: write        # needed to push to GHCR

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Cache NuGet packages
        uses: actions/cache@v4
        with:
          path: ~/.nuget/packages
          key: nuget-${{ runner.os }}-${{ hashFiles('**/*.csproj') }}
          restore-keys: nuget-${{ runner.os }}-

      - name: Format check
        run: dotnet format <project>.csproj --verify-no-changes

      - name: Build
        run: dotnet build <project>.csproj -c Release

      - name: Test
        run: dotnet test <tests>.csproj -c Release

  push-image:
    if: github.ref == 'refs/heads/main' && github.event_name == 'push'
    needs: build
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: docker/login-action@v3
        with:
          registry: ghcr.io
          username: ${{ github.actor }}
          password: ${{ secrets.GITHUB_TOKEN }}   # auto-provided, no setup
      - uses: docker/build-push-action@v5
        with:
          context: .
          file: <Client>/Dockerfile
          push: true
          tags: ghcr.io/alon-shviki/<image>:latest
```

**Format target is the `.csproj`, not the solution.** `dotnet format <name>.slnx` throws (`MSBuildWorkspaceFinder.FindFile`) on the .NET 10 XML solution format, so run it per-project. Repos with two projects list two `dotnet format` lines.

## Non-negotiables

- **No gate gets skipped.** Format/test/build red → fix the code, don't disable the check or delete the test. If `dotnet format --verify-no-changes` fails, run `dotnet format` (no flag) locally and commit the result.
- **Shift left.** A break caught by `dotnet build` in CI costs seconds; the same break in a pulled `:latest` image costs a debugging session.
- **Secrets never live in code or the workflow.** CI needs no custom secrets today — `GITHUB_TOKEN` is auto-provided for GHCR. Runtime secrets (`JWT_KEY`, `POSTGRES_PASSWORD`, DB connection) come from `.env` (gitignored) or GitHub Secrets, never hardcoded and never baked into an image. See the portal's architecture rules (`.claude/rules/architecture.md` in `game-portal`).
- **Branch protection stays on.** `main` requires the `build` check to pass (strict) before merge. Don't merge around a red build.

## When CI fails (agent loop)

`finish-issue` waits on CI and only merges when green, so a red pipeline surfaces immediately. To fix:

```
Copy the exact failure output → give it to the agent verbatim →
agent reproduces locally (dotnet format/build/test) → fixes root cause → pushes → CI re-runs
```

| Failure | First move |
|---------|-----------|
| Format check | Run `dotnet format <project>.csproj` locally, commit the whitespace fix |
| Build error | Read the file:line in the error; check a missing/renamed symbol or package ref |
| Test failure | Reproduce with `dotnet test`; fix the code, not the test |
| Image push fails | Check `permissions: packages: write` and the GHCR tag/casing |
| Flaky test | Fix the flake — don't blindly re-run; it masks real bugs |

## Rollback

There's no Vercel/staging here — "deploy" is a `:latest` image on GHCR that `docker-compose` pulls. To roll back, pin the previous good image by digest (or a dated tag) in `docker-compose.yml` and redeploy, rather than reverting source and rebuilding. If rollbacks become common, start tagging images with the git SHA alongside `:latest` so a specific version is always addressable.

## Optimization

NuGet caching (`actions/cache` on `~/.nuget/packages`, keyed by `hashFiles('**/*.csproj')`) is already in every workflow. If a pipeline still grows past ~10 min: split independent jobs to run in parallel, or shard the test suite. Larger/self-hosted runners are a last resort, not a first reach.

## Verification checklist

After touching a workflow:

- [ ] `build` runs on every PR and push to `main`, and is the required status check
- [ ] NuGet cache step present (`actions/cache` on `~/.nuget/packages`)
- [ ] `dotnet format --verify-no-changes` runs and passes for each `.csproj`
- [ ] `dotnet test` runs for the repo's test project
- [ ] `push-image` is gated to `push` on `main` and `needs: build`
- [ ] `permissions: packages: write` present in the image job
- [ ] No secret hardcoded in the workflow or committed files
- [ ] Image tag matches the repo's row in the table above
- [ ] Pipeline stays under ~10 min
