# Best Practices

The rules this codebase depends on. Each one exists because breaking it produces
a specific, known failure — noted as the "why".

## Data

**Code is the source of truth; assets are a cache.** All game data (moves,
stats, specials, traits, roster, rules, difficulty) is defined in
[DefaultGameData.cs](../../Assets/Scripts/Roster/DefaultGameData.cs). The
`.asset` files under `Assets/Resources/LoCoData/` are serialized copies made by
**Tools > LoCo Fight Game > Create Default Prototype Assets**. After editing
`DefaultGameData.cs`, regenerate the assets. _Why:_
`RosterLoader`/`GameBootstrap` prefer saved assets over the in-code factory, so
stale assets silently shadow your code change — the game runs, your numbers
don't.

**Don't hand-edit generated assets for values that exist in code.** Tuning in
the Inspector is fine for a quick experiment, but port the final value back into
`DefaultGameData.cs` before committing, then regenerate.

## States

**Behavior rules live in `StateProfile`, not scattered if-checks.** Every
`WrestlerState` declares what's allowed in it (move/attack/grapple/reverse,
pinnable/grabbable/strikable, timeout, exit state) in `BuildProfiles()` in
[WrestlerStateMachine.cs](../../Assets/Scripts/Wrestlers/WrestlerStateMachine.cs).
Consumers check `core.States.Profile`, never the state enum directly (except for
genuinely state-specific logic).

