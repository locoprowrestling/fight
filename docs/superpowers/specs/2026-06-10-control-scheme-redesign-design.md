# Control Scheme Redesign Milestone Design

**Status:** Draft for review

**Audience:** Internal team

**Scope:** Current one-player-versus-CPU prototype

**Priority:** Control legibility and hand comfort

## Problem

The current scheme has grown by accretion and fights the player four ways:

1. **Six attack keys on one hand.** J/K/L plus U/I/O forces the right hand to
   leave the strike cluster for specials, pins, and submissions mid-fight.
2. **Ambiguous overloading.** K is a heavy strike standing but a power grapple
   in a lock. After the contextual combat slice, J resolves through a hidden
   six-way priority (ground / corner / rope-stagger / rebound / running /
   light) that the player cannot see outside the F1 debug overlay.
3. **Two direction frames.** Movement is camera-relative, but directional
   grapples are wrestler-facing-relative (stick-up is always "forward"
   regardless of camera), so the same stick push means different things in
   different systems.
4. **Dedicated keys for contextual actions.** Pin (I) and submission (O) are
   only ever legal beside a downed opponent — textbook candidates for a
   context button.

## Objective

Reduce the core fight controls to four buttons plus movement, make every
context-sensitive resolution visible on the HUD, and use one direction frame
everywhere — without changing any combat behavior, validation, damage, or CPU
logic. This is an input- and presentation-layer milestone: `WrestlerCombat`
and everything below it is out of scope except where a method must become
callable from a new button path that already exists.

The milestone remains inside the current prototype scope:

- one player and one CPU opponent
- existing contextual combat families, validation, pins, submissions,
  reversals, specials, and rules
- no input rebinding UI, no Input System migration, no tutorial system

## Target Scheme

### Keyboard

| Action | Key | Semantics |
|---|---|---|
| Move | W / A / S / D | unchanged, camera-relative |
| Run | Left Shift | unchanged |
| **Strike** | J | tap = light family; hold past threshold = heavy family; contextual attacks (ground / corner / rope-stagger / rebound / running) keep their existing precedence on tap |
| **Grapple / Control** | K | outside lock: tap = grapple attempt (corner grapple when valid); in lock: tap = quick grapple, hold = power grapple, held movement direction picks the bucket; beside a downed opponent: tap = pin, hold = submission |
| **Special** | L | replaces U |
| **Dodge** | ; (Left Alt remains an alias) | unchanged behavior |
| Reversal / kickout mash | Space | unchanged |
| Taunt / handshake accept | T | unchanged; handshake cheap shot stays on Strike (J), refuse moves to Grapple (K) |
| Reset / pause / debug | R / Escape / F1 / F2 | unchanged |

Removed as gameplay keys: U, I, O. K's standing heavy strike moves onto held J.

### Controller

| Action | Binding |
|---|---|
| Strike (tap/hold) | X (JoystickButton2) |
| Grapple / Control (tap/hold) | A (JoystickButton0) |
| Special | Y (JoystickButton3) |
| Dodge | B (JoystickButton1) |
| Reversal / mash | RB (JoystickButton5) |
| Run | LB (JoystickButton4) |

Freed buttons (6, 8, 9) are reserved; pause/reset stays on 7.

### Tap versus hold

- One tunable constant `HoldThreshold` (initial value 0.22 s) in
  `PlayerInputLogic`.
- **Tap** fires on release before the threshold.
- **Hold** fires the moment the threshold is crossed while still held — the
  player does not wait for release, and release after a hold fires nothing.
- A single press fires exactly one of tap or hold, never both.
- Accepted tradeoff: tap actions gain up to one tap-duration of latency
  (~80–120 ms typical) versus the old press-down trigger. The threshold is a
  constant so feel can be tuned in one place.

### One direction frame

All directional input is interpreted camera-relative first (the existing
movement mapping), producing a world vector; systems that need a
facing-relative classification (directional grapples) classify that world
vector against the attacker's facing. Net effect: pushing toward the opponent
on screen is always `Forward`. `ResolveMoveDirection` changes signature to
accept the camera basis; the dead zone is unchanged.

