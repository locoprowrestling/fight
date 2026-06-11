# Engineering Knowledge Base

Living notes for working on the fight game. Four docs, four jobs:

- [BestPractices.md](BestPractices.md) — the invariants and conventions this
  codebase relies on. Read before changing combat, AI, states, or visuals.
- [Templates.md](Templates.md) — copy-paste recipes for the common additions:
  wrestler, move, special, trait, state, animation pose, AI behavior.
- [Examples.md](Examples.md) — worked examples and postmortems from real
  changes, kept short and specific.
- [ResearchNotes.md](ResearchNotes.md) — an informational summary of external
  battle-system research and the process for promoting applicable findings into
  project practices.

## How to keep this alive

Add to these docs _in the same change_ that taught you something:

- Fixed a bug whose cause was structural (not a typo)? Add a postmortem entry to
  Examples.md.
- Established a new pattern (a new executor category, a new subsystem)? Add the
  recipe to Templates.md.
- Discovered an invariant the hard way? State it in BestPractices.md with a
  one-line "why".

Keep entries grounded: link the real file (`Assets/Scripts/...`) instead of
describing code from memory, and prefer one honest paragraph over a page of
theory. Delete entries that stop being true — a stale note is worse than no
note.

Research is an input, not an authority. Glean useful practices when they fit
this game's goals and current architecture, but validate them against the code
and adopt them explicitly. Accepted findings belong in `BestPractices.md`,
`Templates.md`, or the relevant design document with concrete repository links;
speculative or rejected ideas stay in `ResearchNotes.md`. The raw source notes
remain in the [`research/`](../../research/) directory.

Relationship to `.unity/notes/` (if present on your machine): that directory is
a git-ignored local working log (dated decisions/learnings, session templates).
This folder is the **tracked, shared layer** — entries from the local log that
prove durable and contributor-relevant get promoted here. Content lives in
exactly one place; the other side carries a pointer at most.

Design-side references live next door: [DesignDoc.md](../DesignDoc.md),
[RopeMechanics.md](../RopeMechanics.md),
[SpecialAbilities.md](../SpecialAbilities.md), [Roster.md](../Roster.md),
[FutureAssetIntegration.md](../FutureAssetIntegration.md),
[TestingChecklist.md](../TestingChecklist.md).
