# Round 3 Special Movement Animation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Re-create the special and finisher movement families cataloged in `examplecode/round3.md` with synchronized 3D attacker/defender animation while preserving the current gameplay authority, timing, positioning, and outcome rules.

**Architecture:** Implement the existing round-two paired-animation plan first, then add a choreography layer that describes start formation, participant clips, phase timing, visual alignment, landing pose, and presentation markers. `MoveData`, `SpecialAbilityData`, `WrestlerCombat`, and special executors remain authoritative; Animator clips and markers only present outcomes already resolved by gameplay.

**Tech Stack:** Unity 6.0.4.10f1, C#, Mecanim Humanoid Animator, AnimatorOverrideController, ScriptableObjects, Animation Events used as presentation markers, Unity Test Framework, existing `IAnimationDriver`, `WrestlerMotor`, and special executor architecture.

---

## Review Summary

`examplecode/round3.md` describes roughly 120 special-move entries across nine contexts:

1. Front standing.
2. Rear standing.
3. Grounded face-up near the head.
4. Grounded face-down.
5. Grounded near the legs.
6. Grounded face-down near a leg.
7. Front corner/turnbuckle.
8. Rear corner/turnbuckle.
9. Running.

The catalog is not an animation specification. Several names are uncertain, and many descriptions omit exact grips, footwork, handedness, timing, or transition poses. Unclear entries must stay blocked until direct visual reference is available.

The movements reduce to these reusable production archetypes:

| Archetype | Examples from the catalog | Main technical requirement |
| --- | --- | --- |
| Standing strike | kicks, lariats, slap and punch combinations | One attacker clip plus directional hit reaction |
| Standing paired impact | DDT, jawbreaker, neckbreaker, side slam | Synchronized attacker/defender clips |
| Lift and drop | powerbomb, vertical drop, overhead slam, piledriver | Paired clips, lift validation, weight-compatible variants |
| Rotating lift | spinning powerbomb, big swing, rotating driver | Scripted pair rotation and stable root offsets |
| Rear grapple | rear suplex, rear DDT, full-nelson driver | Rear start formation and paired clips |
| Ground strike or pin | elbow routine, ground punching, theatrical pin | Ground-zone alignment and optional pin transition |
| Submission | armbar, leg lock, choke, shoulder stretch | Apply pair, hold loops, struggle, release, tap, rope-break clips |
| Corner paired move | elevated DDT, corner head-scissors, top-rope powerbomb | Corner anchors and launch/landing phases |
| Running catch | running side slam, spine buster, flowing hip toss | Moving entry, catch marker, paired impact |
| Multi-step sequence | chained powerbomb/driver, strike combo, slam-to-submission | Authored phase list with interruption windows |
| Effect move | fireball, mist | VFX socket, facing cone, reaction clip, persistent cosmetic state |

Do not build one Animator graph branch per move. Use semantic move states from the round-two plan, roster-owned clip overrides, and data-driven choreography assets.

## Delivery Strategy

### Wave 1: Representative vertical slice

Build twelve moves that prove all high-risk systems:

- DDT: basic front paired impact.
- Rear suplex: rear paired impact.
- Powerbomb: lift and face-up landing.
- Piledriver: inverted lift and face-down landing.
- Spinning powerbomb: rotating lift.
- Armbar: submission apply/hold/release.
- Leg lock: grounded lower-body submission.
- Corner head-scissors: corner paired aerial.
- Top-rope powerbomb: elevated paired drop.
- Running spine buster: moving catch.
- Strike combination: multi-step sequence.
- Mist: effect move with reaction and cosmetic state.

### Wave 2: Family expansion

Author clip variants by reusing Wave 1 start formations, phase types, and exit poses:

- DDT and neckbreaker family.
- Slam and driver family.
- Powerbomb and pin-transition family.
- Rear suplex and rear-driver family.
- Ground pin and ground-strike family.
- Arm, leg, neck, and back submission family.
- Corner aerial and corner-driver family.
- Running strike and running-catch family.

