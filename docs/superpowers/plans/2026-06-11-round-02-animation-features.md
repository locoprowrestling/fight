# Round 2 Animation Features Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add data-driven, per-wrestler animation clip overrides and paired move playback without allowing animation assets to own combat timing, outcomes, or authoritative movement.

**Architecture:** `MoveData` remains the gameplay authority and gains only defender presentation identifiers. A generated `WrestlerAnimatorContract` maps semantic keys such as `body-slam/attacker` to placeholder clips in one base controller; each roster entry may reference a `WrestlerAnimationProfile` that replaces those clips through one `AnimatorOverrideController` created at spawn. `AnimatorAnimationDriver` implements `IAnimationDriver`, disables root motion, and falls back to `PlaceholderAnimationDriver` whenever no valid real visual/profile is available.

**Tech Stack:** Unity 6.4, C#, Mecanim Animator/AnimatorOverrideController, ScriptableObjects, UnityEditor.Animations, NUnit EditMode tests.

---

## File Structure

- Create `Assets/Scripts/Animation/AnimationSemanticKey.cs`: canonical semantic key and Animator state naming.
- Create `Assets/Scripts/Animation/WrestlerAnimatorContract.cs`: generated base-controller slot contract.
- Create `Assets/Scripts/Animation/WrestlerAnimationProfile.cs`: roster-specific clip replacements.
- Create `Assets/Scripts/Animation/AnimatorParameterIds.cs`: centralized Animator parameter hashes.
- Create `Assets/Scripts/Animation/AnimatorAnimationDriver.cs`: production `IAnimationDriver` adapter.
- Create `Assets/Scripts/Editor/WrestlerAnimationProfileValidator.cs`: profile/contract validation.
- Create `Assets/Scripts/Editor/WrestlerAnimatorControllerBuilder.cs`: idempotent generated controller and contract builder.
- Create `Assets/Scripts/Editor/WrestlerAnimationProfileValidatorTests.cs`: semantic/profile validation tests.
- Create `Assets/Scripts/Editor/WrestlerAnimatorControllerBuilderTests.cs`: generated graph contract tests.
- Create `Assets/Scripts/Editor/AnimatorAnimationDriverTests.cs`: override and root-motion tests.
- Create `Documentation/AnimationBriefs/body-slam.md`: first paired move animation brief.
- Modify `Assets/Scripts/Moves/MoveData.cs`: defender state and pose presentation fields.
- Modify `Assets/Scripts/Roster/RosterEntry.cs`: optional animation profile reference.
- Modify `Assets/Scripts/Roster/DefaultGameData.cs`: assign canonical attacker/defender state names.
- Modify `Assets/Scripts/Animation/IAnimationDriver.cs`: add defender-role playback.
- Modify `Assets/Scripts/Animation/PlaceholderAnimationDriver.cs`: preserve procedural fallback.
- Modify `Assets/Scripts/Combat/WrestlerCombat.cs`: start paired grapple presentation from resolved `MoveData`.
- Modify `Assets/Scripts/Wrestlers/WrestlerCore.cs`: instantiate authored visuals and choose the animation driver.
- Modify `Assets/Scripts/Editor/MoveDataValidator.cs`: validate paired presentation identifiers.
- Modify `Assets/Scripts/Editor/MoveDataValidatorTests.cs`: cover paired identifier validation.
- Modify `Assets/Scripts/Editor/PrototypeAssetBuilder.cs`: save generated animation profiles/contracts when present.
- Modify `Documentation/AnimationContract.md`: record semantic override/profile rules.
- Modify `Documentation/FutureAssetIntegration.md`: replace future-tense steps with the implemented workflow.
- Modify `Documentation/TestingChecklist.md`: add real-driver/fallback and paired-playback checks.

### Task 1: Add semantic animation keys and roster-owned profiles

**Files:**
- Create: `Assets/Scripts/Animation/AnimationSemanticKey.cs`
- Create: `Assets/Scripts/Animation/WrestlerAnimatorContract.cs`
- Create: `Assets/Scripts/Animation/WrestlerAnimationProfile.cs`
- Modify: `Assets/Scripts/Roster/RosterEntry.cs`
- Test: `Assets/Scripts/Editor/WrestlerAnimationProfileValidatorTests.cs`

