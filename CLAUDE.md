# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

A playable 3D wrestling prototype for LoCo Pro Wrestling built in Unity 6 (editor version 6000.4.10f1). One player vs one CPU, 16-wrestler data-driven roster, zero external 3D assets — all geometry is Unity primitives and all animation is procedural.

There is no CLI build, lint, or test workflow. Everything runs through the Unity Editor: open `fightgame/` as a Unity project, let scripts compile, run **Tools > LoCo Fight Game > Setup Everything (Assets + Scene)**, open `Assets/Scenes/PrototypeMatch.unity`, press Play. The Unity Test Framework package is installed but there are no tests; QA is the manual checklist in [Documentation/TestingChecklist.md](Documentation/TestingChecklist.md).

All code lives in `Assets/Scripts/` under the single `LoCoFight` namespace (no asmdefs, one Assembly-CSharp). Input uses the legacy Input Manager (`Input.GetKeyDown`), not the new Input System.

## Local Unity knowledge base

Before substantial Unity or UnityMCP work, read the local knowledge base under
`.unity/`. This directory is intentionally ignored by Git and contains
project-specific operating context, upstream documentation snapshots, reusable
templates, examples, and accumulated lessons.

Required reading order:

1. `.unity/PROJECT_GUIDE.md` for Unity architecture and project constraints.
2. `.unity/notes/BEST_PRACTICES.md` for established implementation and
   verification practices.
3. `.unity/notes/DECISIONS.md` and `.unity/notes/LEARNINGS.md` for relevant
   prior choices and discoveries.
4. For UnityMCP tasks,
   `.unity/unity-mcp/FIGHTGAME_OPERATOR_GUIDE.md`, then the relevant entry in
   `.unity/unity-mcp/SOURCE_MAP.md`.
5. Read `.unity/UNITY_LEARN_SOURCES.md` or the pinned upstream UnityMCP snapshot
   only when the task needs deeper reference material.

For UnityMCP work, always begin with the live
`mcpforunity://custom-tools` resource, then inspect instances and editor state.
Live tool schemas, the connected editor, current code, assets, and package lock
take precedence over copied examples or older notes.

After substantial work, update the knowledge base when the task produces a
durable decision, non-obvious troubleshooting result, reusable command,
workflow improvement, template, or example. Follow
`.unity/notes/README.md`; append to decision and learning logs rather than
rewriting history. Promote behavior required by every contributor into tracked
`README.md`, `Documentation/`, code, or project settings because `.unity/` is
not shared through Git. The tracked promotion target for engineering practices,
copy-paste templates, and postmortems is `Documentation/KnowledgeBase/`
(BestPractices.md / Templates.md / Examples.md) — consult it before structural
changes and extend it in the same change that teaches you something.

## Data flow: code defaults → ScriptableObject assets

This is the most important thing to understand before editing game data (moves, stats, specials, traits, roster, rules, AI difficulty):

- `DefaultGameData.CreateAll()` (Assets/Scripts/Roster/DefaultGameData.cs) is the single in-code factory that defines all 16 wrestlers, their movesets, specials, traits, and match rules as in-memory ScriptableObjects.
- The editor menu **Tools > LoCo Fight Game > Create Default Prototype Assets** (`PrototypeAssetBuilder`) serializes that same set to `.asset` files under `Assets/Resources/LoCoData/`.
- At runtime, `RosterLoader` / `GameBootstrap` prefer the saved assets and fall back to the in-code factory, so the game always boots even with zero assets.

Consequence: to change game data, edit `DefaultGameData.cs` and regenerate the assets via the builder menu — otherwise stale `.asset` files in `Resources/LoCoData/` will shadow your code changes. `RosterAssetImporter` similarly imports the `tas-*.png` portraits from `players-web/` into `Assets/Art/RosterPortraits/`.

## Architecture

`GameBootstrap` (Assets/Scripts/Core) builds the entire scene procedurally — arena, match systems, wrestlers, camera, HUD — so the prototype runs from a single component in an empty scene. There are no hand-authored prefab/scene dependencies to maintain.

Layers (gameplay is fully separated from presentation):

- **Wrestler runtime**: `WrestlerCore` is the root component that creates and wires every subsystem in `WrestlerCore.Create()` — `WrestlerMotor`, `WrestlerStateMachine`, `WrestlerCombat`, `WrestlerStatsRuntime`, `BuffDebuffController`, `PassiveTraitController`, `SpecialController`, `DodgeSystem`. Add new wrestler subsystems there.
- **Presentation isolation**: `WrestlerView` (primitive capsule/cube body) and `IAnimationDriver` / `PlaceholderAnimationDriver` are the only places that touch meshes or animation. Real art is meant to replace these without touching combat/AI/rules — keep that boundary intact (see [Documentation/FutureAssetIntegration.md](Documentation/FutureAssetIntegration.md)).
- **State gating**: `WrestlerStateMachine` defines 40+ states, each with a `StateProfile` declaring what is allowed in that state (can move/attack/reverse, can be pinned/grappled, rope interaction, timeout, exit state). Behavior rules belong in profiles, not scattered if-checks.
- **Arena/ropes**: `ArenaRig` holds typed anchors/zones; `RingInteractionSystem` is the *only* rope-math authority (see [Documentation/RopeMechanics.md](Documentation/RopeMechanics.md)).
- **Match flow**: `MatchManager` (singleton via `Instance`) owns setup → handshake ritual → active play → win/reset, with `PinSystem`, `SubmissionSystem`, `RefereeCountSystem` alongside it on the MatchManagers object.
- **Control symmetry**: `PlayerInputController` + `InputBuffer` (human) and `CPUWrestlerAI` + `AISpecialPlanner` + `AIMemory` (CPU) both drive the same `WrestlerCombat` API. New combat actions must be exposed there so both sides can use them.
- **Specials**: each special category has its own executor in Assets/Scripts/Specials (`RushSpecialExecutor`, `AerialSpecialExecutor`, `CounterSpecialExecutor`, etc.), coordinated by `SpecialController`; all 16 are documented in [Documentation/SpecialAbilities.md](Documentation/SpecialAbilities.md).

## Repo contents outside Unity

`prompts/` (character image-generation prompts), `players-web/` (source portrait PNGs), and `logos/` are art-pipeline inputs, not Unity assets. The design docs in `Documentation/` are the authoritative reference for mechanics.

This project is the **upstream source of the shared roster**. Two sibling workspace projects regenerate themselves from it, so a roster/portrait/logo change here ripples outward: `../newfightgame` (OpenBOR 2D game) reads the `Assets/Resources/LoCoData/Roster/*.asset` files and `players-web/` portraits via its `scripts/sync-fightgame-roster.mjs`, and `../LogoGen` (intro videos) reads `logos/`. After changing roster data, regenerate the assets here (see the data-flow section), then re-sync those consumers.

## Useful in-game debug keys

F1 debug overlay, F2 roster selector, R reset after a finish. Full controls table is in [Documentation/README.md](Documentation/README.md).
