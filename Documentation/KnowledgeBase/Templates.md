# Templates

Copy-paste recipes for the common additions. All data lives in
[DefaultGameData.cs](../../Assets/Scripts/Roster/DefaultGameData.cs); after any
data change, regenerate assets via **Tools > LoCo Fight Game > Create Default
Prototype Assets**.

## New wrestler

In `CreateRoster()`:

```csharp
Add(set, "tas-new-wrestler", "New Wrestler", new Color(0.5f, 0.5f, 0.5f),   // placeholder body color
    Stats(set, "New", WeightClass.Middleweight, LiftStrengthClass.Average,  // weight drives rig bulk + CC size
        AIPersonality.Balanced, reversal: 0.5f, dodge: 0.5f, kickout: 0.5f, subResist: 0.5f),
    NewWrestlerSpecial(set),   // factory below; one special per wrestler
    NewWrestlerTraits(set));   // or null if no passive traits
```

Checklist beyond the code:

1. Portrait: drop `tas-new-wrestler.png` in `players-web/`, run the importer
   (`RosterAssetImporter`) so it lands in `Assets/Art/RosterPortraits/`.
   `RosterEntry.sourceImageFileName` is derived from the roster id.
2. Regenerate assets (menu above) — otherwise the saved `RosterDatabase` asset
   won't contain the new entry.
3. Document the character in [Roster.md](../Roster.md) and the special in
   [SpecialAbilities.md](../SpecialAbilities.md).
4. Add a [TestingChecklist.md](../TestingChecklist.md) line if the wrestler has
   unique mechanics.

## New move

In `CreateMoveDatabase()` — use `Move` for strikes (explicit phases) or
`Grapple` for grapples (one duration split 30/40/30):

```csharp
// Move(set, id, name, category, dmg, stamina, startup, active, recovery, stun, momentum, ...)
db.heavyStrikes.Add(Move(set, "spinning-backfist", "Spinning Backfist", MoveCategory.HeavyStrike,
    10, 11, 0.40f, 0.12f, 0.55f, 0.6f, 9, tags: new[] { MoveTag.Clean }));

// Grapple(set, id, name, category, dmg, stamina, totalDuration, stun, momentum, downed:, canPin:, lift:, ...)
db.powerGrapples.Add(Grapple(set, "powerbomb", "Powerbomb", MoveCategory.PowerGrapple,
    17, 21, 1.5f, 0f, 14, downed: 2.1f, canPin: true, lift: true,
    tags: new[] { MoveTag.Clean, MoveTag.Lift, MoveTag.Major }));
```

Notes:

