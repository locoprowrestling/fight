# Example: Grapple Soft-Lock Investigation (completed)

A filled-in `templates/BUG_INVESTIGATION.md` from a real fix, kept as the
reference for how to run one end-to-end.

Date: 2026-06-10

## Reproduction

1. Setup: default match (Zeak Gallent vs JT Staten), `PrototypeMatch.unity`.
2. Action: let the CPU close distance and initiate a grapple lockup.
3. Expected: CPU follows up with a quick/power grapple within the 1.8 s lock.
4. Actual: both wrestlers stand locked; lock times out; CPU instantly
   re-grapples; match never progresses ("game locked up").
5. Frequency: every CPU-initiated lockup.

## Scope

- Unity version: 6000.4.10f1
- Scene: PrototypeMatch
- Wrestlers/rules: any; standard rules
- Relevant state: `WrestlerState.GrappleLock` (profile timeout 1.8 s,
  `canAttack=false`, exit Idle)
- Relevant files: `Assets/Scripts/AI/CPUWrestlerAI.cs`,
  `Assets/Scripts/Combat/WrestlerCombat.cs`,
  `Assets/Scripts/Wrestlers/WrestlerStateMachine.cs`

## Evidence

- Console: `[Grapple] JT Staten locks up with Zeak Gallent` repeating at
  ~2 s intervals (1.8 s timeout + 0.3–0.6 s AI reaction delay — the cadence
  *is* the diagnosis).
- Debug overlay: both wrestlers cycling GrappleLock → Idle → GrappleLock.

## Hypotheses

| Hypothesis | Evidence For | Evidence Against | Status |
|---|---|---|---|
| Moveset has no grapple follow-ups | would also break player | player K/L from lock work | Rejected |
| AI never *reaches* follow-up logic | log cadence = timeout, `Decide()` gates on `canAttack` before `InGrappleLockAsAttacker` | — | Confirmed |
| Game actually frozen (hard lock) | "locked up" report | timestamps keep advancing | Rejected |

## Root Cause

`Decide()` early-returns on `!Profile.canAttack`. `GrappleLock` sets
`canAttack=false` for both wrestlers (correct — no strikes mid-lock), which
made the attacker's follow-up branch below it unreachable. Ordering bug:
capability flags describe the state for everyone; the attacker role still has
a legal action.

## Fix

1. Check `InGrappleLockAsAttacker` before the `canAttack` gate in `Decide()`.
2. In `Act()`/`ChooseGrappleMove`: fall back quick↔power and
   `ReleaseGrapple()` if neither executes, so a lock can never dangle.

## Regression Checks

- [x] CPU lockup resolves into a grapple move within the lock window
- [x] Player attacker paths (L quick / K power from lock) unchanged
- [x] Defender escape (Space, early window) unchanged
- [x] Console clean; offline Roslyn compile check exit 0
- [x] Checklist line added to `Documentation/TestingChecklist.md`

## Durable Learning

Recorded in `LEARNINGS.md` (2026-06-10, capability gates) and promoted to
`Documentation/KnowledgeBase/BestPractices.md` (role-before-gate; the
three-things rule for locking states) and
`Documentation/KnowledgeBase/Examples.md` (postmortem).