**Check role-specific branches before generic capability gates.** A controller
that early-outs on `!Profile.canAttack` will never reach logic for states where
acting is the _point_ but the profile says "can't attack" (e.g. the grapple-lock
attacker choosing a follow-up — see the
[lockup-loop postmortem](Examples.md#postmortem-the-endless-lockup-loop)).
_Why:_ capability flags describe the state, not the role; the attacker and
defender share `GrappleLock` but only one of them may act.

**Every mutual/locking state needs three things:** a timeout in its profile, an
owner responsible for resolving it, and a cleanup path if it dissolves
externally. `GrappleLock` has all three: the 1.8 s profile timeout, the
attacker's follow-up (AI falls back quick↔power and force-releases on failure),
and the stale-role safety net in `WrestlerCombat.Update()`. _Why:_ any one of
these missing turns a dropped edge case into a soft-lock.

**An escape must change the situation, not rewind it.** A successful escape
(kickout, reversal, wake-up) has to reposition the loser, protect the winner's
recovery, or both — kicking out of a pin shoves the attacker away with extra
recovery; `GettingUp` is unstrikable/ungrabbable; mashing while downed shortens
the timer. Audit any escape by asking "what stops the attacker from
immediately re-applying the same hold?" — if the answer is "nothing," it is a
loop, and the player will find it as pin → kickout → can't stand → pin.
_Why:_ symmetric escapes that return both wrestlers to the pre-escape position
make losing states inescapable in practice even when every individual rule is
fair.

## Control symmetry

**New combat actions go through
[WrestlerCombat.cs](../../Assets/Scripts/Combat/WrestlerCombat.cs).**
`PlayerInputController` (human) and `CPUWrestlerAI` (CPU) drive the exact same
API. Never implement an action inside the input controller or the AI directly.
_Why:_ otherwise one side can do things the other can't, and balance/QA
assumptions break.

**Input grammar: one physical press produces at most one move.** Direction is
the spatial modifier. A standing grapple press must persist through acquisition:
capture its direction, resolve tap or hold once, and keep the result pending
until the lock consumes it. Reserve movement input while a target is in grapple
range, and clear smoothed locomotion whenever the state profile disables
movement.
_Why:_ resetting the press at acquisition makes quick/power require an
undocumented second input; letting approach velocity survive `GrappleLock`
slides directional attempts out of the paired animation.

**Make sure every action is reachable from the states players actually occupy.**
The chase is where grapples happen, and `Running` had `grapple: false` — a
thousand K presses died silently. When adding or gating an action, list the
states a player realistically initiates it from and test from each.
_Why:_ a capability gate that excludes the dominant approach state turns a
core verb into a coin-flip.

## Combat architecture

These practices are consistent with the external research, but they are listed
here because they were separately validated against this game's current code.
This section, not the research document, is authoritative.

**Controllers choose actions; gameplay systems resolve them.** Human input and
CPU logic may decide what to attempt, but legality, contests, damage, stamina,
momentum, and state changes remain in `WrestlerCombat` and the relevant combat
system. Animation and UI present results rather than deciding them. _Why:_ one
resolution path prevents player/CPU drift and keeps presentation changes from
altering gameplay.

**Use the existing authority for each rule.** State permissions belong in
`StateProfile`; rope and corner queries belong in `RingInteractionSystem`; match
flow and rules belong in `MatchManager`/`MatchRulesData`; move properties belong
in `MoveData`; character variation belongs in stats, traits, specials, and AI
data. Add a new authority only when no current owner fits. _Why:_ scattering the
same decision across controllers, executors, and UI creates contradictory
behavior that is difficult to tune or test.

**Prefer reusable data and capabilities over wrestler-id branches.** Shared
combat logic should ask what a wrestler, move, trait, special, or ruleset can do
instead of checking a specific roster id. Character-specific behavior should
enter through the existing data or executor extension points. _Why:_ the roster
already shares one combat engine; capability-driven variation keeps new
wrestlers from multiplying special cases.

**Treat position and result state as part of a move outcome.** When adding or
changing a move, verify not only damage but the attacker/defender states,
recovery ownership, downed or stunned result, and rope/corner interaction.
_Why:_ those outcomes determine the next legal action and are therefore
gameplay, not presentation detail.

**Keep moves in context-specific database categories.** `MoveDatabase` separates
light strikes, heavy strikes, quick grapples, power grapples, running attacks,
and ground submissions. Add a new context as an explicit category and resolver
path; do not flatten the moveset into one universal list and reconstruct context
through scattered filtering. _Why:_ the category is the contract between
input/AI intent and legal move selection, and prevents standing, running,
grapple, and grounded actions from being mixed accidentally.

**Every constrained move needs a compatibility contract and a failure path.**
Requirements such as running state, rope/corner context, lift class, target
weight, and grapple role belong in `MoveData` or the owning resolver. A failed
attempt must return cleanly, release any lock it owns, or execute an explicit
fallback such as `fallbackMoveIfLiftFails`. _Why:_ silent no-ops and partially
entered states create stuck wrestlers, unclear controls, and animation/gameplay
disagreement.

**New contextual families resolve context through `CombatContextResolver`.**
Context (grapple lock, ground upper/lower, corner, rope stagger, rebound,
standing) is computed transiently when an action is attempted — never cached as
a second persistent state and never re-derived ad hoc in controllers. Geometry
questions still go to `RingInteractionSystem`, state questions to
`WrestlerStateMachine`. _Why:_ one resolution priority keeps player and CPU
agreeing about which family applies and avoids stale-context execution.

**Validate structurally before spending resources.** Every contextual request
goes through a `ContextualMoveValidator` method that returns a
`MoveValidationResult` (validity, `MoveRejectionReason`, debug message) before
any stamina is spent or temporary state taken. Stamina gates use
`MovePacingRules.RequiredStamina` (max of cost and minimum); only `staminaCost`
is spent. Intentional resolved failures (failed lifts) may still consume
resources. _Why:_ invalid attempts must be free and explainable, and player/CPU
must hit the identical rejection path.

**One physical press resolves at most one action.** Tap/hold buttons go through
`PressTracker` (tap on release before `PlayerInputLogic.HoldThreshold`, hold
committed at the crossing, nothing on release after a hold), and pause / match
end / reset call `Reset()` on every tracker plus `InputBuffer.Clear()` so
nothing fires on resume. _Why:_ double-fired or resumed presses read as phantom
inputs and are nearly impossible to QA after the fact.

**One direction frame.** Directional input is camera-mapped to a world vector
first (exactly like locomotion) and only then classified against a wrestler's
facing (`PlayerInputLogic.ResolveMoveDirection`). Never interpret raw stick axes
against a wrestler's local axes directly. _Why:_ with a moving camera, "up" on
the stick is only meaningful on screen; mixing frames makes identical inputs do
different things in different systems.

**HUD prompts are presentation-only.** `ControlPromptLogic` maps the resolved
combat context to labels; nothing may read prompt strings back into gameplay,
and a wrong prompt is a bug in the mapping, never in combat. _Why:_ the prompt
exists to make context-sensitive controls legible; letting it gate behavior
would create a second, contradictory authority.

**A failed AI attempt must yield control back to Decide.** `CPUWrestlerAI.Act`
runs every frame; any case that calls a `Try*` and then `Rethink()`
unconditionally will, when the `Try*` fails silently, re-arm the decision timer
forever and suppress `Decide()` entirely (the FSM freezes in that state until an
external event flips it). On failure, transition out
(`CurrentState = AIState.IdleThink` or a deliberate fallback state) so Decide
runs on schedule; `Rethink()` belongs to _successful or state-changing_ actions
only. Two lockup soft-locks have now come from this family: the capability-gate
ordering in Decide (2026-06-10) and AttemptGrapple re-attempting into its own
lock while spamming Rethink (2026-06-11) — in both, the repeating
`[Grapple] ... locks up` cadence matching the lock timeout was the diagnostic
signature. _Why:_ a suppressed Decide turns every silent `Try*` failure into a
permanent behavior freeze that manual QA only catches when the player happens to
stay passive.

**Record every contextual request for F1.** Use the `RecordContext` funnel in
`WrestlerCombat` so the overlay shows context, zone, direction, family,
candidate count, selection, tier, validation result, and fallback use; log
rejections. _Why:_ "nothing happened" is the costliest bug class in contextual
input — the overlay must always be able to say why.

**Gameplay data owns effects and timing; animation only presents them.**
`MoveData` and combat routines define startup, active, recovery, damage,
reversal windows, stamina, momentum, and result states.
`IAnimationDriver.PlayMove()` receives presentation identifiers and speed but
does not decide whether or when a move hits. _Why:_ placeholder poses and future
animation clips can be replaced without changing combat behavior.

**Keep roster presentation separate from combat configuration.** `RosterEntry`
owns selection-facing data such as portrait and view prefab, while
`WrestlerDefinition` owns stats, moveset, specials, and traits. Do not clone or
alter combat data solely to create a different portrait or future attire. _Why:_
visual variants should not fork wrestler balance or behavior.

**Getting hit interrupts the victim's in-flight action.** `ApplyHit` (and the
contextual hit appliers) must cancel the defender's running move coroutine when
`StateProfile.canBeInterrupted` allows it; armored states (specials, airborne)
opt out via the flag. Exchanges are decided by whose active frame lands first.
_Why:_ without this, a struck wrestler's attack executes straight through the
hitstun — hits visibly "don't register" and hitstun/stagger data is dead
weight.

**A flag without a consumer is a lie.** `canBeInterrupted` existed on every
state profile for the project's whole life with zero readers — the design said
hits interrupt, the code never did. When adding any capability/permission
flag, add (or point to) its consumer in the same change; when auditing a
behavior bug, grep the relevant flag for readers before trusting it.
_Why:_ unconsumed flags make the data model claim guarantees the runtime
doesn't provide, and they pass every review that doesn't grep.

**Scaling actors means scaling every distance constant that touches them.**
`WrestlerView.GlobalScale` drives the rig and CharacterController; grapple/
strike/pin/submission ranges, contextual move ranges, tie-up snap distance,
knockback, and camera look-at height are all body-relative and must move with
it (capsules collide at the sum of radii — range minus that sum is the real
usable margin). The 1.25× scale-up silently cut grapple margin ~40% and made
tie-ups nearly unlandable.
_Why:_ distance constants encode body size invisibly; scaling one side turns
tuned mechanics into broken ones with no error anywhere.

## Presentation isolation

**Only [WrestlerView.cs](../../Assets/Scripts/Wrestlers/WrestlerView.cs) and
`IAnimationDriver` implementations touch meshes or animation.** Gameplay
reads/writes the wrestler root transform and calls `core.Anim.*`; it never
reaches into body parts. _Why:_ real art replaces the placeholder rig without
touching combat/AI/rules
([FutureAssetIntegration.md](../FutureAssetIntegration.md)).

**An Animator driver is the single writer for Animator parameters, not a second
gameplay controller.** Adapt the facade pattern from
[WrestlerAnimationController.cs](../../examplecode/round%2001/WrestlerAnimationController.cs)
inside the future `AnimatorAnimationDriver : IAnimationDriver`: cache parameter
hashes and keep all `Animator.Set*`/`CrossFade` calls there. Do not copy its
momentum, reversal, submission, pin, or match-outcome decisions. Those already
belong to stats, combat, state, and match systems. _Why:_ "single source of
truth for animation state" is useful only within presentation; applying it to
gameplay creates two authorities that can disagree.

**The gameplay clock owns animation timing.** Author or speed-scale clips to
`MoveData.startupTime`, `activeTime`, and `recoveryTime`; never wait on
`AnimatorStateInfo.length`, normalized time, or a clip-complete callback before
applying damage or leaving a gameplay state. Animation Events may request
presentation effects (sound, VFX, camera punch), but may not activate hit
logic, tick submission pressure, award momentum, or decide a referee count.
_Why:_ clips must remain replaceable without changing balance, and interrupted
animations must not leave a gameplay coroutine waiting forever.

**Keep gameplay-root motion off by default.** `WrestlerMotor`,
`WrestlerCombat`, and special executors own the root transform,
`CharacterController`, snapping, knockback, rope movement, and scripted
positioning. Import locomotion and combat clips in-place unless a future
root-motion bridge explicitly consumes and validates Animator delta movement
through that authority. Root motion on a visual child may be used only when it
is reset/recentered and cannot alter gameplay distance. _Why:_ the sample
[clip manifest](../../examplecode/round%2001/WrestlerAnimationManifest.md) marks many
traveling clips as root-motion clips, but applying those recommendations
directly would bypass collision, ring bounds, and combat positioning.

**A wrestling move is an attacker/defender synchronization contract.** Plan
paired clips with role, facing, start anchors, contact/impact/release markers,
result pose, interruption behavior, and a shared authored duration. Snap or
script the gameplay roots before playback; do not rely on two unrelated clips
to find each other. _Why:_ the sample manifest is a useful clip inventory but
mostly lists one state per move, which is insufficient for a stable two-person
grapple.

**Treat Animator names as a validated data contract.** Keep state and parameter
names centralized, hash parameters once, and validate every non-empty
`MoveData.animationStateName`/special state against the assigned controller
before entering Play mode or building. _Why:_ stringly typed missing states
otherwise degrade into silent fallback animation while gameplay continues.

**Generated Animator Controllers are reproducible scaffolds, not hand-authored
assets.** Editor builders belong under `Assets/Scripts/Editor/`, use the
project's **Tools > LoCo Fight Game/** menu, write to an explicitly generated
path, and must be safe to run repeatedly without silently destroying manual
clip assignments or transitions. Keep the clip manifest beside the controller
contract and update both in one change. _Why:_ the example builder is valuable
for repeatable parameters/states, but its current create-at-path workflow is
not a safe regeneration pipeline for a controller that artists also edit.

**Override stable presentation slots, not gameplay input slots.** Round 2's
[slot override pattern](../../examplecode/round%2002/WrestlerMoveSet.cs) is
useful because one base controller can support wrestler-specific clips, but
`QuickFront[0]` is not a durable animation identity in this project. Bind clips
to semantic keys derived from the authoritative move or event plus role, such
as `body-slam/attacker`, `body-slam/defender`, or `reversal-strong/receiver`.
_Why:_ directional buckets and movesets can be retuned without invalidating
presentation assets.

**Per-wrestler animation variation belongs to roster presentation data.** A
future `WrestlerAnimationProfile` should be referenced by `RosterEntry`, beside
the view prefab and portrait, while `WrestlerDefinition` and `MoveData` remain
the shared gameplay configuration. _Why:_ changing Cody's body-slam clip must
not fork the body-slam's damage, timing, legality, or AI behavior.

**Build and apply clip overrides once per spawned visual.** Create one
`AnimatorOverrideController` while binding the wrestler prefab, validate every
replacement against the base controller, retain that controller for the
wrestler's lifetime, and never rebuild the override map per move or per frame.
_Why:_ per-action override mutation adds allocations and makes concurrent
attacker/defender playback difficult to reason about.

**Motion-library descriptions are animation briefs, not move data.** Round 2's
[move library](../../examplecode/round%2002/WrestlerMoveLibrary.md) usefully
records grips, weight shifts, body mechanics, landing orientation, and search
terms. Preserve those details in an animation brief keyed to the existing
`MoveData.moveId`; do not create a second `WrestlerMove` gameplay asset or copy
suggested duration, momentum, knockdown, and root-motion values over the
authoritative data. _Why:_ reference observations help source and review clips,
but duplicating mechanics guarantees drift.

**Visual parts carry no colliders.** The `CharacterController` on the wrestler
root is the only physics volume, sized from `WrestlerView.RigHeight` ×
`HeightFor(weight)` with radius scaled by `BulkFor(weight)` in
`WrestlerCore.Create`. _Why:_ limb colliders would fight the CharacterController
and make hit detection frame-dependent; ranges are data (`MoveData.range`),
distance checks are flat transform-to-transform (`HitboxProbe`).

**Scale the visual root uniformly only.** Weight-class bulk is applied per-mesh
(x/z), never as a non-uniform `visualRoot.localScale`. _Why:_ non-uniform scale
on a parent shears rotated child joints — bent elbows/knees visibly distort.

**Feel effects extend, defer, and switch off.** `FeelSystem` (hit-stop, camera
punch) is presentation-only and self-bootstrapping: hit-stop _extends_ the
current freeze rather than stacking (rapid lights never become slow motion),
always yields to the pause system's ownership of `Time.timeScale`, and the
whole system is toggleable with zero gameplay diff. Defender reactions (mat
bounce) are pose overlays — the gameplay root never moves.
_Why:_ global effects like timescale are shared state; an impact system that
fights pause or accumulates becomes a gameplay bug wearing a polish costume.

## Single authorities

- Rope math:
  [RingInteractionSystem.cs](../../Assets/Scripts/Ring/RingInteractionSystem.cs)
  — nothing else computes rope distances/directions.
- Match flow: [MatchManager.cs](../../Assets/Scripts/Match/MatchManager.cs)
  (`Instance` singleton) — gate all combat on
  `MatchManager.Instance.IsCombatAllowed`, null-tolerant (`MatchActive` pattern
  in `WrestlerCombat`).
- Wrestler wiring: subsystems are created and bound in `WrestlerCore.Create()` —
  add new per-wrestler components there, nowhere else.

## Unity specifics

- Unity 6.4 (6000.4.x): use `FindAnyObjectByType`, not `FindFirstObjectByType`
  (deprecated in 6.4, warns).
- Input is the **legacy Input Manager** (`Input.GetKeyDown`); don't introduce
  the new Input System.
- One assembly, one namespace (`LoCoFight`), no asmdefs — keep it that way until
  compile times hurt.
- Editor-only code goes under `Assets/Scripts/Editor/`; menu items under
  **Tools > LoCo Fight Game/**.

- Set **Preferences > General > Script Changes While Playing** to
  "Recompile After Finished Playing" on every dev machine. This project wires
  subsystems at runtime via `Bind()` with non-serialized (often interface)
  references; a mid-play domain reload nulls all of it and every hot `Update`
  throws. With an agent committing code during playtests this fires constantly.

- The `SceneOrientationGizmo.SetupCamera` `NullReferenceException` that floods
  the console on open scenes is a Unity 6 editor bug in Unity's own DLL (the
  stack trace shows `/Users/bokken/…` — Unity's build-server path, not yours).
  It does not affect Play mode or builds; ignore it.

## Legacy Input Manager — controller support

**Give joystick axes their own names; never share with keyboard axes.** The
named axes `Joy_Horizontal` and `Joy_Vertical` (type 2, joystick axis 0/1,
Y-inverted, `joyNum: 0`) exist for exactly this. Do not add a joystick type-2
entry under the same `Horizontal`/`Vertical` names as keyboard — `GetAxisRaw`
with duplicate names returns the maximum absolute value across all entries, which
is correct but opaque and breaks any future per-device feature. _Why:_ device
isolation is needed for rebinding, per-player split, and reliable device
detection.

**D-pad support requires two tiers.** D-pad appears as analog HAT axes 5/6
(Xbox/Mac HID) on some platform/driver combinations, and as
`JoystickButton14–17` (XInput digital D-pad) on others. Read the axis pair
first (`Mathf.Abs > 0.5f`); fall back to the button set. `DPad_Horizontal`
(axis 5) and `DPad_Vertical` (axis 6) are already configured in
`InputManager.asset`. _Why:_ no single representation covers every platform; the
two-tier read is the only portable approach without leaving the legacy Input
Manager.

**Controller device detection must cover analog-stick activity, not only button
presses.** A player navigating with the stick alone (no button press) should
flip the HUD to controller prompts. Check stick magnitude against the dead-zone
threshold (`stickMove.sqrMagnitude > StickDeadZone * StickDeadZone`) in addition
to `HasGamepadButtonDown()`. _Why:_ prompts that lag one button press behind the
active device are visibly wrong every session.

**Movement priority: left stick > D-pad > keyboard.** This is the convention
established in `LegacyPlayerInputSource` — merge at the call-site and feed one
`Vector2 move` into the rest of the frame. Never blend two sources; pick the
dominant one by non-zero magnitude. _Why:_ blending produces unexpected diagonal
drift when multiple devices are partially active.

**Current controller layout (Xbox / PS):**

| Button              | Xbox    | PlayStation | Action           |
| ------------------- | ------- | ----------- | ---------------- |
| JoystickButton0     | A       | Cross       | Grapple / Tie-up |
| JoystickButton1     | B       | Circle      | Dodge            |
| JoystickButton2     | X       | Square      | Strike           |
| JoystickButton3     | Y       | Triangle    | Special          |
| JoystickButton4     | LB      | L1          | Run              |
| JoystickButton5     | RB      | R1          | Reversal         |
| JoystickButton7     | Start   | Options     | Pause / Reset    |
| Left stick / D-pad  | —       | —           | Move             |

## Logging

Use the established bracketed tags so the console reads as a match transcript:
`[Move]`, `[Grapple]`, `[Reversal]`, `[Rope]`, `[AI]`, `[Match]`, `[Bootstrap]`.
One line, present tense, names included
(`[Grapple] JT Staten locks up with Zeak Gallent`). _Why:_ the lockup-loop bug
was diagnosed from log cadence alone — uniform logs make timing bugs visible.

**Expose hidden state in
[DebugOverlay.cs](../../Assets/Scripts/UI/DebugOverlay.cs).** When adding a
stateful mechanic, show the minimum information needed to explain its current
decision: active state, timer/progress, selected action or move, eligibility,
and rejection reason where available. _Why:_ combat timing, rope eligibility, AI
intent, reversal windows, pins, and submissions cannot be diagnosed reliably
from animation alone.

**Every failed player action says why, on screen.** Contextual rejections
surface through `MatchHUD.TryShowActionFeedback` (via
`ControlPromptLogic.RejectionText`), and plain `Try*` failures the chain can't
explain (grapple out of range, ungrabbable opponent, lost tie-up) toast
directly. A player-triggered `return false` with no log and no HUD line is a
bug factory — three separate "controls are broken" reports in this project
were silent-failure paths (running grapple gate, stamina lock release, range
margins).
_Why:_ players cannot distinguish "illegal right now" from "broken," and
neither can QA without evidence.

**Animation follows the semantic combat contract.** Gameplay systems and
`MoveData` own timing, outcomes, and authoritative roots; animation drivers
only map resolved events to paired clips, parameters, and presentation markers
([AnimationContract.md](../AnimationContract.md)).

## Verification

There is no CLI build or test suite. Three layers, cheapest first:

1. **Offline compile check** (catches errors without switching to Unity):
   compile all non-Editor scripts with the dotnet SDK's Roslyn against Unity's
   reference DLLs — full command in
   [Examples.md](Examples.md#offline-compile-check). Don't run Unity in batch
   mode while the editor is open.
2. **In-editor**: tab back to Unity, let it compile, press Play. F1 debug
   overlay, F2 roster selector, R reset.
3. **Manual QA**: run the relevant section of
   [TestingChecklist.md](../TestingChecklist.md); add a checklist line for every
   behavior bug you fix.
