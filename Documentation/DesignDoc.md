# LoCo Fight Game — Design Document (Prototype)

## Goal

A compact but complete 3D wrestling match loop:

neutral movement → strike / run / dodge / grapple / position control → hit,
miss, lock-up, reversal, rope interaction → damage, stun, rope stagger, downed,
pin/submission setup → follow-up, special, pin or submission attempt → win/loss.

Scope: 1 player vs 1 CPU, one match at a time, win by pinfall or submission. No
multiplayer, creation suite, campaign, weapons, entrances, or online.

## Core architecture

Gameplay is fully separated from presentation:

| Layer            | Components                                                                                                                                                                                     |
| ---------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Identity / data  | `RosterEntry`, `RosterDatabase`, `WrestlerDefinition`, `WrestlerStatsData`, `MoveData`, `MoveDatabase`, `SpecialAbilityData`, `PassiveTraitData`, `MatchRulesData`, `AIDifficultyData`         |
| Wrestler runtime | `WrestlerCore` (root), `WrestlerMotor`, `WrestlerStateMachine`, `WrestlerCombat`, `WrestlerStatsRuntime`, `BuffDebuffController`, `PassiveTraitController`, `SpecialController`, `DodgeSystem` |
| Presentation     | `WrestlerView` (primitive body), `IAnimationDriver` / `PlaceholderAnimationDriver`                                                                                                             |
| Arena            | `ArenaRig` (typed anchors/zones), `RingBoundary`, `RingInteractionSystem` (the only rope-math authority)                                                                                       |
| Match            | `MatchManager`, `PinSystem`, `SubmissionSystem`, `RefereeCountSystem`, `MatchRulesData`                                                                                                        |
| Control          | `PlayerInputController` + `InputBuffer` (human), `CPUWrestlerAI` + `AISpecialPlanner` + `AIMemory` (CPU) — both consume the same `WrestlerCombat` API                                          |
| UI               | `MatchHUD` (runtime-built uGUI), `DebugOverlay` (F1), `RosterSelectDebug` (F2)                                                                                                                 |

`GameBootstrap` builds the entire scene procedurally so the prototype runs from
a single component in an empty scene.

## Wrestler states

The `WrestlerStateMachine` defines 40+ states (Idle through Victory/Defeat).
Every state has a `StateProfile` declaring: can move / rotate / attack / grapple
/ reverse / dodge, can be pinned / submitted / grappled / struck, rope
interaction, climb, interruptibility, default timeout, and the exit state.
Examples enforced by profiles:

- Downed wrestlers can't strike; Pinned wrestlers can't move; StrikeRecovery
  blocks instant re-attack.
- RopeTrapLocked and airborne wrestlers cannot be normally reversed.
- Victory/Defeat are terminal.

## Meters

- **Health** (100): reduced by moves; 0 health doesn't end the match — it makes
  kickouts and submission escapes collapse.
- **Stamina** (100, 12/sec recovery modified by traits/buffs): spent on every
  action; gates heavy/power/special moves; drained by running and holding
  submissions.
- **Momentum** (0–100): gained on offense/reversals; specials require full
  momentum. Full momentum creates persistent SPECIAL readiness rather than a
  temporary combat state; readiness remains until a validated activation
  spends momentum. Failed validation spends neither momentum nor stamina, and
  successful activation spends each resource exactly once.

## Combat resolution

- Strikes: startup → active (range + facing-dot hit check) → recovery, all from
  `MoveData`. Knockback can produce **Stunned**, **RopeStaggered**, or
  **Cornered** depending on where the defender ends up.
- Grapples: tie-up lock with attacker/defender roles. Simultaneous attempts
  within 0.2 s resolve 40 % timing / 40 % stamina ratio / 20 % random
  (documented in `CombatResolver.ResolveGrappleTie`). From the lock: quick
  grapple, power grapple (lift-validated), or special.
- Lift rules: attacker lift class must cover defender weight class; Johnny
  Crash's Heavyweight Anchor vetoes lifts from non-heavyweights (failed lift =
  stamina loss, stun, defender momentum).
