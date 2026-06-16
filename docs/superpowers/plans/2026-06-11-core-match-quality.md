# Core Match Quality Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use
> superpowers:subagent-driven-development (recommended) or
> superpowers:executing-plans to implement this plan task-by-task. Steps use
> checkbox (`- [ ]`) syntax for tracking.

**Goal:** Deliver directional-read reversals, persistent SPECIAL readiness,
position-driven submission defense, focused CPU personality, and semantic
impact presentation in the existing one-player-versus-CPU match.

**Architecture:** Add pure resolvers for reversal reads, submission crawl math,
personality modifiers, and repetition memory. Keep runtime validation and
outcomes in `WrestlerCombat`, `SubmissionSystem`, and `SpecialController`; keep
input and AI as intent producers; keep `IAnimationDriver`, `FeelSystem`, and
`MatchHUD` presentation-only.

**Tech Stack:** Unity 6.4 (6000.4.10f1), C# 9, ScriptableObjects, legacy Input
Manager, NUnit edit-mode tests, procedural placeholder animation.

---

## Execution Preconditions

- Read
  `docs/superpowers/specs/2026-06-11-core-match-quality-design.md`
  before starting.
- The worktree already contains modified generated move assets,
  `Documentation/KnowledgeBase/BestPractices.md`, untracked research, and
  untracked `examplecode/`. Treat all of them as user-owned.
- Do not reset, checkout, clean, overwrite, or stage pre-existing changes.
- Before each commit, use `git diff --cached --name-only` and stage exact paths
  only.
- `examplecode/` is conceptual reference material. Do not compile, move, or
  copy it into `Assets/`.
- Unity must import every new `.cs` file before committing it. Include the
  generated `.meta` sidecar.
- Do not run Unity batch mode while the Unity editor has this project open.
- Regenerate `Assets/Resources/LoCoData/` only in Task 10, after all schema and
  default-data changes are complete.
- Before regeneration, record the existing dirty asset list:

```bash
git status --short Assets/Resources/LoCoData > /tmp/core-quality-assets-before.txt
```

- After regeneration, compare against that list. Never stage an asset merely
  because regeneration touched it; stage only files whose serialized values
  are required by this milestone and whose prior user changes are understood.

## File Structure

### New Runtime Files

- `Assets/Scripts/Combat/ReversalRead.cs`
  Defines `ReversalReadDirection`, `ReversalOutcome`, and
  `ReversalReadResolver`.
- `Assets/Scripts/Combat/SubmissionEscapeRules.cs`
  Pure crawl direction, stamina scaling, and active-effort calculations.
- `Assets/Scripts/AI/AIPersonalityProfile.cs`
  Immutable bounded personality modifiers and profile lookup.
- `Assets/Scripts/AI/AIDecisionWeights.cs`
  Pure application of difficulty, personality, and repetition penalties.
- `Assets/Scripts/Presentation/CombatPresentationEvent.cs`
  Defines semantic presentation outcomes.

### New Editor Test Files

- `Assets/Scripts/Editor/ReversalReadResolverTests.cs`
- `Assets/Scripts/Editor/ReversalRuntimeRulesTests.cs`
- `Assets/Scripts/Editor/MomentumReadinessTests.cs`
- `Assets/Scripts/Editor/SubmissionEscapeRulesTests.cs`
- `Assets/Scripts/Editor/AIPersonalityProfileTests.cs`
- `Assets/Scripts/Editor/AIMemoryTests.cs`
- `Assets/Scripts/Editor/AIDecisionWeightsTests.cs`
- `Assets/Scripts/Editor/CombatPresentationRulesTests.cs`

### New Documentation

- `Documentation/AnimationContract.md`
  Project-specific semantic animation and presentation manifest.

### Existing Files Modified

- `Assets/Scripts/Moves/MoveData.cs`
- `Assets/Scripts/Roster/DefaultGameData.cs`
- `Assets/Scripts/Editor/MoveDataValidator.cs`
- `Assets/Scripts/Editor/MoveDataValidatorTests.cs`
- `Assets/Scripts/Input/PlayerInputController.cs`
- `Assets/Scripts/Combat/ReversalSystem.cs`
- `Assets/Scripts/Combat/WrestlerCombat.cs`
- `Assets/Scripts/Wrestlers/WrestlerStatsRuntime.cs`
- `Assets/Scripts/Specials/SpecialController.cs`
- `Assets/Scripts/Combat/SubmissionSystem.cs`
- `Assets/Scripts/AI/AIMemory.cs`
- `Assets/Scripts/AI/CPUWrestlerAI.cs`
- `Assets/Scripts/UI/MeterBar.cs`
- `Assets/Scripts/UI/MatchHUD.cs`
- `Assets/Scripts/UI/DebugOverlay.cs`
- `Assets/Scripts/Animation/IAnimationDriver.cs`
- `Assets/Scripts/Animation/PlaceholderAnimationDriver.cs`
- `Assets/Scripts/Combat/FeelSystem.cs`
- `Documentation/DesignDoc.md`
- `Documentation/TestingChecklist.md`
- `Documentation/FutureAssetIntegration.md`
- `Documentation/KnowledgeBase/BestPractices.md`

## Verification Commands

Use the offline runtime compile after every task that changes non-Editor C#:

```bash
UNITY=/Applications/Unity/Hub/Editor/6000.4.10f1/Unity.app/Contents/Resources/Scripting
ROSLYN=$(ls -d /usr/local/share/dotnet/sdk/*/Roslyn/bincore/csc.dll | tail -1)
RSP=/tmp/loco_core_quality.rsp
: > "$RSP"
echo "-nologo -nostdlib+ -target:library -out:/tmp/loco_core_quality.dll -langversion:9.0 -nowarn:0169,0414" >> "$RSP"
echo "-r:$UNITY/NetStandard/ref/2.1.0/netstandard.dll" >> "$RSP"
for d in "$UNITY"/Managed/UnityEngine/*.dll; do echo "-r:$d" >> "$RSP"; done
echo "-r:Library/ScriptAssemblies/UnityEngine.UI.dll" >> "$RSP"
find Assets/Scripts -name "*.cs" -not -path "*/Editor/*" >> "$RSP"
dotnet "$ROSLYN" @"$RSP"
```

Expected: exit code `0`. Existing warnings are allowed; new errors are not.

When Unity is closed, run all edit-mode tests:

```bash
/Applications/Unity/Hub/Editor/6000.4.10f1/Unity.app/Contents/MacOS/Unity \
  -batchmode \
  -projectPath "$PWD" \
  -runTests \
  -testPlatform EditMode \
  -testResults /tmp/loco-core-quality-editmode.xml \
  -quit
```

Expected: exit code `0` and no `<failure>` element in
`/tmp/loco-core-quality-editmode.xml`.

