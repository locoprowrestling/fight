# Contextual Combat Slice Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use
> superpowers:subagent-driven-development (recommended) or
> superpowers:executing-plans to implement this plan task-by-task. Steps use
> checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add grounded positional offense, directional grapples, corner offense,
rope/rebound offense, and move-tier pacing to the existing one-player-versus-CPU
prototype.

**Architecture:** Keep `WrestlerStateMachine` and `RingInteractionSystem`
authoritative for state and geometry. Add focused pure-logic context, direction,
validation, and pacing helpers; route both player and CPU requests through
`WrestlerCombat`; keep `MoveData` authoritative for timing/effects and
`IAnimationDriver` presentation-only.

**Tech Stack:** Unity 6.4, C# 9, ScriptableObjects, legacy Input Manager, NUnit
edit-mode tests, procedural placeholder animation.

---

## Execution Preconditions

- The current worktree contains unrelated and uncommitted gameplay changes. Do
  not reset, checkout, or overwrite them.
- Before using a separate worktree, first ensure the current gameplay baseline
  is committed or otherwise available there. The design and this plan alone do
  not contain the current uncommitted implementation.
- When executing in the current worktree, stage and commit exact paths only.
- Unity must import every new `.cs` file before its task commit. Include the
  generated sidecar `.meta` file for each new runtime or editor-test file.
- Regenerate `Assets/Resources/LoCoData/` after every `DefaultGameData` schema
  or content change.
- After regeneration, inspect `git status --short Assets/Resources/LoCoData` and
  stage only `StarterMoveDatabase.asset`, changed move assets, and their `.meta`
  files. Do not stage the entire `Assets/Resources` tree.
- Do not run Unity batch mode while the Unity editor has this project open.

## File Structure

### New Runtime Files

- `Assets/Scripts/Combat/CombatContext.cs` Defines `CombatContext`,
  `GroundTargetZone`, and context snapshots.
- `Assets/Scripts/Combat/CombatContextResolver.cs` Pure context and ground-zone
  resolution.
- `Assets/Scripts/Combat/MoveValidationResult.cs` Defines structured rejection
  reasons and validation results.
- `Assets/Scripts/Combat/ContextualMoveValidator.cs` Validates state, position,
  stamina, lift, and move-family compatibility.
- `Assets/Scripts/Moves/MoveDirection.cs` Defines
  neutral/forward/backward/left/right selection.
- `Assets/Scripts/Moves/MoveTier.cs` Defines light/medium/heavy/special pacing
  classes.
- `Assets/Scripts/Moves/DirectionalMoveSet.cs` Stores directional grapple
  buckets and neutral fallback selection.
- `Assets/Scripts/Moves/MovePacingRules.cs` Pure tier/stamina validation and
  editor-warning helpers.

### New Editor Test Files

- `Assets/Scripts/Editor/CombatContextResolverTests.cs`
- `Assets/Scripts/Editor/DirectionalMoveSetTests.cs`
- `Assets/Scripts/Editor/ContextualMoveValidatorTests.cs`
- `Assets/Scripts/Editor/MovePacingRulesTests.cs`
- `Assets/Scripts/Editor/MoveDataValidator.cs`
- `Assets/Scripts/Editor/MoveDataValidatorTests.cs`

### Existing Files Modified

- `Assets/Scripts/Moves/MoveCategory.cs`
- `Assets/Scripts/Moves/MoveTag.cs`
- `Assets/Scripts/Moves/MoveData.cs`
- `Assets/Scripts/Moves/MoveDatabase.cs`
- `Assets/Scripts/Input/PlayerInputLogic.cs`
- `Assets/Scripts/Input/PlayerInputController.cs`
- `Assets/Scripts/Editor/PlayerInputLogicTests.cs`
- `Assets/Scripts/Combat/WrestlerCombat.cs`
- `Assets/Scripts/AI/AIState.cs`
- `Assets/Scripts/AI/CPUWrestlerAI.cs`
- `Assets/Scripts/Roster/DefaultGameData.cs`
- `Assets/Scripts/Editor/PrototypeAssetBuilder.cs`
- `Assets/Scripts/UI/DebugOverlay.cs`
- `Assets/Scripts/Animation/PlaceholderAnimationDriver.cs`
- `Documentation/DesignDoc.md`
- `Documentation/TestingChecklist.md`
- `Documentation/KnowledgeBase/BestPractices.md`
- `Documentation/KnowledgeBase/Templates.md`

## Verification Commands

Use the repository's offline compile check after runtime-script changes:

```bash
UNITY=/Applications/Unity/Hub/Editor/6000.4.10f1/Unity.app/Contents/Resources/Scripting
ROSLYN=$(ls -d /usr/local/share/dotnet/sdk/*/Roslyn/bincore/csc.dll | tail -1)
RSP=/tmp/loco_check.rsp
: > "$RSP"
echo "-nologo -nostdlib+ -target:library -out:/tmp/loco_check.dll -langversion:9.0 -nowarn:0169,0414" >> "$RSP"
echo "-r:$UNITY/NetStandard/ref/2.1.0/netstandard.dll" >> "$RSP"
for d in "$UNITY"/Managed/UnityEngine/*.dll; do echo "-r:$d" >> "$RSP"; done
echo "-r:Library/ScriptAssemblies/UnityEngine.UI.dll" >> "$RSP"
find Assets/Scripts -name "*.cs" -not -path "*/Editor/*" >> "$RSP"
dotnet "$ROSLYN" @"$RSP"
```

Expected: exit code `0`; warnings are allowed.

When Unity is closed, run edit-mode tests with:

```bash
/Applications/Unity/Hub/Editor/6000.4.10f1/Unity.app/Contents/MacOS/Unity \
  -batchmode \
  -projectPath "$PWD" \
  -runTests \
  -testPlatform EditMode \
  -testResults /tmp/loco-editmode-results.xml \
  -quit
```

Expected: exit code `0` and no `<failure>` elements in
`/tmp/loco-editmode-results.xml`. If Unity is open, use **Window > General >
Test Runner > EditMode > Run All** instead.

### Task 1: Add Context, Direction, and Tier Primitives

**Files:**

- Create: `Assets/Scripts/Combat/CombatContext.cs`
- Create: `Assets/Scripts/Combat/CombatContextResolver.cs`
- Create: `Assets/Scripts/Moves/MoveDirection.cs`
- Create: `Assets/Scripts/Moves/MoveTier.cs`
- Create: `Assets/Scripts/Editor/CombatContextResolverTests.cs`

- [ ] **Step 1: Write failing context and ground-zone tests**

```csharp
using NUnit.Framework;
using UnityEngine;

namespace LoCoFight.EditorTests
{
    public class CombatContextResolverTests
    {
        [Test]
        public void ResolvePriority_GrappleLockWinsOverAllOtherContexts()
        {
            var result = CombatContextResolver.ResolvePriority(
                grappleLock: true,
                targetDowned: true,
                targetCornered: true,
                targetRopeStaggered: true,
                attackerRebounding: true);

            Assert.That(result, Is.EqualTo(CombatContext.GrappleLock));
        }

        [Test]
        public void ResolvePriority_DownedWinsOverCornerAndRope()
        {
            var result = CombatContextResolver.ResolvePriority(
                grappleLock: false,
                targetDowned: true,
                targetCornered: true,
                targetRopeStaggered: true,
                attackerRebounding: false);

            Assert.That(result, Is.EqualTo(CombatContext.GroundUpper));
        }

        [Test]
        public void ResolveGroundZone_UsesDefenderFacingAxis()
        {
            Assert.That(
                CombatContextResolver.ResolveGroundZone(
                    Vector3.zero, Vector3.forward, new Vector3(0f, 0f, 1f), 0.2f),
                Is.EqualTo(GroundTargetZone.Upper));
            Assert.That(
                CombatContextResolver.ResolveGroundZone(
                    Vector3.zero, Vector3.forward, new Vector3(0f, 0f, -1f), 0.2f),
                Is.EqualTo(GroundTargetZone.Lower));
        }
    }
}
```

- [ ] **Step 2: Run edit-mode tests and verify they fail**

Run the edit-mode command above.

Expected: compile failure because `CombatContextResolver`, `CombatContext`, and
`GroundTargetZone` do not exist.

- [ ] **Step 3: Add the primitive enums and pure resolver**