- [ ] **Step 1: Write failing key-generation tests**

```csharp
[Test]
public void MoveKey_UsesMoveIdAndRole()
{
    Assert.That(AnimationSemanticKey.Move("body-slam", AnimationParticipant.Attacker),
        Is.EqualTo("body-slam/attacker"));
    Assert.That(AnimationSemanticKey.Move("body-slam", AnimationParticipant.Defender),
        Is.EqualTo("body-slam/defender"));
}

[TestCase("")]
[TestCase(" body-slam")]
[TestCase("Body Slam")]
public void MoveKey_RejectsNonCanonicalIds(string moveId)
{
    Assert.Throws<System.ArgumentException>(() =>
        AnimationSemanticKey.Move(moveId, AnimationParticipant.Attacker));
}
```

- [ ] **Step 2: Run the focused tests and verify failure**

Run the Unity EditMode test `WrestlerAnimationProfileValidatorTests`.

Expected: compilation fails because `AnimationSemanticKey` and
`AnimationParticipant` do not exist.

- [ ] **Step 3: Implement canonical keys and profile assets**

```csharp
namespace LoCoFight
{
    public enum AnimationParticipant { Attacker, Defender }

    public static class AnimationSemanticKey
    {
        static readonly System.Text.RegularExpressions.Regex CanonicalMoveId =
            new System.Text.RegularExpressions.Regex(
                "^[a-z0-9]+(?:-[a-z0-9]+)*$",
                System.Text.RegularExpressions.RegexOptions.CultureInvariant);

        public static string Move(string moveId, AnimationParticipant participant)
        {
            if (string.IsNullOrEmpty(moveId) || !CanonicalMoveId.IsMatch(moveId))
                throw new System.ArgumentException("moveId must be canonical kebab-case.", nameof(moveId));

            return $"{moveId}/{participant.ToString().ToLowerInvariant()}";
        }

        public static string State(string semanticKey) =>
            $"Move.{semanticKey.Replace('/', '.')}";
    }
}
```

```csharp
[System.Serializable]
public struct AnimationSlotDefinition
{
    public string semanticKey;
    public string animatorStateName;
    public AnimationClip placeholderClip;
}

[CreateAssetMenu(menuName = "LoCo Fight Game/Wrestler Animator Contract")]
public sealed class WrestlerAnimatorContract : ScriptableObject
{
    public RuntimeAnimatorController baseController;
    public List<AnimationSlotDefinition> slots = new List<AnimationSlotDefinition>();
}
```

```csharp
[System.Serializable]
public struct AnimationClipBinding
{
    public string semanticKey;
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

Add to `RosterEntry`:

```csharp
[Header("Presentation")]
public WrestlerAnimationProfile animationProfile;
```

- [ ] **Step 4: Run the tests**

Expected: key-generation tests pass.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Animation Assets/Scripts/Roster/RosterEntry.cs Assets/Scripts/Editor/WrestlerAnimationProfileValidatorTests.cs
git commit -m "feat: add semantic wrestler animation profiles"
```

### Task 2: Validate contracts and per-wrestler clip bindings

**Files:**
- Create: `Assets/Scripts/Editor/WrestlerAnimationProfileValidator.cs`
- Modify: `Assets/Scripts/Editor/WrestlerAnimationProfileValidatorTests.cs`

- [ ] **Step 1: Write failing validation tests**

Cover these exact cases:

```csharp
[Test]
public void ValidateProfile_RejectsDuplicateSemanticKeys()
{
    var profile = ScriptableObject.CreateInstance<WrestlerAnimationProfile>();
    profile.clips.Add(new AnimationClipBinding { semanticKey = "body-slam/attacker", clip = new AnimationClip() });
    profile.clips.Add(new AnimationClipBinding { semanticKey = "body-slam/attacker", clip = new AnimationClip() });

    Assert.That(WrestlerAnimationProfileValidator.Validate(profile),
        Has.Some.Contains("duplicate"));
}

[Test]
public void ValidateProfile_RejectsUnknownContractKey()
{
    var profile = ProfileWithContract("body-slam/attacker");
    profile.clips.Add(new AnimationClipBinding { semanticKey = "unknown/attacker", clip = new AnimationClip() });

    Assert.That(WrestlerAnimationProfileValidator.Validate(profile),
        Has.Some.Contains("unknown"));
}

[Test]
public void ValidateProfile_RequiresBothRolesWhenEitherRoleIsOverridden()
{
    var profile = ProfileWithContract("body-slam/attacker", "body-slam/defender");
    profile.clips.Add(new AnimationClipBinding { semanticKey = "body-slam/attacker", clip = new AnimationClip() });

    Assert.That(WrestlerAnimationProfileValidator.Validate(profile),
        Has.Some.Contains("body-slam/defender"));
}
```