When Unity is open, use UnityMCP or **Window > General > Test Runner > EditMode >
Run All**. For UnityMCP, inspect `mcpforunity://custom-tools`, active instances,
editor state, and console before mutations.

Run documentation verification after documentation changes:

```bash
npx --yes markdownlint-cli2 \
  docs/superpowers/plans/2026-06-11-core-match-quality.md \
  docs/superpowers/specs/2026-06-11-core-match-quality-design.md \
  Documentation/AnimationContract.md \
  Documentation/DesignDoc.md \
  Documentation/TestingChecklist.md \
  Documentation/FutureAssetIntegration.md \
  Documentation/KnowledgeBase/BestPractices.md
git diff --check
```

## Spec Coverage Map

- Directional-read reversal data and pure resolution: Task 1.
- Shared player/CPU reversal outcomes and diagnostics: Task 2.
- Persistent SPECIAL readiness and exact-once spending: Task 3.
- Position, stamina, and effort submission math: Task 4.
- Player/CPU submission crawl and release invariants: Task 5.
- Bounded personality profiles and weighted decisions: Task 6.
- Repetition memory, personality integration, and AI failure exits: Task 7.
- Impact hierarchy and presentation-only semantic events: Task 8.
- Animation contract and future Animator boundary: Task 9.
- Authored defaults, generated assets, full regression, and authoritative
  documentation: Task 10.
- Failure handling is implemented in Tasks 1, 2, 5, 7, and 8 and verified
  again in Task 10.
- Automated verification is added in Tasks 1 through 8.
- Play-mode verification is incremental in Tasks 2, 3, 5, 7, and 8, then
  completed as a full matrix in Task 10.

## Task 1: Add Reversal Read Primitives And Move Metadata

**Files:**

- Create: `Assets/Scripts/Combat/ReversalRead.cs`
- Create: `Assets/Scripts/Editor/ReversalReadResolverTests.cs`
- Modify: `Assets/Scripts/Moves/MoveData.cs`
- Modify: `Assets/Scripts/Editor/MoveDataValidator.cs`
- Modify: `Assets/Scripts/Editor/MoveDataValidatorTests.cs`

- [ ] **Step 1: Write failing reversal resolver tests**

Create tests covering correct, neutral, incorrect, and disabled strong reads:

```csharp
using NUnit.Framework;

namespace LoCoFight.EditorTests
{
    public class ReversalReadResolverTests
    {
        [Test]
        public void Resolve_CorrectDirectionalReadIsStrong()
        {
            Assert.That(
                ReversalReadResolver.Resolve(
                    ReversalReadDirection.Toward,
                    MoveDirection.Forward,
                    hasDirectionalInput: true,
                    allowsStrongCounter: true),
                Is.EqualTo(ReversalOutcome.Strong));
        }

        [TestCase(MoveDirection.Neutral, false)]
        [TestCase(MoveDirection.Left, true)]
        public void Resolve_NeutralOrIncorrectReadIsBasic(
            MoveDirection submitted,
            bool hasDirection)
        {
            Assert.That(
                ReversalReadResolver.Resolve(
                    ReversalReadDirection.Toward,
                    submitted,
                    hasDirection,
                    allowsStrongCounter: true),
                Is.EqualTo(ReversalOutcome.Basic));
        }

        [Test]
        public void Resolve_DisabledStrongCounterIsBasic()
        {
            Assert.That(
                ReversalReadResolver.Resolve(
                    ReversalReadDirection.Left,
                    MoveDirection.Left,
                    hasDirectionalInput: true,
                    allowsStrongCounter: false),
                Is.EqualTo(ReversalOutcome.Basic));
        }
    }
}
```

- [ ] **Step 2: Run the focused tests and verify failure**

Run the Unity EditMode suite or the new test class.

Expected: compile failure because `ReversalReadDirection`,
`ReversalOutcome`, and `ReversalReadResolver` do not exist.

- [ ] **Step 3: Add reversal read types and pure resolution**

Create:

```csharp
namespace LoCoFight
{
    public enum ReversalReadDirection
    {
        Neutral,
        Toward,
        Away,
        Left,
        Right
    }

    public enum ReversalOutcome
    {
        Basic,
        Strong
    }

    public static class ReversalReadResolver
    {
        public static ReversalOutcome Resolve(
            ReversalReadDirection preferred,
            MoveDirection submitted,
            bool hasDirectionalInput,
            bool allowsStrongCounter)
        {
            if (!allowsStrongCounter || !hasDirectionalInput)
                return ReversalOutcome.Basic;

            MoveDirection expected = preferred == ReversalReadDirection.Toward
                ? MoveDirection.Forward
                : preferred == ReversalReadDirection.Away
                    ? MoveDirection.Backward
                    : preferred == ReversalReadDirection.Left
                        ? MoveDirection.Left
                        : preferred == ReversalReadDirection.Right
                            ? MoveDirection.Right
                            : MoveDirection.Neutral;

            return submitted == expected && expected != MoveDirection.Neutral
                ? ReversalOutcome.Strong
                : ReversalOutcome.Basic;
        }
    }
}
```

- [ ] **Step 4: Extend `MoveData` with backward-compatible defaults**

Add a `Reversal read` header:

```csharp
public ReversalReadDirection preferredCounterDirection =
    ReversalReadDirection.Neutral;
public bool allowsStrongDirectionalCounter;
public float basicReversalMomentum = 8f;
public float strongReversalMomentum = 14f;
public float basicReversalStagger = 0.8f;
public float strongReversalStagger = 1.2f;
public float basicReversalSeparation = 0.7f;
public float strongReversalSeparation = 1.25f;
public string basicReversalPresentationId = "reversal-basic";
public string strongReversalPresentationId = "reversal-strong";
```

Keep `momentumGainOnReversal` temporarily for serialized compatibility. Mark it
with a comment explaining that Task 2 reads it only as a legacy fallback when
`basicReversalMomentum <= 0`.

- [ ] **Step 5: Add structural validation**

Add errors for negative momentum, stagger, or separation and for a strong
counter with a neutral preferred direction:

```csharp
if (move.allowsStrongDirectionalCounter &&
    move.preferredCounterDirection == ReversalReadDirection.Neutral)
    errors.Add($"{id}: strong directional counter requires a direction.");
if (move.basicReversalMomentum < 0f || move.strongReversalMomentum < 0f)
    errors.Add($"{id}: reversal momentum cannot be negative.");
if (move.basicReversalStagger < 0f || move.strongReversalStagger < 0f)
    errors.Add($"{id}: reversal stagger cannot be negative.");
if (move.basicReversalSeparation < 0f || move.strongReversalSeparation < 0f)
    errors.Add($"{id}: reversal separation cannot be negative.");
```