```csharp
// Assets/Scripts/Combat/CombatContext.cs
namespace LoCoFight
{
    public enum CombatContext
    {
        Standing,
        GrappleLock,
        GroundUpper,
        GroundLower,
        Corner,
        RopeStagger,
        RopeRebound
    }

    public enum GroundTargetZone
    {
        None,
        Upper,
        Lower
    }
}
```

```csharp
// Assets/Scripts/Moves/MoveDirection.cs
namespace LoCoFight
{
    public enum MoveDirection
    {
        Neutral,
        Forward,
        Backward,
        Left,
        Right
    }
}
```

```csharp
// Assets/Scripts/Moves/MoveTier.cs
namespace LoCoFight
{
    public enum MoveTier
    {
        Light,
        Medium,
        Heavy,
        Special
    }
}
```

```csharp
// Assets/Scripts/Combat/CombatContextResolver.cs
using UnityEngine;

namespace LoCoFight
{
    public static class CombatContextResolver
    {
        public static CombatContext ResolvePriority(
            bool grappleLock,
            bool targetDowned,
            bool targetCornered,
            bool targetRopeStaggered,
            bool attackerRebounding)
        {
            if (grappleLock) return CombatContext.GrappleLock;
            if (targetDowned) return CombatContext.GroundUpper;
            if (targetCornered) return CombatContext.Corner;
            if (targetRopeStaggered) return CombatContext.RopeStagger;
            if (attackerRebounding) return CombatContext.RopeRebound;
            return CombatContext.Standing;
        }

        public static GroundTargetZone ResolveGroundZone(
            Vector3 defenderPosition,
            Vector3 defenderForward,
            Vector3 attackerPosition,
            float threshold)
        {
            Vector3 toAttacker = MathUtil.Flat(attackerPosition - defenderPosition);
            if (toAttacker.sqrMagnitude < 0.001f) return GroundTargetZone.None;

            float dot = Vector3.Dot(
                MathUtil.Flat(defenderForward).normalized,
                toAttacker.normalized);
            if (dot >= threshold) return GroundTargetZone.Upper;
            if (dot <= -threshold) return GroundTargetZone.Lower;
            return GroundTargetZone.None;
        }
    }
}
```

- [ ] **Step 4: Run tests and offline compile**

Expected: edit-mode tests pass; offline compile exits `0`.

- [ ] **Step 5: Commit the primitives**

```bash
git add \
  Assets/Scripts/Combat/CombatContext.cs \
  Assets/Scripts/Combat/CombatContext.cs.meta \
  Assets/Scripts/Combat/CombatContextResolver.cs \
  Assets/Scripts/Combat/CombatContextResolver.cs.meta \
  Assets/Scripts/Moves/MoveDirection.cs \
  Assets/Scripts/Moves/MoveDirection.cs.meta \
  Assets/Scripts/Moves/MoveTier.cs \
  Assets/Scripts/Moves/MoveTier.cs.meta \
  Assets/Scripts/Editor/CombatContextResolverTests.cs \
  Assets/Scripts/Editor/CombatContextResolverTests.cs.meta
git commit -m "Add contextual combat primitives"
```

### Task 2: Add Directional Move Sets and Contextual Move Data

**Files:**

- Create: `Assets/Scripts/Moves/DirectionalMoveSet.cs`
- Create: `Assets/Scripts/Editor/DirectionalMoveSetTests.cs`
- Modify: `Assets/Scripts/Moves/MoveCategory.cs`
- Modify: `Assets/Scripts/Moves/MoveTag.cs`
- Modify: `Assets/Scripts/Moves/MoveData.cs`
- Modify: `Assets/Scripts/Moves/MoveDatabase.cs`

- [ ] **Step 1: Write failing directional selection tests**

```csharp
using NUnit.Framework;
using UnityEngine;

namespace LoCoFight.EditorTests
{
    public class DirectionalMoveSetTests
    {
        MoveData CreateMove(string id)
        {
            var move = ScriptableObject.CreateInstance<MoveData>();
            move.moveId = id;
            return move;
        }

        [Test]
        public void Pick_UsesRequestedBucket()
        {
            var set = new DirectionalMoveSet();
            var forward = CreateMove("forward");
            set.forward.Add(forward);

            Assert.That(set.Pick(MoveDirection.Forward, out bool fallback), Is.SameAs(forward));
            Assert.That(fallback, Is.False);
        }

        [Test]
        public void Pick_FallsBackToNeutralWhenRequestedBucketIsEmpty()
        {
            var set = new DirectionalMoveSet();
            var neutral = CreateMove("neutral");
            set.neutral.Add(neutral);

            Assert.That(set.Pick(MoveDirection.Left, out bool fallback), Is.SameAs(neutral));
            Assert.That(fallback, Is.True);
        }

        [Test]
        public void Pick_ReturnsNullWhenRequestedAndNeutralBucketsAreEmpty()
        {
            var set = new DirectionalMoveSet();
            Assert.That(set.Pick(MoveDirection.Backward, out _), Is.Null);
        }
    }
}
```

- [ ] **Step 2: Run tests and verify failure**

Expected: compile failure because `DirectionalMoveSet` does not exist.

- [ ] **Step 3: Implement directional buckets**

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LoCoFight
{
    [Serializable]
    public class DirectionalMoveSet
    {
        public List<MoveData> neutral = new List<MoveData>();
        public List<MoveData> forward = new List<MoveData>();
        public List<MoveData> backward = new List<MoveData>();
        public List<MoveData> lateral = new List<MoveData>();

        public MoveData Pick(MoveDirection direction, out bool usedFallback)
        {
            var requested = Bucket(direction);
            if (requested.Count > 0)
            {
                usedFallback = false;
                return requested[UnityEngine.Random.Range(0, requested.Count)];
            }

            usedFallback = direction != MoveDirection.Neutral && neutral.Count > 0;
            if (neutral.Count == 0) return null;
            return neutral[UnityEngine.Random.Range(0, neutral.Count)];
        }

        public IReadOnlyList<MoveData> Bucket(MoveDirection direction)
        {
            switch (direction)
            {
                case MoveDirection.Forward: return forward;
                case MoveDirection.Backward: return backward;
                case MoveDirection.Left:
                case MoveDirection.Right: return lateral;
                default: return neutral;
            }
        }
    }
}
```

- [ ] **Step 4: Extend move categories and tags**

Add to `MoveCategory`:

```csharp
GroundUpperAttack,
GroundLowerAttack,
CornerStrike,
CornerGrapple,
RopeStaggerAttack
```

Add to `MoveTag`:

```csharp
Ground,
GroundUpper,
GroundLower
```

- [ ] **Step 5: Extend `MoveData` with milestone requirements**

Add:

```csharp
[Header("Pacing")]
public MoveTier tier = MoveTier.Light;
public float minimumStamina = 0f;

[Header("Context")]
public bool requiresTargetDowned = false;
public GroundTargetZone requiredGroundZone = GroundTargetZone.None;
public bool requiresTargetCornered = false;
public bool requiresTargetRopeStaggered = false;

[Header("Fallback")]
public MoveData fallbackMove;
```

Keep existing `requiresRunning`, `requiresLift`, `fallbackMoveIfLiftFails`,
rope, and corner fields. Do not add duplicates for conditions they already
represent.

- [ ] **Step 6: Extend `MoveDatabase` without removing existing lists**

Add:

```csharp
public List<MoveData> groundUpperAttacks = new List<MoveData>();
public List<MoveData> groundLowerAttacks = new List<MoveData>();
public DirectionalMoveSet directionalQuickGrapples = new DirectionalMoveSet();
public DirectionalMoveSet directionalPowerGrapples = new DirectionalMoveSet();
public List<MoveData> cornerStrikes = new List<MoveData>();
public List<MoveData> cornerGrapples = new List<MoveData>();
public List<MoveData> ropeStaggerAttacks = new List<MoveData>();
public List<MoveData> ropeReboundAttacks = new List<MoveData>();
```

Add picker methods and include all new lists in `AllMoves`. During migration,
retain `quickGrapples` and `powerGrapples`; Task 5 will populate both legacy
lists and neutral directional buckets so saved assets and current behavior do
not break mid-plan.

Use these picker signatures:

```csharp
public MoveData RandomGroundUpperAttack() => Pick(groundUpperAttacks);
public MoveData RandomGroundLowerAttack() => Pick(groundLowerAttacks);
public MoveData RandomCornerStrike() => Pick(cornerStrikes);
public MoveData RandomCornerGrapple() => Pick(cornerGrapples);
public MoveData RandomRopeStaggerAttack() => Pick(ropeStaggerAttacks);
public MoveData RandomRopeReboundAttack() => Pick(ropeReboundAttacks);
```

- [ ] **Step 7: Run tests and offline compile**

Expected: all edit-mode tests pass; offline compile exits `0`.

- [ ] **Step 8: Commit the data model**

```bash
git add \
  Assets/Scripts/Moves/DirectionalMoveSet.cs \
  Assets/Scripts/Moves/DirectionalMoveSet.cs.meta \
  Assets/Scripts/Moves/MoveCategory.cs \
  Assets/Scripts/Moves/MoveTag.cs \
  Assets/Scripts/Moves/MoveData.cs \
  Assets/Scripts/Moves/MoveDatabase.cs \
  Assets/Scripts/Editor/DirectionalMoveSetTests.cs \
  Assets/Scripts/Editor/DirectionalMoveSetTests.cs.meta