- [ ] **Step 2: Run the tests and verify failure**

Expected: compilation fails because `WrestlerAnimationProfileValidator` does
not exist.

- [ ] **Step 3: Implement validation**

`Validate(profile, Avatar prefabAvatar = null)` must return errors for:

- null profile contract;
- null base controller;
- duplicate or blank contract keys;
- duplicate profile keys;
- null replacement clips;
- profile keys absent from the contract;
- attacker-only or defender-only overrides for the same move id;
- profile avatar mismatch when both the profile and prefab Animator expose
  humanoid avatars.

Use dictionaries and `HashSet<string>`; do not parse or compare display names.

- [ ] **Step 4: Run all validator tests**

Expected: all `WrestlerAnimationProfileValidatorTests` pass.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Editor/WrestlerAnimationProfileValidator.cs Assets/Scripts/Editor/WrestlerAnimationProfileValidatorTests.cs
git commit -m "test: validate wrestler animation profiles"
```

### Task 3: Generate a stable base controller and slot contract

**Files:**
- Create: `Assets/Scripts/Animation/AnimatorParameterIds.cs`
- Create: `Assets/Scripts/Editor/WrestlerAnimatorControllerBuilder.cs`
- Create: `Assets/Scripts/Editor/WrestlerAnimatorControllerBuilderTests.cs`
- Modify: `Assets/Scripts/Roster/DefaultGameData.cs`

- [ ] **Step 1: Assign canonical move state names in the data factory**

In the common `Move(...)` factory, after `moveId` is set:

```csharp
m.animationStateName = AnimationSemanticKey.State(
    AnimationSemanticKey.Move(id, AnimationParticipant.Attacker));
m.defenderAnimationStateName = AnimationSemanticKey.State(
    AnimationSemanticKey.Move(id, AnimationParticipant.Defender));
```

- [ ] **Step 2: Write failing generated-contract tests**

```csharp
[Test]
public void BuildContract_CreatesPairedSlotsForEveryMove()
{
    var set = DefaultGameData.CreateAll();
    var result = WrestlerAnimatorControllerBuilder.BuildInMemory(set.moves);

    foreach (MoveData move in set.moves)
    {
        Assert.That(result.SemanticKeys, Does.Contain(
            AnimationSemanticKey.Move(move.moveId, AnimationParticipant.Attacker)));
        Assert.That(result.SemanticKeys, Does.Contain(
            AnimationSemanticKey.Move(move.moveId, AnimationParticipant.Defender)));
    }
}