Add validator tests for each invalid family and a well-formed strong counter.

- [ ] **Step 6: Run tests and offline compile**

Expected: all edit-mode tests pass; offline compile exits `0`.

- [ ] **Step 7: Commit the reversal data foundation**

```bash
git add \
  Assets/Scripts/Combat/ReversalRead.cs \
  Assets/Scripts/Combat/ReversalRead.cs.meta \
  Assets/Scripts/Editor/ReversalReadResolverTests.cs \
  Assets/Scripts/Editor/ReversalReadResolverTests.cs.meta \
  Assets/Scripts/Moves/MoveData.cs \
  Assets/Scripts/Editor/MoveDataValidator.cs \
  Assets/Scripts/Editor/MoveDataValidatorTests.cs
git commit -m "Add directional reversal read data"
```

## Task 2: Resolve Basic And Strong Reversals Through `WrestlerCombat`

**Files:**

- Create: `Assets/Scripts/Editor/ReversalRuntimeRulesTests.cs`
- Modify: `Assets/Scripts/Combat/ReversalSystem.cs`
- Modify: `Assets/Scripts/Combat/WrestlerCombat.cs`
- Modify: `Assets/Scripts/Input/PlayerInputController.cs`
- Modify: `Assets/Scripts/AI/CPUWrestlerAI.cs`
- Modify: `Assets/Scripts/UI/DebugOverlay.cs`

- [ ] **Step 1: Write failing pure runtime-rule tests**

Extract outcome values into `ReversalSystem.ResolveOutcome` and test legacy
fallback plus strong values:

```csharp
[Test]
public void ResolveOutcome_UsesLegacyMomentumWhenBasicValueIsUnset()
{
    var move = ScriptableObject.CreateInstance<MoveData>();
    move.basicReversalMomentum = 0f;
    move.momentumGainOnReversal = 7f;

    var result = ReversalSystem.ResolveOutcome(move, ReversalOutcome.Basic);

    Assert.That(result.Momentum, Is.EqualTo(7f));
}

[Test]
public void ResolveOutcome_StrongUsesStrongAuthoredValues()
{
    var move = ScriptableObject.CreateInstance<MoveData>();
    move.strongReversalMomentum = 15f;
    move.strongReversalStagger = 1.3f;
    move.strongReversalSeparation = 1.4f;

    var result = ReversalSystem.ResolveOutcome(move, ReversalOutcome.Strong);

    Assert.That(result.Momentum, Is.EqualTo(15f));
    Assert.That(result.Stagger, Is.EqualTo(1.3f));
    Assert.That(result.Separation, Is.EqualTo(1.4f));
}
```

- [ ] **Step 2: Add `ReversalOutcomeData` and resolution**

Add:

```csharp
public readonly struct ReversalOutcomeData
{
    public readonly float Momentum;
    public readonly float Stagger;
    public readonly float Separation;
    public readonly string PresentationId;

    public ReversalOutcomeData(
        float momentum,
        float stagger,
        float separation,
        string presentationId)
    {
        Momentum = momentum;
        Stagger = stagger;
        Separation = separation;
        PresentationId = presentationId;
    }
}
```

`ResolveOutcome` must return safe built-in defaults when `move == null`, use
`momentumGainOnReversal` only for legacy basic momentum fallback, and never
mutate the move.

- [ ] **Step 3: Change the shared reversal API**

Change:

```csharp
public bool TryReversal(
    MoveDirection submittedDirection = MoveDirection.Neutral,
    bool hasDirectionalInput = false)
```

Inside the normal-move branch:

1. Resolve `Basic` or `Strong` from the attacker's `CurrentMove`.
2. Validate and spend stamina exactly once.
3. Interrupt the attacker.
4. Apply authored momentum, stagger, and separation.
5. Record `LastReversalOutcome`, `LastReversalRead`, and
   `LastReversalPresentationId`.

Grapple-lock escapes and special reversals remain basic in this milestone.

- [ ] **Step 4: Apply safe separation**

Add a helper in `WrestlerCombat`:

```csharp
void SeparateAfterReversal(WrestlerCore attacker, float distance)
{
    if (attacker == null || distance <= 0f) return;
    Vector3 away = MathUtil.FlatDirection(
        attacker.transform.position,
        _core.transform.position);
    _core.Motor.ApplyKnockback(away, distance * 0.5f);
    attacker.Motor.ApplyKnockback(-away, distance * 0.5f);
}
```

Do not teleport, bypass ring bounds deliberately, or add root-motion ownership.

- [ ] **Step 5: Pass player camera-relative direction**

Reuse `ResolveGrappleDirection(frame)`:

```csharp
if (frame.ReversalPressed)
{
    MoveDirection read = ResolveGrappleDirection(frame);
    bool directional = read != MoveDirection.Neutral;
    _core.Combat.TryReversal(read, directional);
}
```

This preserves one direction frame across movement, grapples, rolls, and
reversals.

- [ ] **Step 6: Pass CPU reversal reads**

Add a pure selector that receives a `0..1` roll:

```csharp
static MoveDirection ChooseReversalRead(
    ReversalReadDirection preferred,
    AIPersonality personality,
    float roll)
```

The selector may bias personality but must not read or alter
`reversalAccuracy`. `ReactDefensively` performs the accuracy roll first, then
submits the selected direction to the shared combat API.

- [ ] **Step 7: Add F1 diagnostics**

Display:

```text
Reversal: read=Forward outcome=Strong presentation=reversal-strong
```

Keep diagnostics transient and read-only.

- [ ] **Step 8: Run tests, compile, and manual reversal matrix**

Verify:

- Neutral reversal inside a window remains successful.
- Correct authored direction produces strong outcome.
- Incorrect direction produces basic outcome.
- Late attempt spends no stamina.
- Unaffordable attempt changes no state.
- Grapple lock and special reversals still work.
- Player and CPU both call `TryReversal(direction, hasDirection)`.

- [ ] **Step 9: Commit runtime reversal integration**

```bash
git add \
  Assets/Scripts/Editor/ReversalRuntimeRulesTests.cs \
  Assets/Scripts/Editor/ReversalRuntimeRulesTests.cs.meta \
  Assets/Scripts/Combat/ReversalSystem.cs \
  Assets/Scripts/Combat/WrestlerCombat.cs \
  Assets/Scripts/Input/PlayerInputController.cs \
  Assets/Scripts/AI/CPUWrestlerAI.cs \
  Assets/Scripts/UI/DebugOverlay.cs
git commit -m "Add basic and strong reversal outcomes"
```

## Task 3: Add Persistent SPECIAL Readiness Transitions

**Files:**