git commit -m "Add contextual move data model"
```

### Task 3: Add Structured Move Validation

**Files:**

- Create: `Assets/Scripts/Combat/MoveValidationResult.cs`
- Create: `Assets/Scripts/Combat/ContextualMoveValidator.cs`
- Create: `Assets/Scripts/Editor/ContextualMoveValidatorTests.cs`

- [ ] **Step 1: Write failing validator tests**

```csharp
using NUnit.Framework;
using UnityEngine;

namespace LoCoFight.EditorTests
{
    public class ContextualMoveValidatorTests
    {
        [Test]
        public void ValidatePure_RejectsInsufficientStamina()
        {
            var result = ContextualMoveValidator.ValidatePure(
                moveExists: true,
                matchActive: true,
                attackerCanAct: true,
                targetStateValid: true,
                contextValid: true,
                currentStamina: 8f,
                requiredStamina: 10f);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Reason, Is.EqualTo(MoveRejectionReason.InsufficientStamina));
        }

        [Test]
        public void ValidatePure_AcceptsValidRequest()
        {
            var result = ContextualMoveValidator.ValidatePure(
                true, true, true, true, true, 20f, 10f);

            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Reason, Is.EqualTo(MoveRejectionReason.None));
        }
    }
}
```

- [ ] **Step 2: Run tests and verify failure**

Expected: compile failure for missing validation types.

- [ ] **Step 3: Add structured results**

```csharp
namespace LoCoFight
{
    public enum MoveRejectionReason
    {
        None,
        MissingMove,
        MatchInactive,
        WrongAttackerState,
        WrongTargetState,
        WrongGroundZone,
        NotInCorner,
        NotNearRopes,
        NotRebounding,
        OutOfRange,
        InsufficientStamina,
        InsufficientLiftStrength,
        TargetTooHeavy
    }

    public readonly struct MoveValidationResult
    {
        public bool IsValid { get; }
        public MoveRejectionReason Reason { get; }
        public string DebugMessage { get; }

        MoveValidationResult(bool valid, MoveRejectionReason reason, string message)
        {
            IsValid = valid;
            Reason = reason;
            DebugMessage = message;
        }

        public static MoveValidationResult Valid() =>
            new MoveValidationResult(true, MoveRejectionReason.None, "Valid");

        public static MoveValidationResult Reject(
            MoveRejectionReason reason,
            string message) =>
            new MoveValidationResult(false, reason, message);
    }
}
```

- [ ] **Step 4: Implement pure validation ordering**

```csharp
namespace LoCoFight
{
    public static class ContextualMoveValidator
    {
        public static MoveValidationResult ValidatePure(
            bool moveExists,
            bool matchActive,
            bool attackerCanAct,
            bool targetStateValid,
            bool contextValid,
            float currentStamina,
            float requiredStamina)
        {
            if (!moveExists)
                return MoveValidationResult.Reject(
                    MoveRejectionReason.MissingMove, "No move assigned");
            if (!matchActive)
                return MoveValidationResult.Reject(
                    MoveRejectionReason.MatchInactive, "Match is not active");
            if (!attackerCanAct)
                return MoveValidationResult.Reject(
                    MoveRejectionReason.WrongAttackerState, "Attacker cannot act");
            if (!targetStateValid)
                return MoveValidationResult.Reject(
                    MoveRejectionReason.WrongTargetState, "Target state is invalid");
            if (!contextValid)
                return MoveValidationResult.Reject(
                    MoveRejectionReason.WrongGroundZone, "Move context is invalid");
            if (currentStamina < requiredStamina)
                return MoveValidationResult.Reject(
                    MoveRejectionReason.InsufficientStamina, "Insufficient stamina");
            return MoveValidationResult.Valid();
        }
    }
}
```

Add runtime overloads only as each context is implemented. Keep the pure method
as the common ordering contract.

- [ ] **Step 5: Run tests and offline compile**

Expected: validator tests pass; offline compile exits `0`.

- [ ] **Step 6: Commit validation primitives**

```bash
git add \
  Assets/Scripts/Combat/MoveValidationResult.cs \
  Assets/Scripts/Combat/MoveValidationResult.cs.meta \
  Assets/Scripts/Combat/ContextualMoveValidator.cs \
  Assets/Scripts/Combat/ContextualMoveValidator.cs.meta \
  Assets/Scripts/Editor/ContextualMoveValidatorTests.cs \
  Assets/Scripts/Editor/ContextualMoveValidatorTests.cs.meta
git commit -m "Add structured contextual move validation"
```

### Task 4: Resolve Grapple Direction from Player Input

**Files:**

- Modify: `Assets/Scripts/Input/PlayerInputLogic.cs`
- Modify: `Assets/Scripts/Input/PlayerInputController.cs`
- Modify: `Assets/Scripts/Editor/PlayerInputLogicTests.cs`

- [ ] **Step 1: Add failing direction tests**

Add:

```csharp
[Test]
public void ResolveMoveDirection_UsesFacingRelativeForward()
{
    var result = Invoke(
        "ResolveMoveDirection",
        new Vector2(0f, 1f),
        Vector3.forward,
        Vector3.right,
        0.2f);
    Assert.That(result.ToString(), Is.EqualTo("Forward"));
}

[Test]
public void ResolveMoveDirection_UsesNeutralInsideDeadZone()
{
    var result = Invoke(
        "ResolveMoveDirection",
        new Vector2(0.05f, 0.05f),
        Vector3.forward,
        Vector3.right,
        0.2f);
    Assert.That(result.ToString(), Is.EqualTo("Neutral"));
}

[Test]
public void ResolveMoveDirection_UsesLateralAxis()
{
    var result = Invoke(
        "ResolveMoveDirection",
        new Vector2(-1f, 0f),
        Vector3.forward,
        Vector3.right,
        0.2f);
    Assert.That(result.ToString(), Is.EqualTo("Left"));
}
```

- [ ] **Step 2: Run tests and verify failure**

Expected: `ResolveMoveDirection` is missing.

- [ ] **Step 3: Implement relative direction resolution**

```csharp
public static MoveDirection ResolveMoveDirection(
    Vector2 moveInput,
    Vector3 facingForward,
    Vector3 facingRight,
    float deadZone)
{
    Vector2 filtered = ApplyDeadZone(moveInput, deadZone);
    if (filtered == Vector2.zero) return MoveDirection.Neutral;

    Vector3 world = MathUtil.Flat(facingRight) * filtered.x +
                    MathUtil.Flat(facingForward) * filtered.y;
    float forward = Vector3.Dot(world.normalized, MathUtil.Flat(facingForward).normalized);
    float right = Vector3.Dot(world.normalized, MathUtil.Flat(facingRight).normalized);

    if (Mathf.Abs(forward) >= Mathf.Abs(right))
        return forward >= 0f ? MoveDirection.Forward : MoveDirection.Backward;
    return right >= 0f ? MoveDirection.Right : MoveDirection.Left;
}
```

- [ ] **Step 4: Pass direction into grapple follow-up calls**

In `HandleCombat`, resolve direction from `frame.Move`,
`_core.transform.forward`, and `_core.transform.right`, then call:

```csharp
_core.Combat.TryPowerGrappleFromLock(direction);
_core.Combat.TryQuickGrappleFromLock(direction);
```

Task 7 will add these overloads. Until then, add forwarding overloads in
`WrestlerCombat` that ignore direction and call the existing methods so this
commit remains playable:

```csharp
public bool TryQuickGrappleFromLock(MoveDirection direction) =>
    TryQuickGrappleFromLock();

public bool TryPowerGrappleFromLock(MoveDirection direction) =>
    TryPowerGrappleFromLock();