[Test]
public void BuildContract_IsDeterministic()
{
    var set = DefaultGameData.CreateAll();
    var first = WrestlerAnimatorControllerBuilder.Describe(set.moves);
    var second = WrestlerAnimatorControllerBuilder.Describe(set.moves);
    Assert.That(second, Is.EqualTo(first));
}
```

- [ ] **Step 3: Run the tests and verify failure**

Expected: compilation fails because the builder and defender animation field do
not exist.

- [ ] **Step 4: Add centralized Animator parameter names**

`AnimatorParameterIds` must expose string constants plus hashes for:
`MoveSpeed`, `MovePlaybackSpeed`, `GameplayState`, `HitReact`, `ReversalKind`,
`SpecialReady`, and all submission triggers currently represented by
`IAnimationDriver`.

- [ ] **Step 5: Implement the builder**

The builder must:

1. Sort moves by `moveId`.
2. Create one unique placeholder clip per semantic key under
   `Assets/Animations/Generated/Placeholders/`.
3. Create attacker and defender states using the exact names stored on
   `MoveData`.
4. Set each move state's speed parameter to `MovePlaybackSpeed`.
5. Create locomotion and semantic reaction states from
   `Documentation/AnimationContract.md`.
6. Save the controller to
   `Assets/Animations/Generated/Wrestler.controller`.
7. Save `WrestlerAnimatorContract.asset` beside it.
8. Update existing generated assets in place; never delete an artist-authored
   controller.
9. Use `[MenuItem("Tools/LoCo Fight Game/Build Generated Wrestler Animator")]`.

Expose `BuildInMemory(IReadOnlyList<MoveData>)` for asset-free structural tests
and `Describe(IReadOnlyList<MoveData>)` for deterministic snapshots; the menu
entry writes the returned structure to Unity assets.

- [ ] **Step 6: Run builder tests**

Expected: deterministic paired slots exist for every default move and all
required Animator parameters are present exactly once.

- [ ] **Step 7: Commit**

```bash
git add Assets/Scripts/Animation/AnimatorParameterIds.cs Assets/Scripts/Editor/WrestlerAnimatorControllerBuilder.cs Assets/Scripts/Editor/WrestlerAnimatorControllerBuilderTests.cs Assets/Scripts/Roster/DefaultGameData.cs
git commit -m "feat: generate semantic wrestler animator contract"
```

### Task 4: Implement one-time override application

**Files:**
- Create: `Assets/Scripts/Animation/AnimatorAnimationDriver.cs`
- Create: `Assets/Scripts/Editor/AnimatorAnimationDriverTests.cs`

- [ ] **Step 1: Write failing driver tests**

```csharp
[Test]
public void Bind_DisablesRootMotionAndAppliesProfileClip()
{
    var setup = AnimatorTestFactory.Create("body-slam/attacker");
    var driver = setup.Root.AddComponent<AnimatorAnimationDriver>();

    driver.Bind(setup.Animator, setup.Profile);

    Assert.That(setup.Animator.applyRootMotion, Is.False);
    var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
    ((AnimatorOverrideController)setup.Animator.runtimeAnimatorController).GetOverrides(overrides);
    Assert.That(overrides, Has.Some.Matches<KeyValuePair<AnimationClip, AnimationClip>>(
        pair => pair.Value == setup.ReplacementClip));
}

[Test]
public void Bind_CreatesOverrideControllerOnlyOnce()
{
    var setup = AnimatorTestFactory.Create("body-slam/attacker");
    var driver = setup.Root.AddComponent<AnimatorAnimationDriver>();
    driver.Bind(setup.Animator, setup.Profile);
    var first = setup.Animator.runtimeAnimatorController;
    driver.Bind(setup.Animator, setup.Profile);
    Assert.That(setup.Animator.runtimeAnimatorController, Is.SameAs(first));
}
```

- [ ] **Step 2: Run tests and verify failure**

Expected: compilation fails because `AnimatorAnimationDriver` does not exist.

- [ ] **Step 3: Implement the driver**

`Bind(Animator, WrestlerAnimationProfile)` must validate inputs, disable root
motion, create one override controller from the contract, apply replacements by
semantic key, and cache it. Every `IAnimationDriver` method must only set
parameters, cross-fade states, or emit presentation cues.

`PlayMove` and the new defender-role method must:

```csharp
void PlayStateAtSpeed(string stateName, float speed)
{
    if (string.IsNullOrWhiteSpace(stateName)) return;
    _animator.SetFloat(AnimatorParameterIds.MovePlaybackSpeed,
        Mathf.Clamp(speed, 0.25f, 2.5f));
    _animator.CrossFadeInFixedTime(stateName, 0.05f);
}
```

Do not read clip length, mutate stats, move transforms, or call combat systems.

- [ ] **Step 4: Run driver tests**

Expected: all driver tests pass, including root motion disabled and stable
override-controller identity.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Animation/AnimatorAnimationDriver.cs Assets/Scripts/Editor/AnimatorAnimationDriverTests.cs
git commit -m "feat: add animator-backed animation driver"
```

### Task 5: Add paired attacker/defender move presentation

**Files:**
- Modify: `Assets/Scripts/Moves/MoveData.cs`
- Modify: `Assets/Scripts/Animation/IAnimationDriver.cs`
- Modify: `Assets/Scripts/Animation/PlaceholderAnimationDriver.cs`
- Modify: `Assets/Scripts/Animation/AnimatorAnimationDriver.cs`
- Modify: `Assets/Scripts/Combat/WrestlerCombat.cs`
- Modify: `Assets/Scripts/Editor/MoveDataValidator.cs`
- Modify: `Assets/Scripts/Editor/MoveDataValidatorTests.cs`

