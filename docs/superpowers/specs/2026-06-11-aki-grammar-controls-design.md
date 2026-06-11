# AKI-Grammar Controls Design

**Status:** Approved design (supersedes the tap/hold grammar of
2026-06-10-control-scheme-redesign-design.md for the offensive buttons)

**Audience:** Internal team

**Scope:** Current one-player-versus-CPU prototype; input layer + HUD only

**Priority:** Critical — the game is not playable as a wrestling game

## Problem

The tap/hold grammar puts a timing test on every offensive action:

- Standing strikes fire on *release* and require a deliberate hold for heavy —
  in a fast exchange the most common action in the game has built-in latency
  and a failure mode (accidental heavy).
- Grapples require two sequential timing decisions: K to lock, then a second
  tap-vs-hold resolution inside a 1.8 s lock that the opponent can escape —
  the core wrestling verb is the most stressful input in the game.

The AKI research is unambiguous: complexity belongs in *context*, never in
input. One press = one move; direction is the only modifier; the weak/strong
choice happens once, at tie-up initiation, by whether the button is still
held while the wrestlers lock up.

## The grammar

```text
J            press fires instantly, always
             neutral          → light strike
             + held direction → heavy strike
             contextual families (ground / corner / rope / rebound / running)
             keep precedence and also fire on press

K            press = tie-up attempt, instantly
             release before the wrestlers lock  → QUICK tie-up
             still held as the lock forms       → STRONG tie-up
             in lock: K (+ held direction) fires the selected family's
             move instantly on press — quick set or power set by tie-up
             strength; the HUD names the armed strength
             beside a downed opponent: tap = pin, hold = submission
             (unchanged; deliberate, low-frequency actions)

Space / ; / L / T   reversal, dodge, special, taunt — unchanged
```

Lock pressure is removed: `GrappleLock` timeout 1.8 s → 2.5 s. The defender's
early escape window is unchanged.

## Implementation constraints

- `WrestlerCombat`, validation, move data, and CPU behavior are untouched —
  the CPU already calls the directional APIs directly.
- `PressTracker` remains for pin/submission only; strikes and lock moves
  never consult it.
- Tie-up strength is controller state, resolved once per lock
  (~0.28 s sample window), shown in the lock prompt
  ("[K]+direction: Power Grapple").
- Prompts, controls panel, checklist, and docs updated in the same change.

## Acceptance

- Every offensive press produces its action on the press frame (or buffers
  visibly into the next legal frame); nothing in the offense waits for
  release.
- Tie-up strength is deterministic, readable on the HUD before the follow-up,
  and never mis-fires the other set.
- One press still never fires two actions; pause/resume still fires nothing.
- Full parity: every move reachable before remains reachable.
