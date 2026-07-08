---
name: obsidian-vault
description: Search, create, and manage notes in the Orbit Break Obsidian vault with wikilinks. Use when the user wants to find, create, or organize notes in Obsidian.
---

# Obsidian Vault — Orbit Break

## Vault location

`/home/alon/Desktop/orbit-break/Notes/`

This is a standalone game vault. The portal vault reaches it via a symlink
(`~/Desktop/game/Games/orbit-break`) — edit from either side, one source of truth.

Structure:

```
Notes/
  Home.md      ← quick start, run commands, link map
  Tasks.md     ← task list
  Design/      ← game design notes
  Tech/        ← architecture, stack, performance
```

## Naming conventions

- **Title Case** for note filenames.
- Notes are organized into `Design/` and `Tech/` folders — not flat.

## Linking

- Use Obsidian `[[wikilinks]]`, local to this vault: `[[Design/Core Loop]]`.
- Add related-note links at the bottom of each note.
- Don't link out to portal notes; portal-level content lives in the portal vault.

## Workflows

### Search
```bash
find "/home/alon/Desktop/orbit-break/Notes/" -name "*.md" | grep -i "keyword"   # by filename
grep -rl "keyword" "/home/alon/Desktop/orbit-break/Notes/" --include="*.md"      # by content
```
Or use the Grep/Glob tools on the vault path.

### Create a note
1. Title Case filename in the right folder (`Design/` or `Tech/`).
2. Write the content.
3. Add `[[wikilinks]]` to related notes at the bottom.

### Find backlinks
```bash
grep -rl "\[\[Note Title\]\]" "/home/alon/Desktop/orbit-break/Notes/" --include="*.md"
```

## Rules

- `.obsidian/` is gitignored — never commit it.
- Keep game-specific notes here; never duplicate them into the portal vault.
