# LoCo Fight Game — Design Document (Prototype)

## Goal

A compact but complete 3D wrestling match loop:

neutral movement → strike / run / dodge / grapple / position control → hit, miss, lock-up, reversal, rope interaction → damage, stun, rope stagger, downed, pin/submission setup → follow-up, special, pin or submission attempt → win/loss.

Scope: 1 player vs 1 CPU, one match at a time, win by pinfall or submission. No multiplayer, creation suite, campaign, weapons, entrances, or online.

## Core architecture

Gameplay is fully separated from presentation:

| Layer | Components |
|---|---|
| Identity / data | `RosterEntry`, `RosterDatabase`, `WrestlerDefinition`, `WrestlerStatsData`, `MoveData`, `MoveDatabase`, `SpecialAbilityData`, `PassiveTraitData`, `MatchRulesData`, `AIDifficultyData` |
| Wrestler runtime | `WrestlerCore` (root), `WrestlerMotor`, `WrestlerStateMachine`, `WrestlerCombat`, `WrestlerStatsRuntime`, `BuffDebuffController`, `PassiveTraitController`, `SpecialController`, `DodgeSystem` |
| Presentation | `WrestlerView` (primitive body), `IAnimationDriver` / `PlaceholderAnimationDriver` |
| Arena | `ArenaRig` (typed anchors/zones), `RingBoundary`, `RingInteractionSystem` (the only rope-math authority) |
| Match | `MatchManager`, `PinSystem`, `SubmissionSystem`, `RefereeCountSystem`, `MatchRulesData` |
| Control | `PlayerInputController` + `InputBuffer` (human), `CPUWrestlerAI` + `AISpecialPlanner` + `AIMemory` (CPU) — both consume the same `WrestlerCombat` API |
| UI | `MatchHUD` (runtime-built uGUI), `DebugOverlay` (F1), `RosterSelectDebug` (F2) |

`GameBootstrap` builds the entire scene procedurally so the prototype runs from a single component in an empty scene.

## Wrestler states

The `WrestlerStateMachine` defines 40+ states (Idle through Victory/Defeat). Every state has a `StateProfile` declaring: can move / rotate / attack / grapple / reverse / dodge, can be pinned / submitted / grappled / struck, rope interaction, climb, interruptibility, default timeout, and the exit state. Examples enforced by profiles:

- Downed wrestlers can't strike; Pinned wrestlers can't move; StrikeRecovery blocks instant re-attack.
- RopeTrapLocked and airborne wrestlers cannot be normally reversed.
- Victory/Defeat are terminal.

## Meters

- **Health** (100): reduced by moves; 0 health doesn't end the match — it makes kickouts and submission escapes collapse.
- **Stamina** (100, 12/sec recovery modified by traits/buffs): spent on every action; gates heavy/power/special moves; drained by running and holding submissions.
- **Momentum** (0–100): gained on offense/reversals; specials require full momentum and spend it all.

## Combat resolution

- Strikes: startup → active (range + facing-dot hit check) → recovery, all from `MoveData`. Knockback can produce **Stunned**, **RopeStaggered**, or **Cornered** depending on where the defender ends up.
- Grapples: tie-up lock with attacker/defender roles. Simultaneous attempts within 0.2 s resolve 40 % timing / 40 % stamina ratio / 20 % random (documented in `CombatResolver.ResolveGrappleTie`). From the lock: quick grapple, power grapple (lift-validated), or special.
- Lift rules: attacker lift class must cover defender weight class; Johnny Crash's Heavyweight Anchor vetoes lifts from non-heavyweights (failed lift = stamina loss, stun, defender momentum).
- Reversals: timed windows defined per move; costs 8/12/18 stamina (strike/grapple/special) modified by traits and buffs; human cooldown 0.35 s, CPU cooldown by difficulty.
- Dodges: universal sidestep; The Vigilante's data-driven Vanishing Dodge can escape lifts, carries, combos, traps, and running attacks during early phases, with a once-per-match emergency version.

## Pins, submissions, referee

- **Pins**: count at 1/2/3 s; human defender mashes (required effort scales with health, stamina, kickout skill, recent damage, penalties); CPU evaluates a documented chance formula every 0.25 s. Rope break interrupts when `MatchRulesData` allows.
- **Submissions**: pressure vs escape race to 100 with rope breaks, ramping pressure, stamina drains, and per-special pressure rates.
- **Referee five-count**: UI/system referee for illegal rope holds (The Tarantula); auto-release at 5 under standard rules.

## Match rules

`MatchRulesData` presets: Standard (rope breaks + five-count), No Rope Breaks, Hardcore. The rope trap special changes behavior per preset — see [RopeMechanics.md](RopeMechanics.md).

## CPU AI

`CPUWrestlerAI` is a reaction-delay-gated FSM (Approach, Circle, BackOff, strike/grapple attempts, rope/corner herding, special setup, pin/submission, defense). `AISpecialPlanner` knows how to position each special archetype (climb a corner for aerials, stand at the head for JT, back off for Johnny's charge, herd to ropes for Morgana). `AIMemory` cools down repeated actions so the CPU doesn't spam. Difficulty data controls aggression, reversal/dodge accuracy, reaction delay, kickout and escape bonuses.

## Match flow

Loading → Ready → (HandshakeSequence if Zeak is present) → Active → Pin/Submission/RefereeCounting interludes → Finished → press R → Resetting (scene reload; debug roster selection persists via statics).