### Contextual HUD prompts

A small always-visible HUD element shows what the two context buttons will do
right now, using the resolver that already drives combat:

```text
[J] Elbow Drop        [K] Pin (hold: Submission)
```

- Driven by `CombatContextResolver.Resolve` plus lock/downed-proximity checks
  through a pure, edit-mode-testable mapping (`ControlPromptLogic`).
- Key glyphs follow the active device via the existing
  `MatchHUD.TrySetInputDevice` path (J/K vs X/A).
- Presentation-only: the prompt may never gate, trigger, or alter gameplay,
  and a wrong prompt is a bug in the mapping, not in combat.

## Architecture

Ownership after this milestone:

```text
LegacyPlayerInputSource   raw keys/buttons → per-button press phases
PlayerInputFrame          adds press/hold/release phases per core button
PlayerInputLogic          pure tap/hold resolution, direction mapping (tested)
PlayerInputController     phase → WrestlerCombat call routing + buffering
InputBuffer               unchanged retry semantics
ControlPromptLogic        pure (context, lock, proximity) → prompt labels
MatchHUD                  renders prompts; no gameplay reads
WrestlerCombat and below  UNCHANGED — same API both controllers consume
CPUWrestlerAI             UNCHANGED — never reads input
```

## Phase 1: Press-Phase Input Foundation

Deliver:

- Per-button press tracking (`Pressed`, `HeldFor(seconds)`, `Released`) for
  Strike and Grapple in `LegacyPlayerInputSource` / `PlayerInputFrame`.
- Pure `PlayerInputLogic.ResolvePressKind(pressDuration, isHeld, released,
  threshold)` returning None / Tap / HoldCommitted, with edit-mode tests.
- No behavior change yet: existing bindings keep working through this phase.

Acceptance criteria:

- A press shorter than the threshold resolves Tap exactly once, on release.
- A press crossing the threshold resolves HoldCommitted exactly once, at the
  crossing, and Release afterward resolves nothing.
- Pause or match end clears in-flight press state; nothing fires on resume.
- All existing matches play identically (foundation is dormant).

## Phase 2: Unified Direction Frame

Deliver:

- `ResolveMoveDirection(move, cameraForward, cameraRight, attackerForward,
  deadZone)`: camera-map the stick to world, then classify against facing.
- Updated `PlayerInputLogicTests` covering four camera yaw positions and four
  attacker facings.

Acceptance criteria:

- Pushing toward the opponent on screen selects `Forward` for every camera
  position and attacker facing.
- The dead zone still yields `Neutral`.
- Directional bucket selection, fallback, and CPU behavior are otherwise
  unchanged (CPU never used the input path).

## Phase 3: Strike Consolidation

Deliver:

- J/X tap → existing light precedence chain (ground → corner → rope-stagger →
  rebound → running → light strike).
- J/X hold → `TryHeavyStrike` (standing only; contextual families keep their
  single authored attack on tap).
- Remove K/Y as standing heavy strike.

Acceptance criteria:

- Every attack reachable before is still reachable (parity table in the test
  matrix).
- Tap and hold never both fire from one press; buffered taps respect the
  existing `InputBuffer` window.
- Handshake cheap shot (Strike) still works during the ritual.

## Phase 4: Grapple/Control Consolidation

Deliver:

- K/A outside a lock: tap → corner grapple when valid, else grapple attempt.
- K/A in a lock: tap → quick grapple, hold → power grapple, held direction
  picks the bucket (existing `TryQuickGrappleFromLock(direction)` /
  `TryPowerGrappleFromLock(direction)`).
- K/A beside a downed opponent: tap → `TryPin`, hold → `TrySubmission`.
- Special moves to L/Y; U, I, O removed; dodge to ; with Alt alias; handshake
  refuse moves to K.

Acceptance criteria:

- Pin and submission fire only in their legal context and never both from one
  press; kickout/submission mash inputs are unchanged.