- Create: `Assets/Scripts/Editor/MomentumReadinessTests.cs`
- Modify: `Assets/Scripts/Wrestlers/WrestlerStatsRuntime.cs`
- Modify: `Assets/Scripts/Specials/SpecialController.cs`
- Modify: `Assets/Scripts/UI/MeterBar.cs`
- Modify: `Assets/Scripts/UI/MatchHUD.cs`
- Modify: `Assets/Scripts/UI/DebugOverlay.cs`

- [ ] **Step 1: Write failing readiness-transition tests**

Test a pure threshold helper:

```csharp
[Test]
public void UpdateReadiness_FiresOnlyOnThresholdTransitions()
{
    Assert.That(
        WrestlerStatsRuntime.ResolveReadinessTransition(false, 1f),
        Is.EqualTo(1));
    Assert.That(
        WrestlerStatsRuntime.ResolveReadinessTransition(true, 1f),
        Is.EqualTo(0));
    Assert.That(
        WrestlerStatsRuntime.ResolveReadinessTransition(true, 0.5f),
        Is.EqualTo(-1));
}
```

Use `1`, `0`, and `-1` for became-ready, unchanged, and left-ready so the test
does not require a MonoBehaviour instance.

- [ ] **Step 2: Add exact transition ownership to stats**

Add:

```csharp
public bool IsSpecialReady { get; private set; }
public event System.Action<bool> OnSpecialReadyChanged;
```

After every momentum mutation and reset, call one private method that:

1. Computes `Momentum >= MaxMomentum - 0.01f`.
2. Returns immediately when unchanged.
3. Stores the new readiness.
4. Invokes `OnSpecialReadyChanged` exactly once.

`HasFullMomentum` should return `IsSpecialReady`.

- [ ] **Step 3: Verify special resource spending order**

Keep validation before spending. Change `SpecialController.TryActivate` only as
needed to make the exact order obvious:

```csharp
if (!IsCurrentlyValid(out string reason)) return Reject(reason);
SpendActivationResources();
BeginExecution();
```

Do not spend or refund resources from animation callbacks.

- [ ] **Step 4: Add a persistent ready treatment to `MeterBar`**

Add:

```csharp
Color _baseColor;

public void SetReady(bool ready)
{
    if (fill == null) return;
    fill.color = ready
        ? new Color(1f, 0.65f, 0.1f)
        : _baseColor;
}
```

Cache the original fill color during creation.

- [ ] **Step 5: Subscribe the HUD to readiness changes**

In `BindWrestlers`, unsubscribe old bindings before subscribing new ones.

On player transition to ready:

- Set the player momentum bar ready treatment.
- Show `SPECIAL READY` once.
- Record the event for presentation in Task 9.

On CPU transition, update only the CPU bar treatment. On leaving readiness,
restore the base treatment.

- [ ] **Step 6: Run tests and play-mode resource checks**

Verify:

- Reaching full momentum shows one message.
- Remaining full does not repeat the message.
- Failed special attempts preserve stamina and momentum.
- Successful activation spends momentum and stamina once.
- Reset clears ready treatment.

- [ ] **Step 7: Commit SPECIAL readiness**

```bash
git add \
  Assets/Scripts/Editor/MomentumReadinessTests.cs \
  Assets/Scripts/Editor/MomentumReadinessTests.cs.meta \
  Assets/Scripts/Wrestlers/WrestlerStatsRuntime.cs \
  Assets/Scripts/Specials/SpecialController.cs \
  Assets/Scripts/UI/MeterBar.cs \
  Assets/Scripts/UI/MatchHUD.cs \
  Assets/Scripts/UI/DebugOverlay.cs
git commit -m "Add persistent special readiness feedback"
```

## Task 4: Add Pure Submission Escape And Crawl Rules

**Files:**

- Create: `Assets/Scripts/Combat/SubmissionEscapeRules.cs`
- Create: `Assets/Scripts/Editor/SubmissionEscapeRulesTests.cs`

- [ ] **Step 1: Write failing crawl-direction tests**

Cover toward, sideways, away, missing direction, stamina scaling, and no-rope
escape contribution:

```csharp
[TestCase(1f, 1f)]
[TestCase(0f, 0.35f)]
[TestCase(-1f, 0f)]
public void DirectionQuality_MapsDotToCrawlStrength(float dot, float expected)
{
    Assert.That(
        SubmissionEscapeRules.DirectionQuality(dot),
        Is.EqualTo(expected).Within(0.001f));
}

[Test]
public void CrawlRate_LowStaminaIsWeaker()
{
    float full = SubmissionEscapeRules.CrawlRate(1f, 1f, 0.5f);
    float tired = SubmissionEscapeRules.CrawlRate(1f, 0.1f, 0.5f);
    Assert.That(tired, Is.LessThan(full));
}

[Test]
public void CrawlWithoutRopeBreaksConvertsToReducedEscape()
{
    Assert.That(
        SubmissionEscapeRules.CrawlEscapeRate(1f, ropeBreaksActive: false),
        Is.GreaterThan(0f));
}
```

- [ ] **Step 2: Implement bounded pure formulas**

Use:

```csharp
public const float BaseCrawlSpeed = 0.45f;
public const float CrawlStaminaPerSecond = 7f;
public const float NoRopeBreakEscapePerSecond = 5f;

public static float DirectionQuality(float dot)
{
    if (dot >= 0.5f) return 1f;
    if (dot > -0.25f) return 0.35f;
    return 0f;
}

public static float StaminaFactor(float staminaPercent) =>
    Mathf.Lerp(0.25f, 1f, Mathf.Clamp01(staminaPercent));

public static float CrawlRate(
    float directionQuality,
    float staminaPercent,
    float submissionResistance) =>
    BaseCrawlSpeed *
    Mathf.Clamp01(directionQuality) *
    StaminaFactor(staminaPercent) *
    Mathf.Lerp(0.8f, 1.2f, Mathf.Clamp01(submissionResistance));

public static float ActiveEscapePerPress(
    float staminaPercent,
    float escapeMultiplier,
    float escapePenalty) =>
    4f *
    StaminaFactor(staminaPercent) *
    Mathf.Max(0f, escapeMultiplier) *
    Mathf.Clamp01(1f - escapePenalty);
```

- [ ] **Step 3: Add pair-offset and clamp-independent tests**

Test that applying the same delta to attacker and defender preserves their
relative offset. Keep ring clamping out of this helper; it remains owned by
`RingInteractionSystem`.

- [ ] **Step 4: Run tests and compile**

Expected: submission rule tests pass; offline compile exits `0`.

- [ ] **Step 5: Commit pure submission rules**

