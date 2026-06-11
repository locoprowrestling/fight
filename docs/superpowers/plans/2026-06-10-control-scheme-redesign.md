# Control Scheme Redesign Implementation Plan

> **For agentic workers:** Execute task-by-task with the offline compile checks
> (runtime + editor/test variant, `Documentation/KnowledgeBase/Examples.md`)
> after every task, edit-mode tests for all pure logic, and one commit per task
> with exact paths. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Four-button fight controls (Strike J/X tap-light hold-heavy;
Grapple/Control K/A contextual; Special L/Y; Dodge ;/B), one camera-first
direction frame, and an always-visible HUD prompt showing what the two context
buttons will do — with zero changes to `WrestlerCombat` or the CPU.

**Architecture:** Press phases live in `LegacyPlayerInputSource` /
`PlayerInputFrame`; tap/hold resolution is a pure `PressTracker` +
`PlayerInputLogic` constant; routing stays in `PlayerInputController`; prompts
are a pure `ControlPromptLogic` mapping rendered by `MatchHUD`.

**Tech Stack:** Unity 6.4, C# 9, legacy Input Manager, NUnit edit-mode tests.

## Execution Preconditions

- Unity editor may be open: never run batch mode; verify with the offline
  Roslyn checks; pre-generate `.meta` files for new scripts.
- `WrestlerCombat` and `CPUWrestlerAI` are read-only for this milestone.
- Commit exact paths per task.

### Task 1: Press-Phase Input Foundation (dormant)

**Files:** Create `Assets/Scripts/Input/PressTracker.cs`,
`Assets/Scripts/Editor/PressTrackerTests.cs`; modify
`Assets/Scripts/Input/PlayerInputFrame.cs`,
`Assets/Scripts/Input/LegacyPlayerInputSource.cs`,
`Assets/Scripts/Input/PlayerInputLogic.cs`.

- [ ] Failing tests: `PressTracker.Update` resolves Tap on release before the
      threshold, HoldCommitted exactly once at the crossing, nothing on
      release after a hold, and `Reset()` clears an in-flight press.
- [ ] `PressKind { None, Tap, HoldCommitted }` and a UnityEngine-free
      `PressTracker` (tracks down/duration/committed; `Update(pressed, held,
      released, deltaTime, threshold)`).
- [ ] `PlayerInputLogic.HoldThreshold = 0.22f`.
- [ ] Frame fields `StrikeHeld/StrikeReleased` (J / JoystickButton2) and
      `ControlPressed/ControlHeld/ControlReleased` (K / JoystickButton0),
      populated by the source; nothing consumes them yet.
- [ ] Compile checks; commit `Add press-phase input foundation`.

### Task 2: Unified Direction Frame

**Files:** `Assets/Scripts/Input/PlayerInputLogic.cs`,
`Assets/Scripts/Input/PlayerInputController.cs`,
`Assets/Scripts/Editor/PlayerInputLogicTests.cs`.

- [ ] Replace `ResolveMoveDirection` with `(move, cameraForward, cameraRight,
      attackerForward, deadZone)`: camera-map the stick to a world vector,
      classify against attacker forward / `Cross(up, forward)`.
- [ ] Tests: four camera yaws × pushing toward the opponent always resolves
      Forward; dead zone still Neutral; lateral classification.
- [ ] Controller passes the camera basis (fallback world axes when the camera
      is missing) and `_core.transform.forward`.
- [ ] Compile checks; commit `Unify directional input frame`.

### Task 3: Strike Consolidation

**Files:** `Assets/Scripts/Input/PlayerInputController.cs`.

- [ ] Strike `PressTracker` in the controller: Tap → existing light precedence
      chain (buffered, proximity-gated); HoldCommitted → `TryHeavyStrike`
      (buffered). Standing heavy leaves K (`frame.HeavyPressed` branch
      removed); in-lock behavior still flows through the legacy path until
      Task 4.
- [ ] `StopGameplayInput` resets trackers (pause/match-end fires nothing on
      resume).
