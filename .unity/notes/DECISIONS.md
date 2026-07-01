# Decision Log

## 2026-06-10 - Local Unity knowledge base

Status: Accepted

Context:

Development requires project-specific Unity and UnityMCP context that should not
become part of the shipped game or tracked product documentation.

Decision:

Store this working knowledge under the ignored `.unity/` directory.

Evidence:

`.gitignore` contains `.unity/`, and `git check-ignore` confirms the knowledge
base is excluded.

Consequences:

The notes are available locally but are not shared through normal Git clones.
Anything required for every contributor must instead be promoted into tracked
`README.md`, `Documentation/`, code, or project settings.

Revisit when:

The team wants to share all or part of the development knowledge base.

## 2026-06-10 - Pin UnityMCP documentation to the resolved package

Status: Accepted

Context:

The package manifest references UnityMCP's moving `main` branch, while the
package lock records the actual resolved commit.

Decision:

Keep the offline UnityMCP documentation snapshot aligned with
`Packages/packages-lock.json`, not the latest upstream branch.

Evidence:

The installed package resolves to UnityMCP `9.7.1`, commit
`78ee5418415953b79c358bfe6355fcc3fde7912b`.

Consequences:

The local reference matches installed behavior. Newly documented upstream
features must not be assumed available until the package is updated.

Revisit when:

The UnityMCP package lock hash changes.

## 2026-06-10 - Resource-first UnityMCP workflow

Status: Accepted

Context:

UnityMCP exposes live editor resources and mutation tools. Acting on assumed
scene, instance, or compilation state risks modifying the wrong target or using
unavailable APIs.

Decision:

Read custom tools, instances, editor readiness, and task-specific resources
before editor mutations.

Evidence:

The connected project exposes dynamic tool groups, project-specific tools, and
instance routing through live resources.

Consequences:

Unity automation includes a short preflight and explicit verification pass.

Revisit when:

The UnityMCP resource and routing model changes.

## 2026-06-10 - Keep local LLM guidance out of Git

Status: Accepted

Context:

The repository uses local agent guidance and knowledge-base files that may
contain machine-specific paths, tool state, and working notes. They are not
required game source or shared contributor documentation.

Decision:

Ignore root-local AI agent files and directories, including `CLAUDE.md`,
`.claude/`, `.unity/`, `AGENTS.md`, `.codex/`, `.cursor/`, `.windsurf/`,
`.continue/`, `.clinerules/`, `.mcp.json`, and Aider state.

Evidence:

The current repository contains `CLAUDE.md`, `.claude/settings.local.json`, and
the `.unity/` knowledge base. `git check-ignore` confirms they are excluded.

Consequences:

Local guidance is not shared through normal clones. Any instruction required by
all contributors must be promoted into tracked `README.md`, `Documentation/`,
code, or project settings.

Revisit when:

The team decides to standardize and share a specific agent configuration.

## 2026-06-10 - Articulated primitive rig with a procedural pose animator

Status: Accepted

Context:

The original placeholder body was six floating primitives with whole-body tilt
animation. The prototype needs humanoid silhouettes that bend at hips, knees,
and elbows, but real models/Animator clips are out of scope and ragdoll physics
would make gameplay outcomes non-deterministic.

Decision:

Build the placeholder as a joint hierarchy of empty pivots (pelvis → spine →
neck, shoulders → elbows, hips → knees) with collider-free primitive meshes,
and animate it procedurally: `PlaceholderAnimationDriver` computes a full-body
`BodyPose` per frame (state base + walk cycle + one-shot overlays) and eases
joints toward it. Gameplay remains on the single `CharacterController`, sized
from `WrestlerView.RigHeight`/`HeightFor`/`BulkFor` per weight class.

Evidence:

`Assets/Scripts/Wrestlers/WrestlerView.cs`,
`Assets/Scripts/Animation/PlaceholderAnimationDriver.cs`,
`Assets/Scripts/Wrestlers/WrestlerCore.cs` (collider sizing); the
`IAnimationDriver` boundary and all 40+ `WrestlerState` poses are covered.

Consequences:

Visuals bend like humanoids with zero assets and no physics coupling; combat
ranges and balance are unchanged. Pose tuning is constants-in-code
(`ComputePose`). Real rigged models later replace `BuildPlaceholder` and the
driver per `Documentation/FutureAssetIntegration.md`.

Revisit when:

Real character models or authored animation clips are introduced.

## 2026-06-10 - Tracked engineering knowledge base under Documentation/KnowledgeBase

Status: Accepted

Context:

`.unity/` notes are git-ignored by design, but several practices, templates,
and postmortems are needed by every contributor, and prior decisions require
promoting shared knowledge into tracked documentation.

Decision:

Maintain `Documentation/KnowledgeBase/` (BestPractices.md, Templates.md,
Examples.md) as the tracked, shareable layer. `.unity/notes/` stays the local
working log; entries that prove durable and contributor-relevant get promoted
there in the same change.

Evidence:

`Documentation/KnowledgeBase/README.md` defines the three docs and the
"add in the same change" rule; linked from both `README.md` files.

Consequences:

Two layers with one direction of flow (local log → tracked KB). Duplication is
acceptable only as a pointer; content lives in exactly one place.

Revisit when:

The team shares `.unity/` or consolidates documentation elsewhere.

## 2026-06-11 - AKI input grammar for offense

Status: Accepted

Context:

The tap/hold control grammar put a timing test on every offensive action;
playtests called the game unplayable as wrestling.

Decision:

One press = one move; direction is the only modifier. Strikes fire on press
(direction held = heavy). Tie-up strength resolves at initiation (K released
before the lock = quick set, held through the lock-up = power set); in the
lock, K + direction fires instantly. Tap/hold survives only for pin/submission.

Evidence:

docs/superpowers/specs/2026-06-11-aki-grammar-controls-design.md;
research/aki_wrestling_games_development_modding_research_ingestion_pack.md
("complexity in context, not input").

Consequences:

Zero input latency on offense; the strength choice is readable on the HUD
before the follow-up. PressTracker remains only for deliberate actions.

Revisit when:

A controller-facing rebinding UI or new Input System migration lands.

## 2026-06-11 - Presentation-only FeelSystem owns impact feedback

Status: Accepted

Context:

Moves resolved correctly but nothing performed them; impacts had no weight.

Decision:

A self-bootstrapping, toggleable FeelSystem consumes impact notifications from
combat hit appliers and drives tier-scaled hit-stop (extend-don't-stack,
defers to pause) and camera punches. Move silhouettes are placeholder pose
overlays named per-move in data; defender mat-bounce is a visual lift overlay.
Disabling the system must produce zero gameplay diff.

Evidence:

Assets/Scripts/Combat/FeelSystem.cs; spec
docs/superpowers/specs/2026-06-11-game-feel-design.md (phases 1-4 implemented,
audio deferred).

Consequences:

Impact polish has one owner and one off-switch; Time.timeScale has exactly two
writers (pause > feel).

Revisit when:

Real animation clips or audio replace the placeholder layer.