```bash
git add \
  Assets/Scripts/Combat/SubmissionEscapeRules.cs \
  Assets/Scripts/Combat/SubmissionEscapeRules.cs.meta \
  Assets/Scripts/Editor/SubmissionEscapeRulesTests.cs \
  Assets/Scripts/Editor/SubmissionEscapeRulesTests.cs.meta
git commit -m "Add positional submission escape rules"
```

## Task 5: Integrate Submission Crawl For Player And CPU

**Files:**

- Modify: `Assets/Scripts/Combat/SubmissionSystem.cs`
- Modify: `Assets/Scripts/Input/PlayerInputController.cs`
- Modify: `Assets/Scripts/AI/CPUWrestlerAI.cs`
- Modify: `Assets/Scripts/UI/MatchHUD.cs`
- Modify: `Assets/Scripts/UI/DebugOverlay.cs`

- [ ] **Step 1: Add shared submission intent APIs**

Add:

```csharp
Vector3 _crawlIntent;

public void SetDefenderCrawlIntent(WrestlerCore defender, Vector3 worldIntent)
{
    if (!Active || defender != Defender) return;
    _crawlIntent = MathUtil.Flat(worldIntent);
    if (_crawlIntent.sqrMagnitude > 1f) _crawlIntent.Normalize();
}

public void ClearDefenderCrawlIntent(WrestlerCore defender)
{
    if (defender == Defender) _crawlIntent = Vector3.zero;
}
```

Change `AddPlayerEscapeEffort` into shared:

```csharp
public void AddEscapeEffort(WrestlerCore defender)
```

Use `SubmissionEscapeRules.ActiveEscapePerPress`.

- [ ] **Step 2: Convert player movement during submissions**

Before ordinary `HandleMovement`, detect the player defending an active
submission. Map camera-relative movement to world intent, pass it to
`SubmissionSystem`, set motor input to zero, and return.

On neutral input, pause, reset, match end, or release, clear crawl intent.

- [ ] **Step 3: Move the pair toward the nearest legal rope**

In `SubmissionSystem.Update`:

1. Query nearest rope side and contact point.
2. Compute defender-to-rope direction.
3. Compute input dot and direction quality.
4. Calculate crawl rate.
5. Drain crawl stamina only when rate is positive.
6. Apply the same clamped delta to attacker and defender via `Motor.Teleport`.

Wrap scripted movement:

```csharp
Attacker.Motor.SetScriptedControl(true);
Defender.Motor.SetScriptedControl(true);
```

Enable it when the hold starts and disable it in one cleanup method used by
every release path.

- [ ] **Step 4: Handle no-rope-break rules**

When `Rules.RopeBreaksActive` is false:

- Never release from rope contact.
- Convert positive crawl quality into
  `SubmissionEscapeRules.CrawlEscapeRate(...) * dt`.
- Keep positions clamped inside the ring.

- [ ] **Step 5: Add CPU crawl and effort**

When the CPU is the defender:

- Always crawl toward the nearest rope when rope breaks are active.
- With rope breaks disabled, submit a toward-rope intent for reduced escape
  gain.
- Add active escape effort on a difficulty-gated cadence, not every frame.
- Do not reuse reversal accuracy for submission escape.

- [ ] **Step 6: Centralize release cleanup**

Create one cleanup path that:

- Sets `Active = false`.
- Clears `_crawlIntent`.
- Disables scripted control for both wrestlers.
- Restores attacker and defender states.
- Restores `MatchState.Active` only when currently in
  `SubmissionInProgress`.
- Emits exactly one semantic presentation event.
- Clears ownership references after cleanup.

Tap-out must call cleanup before `AnnounceWin` so `AnnounceWin` does not inherit
stale submission ownership.

- [ ] **Step 7: Update HUD and F1**

Show:

```text
Submission: Arm Lock pressure 42 escape 31 rope 1.8 crawl 0.34
```

During player defense, label movement as `Crawl to ropes` while preserving mash
guidance.

- [ ] **Step 8: Run the submission play matrix**

Verify:

- Toward movement visibly moves both wrestlers together.
- Sideways is slower.
- Away produces no movement or stamina drain.
- Low stamina weakens movement and mash.
- Standard rules break immediately on rope contact.
- No-rope-break rules never force release.
- Escape, rope break, tap-out, reset, and match end clear scripted control.

- [ ] **Step 9: Commit runtime submission integration**

```bash
git add \
  Assets/Scripts/Combat/SubmissionSystem.cs \
  Assets/Scripts/Input/PlayerInputController.cs \
  Assets/Scripts/AI/CPUWrestlerAI.cs \
  Assets/Scripts/UI/MatchHUD.cs \
  Assets/Scripts/UI/DebugOverlay.cs
git commit -m "Add positional submission defense"
```

## Task 6: Add Focused Personality Profiles

**Files:**

- Create: `Assets/Scripts/AI/AIPersonalityProfile.cs`
- Create: `Assets/Scripts/AI/AIDecisionWeights.cs`
- Create: `Assets/Scripts/Editor/AIPersonalityProfileTests.cs`
- Create: `Assets/Scripts/Editor/AIDecisionWeightsTests.cs`

- [ ] **Step 1: Write failing profile coverage and bounds tests**

Test every enum value:

```csharp
[Test]
public void For_EveryPersonalityReturnsBoundedProfile()
{
    foreach (AIPersonality personality in
             System.Enum.GetValues(typeof(AIPersonality)))
    {
        var profile = AIPersonalityProfiles.For(personality);
        Assert.That(profile.Aggression, Is.InRange(0.75f, 1.25f));
        Assert.That(profile.Strike, Is.InRange(0.75f, 1.25f));
        Assert.That(profile.Grapple, Is.InRange(0.75f, 1.25f));
        Assert.That(profile.RepetitionTolerance, Is.InRange(0.5f, 1.5f));
    }
}
```

Add identity tests:

- Technician favors grapples/submissions over strikes.
- Powerhouse favors power moves and lower risk.
- HighFlyer favors rope/corner strategy and risk.
- Showman has more breathing room and special setup.
- Evasive has lower aggression.
- Unknown values return Balanced.

- [ ] **Step 2: Add immutable profiles**

Define explicit fields for all spec modifiers. Use a constructor that clamps
every multiplier. `Balanced` uses `1f` for every field.

Do not create ScriptableObjects or modify difficulty assets.

- [ ] **Step 3: Add pure decision-weight composition**

Implement methods such as:

```csharp
public static float Apply(
    float baseWeight,
    float personalityMultiplier,
    float repetitionPenalty) =>
    Mathf.Clamp01(
        Mathf.Clamp01(baseWeight) *
        Mathf.Clamp(personalityMultiplier, 0.75f, 1.25f) *
        Mathf.Clamp01(1f - repetitionPenalty));
```