### Wave 3: Bespoke choreography

Build the moves that cannot be expressed as simple variants:

- Multi-impact chained finishers.
- Long theatrical routines with rope runs.
- Carry/parade and repeated-spin moves.
- Slam directly into submission.
- Counter-only specials.
- Moves with integrated pins and unusual folded landing poses.

### Wave 4: Blocked/uncertain entries

Do not animate entries marked unidentified or visually unclear in the catalog. Create placeholder records with `ReferenceStatus.NeedsVideo` and no production clip assignment. Require direct reference footage before an animation brief can become `Approved`.

## File Structure

- Prerequisite: `docs/superpowers/plans/2026-06-11-round-02-animation-features.md`
- Create: `Assets/Scripts/Animation/MoveChoreographyData.cs`
- Create: `Assets/Scripts/Animation/AnimationPhaseDefinition.cs`
- Create: `Assets/Scripts/Animation/AnimationStartFormation.cs`
- Create: `Assets/Scripts/Animation/PairedMoveCoordinator.cs`
- Create: `Assets/Scripts/Animation/AnimationMarkerRelay.cs`
- Create: `Assets/Scripts/Editor/MoveChoreographyValidator.cs`
- Create: `Assets/Scripts/Editor/MoveChoreographyValidatorTests.cs`
- Create: `Assets/Scripts/Editor/PairedMoveCoordinatorTests.cs`
- Create: `Assets/Scripts/Editor/SpecialPresentationTests.cs`
- Create: `Assets/Scripts/Editor/RoundThreeAnimationBriefBuilder.cs`
- Create: `Documentation/AnimationBriefs/Round03/README.md`
- Create: twelve Wave 1 briefs under `Documentation/AnimationBriefs/Round03/`
- Modify: `Assets/Scripts/Moves/MoveData.cs`
- Modify: `Assets/Scripts/Specials/SpecialAbilityData.cs`
- Modify: `Assets/Scripts/Animation/IAnimationDriver.cs`
- Modify: `Assets/Scripts/Animation/PlaceholderAnimationDriver.cs`
- Modify: `Assets/Scripts/Animation/AnimatorAnimationDriver.cs` from the round-two plan
- Modify: `Assets/Scripts/Combat/WrestlerCombat.cs`
- Modify: `Assets/Scripts/Specials/SpecialExecutor.cs`
- Modify: `Assets/Scripts/Specials/SequenceSpecialExecutor.cs`
- Modify: `Assets/Scripts/Specials/AerialSpecialExecutor.cs`
- Modify: `Assets/Scripts/Wrestlers/WrestlerCore.cs`
- Modify: `Assets/Scripts/Wrestlers/WrestlerMotor.cs`
- Modify: `Assets/Scripts/Editor/MoveDataValidator.cs`
- Modify: `Documentation/AnimationContract.md`
- Modify: `Documentation/FutureAssetIntegration.md`
- Modify: `Documentation/TestingChecklist.md`

### Task 1: Complete and verify the round-two animation foundation

**Files:**
- Execute: `docs/superpowers/plans/2026-06-11-round-02-animation-features.md`
- Verify: `Assets/Scripts/Animation/AnimatorAnimationDriver.cs`
- Verify: `Assets/Scripts/Animation/WrestlerAnimationProfile.cs`
- Verify: `Assets/Scripts/Animation/WrestlerAnimatorContract.cs`
- Verify: `Assets/Scripts/Moves/MoveData.cs`

- [ ] **Step 1: Run the round-two animation tests**

Run the focused Unity EditMode tests:

```text
WrestlerAnimationProfileValidatorTests
WrestlerAnimatorControllerBuilderTests
AnimatorAnimationDriverTests
MoveDataValidatorTests
```

Expected: all tests pass and production source includes paired attacker and defender semantic states.

- [ ] **Step 2: Verify authority boundaries**

Confirm:

```text
Animator.applyRootMotion == false
MoveData owns startup/active/recovery timing
WrestlerCombat owns damage and state transitions
WrestlerMotor owns gameplay-root movement
Animation markers cannot call ApplyDamage, SpendStamina, AddMomentum, or resolve a win
```