- In-lock tap/hold matches the old L/K outcomes exactly (same combat calls).
- The lock follow-up window (1.8 s GrappleLock timeout) comfortably exceeds
  the hold threshold; holding for power never times out the lock by itself.
- No orphaned binding remains: pressing U, I, or O does nothing and the
  controls documentation matches reality.

## Phase 5: Contextual HUD Prompts

Deliver:

- `ControlPromptLogic` pure mapping with edit-mode tests.
- `MatchHUD` prompt element (anchored near the player's meters), updated on
  context change or at a low fixed poll rate, switching glyphs per device.

Acceptance criteria:

- Prompts match the action that actually fires across the manual matrix
  (every context, lock state, and downed proximity).
- Prompts never read or mutate combat state beyond the existing public
  diagnostics; disabling the element changes nothing about gameplay.
- No per-frame string allocation when the prompt has not changed.

## Phase 6: Documentation and Regression

Deliver:

- Updated controls tables in `Documentation/README.md` and the root `README.md`
  pointer, `Documentation/DesignDoc.md` control section, and
  `Documentation/TestingChecklist.md` (new scheme + parity matrix).
- `Documentation/KnowledgeBase/` entries for the press-phase pattern and the
  one-direction-frame rule.
- Full manual regression of the existing checklist under the new scheme.

## CPU Behavior

None of this milestone touches the CPU. `CPUWrestlerAI` calls `WrestlerCombat`
directly and never reads `PlayerInputFrame`. Any change that would require a
combat-API modification beyond what already exists must be flagged before
implementation rather than absorbed silently.

## Debugging

Extend F1 (no new framework):

```text
strike phase + held duration
grapple phase + held duration
resolved press kind (tap/hold)
prompt labels currently displayed
direction frame inputs (camera basis, world vector, classified direction)
```

## Failure Invariants

- One physical press resolves at most one gameplay action.
- Pause, match end, and reset clear press state and the buffer; nothing fires
  on resume.
- Every action reachable in the old scheme is reachable in the new one.
- HUD prompts are presentation-only and cannot gate or trigger gameplay.
- The combat layer's validation remains the only legality authority; the
  input layer never pre-judges legality beyond choosing which Try* to call.
- Controller and keyboard reach identical actions.

## Verification

Every phase must pass:

```text
offline compile check (runtime + editor/test variant)
edit-mode tests for pure press, direction, and prompt logic
phase-specific manual matrix
full existing checklist smoke test under the new scheme
F1 inspection of press phases and prompt correctness
documentation update
```

Manual matrices must cover:

- keyboard and controller
- tap and hold on both core buttons, including presses spanning the threshold
- every combat context (standing, lock, ground, corner, rope-stagger, rebound)
- pause/resume and match-end during a held press
- handshake ritual inputs
- pin/submission mash as defender (unchanged behavior)

## Milestone Exit Criteria

1. Core fight input is Strike, Grapple/Control, Special, Dodge, Reversal plus
   movement; U, I, O are retired.
2. Tap/hold resolution is deterministic, tested, and tunable via one constant.
3. One direction frame: toward-opponent-on-screen is Forward everywhere.
4. The HUD always shows what the two context buttons will do, on both devices.
5. All prior actions remain reachable; full regression passes.
6. No combat, validation, CPU, or balance behavior changed.

## Deferred Scope

- input rebinding UI and saved bindings
- new Input System package migration
- accessibility options (toggle holds, timing assists)
- tutorial or onboarding flow
- combo inputs, taunt wheel, additional context buttons
- any combat-layer redesign

## Sources

- `Documentation/README.md`: current control mapping
- `Assets/Scripts/Input/LegacyPlayerInputSource.cs`: current bindings and frame
- `Assets/Scripts/Input/PlayerInputController.cs`: current routing and buffering
- `Assets/Scripts/Input/InputBuffer.cs`: retry semantics to preserve
- `Assets/Scripts/Combat/WrestlerCombat.cs`: the API surface that must not change
- `docs/superpowers/specs/2026-06-10-contextual-combat-slice-design.md`:
  contextual families, resolution priority, and diagnostics this scheme exposes