Add `ChooseWeighted(float roll, params WeightedAIAction[] actions)` that is
deterministic for a supplied roll and returns `AIState.IdleThink` when no
positive weight exists.

- [ ] **Step 4: Test that personality cannot alter accuracy**

`AIDecisionWeights` accepts no reversal-accuracy or dodge-accuracy values.
Tests should prove profiles contain no accuracy fields and only decision
multipliers.

- [ ] **Step 5: Run tests and compile**

Expected: all profile and weight tests pass; offline compile exits `0`.

- [ ] **Step 6: Commit personality primitives**

```bash
git add \
  Assets/Scripts/AI/AIPersonalityProfile.cs \
  Assets/Scripts/AI/AIPersonalityProfile.cs.meta \
  Assets/Scripts/AI/AIDecisionWeights.cs \
  Assets/Scripts/AI/AIDecisionWeights.cs.meta \
  Assets/Scripts/Editor/AIPersonalityProfileTests.cs \
  Assets/Scripts/Editor/AIPersonalityProfileTests.cs.meta \
  Assets/Scripts/Editor/AIDecisionWeightsTests.cs \
  Assets/Scripts/Editor/AIDecisionWeightsTests.cs.meta
git commit -m "Add focused AI personality profiles"
```

## Task 7: Expand AI Memory And Integrate Personality Decisions

**Files:**

- Create: `Assets/Scripts/Editor/AIMemoryTests.cs`
- Modify: `Assets/Scripts/AI/AIMemory.cs`
- Modify: `Assets/Scripts/AI/CPUWrestlerAI.cs`
- Modify: `Assets/Scripts/UI/DebugOverlay.cs`

- [ ] **Step 1: Write failing memory tests with injected time**

Replace direct `Time.time` dependency with explicit timestamps:

```csharp
[Test]
public void RepetitionPenalty_SuccessCountsMoreThanAttempt()
{
    var memory = new AIMemory();
    memory.NoteAttempt("grapple", 10f);
    float afterAttempt = memory.RepetitionPenalty("grapple", 10f);
    memory.NoteSuccess("grapple", 11f);
    float afterSuccess = memory.RepetitionPenalty("grapple", 11f);

    Assert.That(afterSuccess, Is.GreaterThan(afterAttempt));
}

[Test]
public void RepetitionPenalty_DecaysOverTime()
{
    var memory = new AIMemory();
    memory.NoteSuccess("strike", 0f);

    Assert.That(
        memory.RepetitionPenalty("strike", 8f),
        Is.LessThan(memory.RepetitionPenalty("strike", 1f)));
}
```

- [ ] **Step 2: Implement attempt/success memory**

Track per-family:

- Last attempt time.
- Last success time.
- Consecutive attempts.
- Consecutive successes.

Provide:

```csharp
public bool CanUse(string family, float cooldown, float now)
public void NoteAttempt(string family, float now)
public void NoteSuccess(string family, float now)
public float RepetitionPenalty(string family, float now)
public string DebugSummary(float now)
```

Retain compatibility overloads using `Time.time` only if needed during
migration.

- [ ] **Step 3: Record successes from combat outcomes**

Subscribe CPU initialization to:

- `WrestlerCombat.OnLandedHit`
- `WrestlerCombat.OnReversedOpponent`

Map landed moves to action families. Unsubscribe in `OnDestroy`.

- [ ] **Step 4: Apply personality to existing decisions**

At bind/reset:

```csharp
_personality = AIPersonalityProfiles.For(
    _core.Stats.Data != null
        ? _core.Stats.Data.aiPersonality
        : AIPersonality.Balanced);
```

Use personality and repetition penalties for:

- Breather gate.
- Strike versus grapple.
- Power follow-up.
- Ground attack.
- Pin threshold adjustment.
- Submission threshold adjustment.
- Special setup.
- Rope/corner strategy.

Keep obvious contexts ahead of weighted neutral decisions.

- [ ] **Step 5: Fix all `Try*` failure exits touched by this task**

For every modified `Act` branch:

```csharp
bool succeeded = _core.Combat.TryLightStrike();
_memory.NoteAttempt("light", Time.time);
if (succeeded) Rethink();
else CurrentState = AIState.IdleThink;
```

Never call `Rethink()` unconditionally after a failed attempt.

- [ ] **Step 6: Add diagnostics**

Display personality, key modifiers, recent family, repetition penalty, and
selected family:

```text
AI: Technician state=AttemptSubmission selected=submission rep=0.20
weights strike=0.39 grapple=0.64 special=0.72
```

- [ ] **Step 7: Run deterministic tests and personality smoke tests**

Use F2 to compare:

- Technician versus Brawler.
- HighFlyer versus Powerhouse.
- Showman versus Evasive.

Observe preferences over several decisions, not one roll. Confirm difficulty
still solely controls reversal and dodge accuracy.

- [ ] **Step 8: Commit AI integration**

```bash
git add \
  Assets/Scripts/Editor/AIMemoryTests.cs \
  Assets/Scripts/Editor/AIMemoryTests.cs.meta \
  Assets/Scripts/AI/AIMemory.cs \
  Assets/Scripts/AI/CPUWrestlerAI.cs \
  Assets/Scripts/UI/DebugOverlay.cs
git commit -m "Apply wrestler personality to CPU decisions"
```

## Task 8: Add Semantic Presentation Events

**Files:**

- Create: `Assets/Scripts/Presentation/CombatPresentationEvent.cs`
- Create: `Assets/Scripts/Editor/CombatPresentationRulesTests.cs`
- Modify: `Assets/Scripts/Combat/FeelSystem.cs`
- Modify: `Assets/Scripts/Animation/IAnimationDriver.cs`
- Modify: `Assets/Scripts/Animation/PlaceholderAnimationDriver.cs`
- Modify: `Assets/Scripts/Combat/WrestlerCombat.cs`
- Modify: `Assets/Scripts/Combat/SubmissionSystem.cs`
- Modify: `Assets/Scripts/Wrestlers/WrestlerStatsRuntime.cs`

- [ ] **Step 1: Write failing presentation mapping tests**

Test pure event-to-feel settings:

```csharp
[Test]
public void StrongReversalIsStrongerThanBasicReversal()
{
    var basic = CombatPresentationRules.For(
        CombatPresentationEvent.BasicReversal);
    var strong = CombatPresentationRules.For(
        CombatPresentationEvent.StrongReversal);

    Assert.That(strong.HitStopSeconds, Is.GreaterThan(basic.HitStopSeconds));
    Assert.That(strong.CameraStrength, Is.GreaterThan(basic.CameraStrength));
}

[Test]
public void RopeBreakHasNoHeavyHitStop()
{
    var result = CombatPresentationRules.For(
        CombatPresentationEvent.RopeBreak);
    Assert.That(result.HitStopSeconds, Is.LessThanOrEqualTo(0.02f));
}
```