- [ ] **Step 3: Verify procedural fallback**

Spawn one wrestler without an animation profile and one with a valid profile.

Expected: both can complete a match; the unprofiled wrestler uses `PlaceholderAnimationDriver`, and the profiled wrestler uses `AnimatorAnimationDriver`.

- [ ] **Step 4: Commit the prerequisite**

```bash
git add Assets Documentation
git commit -m "feat: add paired wrestler animation foundation"
```

### Task 2: Add choreography data and validation

**Files:**
- Create: `Assets/Scripts/Animation/MoveChoreographyData.cs`
- Create: `Assets/Scripts/Animation/AnimationPhaseDefinition.cs`
- Create: `Assets/Scripts/Animation/AnimationStartFormation.cs`
- Create: `Assets/Scripts/Editor/MoveChoreographyValidator.cs`
- Test: `Assets/Scripts/Editor/MoveChoreographyValidatorTests.cs`
- Modify: `Assets/Scripts/Moves/MoveData.cs`
- Modify: `Assets/Scripts/Specials/SpecialAbilityData.cs`

- [ ] **Step 1: Write failing validator tests**

Cover:

```csharp
[Test]
public void PairedMove_RequiresAttackerAndDefenderKeys()
{
    var data = ScriptableObject.CreateInstance<MoveChoreographyData>();
    data.participantMode = AnimationParticipantMode.Paired;
    data.attackerStateKey = "ddt/attacker";
    data.defenderStateKey = "";

    Assert.That(MoveChoreographyValidator.Validate(data),
        Has.Some.Contains("defender"));
}

[Test]
public void PhaseDurations_MustMatchAuthoredDuration()
{
    var data = ValidPairedChoreography();
    data.authoredDuration = 1.5f;
    data.phases.Add(new AnimationPhaseDefinition
    {
        phase = AnimationPhase.Setup,
        normalizedStart = 0f,
        normalizedEnd = 0.8f
    });

    Assert.That(MoveChoreographyValidator.Validate(data),
        Has.Some.Contains("cover normalized time"));
}

[Test]
public void IntegratedPin_RequiresFaceUpExit()
{
    var data = ValidPairedChoreography();
    data.followUp = AnimationFollowUp.IntegratedPin;
    data.defenderExitPose = DefenderExitPose.FaceDown;

    Assert.That(MoveChoreographyValidator.Validate(data),
        Has.Some.Contains("FaceUp"));
}
```

- [ ] **Step 2: Run tests and verify failure**

Expected: compilation fails because the choreography types do not exist.

- [ ] **Step 3: Implement the data model**

Use these enums:

```csharp
public enum AnimationParticipantMode { Solo, Paired, SubmissionPair }
public enum AnimationStartFormation
{
    FrontStanding, RearStanding, SideBySide,
    GroundHeadFaceUp, GroundBodyFaceDown, GroundLegs,
    CornerFront, CornerRear, TopCornerPair, RunningCatch
}
public enum AnimationPhase
{
    Setup, Contact, Lift, Carry, Rotation, Impact,
    HoldApply, HoldLoop, Release, Recovery
}
public enum DefenderExitPose { Standing, FaceUp, FaceDown, Seated, SubmissionHold }
public enum AnimationFollowUp { None, PinWindow, IntegratedPin, Submission }
public enum ReferenceStatus { Approved, NeedsVideo, Rejected }
```

`AnimationPhaseDefinition` contains:

```csharp
[System.Serializable]
public sealed class AnimationPhaseDefinition
{
    public AnimationPhase phase;
    [Range(0f, 1f)] public float normalizedStart;
    [Range(0f, 1f)] public float normalizedEnd = 1f;
    public bool allowsInterruption;
    public bool allowsSpecialEscape;
    public string presentationMarker;
}
```

`MoveChoreographyData` contains:

```csharp
[CreateAssetMenu(menuName = "LoCo Fight Game/Move Choreography")]
public sealed class MoveChoreographyData : ScriptableObject
{
    public string presentationId;
    public AnimationParticipantMode participantMode;
    public AnimationStartFormation startFormation;
    public string attackerStateKey;
    public string defenderStateKey;
    public float authoredDuration = 1f;
    public float startDistance = 0.9f;
    public Vector3 defenderLocalOffset;
    public float defenderYaw;
    public DefenderExitPose defenderExitPose;
    public AnimationFollowUp followUp;
    public ReferenceStatus referenceStatus = ReferenceStatus.Approved;
    public List<AnimationPhaseDefinition> phases = new();
}
```

Add optional `MoveChoreographyData choreography` fields to `MoveData` and `SpecialAbilityData`.

- [ ] **Step 4: Implement validation**

Reject:

- blank or duplicate `presentationId`;
- paired records missing either role key;
- normalized phases outside `0..1`, overlapping, or leaving gaps;
- non-positive authored duration or start distance;
- integrated pin with a non-face-up exit;
- submission follow-up without `SubmissionPair`;
- `NeedsVideo` records containing production state keys;
- top-corner formations without setup, contact, and impact phases.

- [ ] **Step 5: Run validator tests**

Expected: all `MoveChoreographyValidatorTests` pass.

- [ ] **Step 6: Commit**

```bash
git add Assets/Scripts/Animation Assets/Scripts/Moves/MoveData.cs Assets/Scripts/Specials/SpecialAbilityData.cs Assets/Scripts/Editor
git commit -m "feat: add validated move choreography data"
```

### Task 3: Add authoritative pair alignment and playback

**Files:**
- Create: `Assets/Scripts/Animation/PairedMoveCoordinator.cs`
- Create: `Assets/Scripts/Animation/AnimationMarkerRelay.cs`
- Test: `Assets/Scripts/Editor/PairedMoveCoordinatorTests.cs`
- Modify: `Assets/Scripts/Animation/IAnimationDriver.cs`
- Modify: `Assets/Scripts/Animation/PlaceholderAnimationDriver.cs`
- Modify: `Assets/Scripts/Animation/AnimatorAnimationDriver.cs`
- Modify: `Assets/Scripts/Wrestlers/WrestlerCore.cs`
- Modify: `Assets/Scripts/Wrestlers/WrestlerMotor.cs`

- [ ] **Step 1: Write failing coordinator tests**

Test:

```csharp
[Test]
public void BeginPair_PlacesDefenderFromAttackerLocalFormation()
{
    var attacker = CreateWrestler(Vector3.zero, Quaternion.identity);
    var defender = CreateWrestler(new Vector3(4f, 0f, 0f), Quaternion.identity);
    var choreography = FrontPair(distance: 0.85f);

    PairedMoveCoordinator.AlignForStart(attacker, defender, choreography);

    Assert.That(Vector3.Distance(attacker.transform.position,
        defender.transform.position), Is.EqualTo(0.85f).Within(0.01f));
    Assert.That(Vector3.Dot(attacker.transform.forward,
        -defender.transform.forward), Is.GreaterThan(0.99f));
}

[Test]
public void AnimationMarker_DoesNotApplyGameplayEffects()
{
    var relay = new GameObject().AddComponent<AnimationMarkerRelay>();
    var stats = CreateStats();
    float healthBefore = stats.Health;

    relay.Emit("impact");

    Assert.That(stats.Health, Is.EqualTo(healthBefore));
}
```

- [ ] **Step 2: Run tests and verify failure**

Expected: compilation fails because coordinator and relay types do not exist.

- [ ] **Step 3: Implement pair alignment**

`PairedMoveCoordinator` must:

1. Validate both participants and choreography.
2. Set scripted control through `WrestlerMotor`.
3. Place roots using attacker-local offsets for the selected formation.
4. Face participants correctly.
5. Ask each `IAnimationDriver` to play its semantic state at a shared speed.
6. Maintain only authored gameplay-root offsets during lift, carry, rotation, and aerial phases.
7. Release scripted control through one cleanup path on success, reversal, escape, reset, or match end.