```

- [ ] **Step 5: Run tests, offline compile, and manual lockup smoke test**

Manual:

1. Enter a grapple lock.
2. Use neutral `L`, directional `L`, neutral `K`, directional `K`.
3. Confirm all still execute existing quick/power behavior.

- [ ] **Step 6: Commit input direction**

```bash
git add \
  Assets/Scripts/Input/PlayerInputLogic.cs \
  Assets/Scripts/Input/PlayerInputController.cs \
  Assets/Scripts/Editor/PlayerInputLogicTests.cs \
  Assets/Scripts/Combat/WrestlerCombat.cs
git commit -m "Capture directional grapple input"
```

### Task 5: Migrate Default Move Data and Add Asset Validation

**Files:**

- Modify: `Assets/Scripts/Roster/DefaultGameData.cs`
- Modify: `Assets/Scripts/Editor/PrototypeAssetBuilder.cs`
- Create: `Assets/Scripts/Editor/MoveDataValidator.cs`
- Create: `Assets/Scripts/Editor/MoveDataValidatorTests.cs`

- [ ] **Step 1: Write failing data-validation tests**

```csharp
using NUnit.Framework;
using UnityEngine;

namespace LoCoFight.EditorTests
{
    public class MoveDataValidatorTests
    {
        [Test]
        public void Validate_ReversalWindowOutsideDurationIsError()
        {
            var move = ScriptableObject.CreateInstance<MoveData>();
            move.moveId = "bad-window";
            move.startupTime = 0.1f;
            move.activeTime = 0.1f;
            move.recoveryTime = 0.1f;
            move.reversalWindowEnd = 0.5f;

            var messages = MoveDataValidator.Validate(move, null);
            Assert.That(messages, Has.Some.Contains("reversal window"));
        }

        [Test]
        public void Validate_DirectionalSetRequiresNeutralFallback()
        {
            var set = new DirectionalMoveSet();
            set.forward.Add(ScriptableObject.CreateInstance<MoveData>());

            var messages = MoveDataValidator.ValidateDirectionalSet("quick", set);
            Assert.That(messages, Has.Some.Contains("neutral"));
        }
    }
}
```

- [ ] **Step 2: Run tests and verify failure**

Expected: `MoveDataValidator` is missing.

- [ ] **Step 3: Implement editor-only validation**

Use namespace `LoCoFight` even though the file lives under `Editor/`, so the
builder and tests use the same type without namespace adapters:

```csharp
using System.Collections.Generic;

namespace LoCoFight
{
    public static class MoveDataValidator
    {
        public static List<string> Validate(MoveData move, MoveDatabase database)
        {
            var errors = new List<string>();
            if (move == null)
            {
                errors.Add("Move reference is null.");
                return errors;
            }

            string id = string.IsNullOrWhiteSpace(move.moveId)
                ? move.name
                : move.moveId;
            if (string.IsNullOrWhiteSpace(move.moveId))
                errors.Add($"{id}: moveId is required.");
            if (move.reversalWindowStart < 0f)
                errors.Add($"{id}: reversal window start cannot be negative.");
            if (move.reversalWindowEnd < move.reversalWindowStart)
                errors.Add($"{id}: reversal window end precedes its start.");
            if (move.reversalWindowEnd > move.TotalDuration)
                errors.Add($"{id}: reversal window exceeds total move duration.");
            if (move.HasTag(MoveTag.Lift) && !move.requiresLift)
                errors.Add($"{id}: Lift tag requires lift validation.");
            if (move.requiredGroundZone != GroundTargetZone.None &&
                !move.requiresTargetDowned)
                errors.Add($"{id}: ground zone requires a downed target.");
            if (move.requiresTargetCornered &&
                move.category != MoveCategory.CornerStrike &&
                move.category != MoveCategory.CornerGrapple)
                errors.Add($"{id}: corner requirement is assigned to a non-corner move.");
            if (move.requiresTargetRopeStaggered &&
                move.category != MoveCategory.RopeStaggerAttack)
                errors.Add($"{id}: rope-stagger requirement is assigned to another family.");
            return errors;
        }

        public static List<string> ValidateDirectionalSet(
            string label,
            DirectionalMoveSet set)
        {
            var errors = new List<string>();
            bool hasDirectional =
                set.forward.Count > 0 ||
                set.backward.Count > 0 ||
                set.lateral.Count > 0;
            if (hasDirectional && set.neutral.Count == 0)
                errors.Add($"{label}: directional set requires a neutral fallback.");
            return errors;
        }

        public static List<string> ValidateAll(DefaultGameDataSet set)
        {
            var errors = new List<string>();
            foreach (MoveData move in set.moves)
                errors.AddRange(Validate(move, set.moveDatabase));
            errors.AddRange(ValidateDirectionalSet(
                "Quick grapples", set.moveDatabase.directionalQuickGrapples));
            errors.AddRange(ValidateDirectionalSet(
                "Power grapples", set.moveDatabase.directionalPowerGrapples));
            return errors;
        }
    }
}
```

- [ ] **Step 4: Assign tiers and migrate neutral grapple buckets**

In `DefaultGameData.Move`, assign defaults:

```csharp
m.tier = cat == MoveCategory.PowerGrapple
    ? MoveTier.Heavy
    : cat == MoveCategory.HeavyStrike
        ? MoveTier.Medium
        : MoveTier.Light;
m.minimumStamina = cat == MoveCategory.PowerGrapple ? stam : 0f;
```

After creating each existing quick/power grapple:

```csharp
db.quickGrapples.Add(move);
db.directionalQuickGrapples.neutral.Add(move);
```

and:

```csharp
db.powerGrapples.Add(move);
db.directionalPowerGrapples.neutral.Add(move);
```

Use local variables so each move is added once to each required collection.

- [ ] **Step 5: Run validation from the builder before saving assets**

In `CreateDefaultAssets`, validate all moves and directional sets. Use
`Debug.LogError` for structural errors and return before deleting/recreating
assets:

```csharp
var errors = MoveDataValidator.ValidateAll(set);
if (errors.Count > 0)
{
    foreach (string error in errors) Debug.LogError($"[MoveData] {error}");
    return;
}
```

- [ ] **Step 6: Run tests, compile, regenerate assets, and inspect console**

Run **Tools > LoCo Fight Game > Create Default Prototype Assets**.

Expected:

- no `[MoveData]` errors
- saved moves retain existing gameplay
- neutral directional buckets are populated

- [ ] **Step 7: Commit migration and validator**

```bash
git add \
  Assets/Scripts/Roster/DefaultGameData.cs \
  Assets/Scripts/Editor/PrototypeAssetBuilder.cs \
  Assets/Scripts/Editor/MoveDataValidator.cs \
  Assets/Scripts/Editor/MoveDataValidator.cs.meta \
  Assets/Scripts/Editor/MoveDataValidatorTests.cs \
  Assets/Scripts/Editor/MoveDataValidatorTests.cs.meta