- [ ] Compile checks; manual sanity items appended to checklist in Task 6;
      commit `Consolidate strikes onto tap and hold`.

### Task 4: Grapple/Control Consolidation and Rebinding

**Files:** `Assets/Scripts/Input/LegacyPlayerInputSource.cs`,
`Assets/Scripts/Input/PlayerInputFrame.cs`,
`Assets/Scripts/Input/PlayerInputController.cs`,
`Assets/Scripts/Input/PlayerInputLogic.cs`,
`Assets/Scripts/Editor/PlayerInputLogicTests.cs`,
`Assets/Scripts/UI/MatchHUD.cs`.

- [ ] Bindings: Special → L / JoystickButton3; Dodge → Semicolon (Alt alias) /
      JoystickButton1; handshake refuse → K (pad unchanged); U/I/O and the
      old Heavy/Grapple/Pin/Submission frame fields removed; keyboard
      activity + mash detection lists updated.
- [ ] Control button routing via its `PressTracker`:
      in lock — Tap → `TryQuickGrappleFromLock(direction)`, Hold →
      `TryPowerGrappleFromLock(direction)`;
      beside a downed opponent (≤1.2) — Tap → `TryPin()`, Hold →
      `TrySubmission()` (both stay unbuffered);
      otherwise — Tap → buffered `TryCornerGrapple() || TryGrappleAttempt()`,
      Hold → nothing.
- [ ] Remove `ResolveLockAction` and its tests (replaced by press kinds).
- [ ] Update `MatchHUD` handshake prompt and controls-hint strings to the new
      scheme.
- [ ] Compile checks; commit `Consolidate grapple control button`.

### Task 5: Contextual HUD Prompts

**Files:** Create `Assets/Scripts/UI/ControlPromptLogic.cs`,
`Assets/Scripts/Editor/ControlPromptLogicTests.cs`; modify
`Assets/Scripts/UI/MatchHUD.cs`, `Assets/Scripts/UI/DebugOverlay.cs`,
`Assets/Scripts/Input/PlayerInputController.cs`.

- [ ] Failing tests, then pure `ControlPromptLogic`: `(CombatContext,
      opponentDownedInRange, device)` → `[glyph] label` strings per the spec
      table (Strike: context attack names; Control: Quick/Power in lock,
      Pin/Submission near downed, Corner Grapple, Grapple).
- [ ] `MatchHUD` bottom-center prompt label, refreshed at ≤5 Hz from
      `_player.Combat.CurrentContext` + downed proximity, device-aware glyphs,
      string set only on change.
- [ ] Controller exposes debug press-phase strings; F1 shows phases and the
      live prompt labels.
- [ ] Compile checks; commit `Add contextual control prompts`.

### Task 6: Documentation and Regression

**Files:** `Documentation/README.md`, `Documentation/DesignDoc.md`,
`Documentation/TestingChecklist.md`,
`Documentation/KnowledgeBase/BestPractices.md`.

- [ ] Rewrite both controls tables; sweep every checklist item that names the
      old keys (grapple L→K, heavy K→hold J, special U→L, pin I / submission O
      → K tap/hold, dodge Alt→;); add tap/hold, direction-frame, prompt, and
      parity matrices.
- [ ] DesignDoc control section: four-button scheme, hold threshold, one
      direction frame, prompts.
- [ ] BestPractices: press-phase pattern (one press → at most one action;
      pause clears) and the one-direction-frame rule.
- [ ] `git diff --check` clean; no new TODO/TBD/FIXME; full offline compile;
      commit `Document control scheme redesign`.

## Final Completion Check

- [ ] One physical press never fires two actions; pause/resume fires nothing.
- [ ] Toward-opponent-on-screen is Forward for every camera yaw.
- [ ] Every old action reachable (parity): light, heavy, grapple, quick,
      power, directional, corner strike/grapple, ground, rope, rebound, pin,
      submission, special, dodge, reversal, mash, handshake, pause/reset.
- [ ] HUD prompts match actual button behavior in every context on both
      devices; presentation-only.
- [ ] `WrestlerCombat`, validation, and CPU untouched.
