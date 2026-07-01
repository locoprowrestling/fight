# Example: UnityMCP Documentation Ingestion

Date: 2026-06-10

## Goal

Create an ignored, offline UnityMCP knowledge base aligned with the package
actually installed in `fightgame`.

## Context

- Project: `/Users/gecko/locoprowrestling/fightgame`
- Unity: `6000.4.10f1`
- Package declaration: Git URL targeting `main`
- Resolved package commit:
  `78ee5418415953b79c358bfe6355fcc3fde7912b`
- Connected instance: `fightgame@fc0efb860bfd225d`

## Workflow

1. Read live custom-tool, project, group, and instance resources.
2. Inspect `Packages/manifest.json` and `Packages/packages-lock.json`.
3. Clone the official `CoplayDev/unity-mcp` repository.
4. Check out the exact resolved lock-file commit.
5. Copy the documentation, generated references, skill references, images, CLI
   guide, license, security policy, and contributor material.
6. Add a project-specific operator guide and source map.
7. Compare copied documentation against the pinned checkout.
8. Confirm `.unity/` remains ignored.

## Verification

| Check | Result | Evidence |
|---|---|---|
| Correct commit | Pass | Lock and checkout both use `78ee541...` |
| Website docs copied | Pass | 87 source and 87 snapshot Markdown pages |
| Complete snapshot | Pass | 130 files, 100 Markdown documents |
| Integrity | Pass | Recursive diff reported no differences |
| Git ignore | Pass | `git check-ignore` matched `.gitignore` |

## Durable Lessons

- Pin documentation to the resolved dependency, not a moving branch.
- Read live UnityMCP resources before trusting static examples.
- Keep source snapshots separate from project-specific operating guidance.
- Record counts and integrity checks so future refreshes are measurable.
