# Game Feel Milestone Design

**Status:** Approved design — phases 1–4 implemented 2026-06-11 (phase 5 audio deferred)

**Audience:** Internal team

**Scope:** Current one-player-versus-CPU prototype

**Priority:** Impact readability and outcome feedback

## Problem

Inputs now resolve correctly and the HUD explains them (control redesign +
fluidity fix-set), but the *outcomes* still read poorly:

1. **Moves look alike.** Elbow Drop and Head Stomp share one overlay; every
   grapple finish is the same generic reach. The player cannot visually
   confirm that the labeled move is the one that happened — the last
   remaining source of "the button doesn't match the screen."
2. **Impacts have no weight.** Hits apply damage on a frame with only a color
   flash; there is no pause, no camera acknowledgment, no defender mass.
3. **Two-actor moves don't read as two-actor.** The defender teleports and
   plays a generic stun pose; AKI-style grapples sell because attacker and
   defender animate one synchronized event (see research: "Multi-actor
   synchronization is non-negotiable").
4. **Silence.** Zero audio, not even procedural cues, so timing has no
   rhythm channel at all.

## Objective

Make every executed move visually and rhythmically identifiable without any
external art or audio assets, by adding a presentation event channel and
consuming it with hit-stop, camera feedback, distinct procedural poses, and
synchronized defender bumps. Combat outcomes, timing, validation, and CPU
behavior do not change: per the established boundary, everything in this
milestone lives behind `IAnimationDriver`, the camera, and a new
presentation-only feel system.

Key AKI lessons applied (research/aki_wrestling_games_development_modding_research_ingestion_pack.md):

- *Timeline events are mandatory* — moves expose startup / contact / impact /
  release / recovery markers; presentation keys off markers, never off
  guessed durations.
- *Separate animation from move logic* — markers are emitted by the
  authoritative combat routines; feel systems are pure consumers.
- *Multi-actor synchronization* — grapples drive both bodies from one event
  stream.

## Architecture

```text
WrestlerCombat routines      emit MoveEvent(kind, move, attacker, defender)
MoveEvent                    Startup | Contact | Impact | Release | Recovery
FeelSystem (new, presentation-only)
                             hit-stop, camera punch, defender bump triggers
IAnimationDriver             gains per-move pose variants + bump overlays
TwoTargetMatchCamera         receives punch/shake impulses only
MoveData                     names its poses (placeholderPoseName variants);
                             timing fields stay authoritative and unchanged
```

Failure invariant: disabling the FeelSystem changes nothing about match
outcomes. Hit-stop is the one global effect (brief `Time.timeScale` dip,
≤ 0.09 s, tier-scaled); it slows both wrestlers identically and never skips
validation or state transitions.

## Phase 1: Presentation Event Channel

- `MoveEvent` struct + a static `FeelSystem.Notify(...)` funnel called from
  the existing strike/grapple/ground/corner/rope routines at their existing
  phase boundaries (no timing changes — the phases already exist).
- F1 shows the last few events with timestamps.

Acceptance: events fire for every move family, player and CPU; zero gameplay
diffs (regression smoke).

## Phase 2: Hit-Stop and Camera Impact

- Impact events trigger hit-stop scaled by `MoveTier` (Light ~0.03 s, Medium
  ~0.05 s, Heavy/Special ~0.08 s) and a camera punch (small FOV/position
  impulse with fast decay) on Heavy/downing impacts.
- All constants in one place; a debug toggle disables the system.

Acceptance: pins, submissions, referee counts, reversal windows, and timers
are unaffected (they tick in scaled time uniformly); rapid light hits never
stack hit-stop into slow motion.

## Phase 3: Distinct Move Silhouettes

- Pose-name variants per authored move (`ground-drop`, `ground-stomp`,
  `corner-smash`, `corner-bulldog`, `rope-chop`, `rebound-lariat`,
  quick-grapple vs power-grapple finishes) mapped to distinct procedural
  overlays in `PlaceholderAnimationDriver`.
- `DefaultGameData` assigns the variant names; asset regeneration required.

Acceptance: every move in the database is visually distinguishable from the
others in its family at a glance; gameplay timing untouched.

## Phase 4: Synchronized Defender Bumps

- Impact events drive defender reactions by result: light recoil (existing),
  heavy stagger, slammed-to-mat arc for downing moves (a fast procedural
  fall replacing the teleport-to-lying snap), corner rebound sway.
- Attacker and defender key off the same event, AKI-style; the defender's
  gameplay state remains set by combat exactly as today.

Acceptance: downing moves read as throws rather than teleports; interrupted
moves leave both rigs in valid poses; ragdoll physics stays out (determinism
boundary).

## Phase 5: Procedural Audio Cues (optional, last)

- A tiny generated-clip palette (thud, slap, slam, bell) played from the
  existing `MoveData` audio event-name hooks via the event channel.
- Defer without guilt if generated audio reads as worse than silence.

## Verification

Per phase: offline compile (runtime + editor variants), edit-mode tests for
pure pieces (event payloads, hit-stop scaling math), manual matrix across
move families and both attackers, full checklist smoke, F1 event inspection,
docs update. Final regression includes the no-feel-system toggle run to prove
the presentation-only invariant.

## Deferred Scope

- real animation clips, Animator migration, root motion
- ragdolls, physics reactions
- crowd, music, commentary
- camera cinematics/replays
- the AKI move-template editor and CAW systems (creation-suite scope)

## Sources

- `research/aki_wrestling_games_development_modding_research_ingestion_pack.md`
- `Documentation/FutureAssetIntegration.md` (the IAnimationDriver boundary)
- `Assets/Scripts/Animation/PlaceholderAnimationDriver.cs`
- `Assets/Scripts/Combat/WrestlerCombat.cs` (phase boundaries to instrument)
- `docs/superpowers/specs/2026-06-10-control-scheme-redesign-design.md`