- [ ] **Step 2: Define semantic events and pure feel rules**

Use:

```csharp
public enum CombatPresentationEvent
{
    LightImpact,
    HeavyImpact,
    BasicReversal,
    StrongReversal,
    SpecialImpact,
    SubmissionEscape,
    RopeBreak,
    TapOut,
    SpecialReady
}
```

Return explicit hit-stop and camera values. Preserve
`FeelSystem.NotifyImpact(MoveTier, bool)` as a compatibility adapter that maps
to the new events.

- [ ] **Step 3: Extend `IAnimationDriver` semantically**

Add:

```csharp
void TriggerReversal(bool strong, string presentationId);
void SetSpecialReady(bool ready);
void TriggerSubmissionApply(bool attacker);
void TriggerSubmissionStruggle();
void TriggerSubmissionRelease(bool ropeBreak, bool escaped);
void TriggerSubmissionTapOut();
```

Replace the old parameterless `TriggerReversal` only after all call sites are
migrated in the same task.

- [ ] **Step 4: Implement procedural placeholder presentation**

Add distinct procedural actions:

- Basic reversal: cyan flash plus short recoil/redirect pose.
- Strong reversal: brighter cyan flash plus larger counter pose.
- SPECIAL ready: persistent emissive/color accent without changing state.
- Submission apply/struggle: reuse current state poses plus one-shot accents.
- Rope break/escape: release recoil.
- Tap-out: repeated arm/hand motion or magenta defeat accent.

Presentation must never move the gameplay root or mutate meters.

- [ ] **Step 5: Emit events only after resolved outcomes**

- `WrestlerCombat`: basic/strong reversal after stamina spending and state
  resolution.
- `SubmissionSystem`: escape, rope break, and tap-out from the centralized
  cleanup path.
- `WrestlerStatsRuntime`: SPECIAL ready from the threshold transition.
- Special executors: continue using Special-tier impact where already resolved.

- [ ] **Step 6: Preserve no-feel gameplay equivalence**

Before and after calling presentation, capture:

- Health.
- Stamina.
- Momentum.
- Wrestler states.
- Root positions.
- Match state.

The testable presentation-rule layer must not accept or return gameplay values.
In play mode, repeat one reversal and one submission escape with
`FeelSystem.Enabled` true and false and compare outcomes.

- [ ] **Step 7: Run tests and compile**

Expected: all presentation tests pass; offline compile exits `0`.

- [ ] **Step 8: Commit semantic presentation**

```bash
git add \
  Assets/Scripts/Presentation/CombatPresentationEvent.cs \
  Assets/Scripts/Presentation/CombatPresentationEvent.cs.meta \
  Assets/Scripts/Editor/CombatPresentationRulesTests.cs \
  Assets/Scripts/Editor/CombatPresentationRulesTests.cs.meta \
  Assets/Scripts/Combat/FeelSystem.cs \
  Assets/Scripts/Animation/IAnimationDriver.cs \
  Assets/Scripts/Animation/PlaceholderAnimationDriver.cs \
  Assets/Scripts/Combat/WrestlerCombat.cs \
  Assets/Scripts/Combat/SubmissionSystem.cs \
  Assets/Scripts/Wrestlers/WrestlerStatsRuntime.cs
git commit -m "Add semantic combat presentation events"
```

## Task 9: Add The Project Animation Contract

**Files:**

- Create: `Documentation/AnimationContract.md`
- Modify: `Documentation/FutureAssetIntegration.md`
- Modify: `Documentation/KnowledgeBase/BestPractices.md`

- [ ] **Step 1: Write the animation contract**

Include a table with these exact semantic rows:

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

State explicitly:

- Animation markers cannot apply damage or resolve gameplay.
- Root motion never owns authoritative positions.
- `MoveData` and gameplay systems own timing.
- Attacker/defender clips must share named synchronization markers.

- [ ] **Step 2: Update future integration guidance**

Document how a future `AnimatorAnimationDriver` maps semantic methods and move
identifiers to Animator parameters. Keep `examplecode/` named as informational,
not production source.

- [ ] **Step 3: Promote the durable best practice carefully**

`Documentation/KnowledgeBase/BestPractices.md` is already modified. Read the
current diff first:

```bash
git diff -- Documentation/KnowledgeBase/BestPractices.md
```

Append only a concise animation-contract rule. Preserve every pre-existing user
change.

- [ ] **Step 4: Lint documentation**

Expected: Markdown lint reports `0` errors and `git diff --check` is clean.

- [ ] **Step 5: Commit documentation using exact paths**

```bash
git add \
  Documentation/AnimationContract.md \
  Documentation/FutureAssetIntegration.md \
  Documentation/KnowledgeBase/BestPractices.md
git diff --cached --name-only
git commit -m "Document combat animation contracts"
```

If `BestPractices.md` contains inseparable user changes, omit it from this
commit and report the deferred documentation line instead of staging unrelated
content.

## Task 10: Author Defaults, Regenerate Assets, And Complete Regression

**Files:**

- Modify: `Assets/Scripts/Roster/DefaultGameData.cs`
- Modify: `Assets/Scripts/Editor/MoveDataValidator.cs`
- Modify: `Documentation/DesignDoc.md`
- Modify: `Documentation/TestingChecklist.md`
- Regenerate selectively:
  `Assets/Resources/LoCoData/Moves/*.asset`

- [ ] **Step 1: Author intentional reversal reads**

Set defaults in `Move(...)`:

```csharp
m.basicReversalMomentum = Mathf.Max(6f, momentum * 0.8f);
m.strongReversalMomentum = m.basicReversalMomentum + 5f;
m.basicReversalStagger = 0.8f;
m.strongReversalStagger = 1.2f;
m.basicReversalSeparation = 0.7f;
m.strongReversalSeparation = 1.25f;
```

Assign directions deliberately after move creation:

- Straight strikes and forward-driving grapples: `Away`.
- Throws using opponent momentum: `Toward`.
- Side-control or lateral throws: `Left` or `Right`.
- Ground submissions: no strong directional counter.
- Specials remain on the existing special-reversal path.

Do not mechanically assign every move the same direction.

- [ ] **Step 2: Add validator warnings for untuned reversible moves**

Warn, but do not block generation, when a non-submission move has:

- `allowsStrongDirectionalCounter == false`.
- Empty presentation identifiers.
- Strong values not greater than basic values.

Warnings make remaining tuning visible without invalidating legacy content.

- [ ] **Step 3: Run all edit-mode tests and offline compile**

Expected: no failed tests; compile exits `0`.

- [ ] **Step 4: Regenerate default assets through Unity**

Run **Tools > LoCo Fight Game > Create Default Prototype Assets**.