Do not parent one wrestler root to another. Keep both roots independent so interruption and cleanup remain deterministic.

- [ ] **Step 4: Extend the driver contract**

Add:

```csharp
void PlayPairedState(
    string semanticStateKey,
    float normalizedStart,
    float playbackSpeed);

void SetPresentationOffset(Vector3 localOffset, Quaternion localRotation);
void ClearPresentationOffset();
```

The procedural driver maps paired states to its closest existing pose. The Animator driver cross-fades to the semantic state and applies offsets only to `VisualRoot`, never the gameplay root.

- [ ] **Step 5: Implement presentation-only marker relay**

Allowed marker outputs:

```text
contact
impact
release
pose-sync
camera-light
camera-heavy
audio:<event-id>
vfx:<event-id>
```

The relay may notify `FeelSystem`, audio, VFX, and the coordinator for visual resynchronization. It may not mutate combat state or resources.

- [ ] **Step 6: Run tests**

Expected: coordinator tests pass; existing combat and animation tests remain green.

- [ ] **Step 7: Commit**

```bash
git add Assets/Scripts/Animation Assets/Scripts/Wrestlers
git commit -m "feat: synchronize paired move presentation"
```

### Task 4: Route normal grapple moves through choreography

**Files:**
- Modify: `Assets/Scripts/Combat/WrestlerCombat.cs`
- Modify: `Assets/Scripts/Editor/MoveDataValidator.cs`
- Test: `Assets/Scripts/Editor/MoveDataValidatorTests.cs`
- Test: `Assets/Scripts/Editor/PairedMoveCoordinatorTests.cs`

- [ ] **Step 1: Add failing combat presentation tests**

Cover:

- a grapple with choreography starts both role states;
- damage still occurs at the existing gameplay active phase;
- missing choreography falls back to current `PlayMove`;
- reversal cleanup restores both participants;
- a lift failure starts no paired animation;
- face-up and face-down exits match `MoveData`.

- [ ] **Step 2: Run tests and verify failure**

Expected: paired playback assertions fail while existing gameplay assertions remain green.

- [ ] **Step 3: Integrate in `GrappleMoveRoutine`**

After all validation and stamina spending succeeds:

```text
BeginMove
set gameplay states
begin paired choreography when present
wait existing startup timing
ApplyHit using existing gameplay code
wait remaining existing move timing
apply authoritative exit placement and state
cleanup pair
EndMove
```

Do not derive `ApplyHit` timing from an Animation Event.

- [ ] **Step 4: Validate data consistency**

Add errors when:

- `MoveData.causesDownedState` conflicts with a standing choreography exit;
- `MoveData.canPinAfter` conflicts with a face-down exit;
- a lift-tagged choreography is assigned to a move without `requiresLift`;
- a rear formation is assigned to a front-only contextual move.

- [ ] **Step 5: Run tests**

Expected: all combat, move-data, and paired coordinator tests pass.

- [ ] **Step 6: Commit**

```bash
git add Assets/Scripts/Combat/WrestlerCombat.cs Assets/Scripts/Editor
git commit -m "feat: present grapple moves with paired choreography"
```

### Task 5: Route specials and sequences through choreography phases

**Files:**
- Modify: `Assets/Scripts/Specials/SpecialExecutor.cs`
- Modify: `Assets/Scripts/Specials/SequenceSpecialExecutor.cs`
- Modify: `Assets/Scripts/Specials/AerialSpecialExecutor.cs`
- Modify: `Assets/Scripts/Specials/RopeTrapSpecialExecutor.cs`
- Modify: `Assets/Scripts/Moves/SequenceStep.cs`
- Modify: `Assets/Scripts/Moves/ComboStep.cs`
- Test: `Assets/Scripts/Editor/SpecialPresentationTests.cs`

- [ ] **Step 1: Write failing special presentation tests**

Cover:

- power grapple phases play setup, lift, carry/rotation, impact, and recovery;
- aerial phases play perch, launch, contact or miss, and landing;
- sequence steps select attacker and defender semantic keys;
- submission apply transitions into existing hold loops;
- interruption cleans presentation offsets and scripted control;
- special damage and meter spending remain exactly once.

- [ ] **Step 2: Run tests and verify failure**

Expected: choreography phase assertions fail.

- [ ] **Step 3: Extend sequence steps**

Add:

```csharp
public string AttackerStateKey;
public string DefenderStateKey;
public string PresentationMarker;
```

Keep `Damage`, `StaminaDamage`, reversal windows, and escape flags authoritative in the existing step data.

- [ ] **Step 4: Integrate generic phase playback**

Map existing executor behavior:

```text
grab/snap -> Setup
carry lift -> Lift
parade -> Carry
spin loop -> Rotation
slam/drop -> Impact
submission entry -> HoldApply
submission system -> HoldLoop
rope break/escape/tap -> Release
```

Use the current executor timers and root movement. Choreography data selects clips and visual offsets only.

- [ ] **Step 5: Run tests**

Expected: special presentation tests and all existing special gameplay tests pass.

- [ ] **Step 6: Commit**

```bash
git add Assets/Scripts/Specials Assets/Scripts/Moves Assets/Scripts/Editor/SpecialPresentationTests.cs
git commit -m "feat: animate special choreography phases"
```

### Task 6: Build animation briefs and the Wave 1 asset set

**Files:**
- Create: `Assets/Scripts/Editor/RoundThreeAnimationBriefBuilder.cs`
- Create: `Documentation/AnimationBriefs/Round03/README.md`
- Create: twelve Wave 1 brief files
- Create: `Assets/AnimationData/Round03/Choreography/*.asset`

- [ ] **Step 1: Define the brief template**

Every brief must state:

```text
Move ID and display name
Reference status and reference links/timecodes
Start formation and exact root spacing
Attacker hand/foot contacts
Defender grip and support responsibilities
Phase timing in seconds and normalized time
Shared contact, impact, release, and pose-sync markers
Expected attacker and defender exit poses
Allowed interruption and escape phases
Weight-class compatibility
Whether the clip may be mirrored
Required audio, VFX, and camera markers
Safety note: no gameplay logic in animation events
```

- [ ] **Step 2: Generate the twelve Wave 1 briefs**

Use the delivery list above. For every move, specify both participant clips and one fallback procedural pose.

- [ ] **Step 3: Create choreography assets**

The editor builder must create or update assets idempotently using canonical IDs:

```text
ddt
rear-suplex
powerbomb
piledriver
spinning-powerbomb
armbar
leg-lock
corner-head-scissors
top-rope-powerbomb
running-spine-buster
strike-combination
mist
```

- [ ] **Step 4: Mark uncertain records**

Create catalog records for every flagged unknown entry with:

```text
referenceStatus = NeedsVideo
attackerStateKey = ""
defenderStateKey = ""
```

Do not generate Animator production states for these records.

- [ ] **Step 5: Validate assets**

Run `MoveChoreographyValidator` over the complete Round 3 asset folder.

Expected: Wave 1 assets pass; uncertain records report informational blocked status, not production-ready status.

- [ ] **Step 6: Commit**

```bash
git add Assets/AnimationData Documentation/AnimationBriefs Assets/Scripts/Editor/RoundThreeAnimationBriefBuilder.cs
git commit -m "docs: define round three animation briefs"
```

### Task 7: Import, retarget, and verify the first paired clips

**Files:**
- Create: `Assets/Animations/Round03/Source/`
- Create: `Assets/Animations/Round03/Retargeted/`
- Modify: roster animation profiles created by the round-two plan
- Modify: `Documentation/FutureAssetIntegration.md`

- [ ] **Step 1: Establish import settings**

For each humanoid source:

```text
Rig Animation Type: Humanoid
Avatar Definition: Create From This Model or Copy From approved base avatar
Root Transform Rotation: Bake Into Pose where required
Root Transform Position Y: Bake Into Pose
Root Transform Position XZ: Bake Into Pose
Loop Time: off for impacts; on only for hold/struggle loops
Animation Compression: Optimal initially, Off while diagnosing contact drift
```

- [ ] **Step 2: Retarget both participants**

Import attacker and defender clips as a tested pair. Do not approve a move when only one role is available.

- [ ] **Step 3: Normalize timing**

Set playback speed so both clips match the choreography's authored duration within 2%. Never change gameplay duration to accommodate an imported clip.

- [ ] **Step 4: Verify three body scales**

Test each Wave 1 pair with:

```text
lightweight vs lightweight
middleweight vs heavyweight
heavyweight vs heavyweight
```

Approve only when hand contact, head clearance, feet, and landing pose remain acceptable. Create a separate heavy-target clip variant when retargeting cannot maintain the lift silhouette.

- [ ] **Step 5: Bind profiles**

Add semantic clip bindings for both participant roles to at least two roster profiles.

- [ ] **Step 6: Commit**

```bash
git add Assets/Animations Assets/AnimationData Assets/Roster Documentation/FutureAssetIntegration.md
git commit -m "feat: import round three paired animation slice"
```

### Task 8: Add runtime and visual regression verification

**Files:**
- Create: `Assets/Scripts/Editor/RoundThreeAnimationCoverageTests.cs`
- Create: `Assets/Scripts/Debug/AnimationMoveLab.cs`
- Modify: `Documentation/TestingChecklist.md`

- [ ] **Step 1: Add coverage tests**

Assert:

- every production-ready choreography has valid attacker and defender keys;
- every Wave 1 key exists in the generated Animator contract;
- every required profile has both role clips;
- no `NeedsVideo` record is bound to a production move;
- gameplay and choreography exit orientation agree;
- all choreography phases cover normalized time exactly once.

- [ ] **Step 2: Add an animation move lab**

Create a development-only scene utility that:

```text
spawns two selected roster rigs
selects one choreography asset
plays at 0.25x, 0.5x, and 1x
scrubs normalized time
draws gameplay roots, visual roots, facing vectors, and contact markers
forces lightweight/middleweight/heavyweight scale pairs
interrupts at every interruptible phase
```

The lab must call the same `PairedMoveCoordinator` used in matches.

- [ ] **Step 3: Add manual checks**

Add checklist rows for:

- no foot sliding before contact;
- no body interpenetration during lift;
- no head penetration on drivers or piledrivers;
- both clips remain synchronized after speed scaling;
- interruption always restores valid roots and states;
- camera shake and VFX occur at markers without affecting damage timing;
- face-up/face-down landing matches gameplay;
- integrated pin and submission transitions contain no visible reset to idle;
- fallback presentation remains functional when either clip is missing.

- [ ] **Step 4: Run Unity verification**

Run:

```text
All EditMode tests
AnimationMoveLab smoke test for twelve Wave 1 moves
PrototypeMatch play-mode match using one real-profile wrestler and one fallback wrestler
macOS development build
```

Expected: no console errors, no stuck scripted-control state, no duplicated damage/resource spending, and no gameplay regression when the animation layer is disabled.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Editor/RoundThreeAnimationCoverageTests.cs Assets/Scripts/Debug/AnimationMoveLab.cs Documentation/TestingChecklist.md
git commit -m "test: verify round three animation coverage"
```

## Definition of Done

Wave 1 is complete when:

- twelve representative movements play with synchronized attacker and defender clips;
- all nine positional categories have at least one proven path or share a proven path with documented formation differences;
- gameplay timing, damage, stamina, momentum, reversals, pinning, submissions, and root positions remain authoritative outside Animator;
- interruption works during every authored reversible or escapable phase;
- three body-scale pairings pass visual review;
- missing clips fall back without changing gameplay;
- all automated tests, play-mode checks, and the development build pass.

The full catalog is complete only after every non-blocked entry has an approved visual reference, brief, choreography asset, paired clip coverage where required, profile binding, and move-lab verification.
