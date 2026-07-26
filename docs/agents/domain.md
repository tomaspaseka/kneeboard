# Domain Docs

How the engineering skills should consume this repo's domain documentation when exploring the
codebase.

## Before exploring, read these

- **`CONTEXT.md`** at the repo root — the domain glossary. Defines Document, Section, Content
  source, Page, Section source, Rasterizer and Page navigation zone.
- **`docs/adr/`** — read the ADRs that touch the area you're about to work in.
- **`arch/`** — `arch/overview.md` for the architecture, `arch/kneeboard-format.md` for the on-disk
  `.kneeboard` format. `CONTEXT.md` links into both.

If any of these files don't exist, **proceed silently**. Don't flag their absence; don't suggest
creating them upfront. The `/domain-modeling` skill (reached via `/grill-with-docs` and
`/improve-codebase-architecture`) creates them lazily when terms or decisions actually get resolved.

## File structure

This is a single-context repo:

```
/
├── CONTEXT.md              ← domain glossary
├── docs/adr/               ← architectural decision records
├── arch/                   ← architecture notes and the .kneeboard format spec
├── Kneeboard/              ← the app
└── Kneeboard.Tests/
```

There is no `CONTEXT-MAP.md`, and there shouldn't be one unless this repo grows into several
bounded contexts with their own glossaries.

## Use the glossary's vocabulary

When your output names a domain concept (in an issue title, a refactor proposal, a hypothesis, a
test name), use the term as defined in `CONTEXT.md`. Don't drift to synonyms the glossary
explicitly avoids.

`CONTEXT.md` calls out one live collision to respect: a **section source** is the module that
produces pages (`ISectionSource`); a **content source** is the field on a section that it consumes
(`ContentSource`). Both end in "source" and they are not the same thing — don't blur them.

If the concept you need isn't in the glossary yet, that's a signal — either you're inventing
language the project doesn't use (reconsider) or there's a real gap (note it for
`/domain-modeling`).

## Flag ADR conflicts

If your output contradicts an existing ADR, surface it explicitly rather than silently overriding:

> _Contradicts ADR-0003 (a page is encoded bytes, not a file path) — but worth reopening because…_