# Then add only the generated database/move assets shown by:
git status --short Assets/Resources/LoCoData
git commit -m "Migrate moves to contextual data"
```

### Task 6: Add Shared Combat Diagnostics and Context Snapshot

**Files:**

- Modify: `Assets/Scripts/Combat/CombatContext.cs`
- Modify: `Assets/Scripts/Combat/CombatContextResolver.cs`
- Modify: `Assets/Scripts/Combat/WrestlerCombat.cs`
- Modify: `Assets/Scripts/UI/DebugOverlay.cs`

- [ ] **Step 1: Define the diagnostic snapshot**

Add:

```csharp
public readonly struct CombatContextSnapshot
{
    public CombatContext Context { get; }
    public GroundTargetZone GroundZone { get; }
    public MoveDirection Direction { get; }
    public string RequestedFamily { get; }
    public int CandidateCount { get; }
    public string SelectedMove { get; }
    public MoveTier Tier { get; }
    public MoveValidationResult Validation { get; }
    public bool UsedFallback { get; }

    public CombatContextSnapshot(
        CombatContext context,
        GroundTargetZone groundZone,
        MoveDirection direction,
        string requestedFamily,
        int candidateCount,
        string selectedMove,
        MoveTier tier,
        MoveValidationResult validation,
        bool usedFallback)
    {
        Context = context;
        GroundZone = groundZone;
        Direction = direction;
        RequestedFamily = requestedFamily;
        CandidateCount = candidateCount;
        SelectedMove = selectedMove;
        Tier = tier;
        Validation = validation;
        UsedFallback = usedFallback;
    }
}
```

- [ ] **Step 2: Add transient context resolution from live wrestlers**

Add:

```csharp
public static CombatContext Resolve(WrestlerCore attacker, WrestlerCore defender)
{
    bool grapple = attacker != null && attacker.Combat != null &&
                   attacker.Combat.InGrappleLockAsAttacker;
    bool downed = defender != null && defender.States.IsDowned;
    bool cornered = defender != null &&
                    defender.States.Current == WrestlerState.Cornered &&
                    RingInteractionSystem.Instance != null &&
                    RingInteractionSystem.Instance.IsInCornerZone(defender);
    bool ropeStaggered = defender != null &&
                         defender.States.Current == WrestlerState.RopeStaggered &&
                         RingInteractionSystem.Instance != null &&
                         RingInteractionSystem.Instance.IsNearRope(
                             defender, RingInteractionSystem.RopeContactRange + 0.2f);
    bool rebounding = attacker != null &&
                      (attacker.States.Current == WrestlerState.RopeReboundRun ||
                       attacker.States.Current == WrestlerState.RopeReboundReturn);

    CombatContext context = ResolvePriority(
        grapple, downed, cornered, ropeStaggered, rebounding);
    if (downed)
    {
        GroundTargetZone zone = ResolveGroundZone(
            defender.transform.position,
            defender.transform.forward,
            attacker.transform.position,
            0.2f);
        return zone == GroundTargetZone.Lower
            ? CombatContext.GroundLower
            : CombatContext.GroundUpper;
    }
    return context;
}
```

- [ ] **Step 3: Add diagnostics to `WrestlerCombat`**

Add public read-only properties:

```csharp
public CombatContextSnapshot LastContextSnapshot { get; private set; }
public CombatContext CurrentContext =>
    CombatContextResolver.Resolve(_core, Opp);
```

Add one helper that records every contextual request and logs rejection:

```csharp
void RecordContext(
    CombatContext context,
    GroundTargetZone zone,
    MoveDirection direction,
    string family,
    int candidates,
    MoveData selected,
    MoveValidationResult validation,
    bool fallback)
{
    LastContextSnapshot = new CombatContextSnapshot(
        context, zone, direction, family, candidates,
        selected != null ? selected.displayName : "",
        selected != null ? selected.tier : MoveTier.Light,
        validation, fallback);

    if (!validation.IsValid)
        Debug.Log($"[Move] {_core.DisplayName} rejected {family}: " +
                  $"{validation.Reason} ({validation.DebugMessage})");
}
```

- [ ] **Step 4: Display context diagnostics in F1**

Add:

```csharp
var snapshot = w.Combat.LastContextSnapshot;
GUILayout.Label(
    $"Context: {w.Combat.CurrentContext} zone={snapshot.GroundZone} " +
    $"dir={snapshot.Direction} family={snapshot.RequestedFamily}");
GUILayout.Label(
    $"Selected: {snapshot.SelectedMove} tier={snapshot.Tier} " +
    $"valid={snapshot.Validation.IsValid} reason={snapshot.Validation.Reason} " +
    $"fallback={snapshot.UsedFallback}");
```

Increase the overlay area height if needed.

- [ ] **Step 5: Compile and manually verify F1**

Expected: F1 shows standing context before any new contextual action and does
not throw when no request has been recorded.

- [ ] **Step 6: Commit diagnostics**

```bash
git add \
  Assets/Scripts/Combat/CombatContext.cs \
  Assets/Scripts/Combat/CombatContextResolver.cs \
  Assets/Scripts/Combat/WrestlerCombat.cs \
  Assets/Scripts/UI/DebugOverlay.cs
git commit -m "Expose contextual combat diagnostics"
```

### Task 7: Implement Grounded Positional Offense

**Files:**

- Modify: `Assets/Scripts/Roster/DefaultGameData.cs`
- Modify: `Assets/Scripts/Combat/ContextualMoveValidator.cs`
- Modify: `Assets/Scripts/Combat/WrestlerCombat.cs`
- Modify: `Assets/Scripts/Input/PlayerInputController.cs`
- Modify: `Assets/Scripts/AI/AIState.cs`
- Modify: `Assets/Scripts/AI/CPUWrestlerAI.cs`
- Modify: `Assets/Scripts/Animation/PlaceholderAnimationDriver.cs`
- Modify: `Documentation/TestingChecklist.md`

- [ ] **Step 1: Add ground-context validation tests**

Add tests to `ContextualMoveValidatorTests`:

```csharp
[Test]
public void ValidateGround_RejectsWrongZone()
{
    var move = ScriptableObject.CreateInstance<MoveData>();
    move.requiresTargetDowned = true;
    move.requiredGroundZone = GroundTargetZone.Upper;

    var result = ContextualMoveValidator.ValidateGround(
        move, targetDowned: true, actualZone: GroundTargetZone.Lower,
        inRange: true, currentStamina: 100f);

    Assert.That(result.Reason, Is.EqualTo(MoveRejectionReason.WrongGroundZone));
}

[Test]
public void ValidateGround_AcceptsMatchingZone()
{
    var move = ScriptableObject.CreateInstance<MoveData>();
    move.requiresTargetDowned = true;
    move.requiredGroundZone = GroundTargetZone.Lower;

    var result = ContextualMoveValidator.ValidateGround(
        move, true, GroundTargetZone.Lower, true, 100f);

    Assert.That(result.IsValid, Is.True);
}
```

- [ ] **Step 2: Implement `ValidateGround`**

Validate in this order:

```text
move exists
target is downed
actual zone matches required zone
attacker is within move.range
attacker has max(staminaCost, minimumStamina)
```

Implement:

```csharp
public static MoveValidationResult ValidateGround(
    MoveData move,
    bool targetDowned,
    GroundTargetZone actualZone,
    bool inRange,
    float currentStamina)
{
    if (move == null)
        return MoveValidationResult.Reject(
            MoveRejectionReason.MissingMove, "No ground move assigned");
    if (!targetDowned)
        return MoveValidationResult.Reject(
            MoveRejectionReason.WrongTargetState, "Target is not downed");
    if (move.requiredGroundZone != GroundTargetZone.None &&
        move.requiredGroundZone != actualZone)
        return MoveValidationResult.Reject(
            MoveRejectionReason.WrongGroundZone, "Wrong ground target zone");
    if (!inRange)
        return MoveValidationResult.Reject(
            MoveRejectionReason.OutOfRange, "Ground target is out of range");
    if (currentStamina < Mathf.Max(move.staminaCost, move.minimumStamina))
        return MoveValidationResult.Reject(
            MoveRejectionReason.InsufficientStamina, "Insufficient stamina");
    return MoveValidationResult.Valid();
}
```

- [ ] **Step 3: Add four ground moves**

Create:

```text
ground-elbow-drop       upper, Medium
ground-head-stomp       upper, Light
ground-knee-drop        lower, Medium
ground-leg-stomp        lower, Light
```

Set:

```csharp
move.requiresTargetDowned = true;
move.requiredGroundZone = GroundTargetZone.Upper; // or Lower
move.placeholderPoseName = "ground";
move.range = 1.25f;
```

Add two to `groundUpperAttacks` and two to `groundLowerAttacks`.

- [ ] **Step 4: Add `TryGroundAttack`**

Implement:

```csharp
public bool TryGroundAttack()
{
    if (Opp == null || !Opp.States.IsDowned) return false;

    GroundTargetZone zone = CombatContextResolver.ResolveGroundZone(
        Opp.transform.position,
        Opp.transform.forward,
        transform.position,
        0.2f);
    MoveData move = zone == GroundTargetZone.Lower
        ? _core.Moveset.RandomGroundLowerAttack()
        : _core.Moveset.RandomGroundUpperAttack();

    MoveValidationResult validation = ContextualMoveValidator.ValidateGround(
        move,
        Opp.States.IsDowned,
        zone,
        move != null && HitboxProbe.InRange(transform, Opp.transform, move.range),
        _core.Stats.Stamina);
    RecordContext(
        zone == GroundTargetZone.Lower
            ? CombatContext.GroundLower
            : CombatContext.GroundUpper,
        zone, MoveDirection.Neutral, "GroundAttack",
        zone == GroundTargetZone.Lower
            ? _core.Moveset.groundLowerAttacks.Count
            : _core.Moveset.groundUpperAttacks.Count,
        move, validation, false);
    if (!validation.IsValid) return false;
    if (!_core.Stats.SpendStamina(move.staminaCost)) return false;

    _moveRoutine = StartCoroutine(GroundAttackRoutine(move));
    return true;
}
```

`GroundAttackRoutine` must:

```text
face target
set attacker StrikeStartup/Active/Recovery
keep defender Downed
play "ground" presentation
apply a ground-specific hit at active phase
end without resetting defender recovery timer beyond move.downedDuration
```

Do not call the standing `CheckHit`, because it intentionally rejects downed
targets. Do not call the normal `ApplyHit`, because its non-knockdown branch can
replace the defender's downed state.

Add:

```csharp
void ApplyGroundHit(MoveData move)
{
    if (Opp == null || !Opp.States.IsDowned) return;

    float damage = CombatResolver.ScaleDamage(_core, move);
    Opp.Stats.ApplyDamage(damage, _core);
    if (move.staminaDamage > 0f)
        Opp.Stats.DrainStamina(move.staminaDamage);
    _core.Stats.AddMomentum(move.momentumGainOnHit);
    Opp.Anim.TriggerHitReact();
    Debug.Log(
        $"[Move] {move.displayName} hit grounded {Opp.DisplayName} " +
        $"for {damage:0.#}");
    OnLandedHit?.Invoke(move);
}
```

Initial ground attacks must not call `Opp.States.Set(Downed)`. The defender's
existing downed timeout continues, which prevents repeated ground attacks from
resetting recovery indefinitely.

- [ ] **Step 5: Route player light input to ground offense first**

Change the buffered light callback:

```csharp
() => _core.Combat.TryGroundAttack() ||
      _core.Combat.TryRunningAttack() ||
      _core.Combat.TryLightStrike()
