# Examples & Postmortems

Short, real, dated. Newest first within each section.

## Postmortem: the punch that didn't stop anything

_2026-06-11 — fixed in
[WrestlerCombat.cs](../../Assets/Scripts/Combat/WrestlerCombat.cs)_

**Symptom:** "I hit a move and the CPU immediately starts pummeling me as if I
hadn't just hit them." Hits applied damage and set `Stunned`, but the CPU's
offense never paused.

**Root cause:** getting hit never interrupted the victim's in-flight move. The
attack *coroutine* kept executing — it set `StrikeActive` on its next phase,
stomping the fresh `Stunned` state, and landed its hit anyway. The
`canBeInterrupted` flag existed on every state profile and had **zero
consumers** for the project's entire life.

**Fix:** `ApplyHit` and the contextual hit appliers call
`defender.Combat.InterruptMove()` when the profile allows; armored states opt
out via the flag. Hitstun durations were then raised so the existing stagger
pose actually plays.

**Diagnosis clue worth remembering:** the state machine and the coroutines are
two parallel sources of truth — `States.Current` can say `Stunned` while a
coroutine is mid-flight and about to overwrite it. When behavior contradicts
the visible state, look for a running coroutine.

**Prevention:** "Getting hit interrupts" and "a flag without a consumer is a
lie" in [BestPractices.md](BestPractices.md#combat-architecture).

## Postmortem: a thousand failed grapples

_2026-06-11 — fixed in
[WrestlerStateMachine.cs](../../Assets/Scripts/Wrestlers/WrestlerStateMachine.cs) /
[WrestlerCombat.cs](../../Assets/Scripts/Combat/WrestlerCombat.cs)_

**Symptom:** grapple attempts felt like they never connected; the player
reported "1000 failed times."

**Root causes (three, stacked):**

1. `Running` had `grapple: false` — every K pressed while chasing (the
   dominant approach state) silently died, and the 0.35 s buffer expired
   before the player stopped running.
2. The 1.25× `GlobalScale` body change shipped without scaling distance
   constants: capsules now met at ~0.88 apart while `GrappleRange` stayed
   1.25, leaving ~0.37 of usable margin against a circling target.
3. Every failure path returned `false` with no feedback, so 1 + 2 read as
   "the button is broken" — including legitimately *lost tie-ups* (the
   simultaneous-grapple resolver making the player the defender).