- [ ] **Step 1: Write failing move validation tests**

```csharp
[Test]
public void Validate_GrappleRequiresDefenderAnimationState()
{
    var move = ScriptableObject.CreateInstance<MoveData>();
    move.moveId = "body-slam";
    move.category = MoveCategory.PowerGrapple;
    move.animationStateName = "Move.body-slam.attacker";
    move.defenderAnimationStateName = "";
    move.reversalWindowEnd = 0.1f;

    Assert.That(MoveDataValidator.Validate(move, null),
        Has.Some.Contains("defender animation"));
}
```

- [ ] **Step 2: Add defender presentation fields**

```csharp
[Header("Animation")]
public string animationStateName;
public string defenderAnimationStateName;
public string placeholderPoseName = "strike";
public string defenderPlaceholderPoseName = "hit";
public float animationSpeed = 1f;
```

- [ ] **Step 3: Extend the driver interface**

Add:

```csharp
void PlayMoveReaction(
    string animationStateName,
    string placeholderPoseName,
    float speed = 1f);
```

`PlaceholderAnimationDriver.PlayMoveReaction` should start `HitRecoil` for
standing results and allow the subsequent gameplay state/`TriggerDowned` call
to choose the downed pose. `AnimatorAnimationDriver.PlayMoveReaction` should
call the same bounded cross-fade helper used by `PlayMove`.

- [ ] **Step 4: Start both tracks from `GrappleMoveRoutine`**

Immediately after the attacker `PlayMove` call:

```csharp
defender.Anim?.PlayMoveReaction(
    move.defenderAnimationStateName,
    move.defenderPlaceholderPoseName,
    move.animationSpeed);
```

Do not change `Phase(startup)`, `ApplyHit(move)`, state transitions, or result
timing.

- [ ] **Step 5: Run validator and combat tests**

Expected: all EditMode tests pass; existing placeholder gameplay behaves the
same except for the added defender presentation overlay.

- [ ] **Step 6: Commit**

```bash
git add Assets/Scripts/Moves/MoveData.cs Assets/Scripts/Animation Assets/Scripts/Combat/WrestlerCombat.cs Assets/Scripts/Editor/MoveDataValidator.cs Assets/Scripts/Editor/MoveDataValidatorTests.cs
git commit -m "feat: play paired grapple animation roles"
```

### Task 6: Wire authored visuals with procedural fallback

**Files:**
- Modify: `Assets/Scripts/Wrestlers/WrestlerCore.cs`
- Test: `Assets/Scripts/Editor/AnimatorAnimationDriverTests.cs`

- [ ] **Step 1: Write failing creation-path tests**

Cover:

1. No view prefab produces `PlaceholderAnimationDriver`.
2. A prefab with `WrestlerView`, `Animator`, and valid profile produces
   `AnimatorAnimationDriver`.
3. A prefab missing Animator or profile logs one actionable warning and falls
   back to the placeholder visual.

- [ ] **Step 2: Implement the creation branch**

In `WrestlerCore.Create`:

```csharp
bool hasAuthoredVisual =
    entry != null &&
    entry.placeholderViewPrefab != null &&
    entry.animationProfile != null;

if (hasAuthoredVisual)
{
    GameObject visual = Object.Instantiate(entry.placeholderViewPrefab, go.transform, false);
    core.View = visual.GetComponentInChildren<WrestlerView>();
    Animator animator = visual.GetComponentInChildren<Animator>();
    if (core.View != null && animator != null)
    {
        var driver = go.AddComponent<AnimatorAnimationDriver>();
        driver.Bind(animator, entry.animationProfile);
        core.Anim = driver;
    }
    else
    {
        Object.Destroy(visual);
        Debug.LogWarning($"[Animation] {entry.displayName}: authored visual is missing WrestlerView or Animator; using procedural fallback.");
    }
}

if (core.Anim == null)
{
    core.View = go.AddComponent<WrestlerView>();
    var driver = go.AddComponent<PlaceholderAnimationDriver>();
    core.Anim = driver;
    core.View.BuildPlaceholder(bodyColor, weight);
    driver.Bind(core.View);
}
```