```

Only buffer when near enough to the opponent or running, as today.

- [ ] **Step 6: Add CPU ground offense**

Add `AIState.AttemptGroundAttack`. In `Decide`, after pin/submission checks:

```csharp
if (_memory.CanUse("ground", 1.4f))
{
    CurrentState = AIState.AttemptGroundAttack;
    return;
}
```

In `Act`:

```csharp
case AIState.AttemptGroundAttack:
    if (InRange(1.25f))
    {
        _memory.Note("ground");
        _core.Combat.TryGroundAttack();
        Rethink();
    }
    else MoveToward(Opp.transform.position, false);
    break;
```

- [ ] **Step 7: Add a ground placeholder action**

Map `"ground"` to a distinct downward strike overlay in
`PlaceholderAnimationDriver`; do not change gameplay timing.

- [ ] **Step 8: Run tests, compile, regenerate assets, and execute ground
      matrix**

Manual matrix:

```text
player upper/lower
CPU upper/lower
near center/rope/corner
insufficient stamina
defender getting up
defender rolling away
pin and submission still available
```

- [ ] **Step 9: Update checklist and commit**

```bash
git add \
  Assets/Scripts/Roster/DefaultGameData.cs \
  Assets/Scripts/Combat/ContextualMoveValidator.cs \
  Assets/Scripts/Combat/WrestlerCombat.cs \
  Assets/Scripts/Input/PlayerInputController.cs \
  Assets/Scripts/AI/AIState.cs \
  Assets/Scripts/AI/CPUWrestlerAI.cs \
  Assets/Scripts/Animation/PlaceholderAnimationDriver.cs \
  Assets/Scripts/Editor/ContextualMoveValidatorTests.cs \
  Documentation/TestingChecklist.md
# Then add only changed move/database assets and their .meta files from:
git status --short Assets/Resources/LoCoData
git commit -m "Add grounded positional offense"
```

### Task 8: Implement Directional Grapple Selection

**Files:**

- Modify: `Assets/Scripts/Roster/DefaultGameData.cs`
- Modify: `Assets/Scripts/Combat/WrestlerCombat.cs`
- Modify: `Assets/Scripts/AI/CPUWrestlerAI.cs`
- Modify: `Documentation/TestingChecklist.md`

- [ ] **Step 1: Assign existing grapple moves to directional buckets**

Clear the broad neutral migration from Task 5 before assigning deliberate
directional content:

```csharp
db.directionalQuickGrapples.neutral.Clear();
db.directionalPowerGrapples.neutral.Clear();
```

Use:

```text
Quick neutral   knee-lift
Quick forward   snapmare
Quick backward  headlock-takedown
Quick lateral   snap-arm-drag

Power neutral   body-slam
Power forward   vertical-drop
Power backward  backbreaker
Power lateral   shoulder-throw
```

Keep all existing legacy lists populated until every caller has migrated.

- [ ] **Step 2: Replace forwarding overloads with real selection**

Implement:

```csharp
public bool TryQuickGrappleFromLock(MoveDirection direction)
{
    MoveData move = _core.Moveset.directionalQuickGrapples
        .Pick(direction, out bool fallback);
    return ExecuteDirectionalGrapple(
        move, direction, "QuickGrapple", fallback);
}

public bool TryPowerGrappleFromLock(MoveDirection direction)
{
    MoveData move = _core.Moveset.directionalPowerGrapples
        .Pick(direction, out bool fallback);
    return ExecuteDirectionalGrapple(
        move, direction, "PowerGrapple", fallback);
}
```

`ExecuteDirectionalGrapple` records diagnostics, then delegates to the existing
`ExecuteGrappleMove`. If the selected move is null, record `MissingMove`,
release the grapple, and return `false`.

Keep parameterless methods as neutral compatibility wrappers:

```csharp
public bool TryQuickGrappleFromLock() =>
    TryQuickGrappleFromLock(MoveDirection.Neutral);
```

- [ ] **Step 3: Add deterministic CPU direction utility**

Add a pure helper:

```csharp
static MoveDirection ChooseGrappleDirection(bool power, float staminaPercent)
{
    if (power && staminaPercent > 0.65f) return MoveDirection.Forward;
    if (staminaPercent < 0.35f) return MoveDirection.Neutral;
    float roll = Random.value;
    if (roll < 0.25f) return MoveDirection.Backward;
    if (roll < 0.5f) return MoveDirection.Left;
    if (roll < 0.75f) return MoveDirection.Right;
    return MoveDirection.Neutral;
}
```

Use it in `ChooseGrappleMove`, retaining quick/power fallback and forced
release.

- [ ] **Step 4: Run compile and directional manual matrix**

Test:

```text
four attacker facings
neutral/forward/backward/left/right
quick/power
empty directional bucket fallback
empty neutral bucket release
failed lift
grapple timeout
CPU selections
```

- [ ] **Step 5: Update checklist and commit**

```bash
git add \
  Assets/Scripts/Roster/DefaultGameData.cs \
  Assets/Scripts/Combat/WrestlerCombat.cs \
  Assets/Scripts/AI/CPUWrestlerAI.cs \
  Documentation/TestingChecklist.md