Inspect Console:

- No validation errors.
- Only understood tuning warnings.

Compare asset status:

```bash
git status --short Assets/Resources/LoCoData
diff -u \
  /tmp/core-quality-assets-before.txt \
  <(git status --short Assets/Resources/LoCoData) || true
```

Review each changed move asset before staging. Preserve user-authored values
already present in dirty assets.

- [ ] **Step 5: Execute the full play-mode matrix**

Reversals:

- Neutral, correct, incorrect, late, and unaffordable attempts.
- Strike, grapple, grapple-lock, and special reversal paths.
- CPU reversal on Easy, Normal, and Hard.

SPECIAL:

- Transition to ready exactly once.
- Failed validation preserves resources.
- Successful activation spends resources once.
- Reset clears readiness.

Submissions:

- Center, sideways, near-rope, exhausted, no-rope-break, escape, rope break,
  tap-out, reset, and match-end paths.

AI:

- Technician, Brawler, HighFlyer, Powerhouse, Showman, and Evasive samples.
- No failed-action freezes.
- No action-family spam loop.

Presentation:

- Light, heavy, basic reversal, strong reversal, special, rope break, escape,
  and tap-out hierarchy.
- Repeat representative cases with `FeelSystem.Enabled = false`.

Regression:

- Movement, run, tie-up, quick/power directional grapples.
- Ground, corner, rope-stagger, and rebound offense.
- Pin, kickout, dodge, specials, pause, reset, and winner flow.

- [ ] **Step 6: Update authoritative documentation**

`Documentation/DesignDoc.md` must describe:

- One-button directional reversal outcomes.
- Persistent SPECIAL readiness.
- Position/stamina/effort submission defense.
- Difficulty versus personality separation.
- Semantic presentation ownership.

`Documentation/TestingChecklist.md` must contain the exact manual matrix from
Step 5.

- [ ] **Step 7: Run final verification**

```bash
npx --yes markdownlint-cli2 \
  Documentation/AnimationContract.md \
  Documentation/DesignDoc.md \
  Documentation/TestingChecklist.md \
  Documentation/FutureAssetIntegration.md \
  Documentation/KnowledgeBase/BestPractices.md \
  docs/superpowers/specs/2026-06-11-core-match-quality-design.md \
  docs/superpowers/plans/2026-06-11-core-match-quality.md
git diff --check
rg -n 'T[B]D|T[O]DO|F[I]XME|implement[[:space:]]+later' \
  Assets/Scripts \
  Documentation \
  docs/superpowers/specs/2026-06-11-core-match-quality-design.md \
  docs/superpowers/plans/2026-06-11-core-match-quality.md
```

Expected:

- Markdown lint: `0` errors.
- `git diff --check`: no output.
- Placeholder scan: no newly introduced matches.
- Full edit-mode suite passes.
- Offline compile exits `0`.
- Unity Console has no compile or runtime errors.

- [ ] **Step 8: Commit final data and regression documentation**

Stage exact reviewed paths only:

```bash
git add \
  Assets/Scripts/Roster/DefaultGameData.cs \
  Assets/Scripts/Editor/MoveDataValidator.cs \
  Documentation/DesignDoc.md \
  Documentation/TestingChecklist.md
```

Then add only individually reviewed generated move assets. These are the
current candidate paths; remove any path from the command if its diff is not
understood or should remain user-owned:

```bash
git add \
  Assets/Resources/LoCoData/Moves/backbreaker.asset \
  Assets/Resources/LoCoData/Moves/big-boot.asset \
  Assets/Resources/LoCoData/Moves/body-slam.asset \
  Assets/Resources/LoCoData/Moves/corner-bulldog.asset \
  Assets/Resources/LoCoData/Moves/corner-forearm.asset \
  Assets/Resources/LoCoData/Moves/ground-arm-lock.asset \
  Assets/Resources/LoCoData/Moves/ground-elbow-drop.asset \
  Assets/Resources/LoCoData/Moves/ground-head-stomp.asset \
  Assets/Resources/LoCoData/Moves/ground-knee-drop.asset \
  Assets/Resources/LoCoData/Moves/ground-leg-stomp.asset \
  Assets/Resources/LoCoData/Moves/headlock-takedown.asset \
  Assets/Resources/LoCoData/Moves/heavy-forearm.asset \
  Assets/Resources/LoCoData/Moves/knee-lift.asset \
  Assets/Resources/LoCoData/Moves/quick-jab.asset \
  Assets/Resources/LoCoData/Moves/rebound-lariat.asset \
  Assets/Resources/LoCoData/Moves/rope-chop-combination.asset \
  Assets/Resources/LoCoData/Moves/rope-snapmare.asset \
  Assets/Resources/LoCoData/Moves/running-clothesline.asset \
  Assets/Resources/LoCoData/Moves/running-tackle.asset \
  Assets/Resources/LoCoData/Moves/short-kick.asset \
  Assets/Resources/LoCoData/Moves/shoulder-throw.asset \
  Assets/Resources/LoCoData/Moves/snap-arm-drag.asset \
  Assets/Resources/LoCoData/Moves/snapmare.asset \
  Assets/Resources/LoCoData/Moves/vertical-drop.asset
```

Verify:

```bash
git diff --cached --name-only
git diff --cached --check
```

Commit:

```bash
git commit -m "Complete core match quality milestone"
```

## Final Completion Check

- [ ] Reversal uses one button plus an optional camera-relative direction.
- [ ] Valid incorrect or neutral reads still produce a basic reversal.
- [ ] Correct authored reads produce a stronger counter.
- [ ] Player and CPU use the same reversal API.
- [ ] Failed timing and insufficient stamina change no wrestler state.
- [ ] SPECIAL readiness persists and signals only on transitions.
- [ ] Special resources are spent exactly once after successful validation.
- [ ] Health, stamina, and momentum remain visible.
- [ ] Submission movement treats ring position as a defensive resource.
- [ ] Low stamina weakens crawl and active escape.
- [ ] No-rope-break rules never force a rope release.
- [ ] Every submission exit clears ownership and scripted control.
- [ ] Personality changes preference but never reaction accuracy.
- [ ] Repetition memory distinguishes attempts from successes.
- [ ] Failed CPU actions always return to a valid decision path.
- [ ] Semantic presentation never changes gameplay outcomes.
- [ ] `IAnimationDriver` supports approved reversal, SPECIAL, and submission
  outcomes.
- [ ] `Documentation/AnimationContract.md` records future animation obligations.
- [ ] Existing contextual offense, pins, specials, pause, reset, and winner flow
  remain functional.
- [ ] Automated tests, offline compile, Unity play-mode checks, Markdown lint,
  and whitespace checks all pass.