- Reversals: one reversal button plus an optional camera-relative direction.
  Valid timing and sufficient stamina always produce a basic reversal; a
  direction matching the move's authored read upgrades it to a strong reversal
  with more momentum, stagger, separation, and presentation. Neutral or
  incorrect reads remain basic. Failed timing or affordability changes no
  wrestler state and spends no stamina. Player and CPU call the same resolver
  and runtime API.
- Dodges: universal sidestep; The Vigilante's data-driven Vanishing Dodge can
  escape lifts, carries, combos, traps, and running attacks during early phases,
  with a once-per-match emergency version.

## Contextual combat

Wrestler state and ring position produce distinct offensive families. Context is
resolved transiently by `CombatContextResolver` at the moment an action is
attempted (never stored as a second persistent state), with fixed priority:

1. Active grapple lock
2. Downed target (ground upper/lower by attacker position along the defender's
   facing axis)
3. Cornered target inside a corner zone
4. Rope-staggered target near a rope
5. Attacker in a rebound state
6. Standing

Families in `MoveDatabase`: `groundUpperAttacks`, `groundLowerAttacks`,
`directionalQuickGrapples` / `directionalPowerGrapples` (neutral / forward /
backward / lateral buckets with neutral fallback), `cornerStrikes`,
`cornerGrapples`, `ropeStaggerAttacks`, `ropeReboundAttacks`.

- **Ground offense**: J near a downed target picks the upper or lower family
  from the attacker's position; standing strikes still whiff on downed targets;
  pins (I) and submissions (O) are unchanged. Ground hits never reset the
  defender's downed timer, so recovery can't be suppressed.
- **Directional grapples**: in a lock, the held movement direction (facing-
  relative, dead-zoned) selects the bucket for L (quick) or K (power); an empty
  bucket falls back to neutral; an empty neutral releases the lock cleanly.
- **Corner offense**: requires both the Cornered state and live corner-zone
  geometry. Each move ends in a documented result (remain cornered / downed
  toward ring center); the defender keeps a reversal window and the timeout
  escape.
- **Rope offense**: rope-stagger attacks require a rope-staggered target near a
  rope (`RingInteractionSystem` stays the only rope authority); the dedicated
  rebound family requires an active rebound state and stays distinct from
  ordinary running attacks.

Every contextual request goes through `ContextualMoveValidator` before any
stamina is spent and returns a structured result (validity, rejection reason,
debug message), recorded in a `CombatContextSnapshot` shown in the F1 overlay.

**Controls** follow the AKI grammar — one press = one move, direction is the
only modifier; nothing in the offense waits for a button release. Strike (J/X)
fires on press: neutral = light, held direction = heavy, contextual families by
precedence. Tie-up/Control (K/A) locks up on press; the initiating press picks
the set — released before the wrestlers lock = quick, still held as the lock
forms (~0.28 s sample) = STRONG/power — and inside the lock, K + held direction
fires the armed set's move instantly. The lock lasts 2.5 s so the attacker never
races a clock. Beside a downed opponent K keeps tap = pin / hold = submission
(`PressTracker`, `PlayerInputLogic.HoldThreshold` 0.18 s — the only remaining
tap/hold). Special (L/Y), Dodge (;/B), Reversal (Space/RB). A ~0.35 s input
buffer carries presses across recovery frames, and the HUD names the armed lock
strength, confirms each started move, and explains rejected presses
(`MatchHUD.TryShowActionFeedback`, `ControlPromptLogic.RejectionText`). All
directional input uses one frame: the stick maps through the camera into the
world (like locomotion) and is then classified against the attacker's facing, so
pushing toward the opponent on screen is always Forward. A bottom-center HUD
prompt (`ControlPromptLogic`, presentation-only) always names what both buttons
will do in the current context, with device-aware glyphs.

**Move tiers** (`MoveTier`) regulate pacing without a match-phase system:

| Tier    | Cost                   | Recovery          | Intended use          |
| ------- | ---------------------- | ----------------- | --------------------- |
| Light   | Low                    | Short             | Frequent setup        |
| Medium  | Moderate               | Moderate          | Sustained control     |
| Heavy   | High                   | Long              | Escalation and payoff |
| Special | Existing special rules | Category-specific | Peak offense          |

`MovePacingRules.RequiredStamina` gates attempts on the greater of `staminaCost`
and `minimumStamina`; only `staminaCost` is spent. Light offense stays available
at low stamina; heavy moves need a real stamina commitment; specials keep their
momentum rules. Player and CPU share the same gates.

## Pins, submissions, referee

- **Pins**: count at 1/2/3 s; human defender mashes (required effort scales with
  health, stamina, kickout skill, recent damage, penalties); CPU evaluates a
  documented chance formula every 0.25 s. Rope break interrupts when
  `MatchRulesData` allows.
- **Submissions**: pressure vs escape race to 100 with rope breaks, ramping
  pressure, stamina drains, and per-special pressure rates. The defender can
  move the attached pair toward the nearest rope; direct movement is strongest,
  sideways movement is reduced, and movement away does nothing. Low stamina
  weakens both crawl speed and active escape effort. When rope breaks are
  disabled, crawl intent contributes reduced escape meter instead. Every exit
  clears submission ownership and scripted movement through one cleanup path.
- **Referee five-count**: UI/system referee for illegal rope holds (The
  Tarantula); auto-release at 5 under standard rules.

## Match rules

`MatchRulesData` presets: Standard (rope breaks + five-count), No Rope Breaks,
Hardcore. The rope trap special changes behavior per preset — see
[RopeMechanics.md](RopeMechanics.md).

## CPU AI

`CPUWrestlerAI` is a reaction-delay-gated FSM (Approach, Circle, BackOff,
strike/grapple attempts, rope/corner herding, special setup, pin/submission,
defense). `AISpecialPlanner` knows how to position each special archetype (climb
a corner for aerials, stand at the head for JT, back off for Johnny's charge,
herd to ropes for Morgana). `AIMemory` cools down repeated actions so the CPU
doesn't spam. Difficulty data controls aggression, reversal/dodge accuracy,
reaction delay, kickout and escape bonuses.

Difficulty and personality are separate. Difficulty owns reaction timing,
accuracy, and defensive bonuses. `AIPersonalityProfile` applies bounded
preference multipliers to valid action families such as strikes, grapples,
power moves, ground offense, submissions, pins, specials, and rope/corner
strategy; it never changes accuracy, damage, health, stamina, or timing.
Attempt and success memory penalizes repeated families, with successful
repetition penalized more heavily. Failed actions always return control to the
decision loop.

Contextual priorities (same `WrestlerCombat` API as the player):

- Downed target: credible pin or submission → ground attack → reposition/wait.
- Cornered target: corner grapple or strike (by `cornerStrategyPreference` and
  tier-pacing affordability) → normal fallback; never herds an already-cornered
  opponent.
- Rope-staggered target: rope-context attack → normal fallback.
- Grapple-lock attacker: directional quick or power follow-up (power gated by
  `MovePacingRules.CanAttempt`) → release the lock if nothing resolves.
- Rebound state: dedicated rebound attack → ordinary running attack.

## Semantic presentation

Gameplay systems emit `CombatPresentationEvent` values only after an outcome is
resolved. `FeelSystem`, `IAnimationDriver`, and HUD effects consume light,
medium, heavy, basic-reversal, strong-reversal, special, submission-release,
rope-break, tap-out, and SPECIAL-ready events without changing gameplay.
Disabling `FeelSystem` changes no state, timing, position, damage, or resource
result. Animation markers may synchronize paired clips and request audio, VFX,
or camera cues, but `MoveData` and gameplay systems remain authoritative for
timing and roots. See [AnimationContract.md](AnimationContract.md).

## Match flow

Loading → Ready → (HandshakeSequence if Zeak is present) → Active →
Pin/Submission/RefereeCounting interludes → Finished → press R → Resetting
(scene reload; debug roster selection persists via statics).