# Then add only changed move/database assets and their .meta files from:
git status --short Assets/Resources/LoCoData
git commit -m "Add directional grapple selection"
```

### Task 9: Implement Corner Offense

**Files:**

- Modify: `Assets/Scripts/Roster/DefaultGameData.cs`
- Modify: `Assets/Scripts/Combat/ContextualMoveValidator.cs`
- Modify: `Assets/Scripts/Combat/WrestlerCombat.cs`
- Modify: `Assets/Scripts/Input/PlayerInputController.cs`
- Modify: `Assets/Scripts/AI/AIState.cs`
- Modify: `Assets/Scripts/AI/CPUWrestlerAI.cs`
- Modify: `Assets/Scripts/Animation/PlaceholderAnimationDriver.cs`
- Modify: `Documentation/TestingChecklist.md`

- [ ] **Step 1: Add corner validation tests**

```csharp
[Test]
public void ValidateCorner_RequiresStateAndGeometry()
{
    var move = ScriptableObject.CreateInstance<MoveData>();
    move.requiresTargetCornered = true;

    Assert.That(
        ContextualMoveValidator.ValidateCorner(
            move, targetCornered: true, inCornerZone: false,
            inRange: true, currentStamina: 100f).Reason,
        Is.EqualTo(MoveRejectionReason.NotInCorner));
}
```

- [ ] **Step 2: Implement corner validation**

```csharp
public static MoveValidationResult ValidateCorner(
    MoveData move,
    bool targetCornered,
    bool inCornerZone,
    bool inRange,
    float currentStamina)
{
    if (move == null)
        return MoveValidationResult.Reject(
            MoveRejectionReason.MissingMove, "No corner move assigned");
    if (!targetCornered)
        return MoveValidationResult.Reject(
            MoveRejectionReason.WrongTargetState, "Target is not cornered");
    if (!inCornerZone)
        return MoveValidationResult.Reject(
            MoveRejectionReason.NotInCorner, "Target left the corner zone");
    if (!inRange)
        return MoveValidationResult.Reject(
            MoveRejectionReason.OutOfRange, "Corner target is out of range");
    if (currentStamina < Mathf.Max(move.staminaCost, move.minimumStamina))
        return MoveValidationResult.Reject(
            MoveRejectionReason.InsufficientStamina, "Insufficient stamina");
    return MoveValidationResult.Valid();
}
```

- [ ] **Step 3: Add two corner moves**

Create:

```text
corner-forearm       CornerStrike, Medium, target remains stunned
corner-bulldog       CornerGrapple, Heavy, target becomes downed
```

Set `requiresTargetCornered`, `requiresCornerZone`, `MoveTag.Corner`, range,
result durations, and `"corner"` placeholder pose.

- [ ] **Step 4: Add corner combat methods**

Add:

```csharp
public bool TryCornerStrike()
public bool TryCornerGrapple()
```

Validation requires:

```text
Opp state == Cornered
RingInteractionSystem.IsInCornerZone(Opp)
attacker within move.range
sufficient stamina
```

Use shared move bookkeeping and cleanup. Corner grapple must set both wrestlers
to owned temporary states and clear them on completion/interruption.

- [ ] **Step 5: Route input by context**

In `HandleCombat`:

```text
Light/J → ground attack, corner strike, rebound attack, normal strike
Grapple/L outside lock → corner grapple, normal grapple attempt
```

Keep the exact precedence from the approved context priority.

- [ ] **Step 6: Add CPU corner state**

Add `AIState.AttemptCornerOffense`. Select corner grapple or strike using
stamina and `cornerStrategyPreference`. Do not use `ForceOpponentToRopes` when
the opponent is already `Cornered`.

- [ ] **Step 7: Add corner presentation and test matrix**

Map `"corner"` to a distinct close-range strike/grapple pose.

Test every corner, state/geometry disagreement, reversal, insufficient stamina,
CPU offense, downed result, and standing result.

- [ ] **Step 8: Update checklist and commit**

```bash
git add \
  Assets/Scripts/Roster/DefaultGameData.cs \
  Assets/Scripts/Combat/ContextualMoveValidator.cs \
  Assets/Scripts/Combat/WrestlerCombat.cs \
  Assets/Scripts/Input/PlayerInputController.cs \
  Assets/Scripts/AI/AIState.cs \
  Assets/Scripts/AI/CPUWrestlerAI.cs \
  Assets/Scripts/Animation/PlaceholderAnimationDriver.cs \
  Assets/Scripts/Editor/ContextualMoveValidatorTests.cs \
  Documentation/TestingChecklist.md
# Then add only changed move/database assets and their .meta files from:
git status --short Assets/Resources/LoCoData
git commit -m "Add contextual corner offense"
```

### Task 10: Implement Rope-Stagger and Rebound Offense

**Files:**

- Modify: `Assets/Scripts/Roster/DefaultGameData.cs`
- Modify: `Assets/Scripts/Combat/ContextualMoveValidator.cs`
- Modify: `Assets/Scripts/Combat/WrestlerCombat.cs`
- Modify: `Assets/Scripts/Input/PlayerInputController.cs`
- Modify: `Assets/Scripts/AI/AIState.cs`
- Modify: `Assets/Scripts/AI/CPUWrestlerAI.cs`
- Modify: `Documentation/TestingChecklist.md`

- [ ] **Step 1: Add rope validation tests**

```csharp
[Test]
public void ValidateRopeStagger_RejectsNormalStunnedTarget()
{
    var move = ScriptableObject.CreateInstance<MoveData>();
    move.requiresTargetRopeStaggered = true;

    var result = ContextualMoveValidator.ValidateRope(
        move, targetRopeStaggered: false, targetNearRope: true,
        attackerRebounding: false, inRange: true, currentStamina: 100f);

    Assert.That(result.Reason, Is.EqualTo(MoveRejectionReason.WrongTargetState));
}

[Test]
public void ValidateRebound_RequiresReboundState()
{
    var move = ScriptableObject.CreateInstance<MoveData>();
    move.requiresRopeRebound = true;

    var result = ContextualMoveValidator.ValidateRope(
        move, false, false, attackerRebounding: false,
        inRange: true, currentStamina: 100f);

    Assert.That(result.Reason, Is.EqualTo(MoveRejectionReason.NotRebounding));
}
```

- [ ] **Step 2: Implement rope validation**

```csharp
public static MoveValidationResult ValidateRope(
    MoveData move,
    bool targetRopeStaggered,
    bool targetNearRope,
    bool attackerRebounding,
    bool inRange,
    float currentStamina)
{
    if (move == null)
        return MoveValidationResult.Reject(
            MoveRejectionReason.MissingMove, "No rope move assigned");
    if (move.requiresTargetRopeStaggered && !targetRopeStaggered)
        return MoveValidationResult.Reject(
            MoveRejectionReason.WrongTargetState, "Target is not rope-staggered");
    if (move.requiresOpponentNearRopes && !targetNearRope)
        return MoveValidationResult.Reject(
            MoveRejectionReason.NotNearRopes, "Target left the ropes");
    if (move.requiresRopeRebound && !attackerRebounding)
        return MoveValidationResult.Reject(
            MoveRejectionReason.NotRebounding, "Attacker is not rebounding");
    if (!inRange)
        return MoveValidationResult.Reject(
            MoveRejectionReason.OutOfRange, "Rope target is out of range");
    if (currentStamina < Mathf.Max(move.staminaCost, move.minimumStamina))
        return MoveValidationResult.Reject(
            MoveRejectionReason.InsufficientStamina, "Insufficient stamina");
    return MoveValidationResult.Valid();
}
```

- [ ] **Step 3: Add contextual rope moves**

Create:

```text
rope-chop-combination    RopeStaggerAttack, Medium
rope-snapmare            RopeStaggerAttack or contextual grapple, Medium
rebound-lariat           RopeReboundAttack, Heavy
```

Set rope requirements and tags explicitly. Keep existing ordinary running
attacks in `runningAttacks`.

- [ ] **Step 4: Add `TryRopeStaggerAttack` and `TryRopeReboundAttack`**

Rope-stagger validation requires both target state and current rope proximity
from `RingInteractionSystem`.

Rebound validation requires attacker state:

```csharp
WrestlerState.RopeReboundRun ||
WrestlerState.RopeReboundReturn
```

Do not use direct rope-distance math outside `RingInteractionSystem`.

- [ ] **Step 5: Update input and CPU precedence**

Player light precedence:

```text
ground
corner
rope stagger
rope rebound
ordinary running
standing light
```

CPU:

- add `AttemptRopeOffense`
- exploit `RopeStaggered`
- use rebound-specific attack during rebound state
- fall back to ordinary running attack outside rebound state

- [ ] **Step 6: Run ruleset and interruption matrix**

Test:

```text
all rope sides
near/touching rope
RopeStaggered versus Stunned
rebound versus normal running
standard/no-rope-break/hardcore
target leaves context during startup
CPU rope offense
```

- [ ] **Step 7: Update checklist and commit**

```bash
git add \
  Assets/Scripts/Roster/DefaultGameData.cs \
  Assets/Scripts/Combat/ContextualMoveValidator.cs \
  Assets/Scripts/Combat/WrestlerCombat.cs \
  Assets/Scripts/Input/PlayerInputController.cs \
  Assets/Scripts/AI/AIState.cs \
  Assets/Scripts/AI/CPUWrestlerAI.cs \
  Assets/Scripts/Editor/ContextualMoveValidatorTests.cs \
  Documentation/TestingChecklist.md
# Then add only changed move/database assets and their .meta files from:
git status --short Assets/Resources/LoCoData
git commit -m "Add rope and rebound offense"
```

### Task 11: Add Move-Tier Pacing Rules

**Files:**

- Create: `Assets/Scripts/Moves/MovePacingRules.cs`
- Create: `Assets/Scripts/Editor/MovePacingRulesTests.cs`
- Modify: `Assets/Scripts/Combat/ContextualMoveValidator.cs`
- Modify: `Assets/Scripts/AI/CPUWrestlerAI.cs`
- Modify: `Assets/Scripts/Editor/MoveDataValidator.cs`
- Modify: `Assets/Scripts/Editor/MoveDataValidatorTests.cs`
- Modify: `Assets/Scripts/Editor/PrototypeAssetBuilder.cs`
- Modify: `Assets/Scripts/UI/DebugOverlay.cs`

- [ ] **Step 1: Write failing pacing tests**

```csharp
using NUnit.Framework;
using UnityEngine;

