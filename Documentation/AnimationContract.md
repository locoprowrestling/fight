# Combat Animation Contract

Animation presents resolved gameplay. It does not decide combat outcomes,
resource changes, timing, or authoritative wrestler positions.

## Semantic Events

| Event | Roles | Loop | Root movement | Marker use | Exit |
| --- | --- | --- | --- | --- | --- |
| Basic reversal | defender + attacker sell | no | none | audio/VFX only | valid combat states |
| Strong reversal | defender counter + attacker sell | no | visual root only | camera/audio/VFX | authored stagger |
| SPECIAL ready | owning wrestler | optional idle overlay | none | audio/VFX | momentum below full |
| Submission apply | attacker + defender | no | scripted gameplay roots | pose sync only | hold loop |
| Submission struggle | defender | yes | none | audio/VFX only | escape/break/tap |
| Rope break | both | no | scripted separation | audio/VFX | recovery/downed |
| Submission escape | both | no | scripted separation | audio/VFX | recovery/downed |
| Tap-out | defender | no | none | audio/VFX | defeat |

## Authority Rules

- Animation markers cannot apply damage or resolve gameplay.
- Root motion never owns authoritative positions.
- `MoveData` and gameplay systems own timing.
- Attacker and defender clips must share named synchronization markers.
- `WrestlerMotor`, `WrestlerCombat`, and `SubmissionSystem` own movement,
  snapping, separation, and state transitions.
- `IAnimationDriver` implementations may select clips, set parameters, adjust
  playback speed, and emit presentation-only audio, VFX, or camera cues.

## Paired Clip Contract

Each paired move must define:

- The authoritative move or special identifier.
- Attacker and defender Animator state names.
- Matching start anchors and facing.
- Shared marker names for contact, impact, release, and pose synchronization.
- A shared authored duration or compatible playback-speed range.
- The expected gameplay state for each wrestler on exit.
- Valid interruption points and the fallback presentation state.

Markers align presentation tracks. Gameplay still applies hits, stamina costs,
momentum, reversals, submissions, and exits from its own data-driven timeline.

## Animator Mapping

A future `AnimatorAnimationDriver` remains an adapter behind
`IAnimationDriver`:

| Semantic input | Animator mapping |
| --- | --- |
| `PlayMove(animationStateName, ..., speed)` | Cross-fade to the state named by `MoveData` or special data and set a bounded playback-speed parameter. |
| `PlayState(stateName)` | Map the gameplay state to a presentation state without changing gameplay state. |
| `TriggerReversal(strong, presentationId)` | Select basic or strong paired reactions and route the identifier to presentation effects. |
| `SetSpecialReady(ready)` | Toggle a persistent overlay or layer weight only on readiness transitions. |
| Submission trigger methods | Enter paired apply, struggle, release, rope-break, escape, or tap-out presentation states. |
| Movement and reaction methods | Set locomotion parameters or one-shot presentation triggers. |

Animator parameter and state names must be centralized and validated against
the assigned controller. Unknown move identifiers or missing states must
produce actionable editor validation instead of silently changing gameplay.

The files under `examplecode/` are informational references only. They are not
production source, do not compile with the game, and must not become a second
gameplay authority.