- Range, reversal window, `requiresRunning`, and `placeholderPoseName` are
  derived from the category inside `Move()` — only override on the returned
  instance for exceptions (see `big-boot`'s `downsBelowHealthPercent`).
- `lift: true` moves are gated by `LiftStrengthClass` vs `WeightClass`
  (`CombatResolver.ValidateLift`); set `fallbackMoveIfLiftFails` or accept the
  fail-and-stun path.
- Both player and CPU pick moves via `MoveDatabase.Random*` — adding to the
  right list is the whole integration.
- Put the move in the database list matching its legal context. A new context
  needs an explicit list/resolver path, not inclusion in a loosely related
  existing list.
- Declare every compatibility requirement the resolver needs: running, grapple
  role, lift strength, rope/corner state, target state, or other positional
  constraint.
- Verify the failure path as well as the success path: insufficient stamina,
  invalid context, failed lift, missed hit, and interrupted grapple must leave
  both wrestlers in valid states.
- Keep damage, timing, stamina, momentum, reversal windows, and state
  consequences in move/combat data. Animation names and placeholder poses are
  presentation hooks only.

## New contextual move

Contextual families (ground, corner, rope stagger, rebound, directional
grapples) follow the same `Move`/`Grapple` factories plus a compatibility
contract and a database family:

```csharp
// Ground attack — zone-gated against a downed defender:
var axeHandle = Move(set, "ground-axe-handle", "Axe Handle", MoveCategory.GroundUpperAttack,
    7, 8, 0.30f, 0.12f, 0.50f, 0f, 7, tags: new[] { MoveTag.Clean, MoveTag.Ground, MoveTag.GroundUpper });
axeHandle.tier = MoveTier.Medium;            // pacing class
ConfigureGroundAttack(axeHandle, GroundTargetZone.Upper); // requiresTargetDowned + zone + pose + range
db.groundUpperAttacks.Add(axeHandle);

// Directional grapple — assign a bucket; neutral is the required fallback:
db.directionalQuickGrapples.forward.Add(snapmare);
```

Required compatibility fields by family (validated in
`ContextualMoveValidator`):

- Ground: `requiresTargetDowned`, `requiredGroundZone` (Upper/Lower)
- Corner: `requiresTargetCornered`, `requiresCornerZone`
- Rope stagger: `requiresTargetRopeStaggered`, `requiresOpponentNearRopes`
- Rebound: `requiresRopeRebound`
- Heavy tier: set `minimumStamina` (gate only — `staminaCost` is what's spent)

Checklist before calling it done:

1. The move sits in exactly one contextual family list (or directional bucket);
   directional sets keep a non-empty neutral bucket (`MoveDataValidator` errors
   otherwise).
2. Validation rejects each missing requirement with the right
   `MoveRejectionReason` and spends no stamina (watch F1 while testing).
3. The result state is documented and applied (remain in context, stunned,
   downed, or exit) and an interrupted attempt leaves both wrestlers in valid,
   timeout-recoverable states.
4. The CPU can reach the move through the shared `WrestlerCombat` API (it must
   not need controller-specific code).
5. Regenerate assets via **Tools > LoCo Fight Game > Create Default Prototype
   Assets** and check the console for `[MoveData]` errors/warnings.

## New special

Factory next to the other `*Special(set)` methods:

```csharp
static SpecialAbilityData NewWrestlerSpecial(DefaultGameDataSet set)
{
    var s = Special(set, "special-id", "Display Name", "tas-new-wrestler", SpecialCategory.Rush, stamina: 25, damage: 22);
    // then category-specific fields, e.g. for a counter:
    // s.counterWindow = 0.75f; s.requiresOpponentStanding = true;
    return s;
}
```

Existing categories each have an executor in `Assets/Scripts/Specials/`
(`RushSpecialExecutor`, `AerialSpecialExecutor`, `CounterSpecialExecutor`,
`DirtySpecialExecutor`, `RopeTrapSpecialExecutor`, `SequenceSpecialExecutor`).
For a **new category**: add the enum value, write a
`NewCategoryExecutor : SpecialExecutor`, wire it in `SpecialController`, and
document it in [SpecialAbilities.md](../SpecialAbilities.md). Specials must
respect reversal windows (`SpecialController.ReversalWindowOpen`) so the
defender always has counterplay.

## New passive trait

```csharp
// Trait(set, id, name, ownerRosterId, effectType, value:, tier1:, tier2Threshold:, tier2Value:, momentum:, duration:, once:, ui:)
Trait(set, "iron-chin", "Iron Chin", "tas-new-wrestler",
    PassiveTraitEffectType.DamageReductionBelowHealth, value: 0.15f, tier1: 30f,
    ui: "Iron Chin: taking less damage!");
```

If the effect type is new, add it to `PassiveTraitEffectType` and handle it in
[PassiveTraitController.cs](../../Assets/Scripts/Traits/PassiveTraitController.cs)
— that controller is the only consumer.

## New wrestler state

1. Add the enum value in
   [WrestlerStateMachine.cs](../../Assets/Scripts/Wrestlers/WrestlerStateMachine.cs).
2. Add its `StateProfile` in `BuildProfiles()` — decide every flag deliberately,
   and give any non-terminal state a `timeout` + `exit` so it can't strand a
   wrestler:

```csharp
d[WrestlerState.NewState] = P(rotate: true, strikable: true, timeout: 1.0f, exit: WrestlerState.Idle);
```

1. Give it a visual: add a `case "NewState":` pose in `ComputePose()` in
   [PlaceholderAnimationDriver.cs](../../Assets/Scripts/Animation/PlaceholderAnimationDriver.cs)
   (unhandled states fall back to locomotion/stand).
2. If it's a mutual state (two wrestlers locked together), apply the
   three-things rule from [BestPractices.md](BestPractices.md#states): timeout,
   owner, external-dissolve cleanup.

## New animation pose

Poses are `BodyPose` cases in `ComputePose()` — local Euler targets per joint
plus `root` tilt, `lift` (y), `shift` (z). Joint sign conventions are in
[Examples.md](Examples.md#joint-sign-conventions); start from a helper
(`StandPose()`, `FightStance()`, `Crouch()`, `LyingPose()`) and override:

```csharp
case "NewState":
    p = FightStance();
    p.spine = new Vector3(20f, 0f, 0f);            // bend forward 20°
    p.lShoulder = new Vector3(-90f, 0f, -10f);     // left arm straight forward
    p.lElbow = new Vector3(-45f, 0f, 0f);          // elbow bent 45°
    p.lift = -0.10f;                               // crouch: lower the root with bent knees
    break;
```

One-shot gestures (a punch, a reach) are `ActionKind` overlays in
`ApplyAction()`, not states — they ride on top of whatever pose is active and
expire on their own.

## New move silhouette (placeholder overlay)

Every move family should be visually identifiable. Three pieces, all in
[PlaceholderAnimationDriver.cs](../../Assets/Scripts/Animation/PlaceholderAnimationDriver.cs)
plus one data line:

```csharp
// 1. ActionKind enum: add the overlay kind.
enum ActionKind { ..., OverheadSlam, SnapThrow, Chop, Lariat, Stomp, MatBounce }

// 2. PlayMove(): map the pose name to the overlay (duration / speed).
case "slam":
    StartAction(ActionKind.OverheadSlam, 1.0f / Mathf.Max(0.5f, speed));
    break;

// 3. ApplyAction(): the pose math. 'w' is the ramp-hold-ease envelope,
//    't' is normalized time; use SmoothStep sub-phases for wind-up vs delivery.
case ActionKind.OverheadSlam:
{
    float lift = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.4f));
    float slam = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 0.5f) / 0.25f));
    // ... joint targets via Vector3.Lerp(p.x, target, w) — see existing cases.
    break;
}
```

```csharp
// 4. DefaultGameData: name the pose on the move, then REGENERATE ASSETS.
bodySlam.placeholderPoseName = "slam";
```

Rules: overlays never move the gameplay root (visual `lift`/`shift` only — see
`MatBounce`); joint sign conventions are in [Examples.md](Examples.md);
durations are presentation-only and must not be load-bearing for combat
timing. Impact weight (hit-stop / camera punch) comes from
`FeelSystem.NotifyImpact(tier, downs)` in the combat hit applier, not from the
animation.

## Real Animator driver

Adapt the parameter-facade pattern from
[WrestlerAnimationController.cs](../../examplecode/round%2001/WrestlerAnimationController.cs),
but implement the existing interface and keep gameplay decisions outside it:

```csharp
using UnityEngine;

namespace LoCoFight
{
    [RequireComponent(typeof(Animator))]
    public sealed class AnimatorAnimationDriver : MonoBehaviour, IAnimationDriver
    {
        static readonly int MoveSpeed = Animator.StringToHash("MoveSpeed");
        static readonly int MovePlaybackSpeed = Animator.StringToHash("MovePlaybackSpeed");
        static readonly int HitReact = Animator.StringToHash("HitReact");

        Animator _animator;

        void Awake()
        {
            _animator = GetComponent<Animator>();
            _animator.applyRootMotion = false; // WrestlerMotor owns the gameplay root.
        }

        public void PlayMove(string stateName, string placeholderPoseName, float speed = 1f)
        {
            if (string.IsNullOrWhiteSpace(stateName)) return;
            // Configure move states to use this parameter as their speed multiplier.
            _animator.SetFloat(MovePlaybackSpeed, Mathf.Max(0.01f, speed));
            _animator.CrossFadeInFixedTime(stateName, 0.05f);
        }

        public void PlayState(string stateName) { /* map WrestlerState names to presentation states */ }
        public void SetMovementSpeed(float speed) => _animator.SetFloat(MoveSpeed, speed);
        public void TriggerHitReact() => _animator.SetTrigger(HitReact);
        public void TriggerReversal(bool strong, string presentationId) { }
        public void TriggerDodge() { }
        public void TriggerDowned() { }
        public void TriggerGetUp() { }
        public void TriggerRopeStagger() { }
        public void TriggerCornered() { }
        public void TriggerAerialLaunch() { }
        public void TriggerAerialLanding(bool hit) { }
        public void TriggerSpecial(string specialId) { }
        public void SetSpecialReady(bool ready) { }
        public void TriggerSubmissionApply(bool attacker) { }
        public void TriggerSubmissionStruggle() { }
        public void TriggerSubmissionRelease(bool ropeBreak, bool escaped) { }
        public void TriggerSubmissionTapOut() { }
    }
}
```

Before replacing the placeholder driver:

1. Put the component and `Animator` on the wrestler visual prefab.
2. Keep the `CharacterController` and gameplay root outside the animated
   skeleton.
3. Validate required Animator parameters and all non-empty move/special state
   names in editor code; do not silently accept missing states.
4. Replace the driver wiring in
   [WrestlerCore.cs](../../Assets/Scripts/Wrestlers/WrestlerCore.cs) without
   changing combat, AI, state, or match callers.
5. Verify pause, interruption, knockback, rope rebound, reset, and two-wrestler
   alignment with root motion disabled.

Do not copy momentum fields or methods such as `AttemptReversal`,
`TryExecuteFinisher`, or `AttemptPinfall` from the sample controller. They are
gameplay operations in this project.

## Animation clip manifest entry

Start from the inventory idea in
[WrestlerAnimationManifest.md](../../examplecode/round%2001/WrestlerAnimationManifest.md),
but use one row per participant track:

```markdown
| Move ID | Role | Animator State | Clip Asset | Loop | In Place | Duration | Start Anchor | Markers | Result State | Interrupts | Source / License |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| powerbomb | Attacker | Move.powerbomb.attacker | powerbomb_attacker | No | Yes | 1.50 s | Front grapple A | contact 0.30, impact 1.05, release 1.15 | Recovery | Hit before lift | Unverified - do not ship |
| powerbomb | Defender | Move.powerbomb.defender | powerbomb_defender | No | Yes | 1.50 s | Front grapple B | contact 0.30, impact 1.05, release 1.15 | Downed | Hit before lift | Unverified - do not ship |
```

Manifest rules:

- `Move ID` must match the authoritative move data.
- Paired tracks have the same authored duration and marker meanings.
- `In Place` is normally `Yes`; root movement remains scripted by gameplay.
- Markers are presentation synchronization points. Combat timing still comes
  from `MoveData`.
- Record import rig/avatar, source file, creator, and license before an asset
  is considered usable.
- Include locomotion, reactions, grounded loops, transitions, entrances, and
  outcomes, not only offensive moves.

## Generated Animator Controller

The sample
[WrestlerAnimatorBuilder.cs](../../examplecode/round%2001/WrestlerAnimatorBuilder.cs)
shows the basic `UnityEditor.Animations` APIs. A project builder should:

1. Live under `Assets/Scripts/Editor/` and use
   `[MenuItem("Tools/LoCo Fight Game/Build Generated Wrestler Animator")]`.
2. Write to a clearly generated path such as
   `Assets/Animations/Generated/Wrestler.controller`.
3. Define parameter names once in a shared contract used by both builder and
   runtime driver.
4. Create broad presentation states and transitions from the manifest/data;
   do not hard-code the playable moveset as 20 fixed grapple slots.
5. Assign clips explicitly and report missing clips as actionable validation
   errors.
6. Be idempotent: rebuilding produces the same graph and never overwrites a
   controller intended for manual editing.
7. Add an editor test that builds into a temporary path and asserts required
   parameters, states, transitions, and clip assignments.

## Per-wrestler animation profile

Adapt Round 2's
[Animator override approach](../../examplecode/round%2002/WrestlerMoveSet.cs)
without duplicating gameplay moves:

```csharp
[System.Serializable]
public struct AnimationClipBinding
{
    public string semanticKey; // e.g. "body-slam/attacker"
    public AnimationClip clip;
}

[CreateAssetMenu(menuName = "LoCo Fight Game/Wrestler Animation Profile")]
public sealed class WrestlerAnimationProfile : ScriptableObject
{
    public WrestlerAnimatorContract contract;
    public Avatar avatar;
    public List<AnimationClipBinding> clips = new List<AnimationClipBinding>();
}
```

Profile rules:

1. Reference the profile from `RosterEntry`, not `WrestlerDefinition`.
2. Use semantic keys from authoritative ids plus role; never use array position
   or directional input as the identity.
3. Keep only presentation data in the profile: controller, clips, avatar/rig
   compatibility, and optional presentation notes.
4. Build one `AnimatorOverrideController` during driver binding and retain it.
5. Reject duplicate keys, null clips, unknown keys, missing required paired
   roles, and clips incompatible with the prefab avatar.
6. Missing optional bindings fall back to the base controller's placeholder;
   missing required bindings block validation for that profile.

## Move animation brief

Use the detailed body-mechanics observations in Round 2's
[move library](../../examplecode/round%2002/WrestlerMoveLibrary.md), but key the
brief to the existing move:

```markdown
# body-slam animation brief

- Authoritative move: `MoveData.moveId = body-slam`
- Roles: `body-slam/attacker`, `body-slam/defender`
- Start: front grapple, roots aligned by combat at the standard grapple offset
- Attacker mechanics: right arm under near leg, left arm around upper back,
  lift opponent horizontal, quarter-turn, controlled back-first plant
- Defender mechanics: brace at contact, assisted lift, horizontal suspension,
  back-first landing, settle into the authored downed pose
- Shared markers: `contact`, `lift`, `impact`, `release`
- Exit presentation: attacker recovery; defender face-up downed
- Interrupt rule: gameplay may interrupt before `lift`; both tracks cross-fade
  to their current gameplay-state presentation
- Root policy: in-place clips; gameplay roots remain authoritative
- Timing source: `MoveData.TotalDuration` and phase fields
- Asset provenance: source file, creator, license, import avatar, revision
```

Do not copy `clipDuration`, momentum, knockdown, landing state, or root-motion
flags from the reference library into a second move asset. Compare the brief
against `MoveData` and resolve conflicts in the authoritative gameplay data.

## New InputManager axis entry

Add directly to `ProjectSettings/InputManager.asset` inside `m_Axes:`. Minimal
joystick axis template (raw analog, dead zone handled in code):

```yaml
- serializedVersion: 3
  m_Name: Joy_AxisName
  descriptiveName:
  descriptiveNegativeName:
  negativeButton:
  positiveButton:
  altNegativeButton:
  altPositiveButton:
  gravity: 0
  dead: 0.001
  sensitivity: 1
  snap: 0
  invert: 0          # set to 1 to flip (e.g. Joy_Vertical inverts Y)
  type: 2            # 0 = key/button, 1 = mouse, 2 = joystick axis
  axis: 0            # 0 = left-stick X, 1 = left-stick Y, 5 = D-pad X, 6 = D-pad Y
  joyNum: 0          # 0 = any joystick; 1–4 = specific slot
```

Existing named axes:

| Name              | axis | invert | Purpose                          |
| ----------------- | ---- | ------ | -------------------------------- |
| `Joy_Horizontal`  | 0    | 0      | Left-stick X                     |
| `Joy_Vertical`    | 1    | 1      | Left-stick Y (up = forward)      |
| `DPad_Horizontal` | 5    | 0      | D-pad X (HAT axis, Xbox/Mac HID) |
| `DPad_Vertical`   | 6    | 0      | D-pad Y (HAT axis, Xbox/Mac HID) |

The `dead: 0.001` entry is intentional — dead zone filtering is applied in code
via `PlayerInputLogic.ApplyDeadZone` so the axis returns the true hardware value.

## Controller + keyboard input merge

Three-source movement pattern used in `LegacyPlayerInputSource.ReadFrame()`:

```csharp
Vector2 keyboardMove = ReadKeyboardMove();          // WASD, direct GetKey
Vector2 stickMove    = new Vector2(
    Input.GetAxisRaw("Joy_Horizontal"),
    Input.GetAxisRaw("Joy_Vertical"));
Vector2 dpadMove     = ReadDPadMove();              // axis 5/6 with button14-17 fallback

// Dominant source wins; never blend two sources.
Vector2 rawMove = stickMove.sqrMagnitude > 0.01f ? stickMove
                : dpadMove.sqrMagnitude  > 0.01f ? dpadMove
                : keyboardMove;
Vector2 move = PlayerInputLogic.ApplyDeadZone(rawMove, StickDeadZone);
```

D-pad read (two-tier, platform-agnostic):

```csharp
static Vector2 ReadDPadMove()
{
    float dx = Input.GetAxisRaw("DPad_Horizontal");
    float dy = Input.GetAxisRaw("DPad_Vertical");
    if (Mathf.Abs(dx) > 0.5f || Mathf.Abs(dy) > 0.5f)
        return new Vector2(dx, dy).normalized;

    // XInput digital D-pad (Windows / some drivers).
    Vector2 d = Vector2.zero;
    if (Input.GetKey(KeyCode.JoystickButton14)) d.y += 1f; // Up
    if (Input.GetKey(KeyCode.JoystickButton15)) d.y -= 1f; // Down
    if (Input.GetKey(KeyCode.JoystickButton16)) d.x -= 1f; // Left
    if (Input.GetKey(KeyCode.JoystickButton17)) d.x += 1f; // Right
    return d.normalized;
}
```

Device detection must check stick magnitude as well as button presses so the
HUD switches when the player navigates with only the analog stick:

```csharp
bool gamepadActivity = HasGamepadButtonDown()
    || stickMove.sqrMagnitude > StickDeadZone * StickDeadZone
    || dpadMove.sqrMagnitude  > 0.01f;
```

## New AI behavior

1. Add the `AIState` value and the decision in `Decide()` in
   [CPUWrestlerAI.cs](../../Assets/Scripts/AI/CPUWrestlerAI.cs). Placement is
   priority: role-specific follow-ups first, then the `canAttack` gate, then
   stamina caution, then situational offense.
2. Add the `case` in `Act()` — call the same `WrestlerCombat` methods the player
   uses, `_memory.Note(...)`/`_memory.CanUse(...)` to avoid spamming, and
   `Rethink()` after acting.
3. Tune per difficulty via
   [AIDifficultyData](../../Assets/Scripts/AI/AIDifficultyData.cs) fields rather
   than hard-coded constants.