namespace LoCoFight.EditorTests
{
    public class MovePacingRulesTests
    {
        [Test]
        public void RequiredStamina_UsesGreaterOfCostAndMinimum()
        {
            var move = ScriptableObject.CreateInstance<MoveData>();
            move.staminaCost = 12f;
            move.minimumStamina = 20f;

            Assert.That(MovePacingRules.RequiredStamina(move), Is.EqualTo(20f));
        }

        [Test]
        public void CanAttempt_LightMoveRemainsAvailableWhenCostIsAffordable()
        {
            var move = ScriptableObject.CreateInstance<MoveData>();
            move.tier = MoveTier.Light;
            move.staminaCost = 4f;

            Assert.That(MovePacingRules.CanAttempt(move, 5f), Is.True);
        }

        [Test]
        public void CanAttempt_HeavyMoveRequiresMinimumStamina()
        {
            var move = ScriptableObject.CreateInstance<MoveData>();
            move.tier = MoveTier.Heavy;
            move.staminaCost = 15f;
            move.minimumStamina = 30f;

            Assert.That(MovePacingRules.CanAttempt(move, 20f), Is.False);
        }
    }
}
```

- [ ] **Step 2: Implement pacing rules**

```csharp
using UnityEngine;

namespace LoCoFight
{
    public static class MovePacingRules
    {
        public static float RequiredStamina(MoveData move)
        {
            if (move == null) return float.PositiveInfinity;
            return Mathf.Max(move.staminaCost, move.minimumStamina);
        }

        public static bool CanAttempt(MoveData move, float currentStamina)
        {
            return move != null && currentStamina >= RequiredStamina(move);
        }
    }
}
```

- [ ] **Step 3: Use pacing rules in validation**

Replace duplicated stamina checks with `MovePacingRules.RequiredStamina(move)`.
Do not spend `minimumStamina`; continue spending only `staminaCost`.

- [ ] **Step 4: Add advisory editor warnings**

Add a failing test:

```csharp
[Test]
public void ValidateWarnings_HeavyMoveWithoutMinimumStaminaWarns()
{
    var move = ScriptableObject.CreateInstance<MoveData>();
    move.moveId = "heavy";
    move.tier = MoveTier.Heavy;
    move.minimumStamina = 0f;

    var warnings = MoveDataValidator.ValidateWarnings(move, null);
    Assert.That(warnings, Has.Some.Contains("minimum stamina"));
}
```

Add:

```csharp
public static List<string> ValidateWarnings(
    MoveData move,
    MoveDatabase database)
{
    var warnings = new List<string>();
    if (move == null) return warnings;
    string id = string.IsNullOrWhiteSpace(move.moveId) ? move.name : move.moveId;

    if (move.tier == MoveTier.Heavy && move.minimumStamina <= 0f)
        warnings.Add($"{id}: heavy move has no minimum stamina.");
    if (move.tier == MoveTier.Heavy && move.recoveryTime < 0.45f)
        warnings.Add($"{id}: heavy move recovery is shorter than 0.45 seconds.");
    if (move.tier == MoveTier.Light && move.minimumStamina > move.staminaCost)
        warnings.Add($"{id}: light move minimum stamina exceeds its cost.");
    if (move.tier == MoveTier.Special && database != null)
        warnings.Add($"{id}: special-tier MoveData belongs in SpecialController data.");

    return warnings;
}
```

In `PrototypeAssetBuilder.CreateDefaultAssets`, after structural validation:

```csharp
foreach (MoveData move in set.moves)
{
    foreach (string warning in MoveDataValidator.ValidateWarnings(
                 move, set.moveDatabase))
        Debug.LogWarning($"[MoveData] {warning}");
}
```

Warnings do not stop asset generation.

- [ ] **Step 5: Make CPU obey pacing during selection**

Before choosing contextual power/heavy offense, use:

```csharp
MovePacingRules.CanAttempt(candidate, _core.Stats.Stamina)
```

If no heavy candidate is affordable, select a light/medium action or reposition.

- [ ] **Step 6: Run tests, compile, regenerate assets, and play pacing matrix**

Verify:

```text
light offense at low-but-affordable stamina
heavy rejection below minimum
heavy execution above minimum
special momentum behavior unchanged
CPU does not spam unaffordable heavy moves
F1 shows tier and rejection
```

- [ ] **Step 7: Commit pacing rules**

```bash
git add \
  Assets/Scripts/Moves/MovePacingRules.cs \
  Assets/Scripts/Moves/MovePacingRules.cs.meta \
  Assets/Scripts/Editor/MovePacingRulesTests.cs \
  Assets/Scripts/Editor/MovePacingRulesTests.cs.meta \
  Assets/Scripts/Combat/ContextualMoveValidator.cs \
  Assets/Scripts/AI/CPUWrestlerAI.cs \
  Assets/Scripts/Editor/MoveDataValidator.cs \
  Assets/Scripts/Editor/MoveDataValidatorTests.cs \
  Assets/Scripts/Editor/PrototypeAssetBuilder.cs \
  Assets/Scripts/UI/DebugOverlay.cs
# Then add only changed move/database assets and their .meta files from:
git status --short Assets/Resources/LoCoData
git commit -m "Add move tier pacing rules"
```

### Task 12: Complete Regression Coverage and Documentation

**Files:**

- Modify: `Documentation/DesignDoc.md`
- Modify: `Documentation/TestingChecklist.md`
- Modify: `Documentation/KnowledgeBase/BestPractices.md`
- Modify: `Documentation/KnowledgeBase/Templates.md`
- Modify: `README.md` only if controls change from the documented mapping

- [ ] **Step 1: Run all edit-mode tests**

Expected: no failures in `/tmp/loco-editmode-results.xml`.

- [ ] **Step 2: Run offline compile**

Expected: exit code `0`.

- [ ] **Step 3: Regenerate default assets and inspect validation output**

Run:

```text
Tools > LoCo Fight Game > Create Default Prototype Assets
```

Expected:

- no structural move-data errors
- only intentional advisory warnings
- all new move assets and database references saved

- [ ] **Step 4: Execute full manual regression**

Run every section of `Documentation/TestingChecklist.md`, including:

```text
launch and roster
movement and camera
existing strikes and grapples
ground upper/lower offense
directional quick/power grapples
corner strike/grapple
rope stagger and rebound offense
reversals
pins and submissions
all specials
CPU contextual behavior
reset
standard/no-rope-break/hardcore rules
```

Record any tuning observations separately; do not change scope during the
regression pass.

- [ ] **Step 5: Update authoritative docs**

`DesignDoc.md`:

- add contextual move families
- add directional grapple controls
- document move tiers without match phases
- document CPU contextual priorities

`BestPractices.md`:

- require new contextual families to use `CombatContextResolver`
- require structured validation before resource spending
- require F1 diagnostics for rejected contextual actions

`Templates.md`:

- show how to add a contextual move
- show required compatibility fields
- show directional bucket assignment
- include validation and failure-path checklist

- [ ] **Step 6: Verify documentation links and whitespace**

Run:

```bash
git diff --check
rg -n 'TODO|TBD|FIXME' \
  Documentation \
  Assets/Scripts \
  docs/superpowers/specs/2026-06-10-contextual-combat-slice-design.md
```

Expected: no whitespace errors and no new placeholders.

- [ ] **Step 7: Commit final regression and documentation**

```bash
git add \
  Documentation/DesignDoc.md \
  Documentation/TestingChecklist.md \
  Documentation/KnowledgeBase/BestPractices.md \
  Documentation/KnowledgeBase/Templates.md \
  README.md
git commit -m "Document contextual combat milestone"
```

## Final Completion Check

- [ ] Ground upper/lower offense is deterministic and interruptible.
- [ ] Directional grapples are facing-relative with neutral fallback.
- [ ] Corner offense requires both state and geometry.
- [ ] Rope offense uses `RingInteractionSystem` exclusively.
- [ ] Rebound attacks remain distinct from ordinary running attacks.
- [ ] Move tiers regulate stamina/recovery without match phases.
- [ ] Player and CPU use the same combat APIs and validation.
- [ ] Every contextual failure leaves valid states and ownership.
- [ ] F1 explains context, selection, fallback, tier, and rejection.
- [ ] Existing match flow, rulesets, specials, pins, submissions, reversals, and
      reset behavior pass regression.
- [ ] No deferred creation-suite or broader match scope was introduced.