**Fix:** grapple legal from `Running`; all combat ranges scaled with the
bodies; every failure toasts a reason ("Too far away" / "Can't grab them right
now" / "Out-wrestled in the tie-up!").

**Prevention:** "actions reachable from the states players occupy," "scaling
actors means scaling every distance constant," and "every failed player action
says why" in [BestPractices.md](BestPractices.md).

## Postmortem: the pin-spam loop

_2026-06-11 — fixed in
[PinSystem.cs](../../Assets/Scripts/Combat/PinSystem.cs) /
[WrestlerStateMachine.cs](../../Assets/Scripts/Wrestlers/WrestlerStateMachine.cs)_

**Symptom:** CPU pins → player kicks out → player "can't stand up" → CPU pins
again, indefinitely.

**Root cause:** a kickout set the defender `Downed` for another 0.9 s and the
attacker into only 0.6 s of recovery — the pinner always acted first. Then
`GettingUp` (0.7 s, no move/reverse/dodge) was freely strikable, so at low
health any heavy re-downed the riser on the spot. Every rule was individually
defensible; together the escape returned the loser to the losing state.

**Fix:** the kickout shoves the attacker away (1.4 m + 1.0 s recovery) while
the defender rises from a short 0.45 s downed state; `GettingUp` grants
strike/grab immunity; mashing while downed shaves the timer (with
`ExtendTimeout` clamped so it can't fall through to the profile default).

**Prevention:** "an escape must change the situation, not rewind it" in
[BestPractices.md](BestPractices.md#states).

## Postmortem: the endless lockup loop

_2026-06-10 — fixed in
[CPUWrestlerAI.cs](../../Assets/Scripts/AI/CPUWrestlerAI.cs)_

**Symptom:** match soft-locked when the CPU grappled; console showed
`[Grapple] JT Staten locks up with Zeak Gallent` repeating every ~2 s, forever.

**Root cause:** `Decide()` early-returned on `!Profile.canAttack` before
reaching the `InGrappleLockAsAttacker` branch. The `GrappleLock` profile sets
`canAttack: false` (correct — you can't strike mid-lock), so the CPU attacker
went to `Recover`, the 1.8 s lock timed out, both wrestlers reset to Idle, and
the AI immediately re-grappled. The bug was _ordering_, not the flag: capability
gates describe the state for everyone, but the attacker role had a legal
follow-up action the gate hid.

**Fix:** check the role-specific branch first; in `Act()`, fall back quick↔power
grapple and `ReleaseGrapple()` if neither executes, so a lock can never dangle.

**Diagnosis clue worth remembering:** the ~2 s log cadence matched lock timeout
(1.8 s) + AI reaction delay exactly — uniform `[Tag]` logging turned a "game
frozen" report into a readable timing signature.

**Prevention:** the "role-specific before capability gates" and "three things
every locking state needs" rules in [BestPractices.md](BestPractices.md#states),
plus a regression line in [TestingChecklist.md](../TestingChecklist.md).

**Sequel (2026-06-11):** the loop returned with a different mechanism — after a
successful `TryGrappleAttempt`, `Act()` stayed in `AttemptGrapple` and called
`Rethink()` every frame, permanently deferring `Decide()` so the fix above
never ran. It only reproduced with a *passive* player (any attack trips a
defensive reaction that unfreezes the FSM), which is why active-play QA passed.
Rule: `Rethink()` is for successful or state-changing actions only; a failed
`Try*` must transition out — see "A failed AI attempt must yield control back
to Decide" in [BestPractices.md](BestPractices.md).

## Worked example: the articulated humanoid rig

_2026-06-10 —
[WrestlerView.cs](../../Assets/Scripts/Wrestlers/WrestlerView.cs) +
[PlaceholderAnimationDriver.cs](../../Assets/Scripts/Animation/PlaceholderAnimationDriver.cs)_

The placeholder body is a joint hierarchy of empty pivots (pelvis → spine →
neck; shoulders → elbows; hips → knees) with primitive meshes offset by half
their length under each pivot, so rotating a pivot bends the body at the joint.
The animation driver computes a full-body `BodyPose` every frame (state base +
walk cycle + one-shot overlay) and eases joints toward it with
`1 - exp(-14 · dt)`.

### Joint sign conventions

Limbs hang along local −Y with pivots axis-aligned to the body, so consistent
rules apply (left/right mirror on Z):

| Rotation               | Effect                                                               |
| ---------------------- | -------------------------------------------------------------------- |
| `shoulder.x = -90`     | arm straight forward; `-180` straight up; `+40` behind the back      |
| `elbow.x` negative     | elbow flexes (forearm toward chest); `0` straight                    |
| `hip.x` negative       | leg swings forward; positive = backward                              |
| `knee.x` positive      | knee flexes (shin/boot swing back); never negative                   |
| `shoulder.z`           | arm out to the side: negative on the left arm, positive on the right |
| `spine.y` / `pelvis.x` | torso twist / whole-upper-body lean                                  |

### The lying-pose trick

`visualRoot` pivots at the feet, so a ±88° root tilt alone would sprawl the body
~1.8 m off its collider. `LyingPose()` compensates with `shift` (slide the root
along z so the body re-centers over the CharacterController — `+0.85` on the
back, `-0.85` face-down) and `lift` (`+0.16` so the torso rests on the mat
instead of sinking in). Gameplay distances are unaffected: they use the wrestler
root transform, which never moves.

### Scaling rule

Weight class scales mesh x/z per part (`BulkFor`) and the root _uniformly_
(`HeightFor`). A non-uniform root scale shears rotated child joints — if a bent
elbow ever looks smeared, look for a non-uniform scale upstream.

## Worked example: adapting Round 1 animation references

_2026-06-11 —
[runtime facade](../../examplecode/round%2001/WrestlerAnimationController.cs),
[controller builder](../../examplecode/round%2001/WrestlerAnimatorBuilder.cs),
and
[clip manifest](../../examplecode/round%2001/WrestlerAnimationManifest.md)_

The sample is a planning/reference pack, not drop-in game code. Its useful
parts and required adaptations are:

| Source pattern | Decision for this repo | Reason |
| --- | --- | --- |
| One component owns all `Animator.Set*` calls | **Keep** inside `AnimatorAnimationDriver : IAnimationDriver` | Preserves the existing presentation boundary and makes parameter changes local. |
| `Animator.StringToHash` for stable parameters | **Keep**, plus startup/editor validation | Efficient and typo-resistant after the names have been validated. |
| An editor script creates parameters, states, and transitions | **Adapt** to `Assets/Scripts/Editor/`, the LoCo menu, and a generated-only output path | Reproducible setup is useful; overwriting an artist-edited controller is not. |
| A clip manifest records state name, asset name, loop, motion, duration, and intent | **Keep**, expanded with attacker/defender roles, sync markers, result states, and source/license | Turns animation sourcing into a reviewable asset contract. |
| The animation component owns momentum, reversal success, submissions, pins, and match results | **Reject** | Existing gameplay systems already own those decisions. |
| Animation Events activate hitboxes, submission ticks, move completion, or referee counts | **Reject** for gameplay; **keep** for audio/VFX/camera cues | `MoveData` and combat coroutines own authoritative timing. |
| Root motion drives locomotion, grapples, rebounds, and finishers | **Reject by default** | `WrestlerMotor`, combat snapping, and special executors own gameplay-root movement. |
| One clip/state represents a grapple move | **Expand to a synchronized pair** | Both wrestlers need compatible tracks, anchors, markers, interruption, and result poses. |
| A flat state hard-coded for every moveset slot | **Use only as an early scaffold** | Production states may be generated from semantic move ids, but the gameplay moveset must not be encoded as fixed Animator slot choices. |

Example adaptation for a powerbomb:

1. `MoveData` remains authoritative for startup, impact, recovery, damage,
   stamina, downed result, and interruption.
2. The animation manifest defines `powerbomb_attacker` and
   `powerbomb_defender`, a shared duration, front-grapple anchors, and
   presentation markers such as `impact`.
3. Combat snaps both gameplay roots into a valid pair before playback, then
   calls each wrestler's `IAnimationDriver`.
4. The driver plays or crossfades both clips at a speed that fits
   `MoveData.TotalDuration`; it does not wait for clip completion.
5. The combat coroutine applies the hit on its active phase. An `impact`
   animation event may add sound, VFX, or camera punch, but cannot apply the
   hit again.

## Worked example: adapting Round 2 move overrides

_2026-06-11 —
[move metadata](../../examplecode/round%2002/WrestlerMove.cs),
[per-wrestler moveset](../../examplecode/round%2002/WrestlerMoveSet.cs),
[slot controller builder](../../examplecode/round%2002/WrestlerAnimatorBuilder.cs),
and [move library](../../examplecode/round%2002/WrestlerMoveLibrary.md)_

Round 2 solves a real content problem: multiple wrestlers can share one
Animator graph while replacing move clips per character. The implementation
shape needs adaptation:

| Round 2 pattern | Decision for this repo | Adaptation |
| --- | --- | --- |
| `AnimatorOverrideController` per wrestler | **Keep** | Create and apply it once when binding a real wrestler visual. |
| Placeholder clips as override keys | **Keep** | Generate stable semantic keys from `moveId + participant role`, not directional slot numbers. |
| `WrestlerMoveSet` owns 20 gameplay slots | **Reject** | `MoveDatabase` and `DirectionalMoveSet` already own legal move selection. |
| `WrestlerMove` duplicates duration, momentum, knockdown, and orientation | **Reject** | Keep those fields in `MoveData`; animation profiles contain clips and presentation metadata only. |
| `LastExecutedMove` lets gameplay read animation metadata | **Reject** | Gameplay already knows the current `MoveData`; presentation must remain downstream. |
| Camera shake event from animation completion | **Adapt** | Route presentation-only markers to `FeelSystem`; impact tier still comes from resolved combat. |
| Detailed body-mechanics catalogue | **Keep** | Convert each useful entry into an animation brief keyed to the existing move id and paired roles. |

For the current `body-slam` vertical slice, the stable bindings should be
`body-slam/attacker` and `body-slam/defender`. Every wrestler may point those
keys at different clips, while both bindings still use the same
`MoveData.TotalDuration`, impact phase, downed result, and lift validation.

## Offline compile check

No CLI build exists, and Unity batch mode is unsafe while the editor is open.
This compiles every non-Editor script against Unity 6000.4.10f1's reference
assemblies in ~2 s (exit 0 + warnings only = good):

```bash
UNITY=/Applications/Unity/Hub/Editor/6000.4.10f1/Unity.app/Contents/Resources/Scripting
ROSLYN=$(ls -d /usr/local/share/dotnet/sdk/*/Roslyn/bincore/csc.dll | tail -1)
RSP=/tmp/loco_check.rsp; : > $RSP
echo "-nologo -nostdlib+ -target:library -out:/tmp/loco_check.dll -langversion:9.0 -nowarn:0169,0414" >> $RSP
echo "-r:$UNITY/NetStandard/ref/2.1.0/netstandard.dll" >> $RSP
for d in $UNITY/Managed/UnityEngine/*.dll; do echo "-r:$d" >> $RSP; done
echo "-r:Library/ScriptAssemblies/UnityEngine.UI.dll" >> $RSP
find Assets/Scripts -name "*.cs" -not -path "*/Editor/*" >> $RSP
dotnet "$ROSLYN" @$RSP
```

Gotcha that motivated the response file: passing a multi-line file list as a
shell variable mangles it into one giant "filename" (CS1504).

### Editor + test variant

To also type-check `Assets/Scripts/Editor/` (asset builder, validators, NUnit
edit-mode tests), extend the same response file with the netfx mscorlib shim and
the nunit package DLL, and compile **all** of `Assets/Scripts`:

```bash
echo "-r:$UNITY/NetStandard/compat/2.1.0/shims/netfx/mscorlib.dll" >> $RSP
echo "-r:$(ls -d Library/PackageCache/com.unity.ext.nunit@*/net40/unity-custom/nunit.framework.dll | tail -1)" >> $RSP
find Assets/Scripts -name "*.cs" >> $RSP
```

Two gotchas: do **not** also reference `$UNITY/Managed/UnityEditor.dll` — the
`Managed/UnityEngine/*.dll` glob already pulls in `UnityEditor.CoreModule` and
the duplicate types collide (CS0433); and the net40 nunit DLL needs that
mscorlib shim or every `[Test]` attribute fails with CS0012.