Refactor local variable ordering as needed so `bodyColor` is available before
the fallback branch. Do not alter the root `CharacterController`.

- [ ] **Step 3: Run creation-path tests**

Expected: all three paths pass and root motion remains disabled for authored
visuals.

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/Wrestlers/WrestlerCore.cs Assets/Scripts/Editor/AnimatorAnimationDriverTests.cs
git commit -m "feat: select authored wrestler animation visuals"
```

### Task 7: Build and verify the body-slam vertical slice

**Files:**
- Create: `Documentation/AnimationBriefs/body-slam.md`
- Modify: `Assets/Scripts/Editor/PrototypeAssetBuilder.cs`
- Modify: `Documentation/AnimationContract.md`
- Modify: `Documentation/FutureAssetIntegration.md`
- Modify: `Documentation/TestingChecklist.md`

- [ ] **Step 1: Add the body-slam brief**

Use the exact brief structure in
`Documentation/KnowledgeBase/Templates.md#move-animation-brief`. The
authoritative move is `body-slam`; roles are `body-slam/attacker` and
`body-slam/defender`; clips are in-place; gameplay owns lift validation,
startup, impact, recovery, downed result, and root positions.

- [ ] **Step 2: Generate the controller and contract**

Run:

```text
Tools > LoCo Fight Game > Build Generated Wrestler Animator
```

Expected:

- `Assets/Animations/Generated/Wrestler.controller`
- `Assets/Animations/Generated/WrestlerAnimatorContract.asset`
- paired placeholder clips for every default move
- console contains no animation contract errors

- [ ] **Step 3: Add a development profile**

Create `Assets/Resources/LoCoData/AnimationProfiles/DefaultHumanoid.asset` with
the generated contract and paired `body-slam` test clips. The clips may be
minimal authored test clips, but they must be distinct attacker and defender
assets and compatible with the same Avatar.

- [ ] **Step 4: Update asset generation**

Ensure `PrototypeAssetBuilder` creates the
`Assets/Resources/LoCoData/AnimationProfiles` folder and preserves manually
authored profiles. It must not delete or regenerate imported clips.

- [ ] **Step 5: Run Unity verification**

Run EditMode tests:

- `WrestlerAnimationProfileValidatorTests`
- `WrestlerAnimatorControllerBuilderTests`
- `AnimatorAnimationDriverTests`
- `MoveDataValidatorTests`

Then run a Play Mode smoke check:

1. Spawn one authored-profile wrestler and one fallback wrestler.
2. Execute `body-slam`.
3. Assert both paired Animator states start in the same frame.
4. Assert impact still occurs at the `MoveData` active phase.
5. Interrupt before impact and assert both wrestlers cross-fade to states
   matching current gameplay states.
6. Assert both root transforms remain controlled by gameplay and stay inside
   ring bounds.
7. Pause/resume and reset the match; assert no trigger or clip remains stuck.

- [ ] **Step 6: Run project-wide verification**

Run the existing offline compile response-file check from
`Documentation/KnowledgeBase/Examples.md`, then:

```bash
git diff --check
git status --short
```

Expected: compilation succeeds with warnings only, documentation links resolve,
and no generated or imported asset outside the planned paths is modified.

- [ ] **Step 7: Commit**

```bash
git add Assets/Animations/Generated Assets/Resources/LoCoData/AnimationProfiles Documentation/AnimationBriefs Documentation/AnimationContract.md Documentation/FutureAssetIntegration.md Documentation/TestingChecklist.md Assets/Scripts/Editor/PrototypeAssetBuilder.cs
git commit -m "feat: add body slam animation override slice"
```

## Self-Review

- Round 2 per-wrestler overrides are covered by Tasks 1, 2, and 4.
- Stable semantic slots replace directional slot identities in Tasks 1 and 3.
- Duplicate gameplay data is avoided; `MoveData` remains authoritative in
  Tasks 3 and 5.
- Paired attacker/defender playback is covered by Task 5.
- Authored visuals and procedural fallback are covered by Task 6.
- The first reviewable move brief and runtime slice are covered by Task 7.
- No task permits clip length, Animation Events, or root motion to decide
  gameplay.
