# Fight Game 2D Redesign Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Convert the Unity 3D wrestling prototype into a 2D sprite-based game with a false-2D, side-on, depth-laned playing field, proven by a complete Zeak vs JT match, while keeping the existing gameplay model unchanged.

**Architecture:** View-flattening (Approach A). Gameplay keeps running in the existing 3D world space (X = ring length, Z = depth lanes, Y = height). Only presentation changes: an orthographic front camera, a `DepthProjector` that fakes depth with screen offset, scale, and sorting, a code-built paper-doll sprite rig, and a procedural animation driver behind the existing `IAnimationDriver` seam. New pure-logic lives in a small auto-referenced assembly (`LoCoFight.Core2D`) so it is unit-testable headlessly.

**Tech Stack:** Unity 6 LTS (6000.x), C#, built-in render pipeline, `SpriteRenderer` (unlit), Unity Test Framework (EditMode), `com.unity.2d.sprite` for sprite import tooling.

## Global Constraints

Every task's requirements implicitly include these (values copied from the spec):

- Unity version: Unity 6 LTS (6000.x). Pin `ProjectSettings/ProjectVersion.txt`.
- Render pipeline: built-in, with unlit `SpriteRenderer`s. No URP.
- Packages added: `com.unity.2d.sprite`, `com.unity.test-framework`. Do NOT add `com.unity.2d.animation` (paper-doll uses plain `SpriteRenderer` transforms, not mesh skinning) or any URP package.
- Gameplay model is unchanged: roster, moves, specials, traits, AI, ropes, pins, submissions, and all ring math stay as-is. Only presentation, camera, depth input, and the lane-alignment gate change.
- Playing field: side-on with exactly 3 depth lanes (front rope, mid, back rope).
- Animation: procedural bone animation in code (not hand-keyed Unity clips).
- Slice scope: Zeak Gallent vs JT Staten only. The other 14 wrestlers, special/trait visuals, menus, and audio are out of scope.
- Copy rule: no em-dashes in any authored copy (use periods, commas, or parentheses). Applies to the prompt/manifest files in Task 15.
- The rig and arena are built in C# (matching the existing procedural `WrestlerView.BuildPlaceholder`, `ArenaRig.BuildPrimitiveArena`, and `GameBootstrap`), not authored as prefab/scene assets.

## Verification note

This environment has no Unity editor installed, so EditMode tests and the manual QA pass run on the executor's machine (Unity 6 LTS). EditMode tests run headlessly with:

```bash
"<UnityEditorPath>/Unity" -batchmode -runTests -projectPath . -testPlatform EditMode -testResults ./EditModeResults.xml -quit
```

Expected on success: exit code 0 and `<test-run ... result="Passed">` in `EditModeResults.xml`. Tasks 1 through 10 are verified this way. Tasks 6, 7, 11, 12, 13, 14 add visual behavior that is finally confirmed by the Task 16 manual QA pass in the editor; each notes what its EditMode/compile check covers versus what needs the in-editor pass.

---

## File Structure

New runtime logic assembly (auto-referenced, so existing `Assembly-CSharp` code can call it; unit-testable):

- `Assets/Scripts/Core2D/LoCoFight.Core2D.asmdef`: assembly definition.
- `Assets/Scripts/Core2D/LaneSystem.cs`: lane Z values, nearest/snap/step, lane-alignment test.
- `Assets/Scripts/Core2D/DepthProjection.cs`: depth-to-screen offset, scale, sorting math.
- `Assets/Scripts/Core2D/FacingUtil.cs`: facing direction and X-flip math.
- `Assets/Scripts/Core2D/CameraFraming.cs`: orthographic size and midpoint math.

New presentation components (in the default `Assembly-CSharp`, alongside existing scripts):

- `Assets/Scripts/View/DepthProjector.cs`: billboards the rig, applies facing flip and depth offset/scale/sort, drives a ground shadow.
- `Assets/Scripts/View/WrestlerRig.cs`: builds and holds the paper-doll `SpriteRenderer` hierarchy; loads part sprites from `Resources/Parts/<id>/<slot>` with a generated placeholder fallback.
- `Assets/Scripts/Animation/Sprite2DAnimationDriver.cs`: implements `IAnimationDriver` with procedural bone animation.
- `Assets/Scripts/Arena/Arena2DBackdrop.cs`: builds the layered sprite ring.

Modified:

- `Assets/Scripts/Camera/TwoTargetMatchCamera.cs`: orthographic framing.
- `Assets/Scripts/Wrestlers/WrestlerView.cs`: `Build2DRig` replaces primitive parts.
- `Assets/Scripts/Input/PlayerInputController.cs`: lane-snapped depth input.
- `Assets/Scripts/Combat/WrestlerCombat.cs`: lane-alignment gate on strike and grapple.
- `Assets/Scripts/AI/CPUWrestlerAI.cs`: lane-alignment nudge in approach.
- `Assets/Scripts/Arena/ArenaRig.cs`: skip visible primitive surfaces, keep zones/anchors.
- `Assets/Scripts/Core/GameBootstrap.cs`: orthographic camera, sprite backdrop, driver/projector wiring.
- `Packages/manifest.json`: add packages.
- `Documentation/TestingChecklist.md`: 2D QA items.

New tests:

- `Assets/Tests/EditMode/LoCoFight.EditModeTests.asmdef`
- `Assets/Tests/EditMode/LaneSystemTests.cs`
- `Assets/Tests/EditMode/DepthProjectionTests.cs`
- `Assets/Tests/EditMode/FacingUtilTests.cs`
- `Assets/Tests/EditMode/CameraFramingTests.cs`

New art-pipeline docs (Task 15):

- `prompts/part-manifest.md`
- `prompts/parts/_template.md`
- `prompts/parts/zeak-gallent/<slot>.md` (16 files)
- `prompts/parts/jt-staten/<slot>.md` (16 files)

---

## Task 1: Project setup, packages, and the EditMode test harness

**Files:**
- Modify: `Packages/manifest.json`
- Create: `ProjectSettings/ProjectVersion.txt`
- Create: `Assets/Scripts/Core2D/LoCoFight.Core2D.asmdef`
- Create: `Assets/Tests/EditMode/LoCoFight.EditModeTests.asmdef`
- Create: `Assets/Tests/EditMode/HarnessSmokeTest.cs`

**Interfaces:**
- Consumes: nothing.
- Produces: the `LoCoFight.Core2D` assembly (auto-referenced) and a runnable EditMode test assembly that references it.

- [ ] **Step 1: Add packages to the manifest**

Replace `Packages/manifest.json` with:

```json
{
  "dependencies": {
    "com.unity.ugui": "1.0.0",
    "com.unity.2d.sprite": "1.0.0",
    "com.unity.test-framework": "1.4.5",
    "com.unity.modules.audio": "1.0.0",
    "com.unity.modules.imgui": "1.0.0",
    "com.unity.modules.physics": "1.0.0",
    "com.unity.modules.ui": "1.0.0",
    "com.unity.modules.uielements": "1.0.0",
    "com.unity.modules.animation": "1.0.0",
    "com.unity.modules.particlesystem": "1.0.0",
    "com.unity.modules.unitywebrequest": "1.0.0"
  }
}
```

(Unity resolves `com.unity.2d.sprite` and `com.unity.test-framework` to the versions bundled with the installed 6000.x editor on first open; the pinned numbers above are floors.)

- [ ] **Step 2: Pin the Unity version**

Create `ProjectSettings/ProjectVersion.txt` with the installed editor build. Use your installed Unity 6 LTS patch; if unsure, use the latest `6000.0` LTS:

```
m_EditorVersion: 6000.0.32f1
m_EditorVersionWithRevision: 6000.0.32f1 (b2e806cf271c)
```

If your installed build differs, set both lines to it. Unity rewrites this file on first open to match the actual editor; commit whatever it produces.

- [ ] **Step 3: Create the Core2D runtime assembly definition**

Create `Assets/Scripts/Core2D/LoCoFight.Core2D.asmdef`:

```json
{
    "name": "LoCoFight.Core2D",
    "rootNamespace": "LoCoFight",
    "references": [],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

`autoReferenced: true` means the existing `Assembly-CSharp` code (combat, AI, etc.) can call into this assembly without changes.

- [ ] **Step 4: Create the EditMode test assembly definition**

Create `Assets/Tests/EditMode/LoCoFight.EditModeTests.asmdef`:

```json
{
    "name": "LoCoFight.EditModeTests",
    "rootNamespace": "LoCoFight.Tests",
    "references": [
        "LoCoFight.Core2D",
        "UnityEngine.TestRunner",
        "UnityEditor.TestRunner"
    ],
    "includePlatforms": [
        "Editor"
    ],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": [
        "nunit.framework.dll"
    ],
    "autoReferenced": false,
    "defineConstraints": [
        "UNITY_INCLUDE_TESTS"
    ],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- [ ] **Step 5: Write a smoke test that proves the harness runs**

Create `Assets/Tests/EditMode/HarnessSmokeTest.cs`:

```csharp
using NUnit.Framework;

namespace LoCoFight.Tests
{
    public class HarnessSmokeTest
    {
        [Test]
        public void TestRunnerExecutes()
        {
            Assert.AreEqual(4, 2 + 2);
        }
    }
}
```

- [ ] **Step 6: Open the project once in Unity 6, then run EditMode tests**

Open `fightgame/` in Unity 6 LTS (this generates the rest of `ProjectSettings/` and `.meta` files). Then run:

```bash
"<UnityEditorPath>/Unity" -batchmode -runTests -projectPath . -testPlatform EditMode -testResults ./EditModeResults.xml -quit
```

Expected: exit code 0; `EditModeResults.xml` contains `result="Passed"` for `TestRunnerExecutes`.

- [ ] **Step 7: Commit**

```bash
git add Packages/manifest.json ProjectSettings Assets/Scripts/Core2D Assets/Tests
git commit -m "chore: pin Unity 6 LTS, add 2D/test packages, EditMode harness"
```

---

## Task 2: LaneSystem (lane geometry and alignment)

**Files:**
- Create: `Assets/Scripts/Core2D/LaneSystem.cs`
- Test: `Assets/Tests/EditMode/LaneSystemTests.cs`

**Interfaces:**
- Consumes: nothing.
- Produces:
  - `LaneSystem.LaneCount` (int = 3)
  - `LaneSystem.LaneZ` (float[3], front/mid/back = -1.2, 0, 1.2)
  - `LaneSystem.StrikeAlignmentTolerance` (float = 0.6)
  - `int LaneSystem.NearestLaneIndex(float z)`
  - `float LaneSystem.SnapZ(float z)`
  - `int LaneSystem.StepLane(int index, int direction)`
  - `bool LaneSystem.LanesAligned(float zA, float zB, float tolerance)`
  - `bool LaneSystem.LanesAligned(UnityEngine.Transform a, UnityEngine.Transform b, float tolerance)`

- [ ] **Step 1: Write the failing tests**

Create `Assets/Tests/EditMode/LaneSystemTests.cs`:

```csharp
using NUnit.Framework;

namespace LoCoFight.Tests
{
    public class LaneSystemTests
    {
        [Test]
        public void ThreeLanesFrontMidBack()
        {
            Assert.AreEqual(3, LaneSystem.LaneCount);
            Assert.AreEqual(-1.2f, LaneSystem.LaneZ[0], 0.0001f);
            Assert.AreEqual(0f, LaneSystem.LaneZ[1], 0.0001f);
            Assert.AreEqual(1.2f, LaneSystem.LaneZ[2], 0.0001f);
        }

        [Test]
        public void NearestLaneIndexPicksClosest()
        {
            Assert.AreEqual(0, LaneSystem.NearestLaneIndex(-2f));
            Assert.AreEqual(0, LaneSystem.NearestLaneIndex(-0.7f));
            Assert.AreEqual(1, LaneSystem.NearestLaneIndex(0.2f));
            Assert.AreEqual(2, LaneSystem.NearestLaneIndex(5f));
        }

        [Test]
        public void SnapZReturnsLaneValue()
        {
            Assert.AreEqual(0f, LaneSystem.SnapZ(0.3f), 0.0001f);
            Assert.AreEqual(1.2f, LaneSystem.SnapZ(0.9f), 0.0001f);
        }

        [Test]
        public void StepLaneClampsToEnds()
        {
            Assert.AreEqual(0, LaneSystem.StepLane(0, -1));
            Assert.AreEqual(1, LaneSystem.StepLane(0, 1));
            Assert.AreEqual(2, LaneSystem.StepLane(2, 1));
        }

        [Test]
        public void LanesAlignedWithinTolerance()
        {
            Assert.IsTrue(LaneSystem.LanesAligned(0f, 0.5f, 0.6f));
            Assert.IsFalse(LaneSystem.LanesAligned(0f, 1.2f, 0.6f));
        }
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

```bash
"<UnityEditorPath>/Unity" -batchmode -runTests -projectPath . -testPlatform EditMode -testResults ./EditModeResults.xml -quit
```

Expected: FAIL (compile error, `LaneSystem` does not exist).

- [ ] **Step 3: Implement LaneSystem**

Create `Assets/Scripts/Core2D/LaneSystem.cs`:

```csharp
using UnityEngine;

namespace LoCoFight
{
    /// Depth-lane geometry for the side-on false-2D field. Pure logic; no scene state.
    public static class LaneSystem
    {
        public const int LaneCount = 3;

        /// Index 0 = front rope (nearest camera), 1 = mid, 2 = back rope.
        public static readonly float[] LaneZ = { -1.2f, 0f, 1.2f };

        /// Max world-Z difference at which strikes and grapples may connect.
        public const float StrikeAlignmentTolerance = 0.6f;

        public static int NearestLaneIndex(float z)
        {
            int best = 0;
            float bestDist = Mathf.Abs(z - LaneZ[0]);
            for (int i = 1; i < LaneCount; i++)
            {
                float d = Mathf.Abs(z - LaneZ[i]);
                if (d < bestDist) { bestDist = d; best = i; }
            }
            return best;
        }

        public static float SnapZ(float z) => LaneZ[NearestLaneIndex(z)];

        public static int StepLane(int index, int direction) =>
            Mathf.Clamp(index + direction, 0, LaneCount - 1);

        public static bool LanesAligned(float zA, float zB, float tolerance) =>
            Mathf.Abs(zA - zB) <= tolerance;

        public static bool LanesAligned(Transform a, Transform b, float tolerance) =>
            LanesAligned(a.position.z, b.position.z, tolerance);
    }
}
```

- [ ] **Step 4: Run the tests to verify they pass**

```bash
"<UnityEditorPath>/Unity" -batchmode -runTests -projectPath . -testPlatform EditMode -testResults ./EditModeResults.xml -quit
```

Expected: PASS for all `LaneSystemTests`.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Core2D/LaneSystem.cs Assets/Tests/EditMode/LaneSystemTests.cs
git commit -m "feat: add LaneSystem depth-lane geometry with tests"
```

---

## Task 3: DepthProjection (depth-to-screen math)

**Files:**
- Create: `Assets/Scripts/Core2D/DepthProjection.cs`
- Test: `Assets/Tests/EditMode/DepthProjectionTests.cs`

**Interfaces:**
- Consumes: nothing.
- Produces:
  - `float DepthProjection.ScreenYOffset(float z, float yFactor)`
  - `float DepthProjection.DepthScale(float z, float scalePerUnit, float minScale, float maxScale)`
  - `int DepthProjection.SortingOrder(float z, int unitsPerStep)`

Convention: front lane is `z = -1.2` (drawn lower, larger, in front); back lane is `z = +1.2` (drawn higher, smaller, behind).

- [ ] **Step 1: Write the failing tests**

Create `Assets/Tests/EditMode/DepthProjectionTests.cs`:

```csharp
using NUnit.Framework;

namespace LoCoFight.Tests
{
    public class DepthProjectionTests
    {
        [Test]
        public void BackLaneDrawsHigher()
        {
            float front = DepthProjection.ScreenYOffset(-1.2f, 0.35f);
            float back = DepthProjection.ScreenYOffset(1.2f, 0.35f);
            Assert.Less(front, back);
            Assert.AreEqual(0f, DepthProjection.ScreenYOffset(0f, 0.35f), 0.0001f);
        }

        [Test]
        public void BackLaneDrawsSmaller()
        {
            float front = DepthProjection.DepthScale(-1.2f, 0.06f, 0.8f, 1.15f);
            float mid = DepthProjection.DepthScale(0f, 0.06f, 0.8f, 1.15f);
            float back = DepthProjection.DepthScale(1.2f, 0.06f, 0.8f, 1.15f);
            Assert.Greater(front, mid);
            Assert.Greater(mid, back);
            Assert.AreEqual(1f, mid, 0.0001f);
        }

        [Test]
        public void DepthScaleClamps()
        {
            Assert.AreEqual(0.8f, DepthProjection.DepthScale(100f, 0.06f, 0.8f, 1.15f), 0.0001f);
            Assert.AreEqual(1.15f, DepthProjection.DepthScale(-100f, 0.06f, 0.8f, 1.15f), 0.0001f);
        }

        [Test]
        public void FrontLaneSortsInFront()
        {
            int front = DepthProjection.SortingOrder(-1.2f, 100);
            int back = DepthProjection.SortingOrder(1.2f, 100);
            Assert.Greater(front, back);
        }
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run the EditMode command. Expected: FAIL (`DepthProjection` does not exist).

- [ ] **Step 3: Implement DepthProjection**

Create `Assets/Scripts/Core2D/DepthProjection.cs`:

```csharp
using UnityEngine;

namespace LoCoFight
{
    /// Fakes depth for the side-on view. Pure logic; no scene state.
    /// Convention: smaller z is nearer the camera (front lane), larger z is farther (back).
    public static class DepthProjection
    {
        /// Farther lanes draw higher on screen.
        public static float ScreenYOffset(float z, float yFactor) => z * yFactor;

        /// Farther lanes draw smaller. Mid lane (z = 0) is scale 1.
        public static float DepthScale(float z, float scalePerUnit, float minScale, float maxScale) =>
            Mathf.Clamp(1f - z * scalePerUnit, minScale, maxScale);

        /// Nearer lanes get a higher sorting order so they overlap farther ones.
        public static int SortingOrder(float z, int unitsPerStep) =>
            Mathf.RoundToInt(-z * unitsPerStep);
    }
}
```

- [ ] **Step 4: Run the tests to verify they pass**

Run the EditMode command. Expected: PASS for all `DepthProjectionTests`.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Core2D/DepthProjection.cs Assets/Tests/EditMode/DepthProjectionTests.cs
git commit -m "feat: add DepthProjection depth-faking math with tests"
```

---

## Task 4: FacingUtil (facing direction and flip)

**Files:**
- Create: `Assets/Scripts/Core2D/FacingUtil.cs`
- Test: `Assets/Tests/EditMode/FacingUtilTests.cs`

**Interfaces:**
- Consumes: nothing.
- Produces:
  - `bool FacingUtil.FacingRight(float selfX, float opponentX)`
  - `float FacingUtil.FlipScaleX(bool facingRight)` (returns +1 facing right, -1 facing left)

- [ ] **Step 1: Write the failing tests**

Create `Assets/Tests/EditMode/FacingUtilTests.cs`:

```csharp
using NUnit.Framework;

namespace LoCoFight.Tests
{
    public class FacingUtilTests
    {
        [Test]
        public void FacesOpponentToTheRight()
        {
            Assert.IsTrue(FacingUtil.FacingRight(0f, 3f));
            Assert.IsFalse(FacingUtil.FacingRight(3f, 0f));
        }

        [Test]
        public void FlipScaleMatchesFacing()
        {
            Assert.AreEqual(1f, FacingUtil.FlipScaleX(true), 0.0001f);
            Assert.AreEqual(-1f, FacingUtil.FlipScaleX(false), 0.0001f);
        }
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run the EditMode command. Expected: FAIL (`FacingUtil` does not exist).

- [ ] **Step 3: Implement FacingUtil**

Create `Assets/Scripts/Core2D/FacingUtil.cs`:

```csharp
namespace LoCoFight
{
    /// Side-on facing helpers. The rig is authored facing screen-right and flips via X scale.
    public static class FacingUtil
    {
        public static bool FacingRight(float selfX, float opponentX) => opponentX >= selfX;

        public static float FlipScaleX(bool facingRight) => facingRight ? 1f : -1f;
    }
}
```

- [ ] **Step 4: Run the tests to verify they pass**

Run the EditMode command. Expected: PASS for all `FacingUtilTests`.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Core2D/FacingUtil.cs Assets/Tests/EditMode/FacingUtilTests.cs
git commit -m "feat: add FacingUtil side-on facing math with tests"
```

---

## Task 5: CameraFraming (orthographic framing math)

**Files:**
- Create: `Assets/Scripts/Core2D/CameraFraming.cs`
- Test: `Assets/Tests/EditMode/CameraFramingTests.cs`

**Interfaces:**
- Consumes: nothing.
- Produces:
  - `float CameraFraming.MidpointX(float xA, float xB)`
  - `float CameraFraming.OrthographicSizeFor(float separationX, float baseSize, float sizePerUnit, float minSize, float maxSize)`

- [ ] **Step 1: Write the failing tests**

Create `Assets/Tests/EditMode/CameraFramingTests.cs`:

```csharp
using NUnit.Framework;

namespace LoCoFight.Tests
{
    public class CameraFramingTests
    {
        [Test]
        public void MidpointIsAverage()
        {
            Assert.AreEqual(0f, CameraFraming.MidpointX(-2f, 2f), 0.0001f);
            Assert.AreEqual(1f, CameraFraming.MidpointX(0f, 2f), 0.0001f);
        }

        [Test]
        public void SizeGrowsWithSeparationAndClamps()
        {
            float near = CameraFraming.OrthographicSizeFor(1f, 3f, 0.5f, 3f, 7f);
            float far = CameraFraming.OrthographicSizeFor(6f, 3f, 0.5f, 3f, 7f);
            Assert.Less(near, far);
            Assert.AreEqual(3f, CameraFraming.OrthographicSizeFor(0f, 3f, 0.5f, 3f, 7f), 0.0001f);
            Assert.AreEqual(7f, CameraFraming.OrthographicSizeFor(100f, 3f, 0.5f, 3f, 7f), 0.0001f);
        }
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run the EditMode command. Expected: FAIL (`CameraFraming` does not exist).

- [ ] **Step 3: Implement CameraFraming**

Create `Assets/Scripts/Core2D/CameraFraming.cs`:

```csharp
using UnityEngine;

namespace LoCoFight
{
    /// Orthographic two-target framing math. Pure logic; no scene state.
    public static class CameraFraming
    {
        public static float MidpointX(float xA, float xB) => (xA + xB) * 0.5f;

        public static float OrthographicSizeFor(
            float separationX, float baseSize, float sizePerUnit, float minSize, float maxSize) =>
            Mathf.Clamp(baseSize + separationX * sizePerUnit, minSize, maxSize);
    }
}
```

- [ ] **Step 4: Run the tests to verify they pass**

Run the EditMode command. Expected: PASS for all `CameraFramingTests`.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Core2D/CameraFraming.cs Assets/Tests/EditMode/CameraFramingTests.cs
git commit -m "feat: add CameraFraming orthographic framing math with tests"
```

---

## Task 6: Rework TwoTargetMatchCamera to orthographic

**Files:**
- Modify: `Assets/Scripts/Camera/TwoTargetMatchCamera.cs` (full rewrite of the file)

**Interfaces:**
- Consumes: `CameraFraming.MidpointX`, `CameraFraming.OrthographicSizeFor`, and the existing `WrestlerCore`/`WrestlerState` types for the aerial check.
- Produces: `SetTargets(Transform a, Transform b)` (unchanged signature, still called by `GameBootstrap`).

Verification: this is presentation. EditMode coverage is the Task 5 framing math. Final visual confirmation is the Task 16 manual pass. `viewSide` lets you flip the camera to the other side of the ring in one place if the scene renders mirrored.

- [ ] **Step 1: Replace the file**

Replace the entire contents of `Assets/Scripts/Camera/TwoTargetMatchCamera.cs` with:

```csharp
using UnityEngine;

namespace LoCoFight
{
    /// Orthographic, straight-on broadcast camera for the side-on 2D view.
    /// Tracks the horizontal midpoint and zooms (orthographic size) with separation.
    public class TwoTargetMatchCamera : MonoBehaviour
    {
        public Transform targetA;
        public Transform targetB;

        [Header("Framing")]
        public float baseSize = 3.2f;
        public float sizePerUnit = 0.45f;
        public float minSize = 3.0f;
        public float maxSize = 6.5f;
        public float aerialSizeBoost = 0.8f;

        [Header("Placement")]
        [Tooltip("Vertical center of the framing, in world units above the mat.")]
        public float framingHeight = 1.3f;
        [Tooltip("Distance the camera sits in front of the ring along Z.")]
        public float cameraDepth = 12f;
        [Tooltip("+1 views from -Z, -1 views from +Z. Flip once if the scene renders mirrored.")]
        public float viewSide = 1f;
        public float smoothTime = 0.12f;

        UnityEngine.Camera _cam;
        Vector3 _velocity;
        float _currentSize;

        public void SetTargets(Transform a, Transform b)
        {
            targetA = a;
            targetB = b;
            EnsureCamera();
            _currentSize = baseSize;
            transform.position = ComputeDesiredPosition();
            transform.rotation = ComputeRotation();
            _cam.orthographic = true;
            _cam.orthographicSize = _currentSize;
        }

        void EnsureCamera()
        {
            if (_cam == null) _cam = GetComponent<UnityEngine.Camera>();
            if (_cam != null) _cam.orthographic = true;
        }

        void LateUpdate()
        {
            if (targetA == null || targetB == null) return;
            EnsureCamera();

            Vector3 desired = ComputeDesiredPosition();
            transform.position = Vector3.SmoothDamp(transform.position, desired, ref _velocity, smoothTime);
            transform.rotation = ComputeRotation();

            float separation = Mathf.Abs(targetA.position.x - targetB.position.x);
            float targetSize = CameraFraming.OrthographicSizeFor(separation, baseSize, sizePerUnit, minSize, maxSize);
            if (IsAerial(targetA) || IsAerial(targetB)) targetSize += aerialSizeBoost;
            _currentSize = Mathf.Lerp(_currentSize, targetSize, Time.deltaTime * 6f);
            if (_cam != null) _cam.orthographicSize = _currentSize;
        }

        Vector3 ComputeDesiredPosition()
        {
            float midX = CameraFraming.MidpointX(targetA.position.x, targetB.position.x);
            return new Vector3(midX, framingHeight, -viewSide * cameraDepth);
        }

        Quaternion ComputeRotation()
        {
            // Look straight along Z toward the ring.
            return Quaternion.LookRotation(new Vector3(0f, 0f, viewSide), Vector3.up);
        }

        static bool IsAerial(Transform t)
        {
            var core = t.GetComponent<WrestlerCore>();
            if (core == null) return false;
            var s = core.States.Current;
            return s == WrestlerState.AerialAirborne || s == WrestlerState.TurnbuckleClimb ||
                   s == WrestlerState.AerialSetup || s == WrestlerState.SpecialActive;
        }
    }
}
```

- [ ] **Step 2: Verify it compiles**

In Unity, confirm no compile errors in the Console (or run the EditMode command, which fails on compile errors). Expected: clean compile; all earlier tests still PASS.

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/Camera/TwoTargetMatchCamera.cs
git commit -m "feat: make match camera orthographic for side-on 2D view"
```

---

## Task 7: DepthProjector component (billboard, facing, depth, shadow)

**Files:**
- Create: `Assets/Scripts/View/DepthProjector.cs`

**Interfaces:**
- Consumes: `DepthProjection.*`, `FacingUtil.*`, the wrestler root `Transform` (for world Z and forward), and a `visualRoot` `Transform` (the rig parent).
- Produces: `Bind(Transform wrestlerRoot, Transform visualRoot)` and per-frame projection of the visual root.

Verification: presentation. Depth/facing math is covered by Tasks 3 and 4 EditMode tests; on-screen behavior is confirmed in Task 16.

- [ ] **Step 1: Create the component**

Create `Assets/Scripts/View/DepthProjector.cs`:

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace LoCoFight
{
    /// Projects a wrestler's 3D world position into the flat side-on view:
    /// billboards the rig to face the camera, flips it by facing, applies the
    /// depth screen-offset and scale, sets sprite sorting, and drives a shadow.
    public class DepthProjector : MonoBehaviour
    {
        [Header("Depth")]
        public float yFactor = 0.35f;
        public float scalePerUnit = 0.06f;
        public float minScale = 0.8f;
        public float maxScale = 1.15f;
        public int sortUnitsPerStep = 100;

        [Header("Shadow")]
        public bool drawShadow = true;
        public float shadowWidth = 0.7f;

        Transform _root;
        Transform _visual;
        readonly List<SpriteRenderer> _renderers = new List<SpriteRenderer>();
        SpriteRenderer _shadow;
        bool _facingRight = true;
        float _baseVisualY;

        public void Bind(Transform wrestlerRoot, Transform visualRoot)
        {
            _root = wrestlerRoot;
            _visual = visualRoot;
            _baseVisualY = visualRoot.localPosition.y;
            _renderers.Clear();
            visualRoot.GetComponentsInChildren(true, _renderers);
            if (drawShadow) CreateShadow();
        }

        void CreateShadow()
        {
            var go = new GameObject("Shadow");
            go.transform.SetParent(_root, false);
            _shadow = go.AddComponent<SpriteRenderer>();
            _shadow.sprite = PlaceholderSprites.Ellipse(new Color(0f, 0f, 0f, 0.35f));
            _shadow.sortingOrder = -10000;
            go.transform.localScale = new Vector3(shadowWidth, shadowWidth * 0.35f, 1f);
        }

        void LateUpdate()
        {
            if (_root == null || _visual == null) return;

            float z = _root.position.z;

            // Billboard: cancel any gameplay-transform rotation so the rig faces the camera.
            _visual.rotation = Quaternion.identity;

            // Facing from the gameplay transform's forward.x (updated by the motor).
            float fx = _root.forward.x;
            if (Mathf.Abs(fx) > 0.01f) _facingRight = fx >= 0f;

            float depthScale = DepthProjection.DepthScale(z, scalePerUnit, minScale, maxScale);
            float sign = FacingUtil.FlipScaleX(_facingRight);
            _visual.localScale = new Vector3(sign * depthScale, depthScale, 1f);

            var lp = _visual.localPosition;
            lp.y = _baseVisualY + DepthProjection.ScreenYOffset(z, yFactor);
            _visual.localPosition = lp;

            int order = DepthProjection.SortingOrder(z, sortUnitsPerStep);
            for (int i = 0; i < _renderers.Count; i++)
                _renderers[i].sortingOrder = order + _renderers[i].sortingOrder % 100;

            if (_shadow != null)
            {
                var sp = _shadow.transform.localPosition;
                sp.x = 0f;
                sp.y = 0.02f;
                sp.z = 0f;
                _shadow.transform.localPosition = sp;
                _shadow.sortingOrder = order - 1;
            }
        }
    }
}
```

Note: this references `PlaceholderSprites.Ellipse`, created in Task 11. Task 11 is implemented before this component is wired in Task 14, so the symbol exists at integration time. If you implement Task 7 before Task 11, temporarily set `drawShadow = false` defaults are fine; the field still compiles only once `PlaceholderSprites` exists. Implement Task 11 in the same branch before compiling.

- [ ] **Step 2: Defer compile check to Task 11**

`DepthProjector` depends on `PlaceholderSprites` (Task 11). Do not compile-gate this task alone; it compiles after Task 11 lands. Commit now; the combined compile check happens at the end of Task 11.

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/View/DepthProjector.cs
git commit -m "feat: add DepthProjector for billboard, facing, depth, and shadow"
```

---

## Task 8: Lane-snapped depth input

**Files:**
- Modify: `Assets/Scripts/Input/PlayerInputController.cs` (the `HandleMovement` method)

**Interfaces:**
- Consumes: `LaneSystem.SnapZ`, the existing `_core.Motor.SetMoveInput(Vector3, bool)`, and `_core.transform.position`.
- Produces: depth input that biases the wrestler toward the nearest lane while horizontal stays free.

Approach: keep free horizontal input. For depth, convert the player's vertical intent into a pull toward the snapped lane center, so the wrestler settles onto a lane when not pressing up/down and steps cleanly when pressing. This avoids changing `WrestlerMotor` (world Z stays continuous, per the spec).

- [ ] **Step 1: Replace HandleMovement**

In `Assets/Scripts/Input/PlayerInputController.cs`, replace the entire `HandleMovement()` method with:

```csharp
        void HandleMovement()
        {
            float x = 0f;
            float depth = 0f;
            if (Input.GetKey(KeyCode.A)) x -= 1f;
            if (Input.GetKey(KeyCode.D)) x += 1f;
            if (Input.GetKey(KeyCode.W)) depth += 1f; // toward back lane (+Z)
            if (Input.GetKey(KeyCode.S)) depth -= 1f; // toward front lane (-Z)

            // Horizontal is free. Depth is lane-biased: when the player is not
            // pressing W/S, pull gently toward the nearest lane center so the
            // wrestler settles onto a lane. World Z stays continuous.
            float zNow = _core.transform.position.z;
            float zMove;
            if (Mathf.Abs(depth) > 0.01f)
            {
                zMove = depth;
            }
            else
            {
                float snapTarget = LaneSystem.SnapZ(zNow);
                zMove = Mathf.Clamp(snapTarget - zNow, -1f, 1f);
                if (Mathf.Abs(zMove) < 0.02f) zMove = 0f;
            }

            Vector3 input = new Vector3(x, 0f, zMove);
            if (input.sqrMagnitude > 1f) input.Normalize();

            _core.Motor.SetMoveInput(input, Input.GetKey(KeyCode.LeftShift));

            // Roll away while downed: direction key + Space.
            if (_core.States.Current == WrestlerState.Downed && Input.GetKeyDown(KeyCode.Space) &&
                (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D)))
            {
                Vector3 side = Input.GetKey(KeyCode.A) ? Vector3.left : Vector3.right;
                var ring = RingInteractionSystem.Instance;
                Vector3 target = transform.position + side * 1.5f;
                if (ring != null) target = ring.Bounds.ClampInside(target);
                _core.States.Set(WrestlerState.RollingAway, 0.5f);
                _core.Motor.Teleport(target);
            }
        }
```

This removes the old camera-relative remap (the orthographic camera looks straight down Z, so world axes map directly: A/D to world X, W/S to world Z).

- [ ] **Step 2: Verify it compiles**

In Unity, confirm a clean compile. Expected: no errors; earlier EditMode tests still PASS.

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/Input/PlayerInputController.cs
git commit -m "feat: lane-snapped depth input with free horizontal movement"
```

---

## Task 9: Lane-alignment gate on strikes and grapples

**Files:**
- Modify: `Assets/Scripts/Combat/WrestlerCombat.cs` (the range checks in `TryStrike` at line ~130 and `TryGrappleAttempt` at line ~218)

**Interfaces:**
- Consumes: `LaneSystem.LanesAligned(Transform, Transform, float)` and `LaneSystem.StrikeAlignmentTolerance`.
- Produces: strikes and grapples that only connect when attacker and defender are on (nearly) the same lane.

The lane test is already covered by `LaneSystemTests.LanesAlignedWithinTolerance` (Task 2). This task wires it into combat. The in-match behavior (whiff across lanes) is confirmed in Task 16.

- [ ] **Step 1: Add the gate to TryStrike**

In `Assets/Scripts/Combat/WrestlerCombat.cs`, find this line inside `TryStrike`:

```csharp
            if (!HitboxProbe.InRange(transform, Opp.transform, move.range)) return false;
```

Add immediately after it:

```csharp
            if (!LaneSystem.LanesAligned(transform, Opp.transform, LaneSystem.StrikeAlignmentTolerance)) return false;
```

- [ ] **Step 2: Add the gate to TryGrappleAttempt**

In the same file, find this line inside `TryGrappleAttempt`:

```csharp
            if (!HitboxProbe.InRange(transform, Opp.transform, GrappleRange)) return false;
```

Add immediately after it:

```csharp
            if (!LaneSystem.LanesAligned(transform, Opp.transform, LaneSystem.StrikeAlignmentTolerance)) return false;
```

- [ ] **Step 3: Verify it compiles**

In Unity, confirm a clean compile. Expected: no errors; earlier EditMode tests still PASS.

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/Combat/WrestlerCombat.cs
git commit -m "feat: require lane alignment for strikes and grapples"
```

---

## Task 10: AI lane-alignment nudge

**Files:**
- Modify: `Assets/Scripts/AI/CPUWrestlerAI.cs` (the approach/movement step)

**Interfaces:**
- Consumes: `LaneSystem.LanesAligned`, `LaneSystem.StrikeAlignmentTolerance`, the opponent transform, and the AI's existing move-direction output.
- Produces: an AI that closes onto the opponent's lane before committing to strikes.

Because the existing AI already closes XZ distance toward the opponent, it largely lines up on its own. This task adds an explicit bias so the CPU does not hover one lane off. The exact approach method name is in `CPUWrestlerAI.cs`; locate where it computes a movement direction toward the opponent and feeds `Motor.SetMoveInput`.

- [ ] **Step 1: Inspect the approach code**

Open `Assets/Scripts/AI/CPUWrestlerAI.cs` and find where it sets movement toward the opponent (search for `SetMoveInput`). Identify the local `Vector3` direction it builds (call it `dir`).

- [ ] **Step 2: Add the lane bias before SetMoveInput**

Immediately before the `SetMoveInput(...)` call in the approach path, insert (adapting the direction variable name to the one in the file):

```csharp
            // 2D lane bias: if off the opponent's lane, prioritize closing depth so
            // strikes can land in the side-on view.
            float zSelf = transform.position.z;
            float zOpp = _core.Opponent != null ? _core.Opponent.transform.position.z : zSelf;
            if (!LaneSystem.LanesAligned(zSelf, zOpp, LaneSystem.StrikeAlignmentTolerance))
            {
                dir.z = Mathf.Sign(zOpp - zSelf);
                dir.x *= 0.4f; // ease off horizontal until aligned
            }
```

If the file's direction variable is not named `dir`, rename the snippet's `dir` to match. If the direction is normalized after this, keep this insertion before the normalization.

- [ ] **Step 3: Verify it compiles**

In Unity, confirm a clean compile. Expected: no errors; earlier EditMode tests still PASS.

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/AI/CPUWrestlerAI.cs
git commit -m "feat: bias CPU approach toward the opponent's lane"
```

---

## Task 11: Paper-doll rig and placeholder sprites

**Files:**
- Create: `Assets/Scripts/View/PlaceholderSprites.cs`
- Create: `Assets/Scripts/View/RigSlot.cs`
- Create: `Assets/Scripts/View/WrestlerRig.cs`

**Interfaces:**
- Consumes: `Resources.Load<Sprite>($"Parts/{characterId}/{slot}")` for real art (optional), with a generated placeholder fallback.
- Produces:
  - `enum RigSlot { Pelvis, Torso, Head, Headpiece, UpperArmNear, ForearmNear, HandNear, UpperArmFar, ForearmFar, HandFar, ThighNear, ShinNear, FootNear, ThighFar, ShinFar, FootFar }`
  - `PlaceholderSprites.Box(Color, float w, float h, Vector2 pivot)` and `PlaceholderSprites.Ellipse(Color)` returning `Sprite`
  - `WrestlerRig.Build(Transform parent, string characterId, Color primary)` returning a built `WrestlerRig` whose `Root` is the visual root and `Joint(RigSlot)` returns each joint transform.

This builds the rig entirely in code (like the existing `WrestlerView.BuildPlaceholder`), so no editor-authored prefab is needed. Real per-part art drops into `Assets/Resources/Parts/<id>/<slot>.png` later with no code change.

- [ ] **Step 1: Create the placeholder sprite factory**

Create `Assets/Scripts/View/PlaceholderSprites.cs`:

```csharp
using UnityEngine;

namespace LoCoFight
{
    /// Generates simple solid sprites at runtime so the rig works before real art exists.
    public static class PlaceholderSprites
    {
        const float PixelsPerUnit = 100f;

        public static Sprite Box(Color color, float widthUnits, float heightUnits, Vector2 pivot)
        {
            int w = Mathf.Max(2, Mathf.RoundToInt(widthUnits * PixelsPerUnit));
            int h = Mathf.Max(2, Mathf.RoundToInt(heightUnits * PixelsPerUnit));
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            var px = new Color[w * h];
            for (int i = 0; i < px.Length; i++) px[i] = color;
            // 1px darker outline for readability.
            Color edge = color * 0.55f; edge.a = 1f;
            for (int x = 0; x < w; x++) { px[x] = edge; px[(h - 1) * w + x] = edge; }
            for (int y = 0; y < h; y++) { px[y * w] = edge; px[y * w + (w - 1)] = edge; }
            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), pivot, PixelsPerUnit);
        }

        public static Sprite Ellipse(Color color)
        {
            const int s = 64;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            var px = new Color[s * s];
            float r = s * 0.5f;
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float dx = (x - r) / r, dy = (y - r) / r;
                    px[y * s + x] = (dx * dx + dy * dy) <= 1f ? color : Color.clear;
                }
            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), PixelsPerUnit);
        }
    }
}
```

- [ ] **Step 2: Create the slot enum**

Create `Assets/Scripts/View/RigSlot.cs`:

```csharp
namespace LoCoFight
{
    /// Paper-doll rig joints. Authored facing screen-right.
    public enum RigSlot
    {
        Pelvis, Torso, Head, Headpiece,
        UpperArmNear, ForearmNear, HandNear,
        UpperArmFar, ForearmFar, HandFar,
        ThighNear, ShinNear, FootNear,
        ThighFar, ShinFar, FootFar
    }
}
```

- [ ] **Step 3: Create the rig builder**

Create `Assets/Scripts/View/WrestlerRig.cs`:

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace LoCoFight
{
    /// Builds and holds the code-built paper-doll SpriteRenderer hierarchy.
    /// Loads real parts from Resources/Parts/<id>/<slot> when present, else uses placeholders.
    public class WrestlerRig : MonoBehaviour
    {
        public Transform Root { get; private set; }
        readonly Dictionary<RigSlot, Transform> _joints = new Dictionary<RigSlot, Transform>();

        public Transform Joint(RigSlot slot) => _joints.TryGetValue(slot, out var t) ? t : null;

        // Slot layout: local position of each joint relative to its parent (rig units),
        // sprite size (w,h), pivot (0..1), and base sorting offset (near limbs in front).
        struct SlotDef
        {
            public RigSlot slot, parent;
            public Vector3 local;
            public float w, h;
            public Vector2 pivot;
            public int sort;
            public bool useParent; // parent is another slot vs the root
        }

        static readonly SlotDef[] Layout =
        {
            // slot,            parent,          local pos,                 w,    h,    pivot,                 sort, useParent
            Def(RigSlot.Pelvis,       RigSlot.Pelvis, new Vector3(0f,0.78f,0f), 0.42f,0.22f, new Vector2(0.5f,0.5f),  0, false),
            Def(RigSlot.Torso,        RigSlot.Pelvis, new Vector3(0f,0.11f,0f), 0.50f,0.55f, new Vector2(0.5f,0f),   10, true),
            Def(RigSlot.Head,         RigSlot.Torso,  new Vector3(0f,0.55f,0f), 0.34f,0.34f, new Vector2(0.5f,0f),   12, true),
            Def(RigSlot.Headpiece,    RigSlot.Head,   new Vector3(0f,0.10f,0f), 0.40f,0.30f, new Vector2(0.5f,0.2f), 13, true),

            Def(RigSlot.UpperArmFar,  RigSlot.Torso,  new Vector3(-0.10f,0.50f,0f), 0.16f,0.34f, new Vector2(0.5f,1f), -5, true),
            Def(RigSlot.ForearmFar,   RigSlot.UpperArmFar, new Vector3(0f,-0.30f,0f), 0.14f,0.30f, new Vector2(0.5f,1f), -5, true),
            Def(RigSlot.HandFar,      RigSlot.ForearmFar,  new Vector3(0f,-0.26f,0f), 0.16f,0.16f, new Vector2(0.5f,1f), -5, true),

            Def(RigSlot.UpperArmNear, RigSlot.Torso,  new Vector3(0.10f,0.50f,0f), 0.17f,0.35f, new Vector2(0.5f,1f), 20, true),
            Def(RigSlot.ForearmNear,  RigSlot.UpperArmNear, new Vector3(0f,-0.31f,0f), 0.15f,0.31f, new Vector2(0.5f,1f), 20, true),
            Def(RigSlot.HandNear,     RigSlot.ForearmNear,  new Vector3(0f,-0.27f,0f), 0.17f,0.17f, new Vector2(0.5f,1f), 20, true),

            Def(RigSlot.ThighFar,     RigSlot.Pelvis, new Vector3(-0.10f,-0.05f,0f), 0.18f,0.34f, new Vector2(0.5f,1f), -3, true),
            Def(RigSlot.ShinFar,      RigSlot.ThighFar, new Vector3(0f,-0.30f,0f), 0.16f,0.32f, new Vector2(0.5f,1f), -3, true),
            Def(RigSlot.FootFar,      RigSlot.ShinFar,  new Vector3(0.04f,-0.28f,0f), 0.22f,0.12f, new Vector2(0.3f,1f), -3, true),

            Def(RigSlot.ThighNear,    RigSlot.Pelvis, new Vector3(0.10f,-0.05f,0f), 0.19f,0.35f, new Vector2(0.5f,1f), 15, true),
            Def(RigSlot.ShinNear,     RigSlot.ThighNear, new Vector3(0f,-0.31f,0f), 0.17f,0.33f, new Vector2(0.5f,1f), 15, true),
            Def(RigSlot.FootNear,     RigSlot.ShinNear,  new Vector3(0.04f,-0.29f,0f), 0.23f,0.13f, new Vector2(0.3f,1f), 15, true),
        };

        static SlotDef Def(RigSlot s, RigSlot p, Vector3 local, float w, float h, Vector2 pivot, int sort, bool useParent)
            => new SlotDef { slot = s, parent = p, local = local, w = w, h = h, pivot = pivot, sort = sort, useParent = useParent };

        public static WrestlerRig Build(Transform parent, string characterId, Color primary)
        {
            var rootGo = new GameObject("Rig2D");
            rootGo.transform.SetParent(parent, false);
            var rig = rootGo.AddComponent<WrestlerRig>();
            rig.Root = rootGo.transform;

            foreach (var d in Layout)
            {
                Transform parentT = d.slot == RigSlot.Pelvis
                    ? rig.Root
                    : (d.useParent ? rig._joints[d.parent] : rig.Root);

                var go = new GameObject(d.slot.ToString());
                go.transform.SetParent(parentT, false);
                go.transform.localPosition = d.local;

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = LoadPart(characterId, d.slot) ?? PlaceholderSprites.Box(TintFor(d.slot, primary), d.w, d.h, d.pivot);
                sr.sortingOrder = d.sort;
                rig._joints[d.slot] = go.transform;
            }

            return rig;
        }

        static Sprite LoadPart(string characterId, RigSlot slot)
        {
            string slugId = characterId.StartsWith("tas-") ? characterId.Substring(4) : characterId;
            return Resources.Load<Sprite>($"Parts/{slugId}/{SlotFile(slot)}");
        }

        static string SlotFile(RigSlot slot)
        {
            switch (slot)
            {
                case RigSlot.UpperArmNear: return "upper-arm-near";
                case RigSlot.ForearmNear: return "forearm-near";
                case RigSlot.HandNear: return "hand-near";
                case RigSlot.UpperArmFar: return "upper-arm-far";
                case RigSlot.ForearmFar: return "forearm-far";
                case RigSlot.HandFar: return "hand-far";
                case RigSlot.ThighNear: return "thigh-near";
                case RigSlot.ShinNear: return "shin-near";
                case RigSlot.FootNear: return "foot-near";
                case RigSlot.ThighFar: return "thigh-far";
                case RigSlot.ShinFar: return "shin-far";
                case RigSlot.FootFar: return "foot-far";
                default: return slot.ToString().ToLowerInvariant();
            }
        }

        static Color TintFor(RigSlot slot, Color primary)
        {
            switch (slot)
            {
                case RigSlot.Head: return new Color(0.95f, 0.78f, 0.62f);
                case RigSlot.Headpiece: return primary * 0.7f;
                case RigSlot.HandNear:
                case RigSlot.HandFar: return new Color(0.95f, 0.78f, 0.62f);
                case RigSlot.UpperArmFar:
                case RigSlot.ForearmFar:
                case RigSlot.ThighFar:
                case RigSlot.ShinFar:
                case RigSlot.FootFar: return primary * 0.8f;
                default: return primary;
            }
        }
    }
}
```

- [ ] **Step 3a: Verify the rig and DepthProjector compile**

In Unity, confirm a clean compile (this is also the deferred compile gate for Task 7). Expected: no errors; earlier EditMode tests still PASS.

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/View/PlaceholderSprites.cs Assets/Scripts/View/RigSlot.cs Assets/Scripts/View/WrestlerRig.cs
git commit -m "feat: add code-built paper-doll rig with placeholder parts"
```

---

## Task 12: Sprite2DAnimationDriver (procedural bone animation)

**Files:**
- Create: `Assets/Scripts/Animation/Sprite2DAnimationDriver.cs`

**Interfaces:**
- Consumes: `WrestlerRig` (joint transforms) and the existing `IAnimationDriver` interface.
- Produces: a `MonoBehaviour` implementing every `IAnimationDriver` member; `Bind(WrestlerRig rig)`.

The existing interface is the contract (do not change it). It declares: `PlayMove(string, string, float)`, `PlayState(string)`, `SetMovementSpeed(float)`, `TriggerHitReact()`, `TriggerReversal()`, `TriggerDodge()`, `TriggerDowned()`, `TriggerGetUp()`, `TriggerRopeStagger()`, `TriggerCornered()`, `TriggerAerialLaunch()`, `TriggerAerialLanding(bool)`, `TriggerSpecial(string)`. Verification: compile here; motion quality confirmed in Task 16.

- [ ] **Step 1: Create the driver**

Create `Assets/Scripts/Animation/Sprite2DAnimationDriver.cs`:

```csharp
using System.Collections;
using UnityEngine;

namespace LoCoFight
{
    /// Procedural bone animation over the paper-doll rig. Implements the gameplay-facing
    /// IAnimationDriver so combat, AI, and state code are untouched.
    public class Sprite2DAnimationDriver : MonoBehaviour, IAnimationDriver
    {
        WrestlerRig _rig;
        float _moveSpeed;
        float _cycle;
        float _torsoTilt;       // degrees, eased toward target
        float _torsoTiltTarget;
        Coroutine _swing;
        Coroutine _flash;

        public void Bind(WrestlerRig rig) => _rig = rig;

        void Update()
        {
            if (_rig == null) return;

            // Locomotion: swing thighs and arms by a sin cycle scaled by speed.
            _cycle += Time.deltaTime * (4f + _moveSpeed * 10f);
            float amp = Mathf.Clamp01(_moveSpeed) * 28f;
            float s = Mathf.Sin(_cycle) * amp;

            SetLocalZ(RigSlot.ThighNear, s);
            SetLocalZ(RigSlot.ThighFar, -s);
            SetLocalZ(RigSlot.ShinNear, -Mathf.Max(0f, s) * 0.6f);
            SetLocalZ(RigSlot.ShinFar, -Mathf.Max(0f, -s) * 0.6f);

            // Arms counter-swing unless a strike/grapple swing coroutine owns them.
            if (_swing == null)
            {
                SetLocalZ(RigSlot.UpperArmNear, -s * 0.6f);
                SetLocalZ(RigSlot.UpperArmFar, s * 0.6f);
            }

            // Torso tilt eases toward the current state's target.
            _torsoTilt = Mathf.Lerp(_torsoTilt, _torsoTiltTarget, Time.deltaTime * 10f);
            var torso = _rig.Joint(RigSlot.Torso);
            if (torso != null) torso.localRotation = Quaternion.Euler(0f, 0f, _torsoTilt);

            // Subtle idle bob.
            float bob = Mathf.Sin(Time.time * 6f) * (0.01f + _moveSpeed * 0.02f);
            var pelvis = _rig.Joint(RigSlot.Pelvis);
            if (pelvis != null)
            {
                var lp = pelvis.localPosition; lp.y = 0.78f + bob; pelvis.localPosition = lp;
            }
        }

        public void SetMovementSpeed(float speed) => _moveSpeed = speed;

        public void PlayMove(string animationStateName, string placeholderPoseName, float speed = 1f)
        {
            switch (placeholderPoseName)
            {
                case "strike": StartSwing(RigSlot.UpperArmNear, RigSlot.ForearmNear, -110f, 0.10f, 0.16f); break;
                case "grapple": StartSwing(RigSlot.UpperArmNear, RigSlot.ForearmNear, -70f, 0.12f, 0.18f, RigSlot.UpperArmFar, RigSlot.ForearmFar); break;
                case "special": Flash(new Color(1f, 0.6f, 0f)); StartSwing(RigSlot.UpperArmNear, RigSlot.ForearmNear, -120f, 0.08f, 0.20f, RigSlot.UpperArmFar, RigSlot.ForearmFar); break;
            }
        }

        public void PlayState(string stateName)
        {
            switch (stateName)
            {
                case "Downed":
                case "Pinned":
                case "RollingAway":
                case "Defeat": _torsoTiltTarget = 82f; break;
                case "Stunned": _torsoTiltTarget = 14f; break;
                case "RopeStaggered": _torsoTiltTarget = -18f; break;
                case "Cornered": _torsoTiltTarget = -10f; break;
                case "GettingUp": _torsoTiltTarget = 40f; break;
                case "TurnbuckleClimb":
                case "AerialSetup": _torsoTiltTarget = -8f; break;
                case "Pinning":
                case "SubmissionApplying": _torsoTiltTarget = 55f; break;
                case "SubmissionDefending": _torsoTiltTarget = 70f; break;
                case "RopeTrapLocked": _torsoTiltTarget = -42f; break;
                case "Victory": _torsoTiltTarget = 0f; ArmsUp(); break;
                default: _torsoTiltTarget = 0f; break;
            }
        }

        public void TriggerHitReact() { Flash(Color.red); _torsoTiltTarget = Mathf.Max(_torsoTiltTarget, 18f); }
        public void TriggerReversal() => Flash(Color.cyan);
        public void TriggerDodge() => Flash(Color.white);
        public void TriggerDowned() => _torsoTiltTarget = 82f;
        public void TriggerGetUp() => _torsoTiltTarget = 40f;
        public void TriggerRopeStagger() => _torsoTiltTarget = -18f;
        public void TriggerCornered() => _torsoTiltTarget = -10f;
        public void TriggerAerialLaunch() => _torsoTiltTarget = -28f;
        public void TriggerAerialLanding(bool hit) => Flash(hit ? Color.green : Color.magenta);
        public void TriggerSpecial(string specialId) => Flash(new Color(1f, 0.6f, 0f));

        void ArmsUp()
        {
            SetLocalZ(RigSlot.UpperArmNear, 150f);
            SetLocalZ(RigSlot.UpperArmFar, -150f);
        }

        void StartSwing(RigSlot upper, RigSlot fore, float peakDeg, float outT, float inT,
            RigSlot upper2 = RigSlot.Pelvis, RigSlot fore2 = RigSlot.Pelvis)
        {
            if (_swing != null) StopCoroutine(_swing);
            _swing = StartCoroutine(SwingRoutine(upper, fore, peakDeg, outT, inT, upper2, fore2));
        }

        IEnumerator SwingRoutine(RigSlot upper, RigSlot fore, float peak, float outT, float inT, RigSlot upper2, RigSlot fore2)
        {
            float t = 0f;
            while (t < outT)
            {
                t += Time.deltaTime;
                float k = t / outT;
                SetLocalZ(upper, Mathf.Lerp(0f, peak, k));
                SetLocalZ(fore, Mathf.Lerp(0f, peak * 0.5f, k));
                if (upper2 != RigSlot.Pelvis) SetLocalZ(upper2, Mathf.Lerp(0f, -peak, k));
                yield return null;
            }
            t = 0f;
            while (t < inT)
            {
                t += Time.deltaTime;
                float k = t / inT;
                SetLocalZ(upper, Mathf.Lerp(peak, 0f, k));
                SetLocalZ(fore, Mathf.Lerp(peak * 0.5f, 0f, k));
                if (upper2 != RigSlot.Pelvis) SetLocalZ(upper2, Mathf.Lerp(-peak, 0f, k));
                yield return null;
            }
            _swing = null;
        }

        void SetLocalZ(RigSlot slot, float degrees)
        {
            var t = _rig.Joint(slot);
            if (t != null) t.localRotation = Quaternion.Euler(0f, 0f, degrees);
        }

        void Flash(Color color)
        {
            if (_flash != null) StopCoroutine(_flash);
            _flash = StartCoroutine(FlashRoutine(color));
        }

        IEnumerator FlashRoutine(Color color)
        {
            var torso = _rig.Joint(RigSlot.Torso);
            var sr = torso != null ? torso.GetComponent<SpriteRenderer>() : null;
            if (sr == null) { _flash = null; yield break; }
            Color baseColor = sr.color;
            sr.color = color;
            yield return new WaitForSeconds(0.12f);
            sr.color = baseColor;
            _flash = null;
        }
    }
}
```

- [ ] **Step 2: Verify it compiles**

In Unity, confirm a clean compile. Expected: no errors; earlier EditMode tests still PASS.

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/Animation/Sprite2DAnimationDriver.cs
git commit -m "feat: add procedural 2D sprite animation driver"
```

---

## Task 13: Arena 2D backdrop and ArenaRig visual swap

**Files:**
- Create: `Assets/Scripts/Arena/Arena2DBackdrop.cs`
- Modify: `Assets/Scripts/Arena/ArenaRig.cs` (skip building visible primitive surfaces/ropes/posts)

**Interfaces:**
- Consumes: ring half-extent (4 units) and lane Z values (`LaneSystem.LaneZ`).
- Produces: `Arena2DBackdrop.Build(float halfExtent)` that creates the layered sprite ring; an `ArenaRig` that still builds all invisible zones/anchors but no visible meshes.

Verification: presentation; confirmed in Task 16. The gameplay-zone behavior is unchanged and already covered by existing systems.

- [ ] **Step 1: Create the backdrop builder**

Create `Assets/Scripts/Arena/Arena2DBackdrop.cs`:

```csharp
using UnityEngine;

namespace LoCoFight
{
    /// Layered sprite ring for the side-on 2D view. Back ropes render behind
    /// wrestlers; front ropes render in front. Keyed to world coordinates.
    public static class Arena2DBackdrop
    {
        public static GameObject Build(float halfExtent)
        {
            var root = new GameObject("Arena2DBackdrop");

            // Mat floor: a wide low quad behind everything.
            Quad(root.transform, "Mat", new Vector3(0f, 0.25f, 0f),
                new Vector3(halfExtent * 2.4f, 1.4f, 1f), new Color(0.20f, 0.22f, 0.30f), -9000);

            // Back ropes (three stacked lines) behind wrestlers, placed at the back lane.
            float backZ = LaneSystem.LaneZ[LaneSystem.LaneCount - 1];
            for (int i = 0; i < 3; i++)
                Quad(root.transform, $"BackRope{i}", new Vector3(0f, 0.9f + i * 0.45f, backZ),
                    new Vector3(halfExtent * 2.2f, 0.05f, 1f), new Color(0.85f, 0.85f, 0.9f), -8000 - i);

            // Posts at the X extents.
            foreach (float sx in new[] { -1f, 1f })
                Quad(root.transform, "Post", new Vector3(sx * halfExtent, 1.1f, backZ),
                    new Vector3(0.18f, 2.2f, 1f), new Color(0.7f, 0.1f, 0.1f), -7900);

            // Front ropes in front of wrestlers, at the front lane.
            float frontZ = LaneSystem.LaneZ[0];
            for (int i = 0; i < 3; i++)
                Quad(root.transform, $"FrontRope{i}", new Vector3(0f, 0.9f + i * 0.45f, frontZ),
                    new Vector3(halfExtent * 2.2f, 0.05f, 1f), new Color(0.9f, 0.9f, 0.95f), 9000 + i);

            return root;
        }

        static void Quad(Transform parent, string name, Vector3 pos, Vector3 scale, Color color, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = PlaceholderSprites.Box(color, 1f, 1f, new Vector2(0.5f, 0.5f));
            sr.sortingOrder = order;
        }
    }
}
```

- [ ] **Step 2: Stop ArenaRig from building visible meshes**

In `Assets/Scripts/Arena/ArenaRig.cs`, find the `BuildPrimitiveArena` method body where it calls the visible-surface builders:

```csharp
            var ringRoot = Child(root.transform, "RingRoot");
            BuildSurfaces(rig, ringRoot);
            BuildRopesAndPosts(rig, ringRoot);
            BuildZonesAndAnchors(rig, ringRoot);
```

Replace those four lines with:

```csharp
            var ringRoot = Child(root.transform, "RingRoot");
            // 2D mode: the visible mat/ropes/posts come from Arena2DBackdrop.
            // Keep only the invisible gameplay zones and anchors here.
            BuildZonesAndAnchors(rig, ringRoot);
```

Leave `BuildSurfaces` and `BuildRopesAndPosts` defined (unused now) so nothing else breaks; they can be removed in a later cleanup.

- [ ] **Step 3: Verify it compiles**

In Unity, confirm a clean compile. Expected: no errors (an unused-method warning for `BuildSurfaces`/`BuildRopesAndPosts` is acceptable); earlier EditMode tests still PASS.

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/Arena/Arena2DBackdrop.cs Assets/Scripts/Arena/ArenaRig.cs
git commit -m "feat: layered 2D arena backdrop; keep gameplay zones only"
```

---

## Task 14: Wire the 2D presentation into WrestlerView and GameBootstrap

**Files:**
- Modify: `Assets/Scripts/Wrestlers/WrestlerView.cs` (add `Build2DRig`)
- Modify: `Assets/Scripts/Core/GameBootstrap.cs` (orthographic camera, backdrop, driver/projector wiring)

**Interfaces:**
- Consumes: `WrestlerRig.Build`, `DepthProjector`, `Sprite2DAnimationDriver`, `Arena2DBackdrop.Build`.
- Produces: a running 2D match. `WrestlerView.Build2DRig(string characterId, Color color)` builds the rig and returns its `WrestlerRig`.

The exact spawn/binding code lives in `MatchManager.SetupMatch` and the wrestler factory. This task wires the view at the point where `WrestlerView.BuildPlaceholder` is currently called and swaps the animation driver. Inspect `MatchManager` and the wrestler-spawn path to find where `BuildPlaceholder`, `PlaceholderAnimationDriver`, and `WrestlerView` are attached, and apply the swap there as well if it is not in `GameBootstrap`.

- [ ] **Step 1: Add Build2DRig to WrestlerView**

In `Assets/Scripts/Wrestlers/WrestlerView.cs`, add this method to the `WrestlerView` class (keep `BuildPlaceholder` for reference, or remove its call site in step 2):

```csharp
        public WrestlerRig rig;

        /// Builds the 2D paper-doll rig and exposes the visual root for the DepthProjector.
        public WrestlerRig Build2DRig(string characterId, Color color)
        {
            if (visualRoot != null) Destroy(visualRoot.gameObject);
            rig = WrestlerRig.Build(transform, characterId, color);
            visualRoot = rig.Root;
            return rig;
        }
```

- [ ] **Step 2: Find and update the wrestler build/bind site**

Search the spawn path for where each wrestler currently gets its placeholder visuals and driver:

```bash
grep -rn "BuildPlaceholder\|PlaceholderAnimationDriver" Assets/Scripts
```

At each spawn site (likely in `MatchManager` and/or `GameBootstrap`), replace the placeholder visual + driver setup. The pattern to install per wrestler is:

```csharp
            // 2D presentation: build the rig, attach the procedural driver and depth projector.
            var rig = view.Build2DRig(rosterId, bodyColor);

            var driver = wrestlerGo.AddComponent<Sprite2DAnimationDriver>();
            driver.Bind(rig);
            // _core.Anim must point at this driver instead of PlaceholderAnimationDriver.

            var projector = wrestlerGo.AddComponent<DepthProjector>();
            projector.Bind(wrestlerGo.transform, rig.Root);
```

Where the existing code does `core.Anim = somePlaceholderDriver`, set `core.Anim = driver` (the field/property that stores the `IAnimationDriver` on `WrestlerCore`; confirm its name in `WrestlerCore`). `rosterId` is the character id used for `Resources/Parts/<id>`; `bodyColor` is the per-wrestler color already chosen for the placeholder body.

- [ ] **Step 3: Make the camera orthographic and build the backdrop in GameBootstrap**

In `Assets/Scripts/Core/GameBootstrap.cs`, in the camera section (step 5 of `Awake`), after the `TwoTargetMatchCamera` component is ensured, force orthographic and drop the now-unneeded light. Replace:

```csharp
            if (FindObjectOfType<Light>() == null)
            {
                var lightGo = new GameObject("Directional Light");
                var light = lightGo.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.1f;
                lightGo.transform.rotation = Quaternion.Euler(55f, -30f, 0f);
            }
```

with:

```csharp
            cam.orthographic = true;
            cam.backgroundColor = new Color(0.08f, 0.09f, 0.13f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            // Unlit sprites need no scene light in 2D.
```

Then, right after the arena is built near the top of `Awake` (after `RingInteractionSystem` init), add:

```csharp
            // Visible 2D ring (gameplay zones come from ArenaRig).
            Arena2DBackdrop.Build(4f);
```

(4f matches `ArenaRig.HalfExtent`.)

- [ ] **Step 4: Verify it compiles and run the EditMode suite**

In Unity, confirm a clean compile and run:

```bash
"<UnityEditorPath>/Unity" -batchmode -runTests -projectPath . -testPlatform EditMode -testResults ./EditModeResults.xml -quit
```

Expected: PASS for all EditMode tests; no compile errors.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Wrestlers/WrestlerView.cs Assets/Scripts/Core/GameBootstrap.cs Assets/Scripts/Match/MatchManager.cs
git commit -m "feat: wire 2D rig, driver, depth projector, and backdrop into the match"
```

(Include `MatchManager.cs` in the commit only if you changed it in step 2.)

---

## Task 15: Part-sprite pipeline (manifest and per-part prompts)

**Files:**
- Create: `prompts/part-manifest.md`
- Create: `prompts/parts/_template.md`
- Create: `prompts/parts/zeak-gallent/<slot>.md` (16 files)
- Create: `prompts/parts/jt-staten/<slot>.md` (16 files)

**Interfaces:**
- Consumes: the existing `prompts/style-guide.md`, `prompts/zeak-gallent.md`, and `prompts/jt-staten.md` for per-character look.
- Produces: the art-generation contract the user follows to produce `Assets/Resources/Parts/<id>/<slot>.png`, matching `WrestlerRig.SlotFile` names.

No em-dashes in any of these files (global constraint).

- [ ] **Step 1: Write the manifest**

Create `prompts/part-manifest.md`:

```markdown
# Part Manifest

Each wrestler is a paper-doll rig of separate sprite parts. Generate one
transparent PNG per slot, authored facing screen-right, at roughly 100 pixels
per world unit. Set each sprite's pivot in the Unity Sprite Editor to the joint
listed below so the parts assemble correctly. Save files to
`Assets/Resources/Parts/<character-id>/<slot>.png` (character-id is the portrait
slug without the `tas-` prefix, for example `zeak-gallent`).

| Slot file | Joint pivot | Target size (px) | Notes |
|---|---|---|---|
| `pelvis` | center | 42 x 22 | Hips, waistband. |
| `torso` | bottom-center | 50 x 55 | Chest and abdomen, neck stub at top. |
| `head` | bottom-center (neck) | 34 x 34 | Face, neutral expression, no headpiece. |
| `headpiece` | bottom-center | 40 x 30 | Hood, mask, or hat. Empty/transparent if none. |
| `upper-arm-near` | top-center (shoulder) | 17 x 35 | Camera-side upper arm. |
| `forearm-near` | top-center (elbow) | 15 x 31 | Camera-side forearm. |
| `hand-near` | top-center (wrist) | 17 x 17 | Camera-side hand, relaxed fist. |
| `upper-arm-far` | top-center (shoulder) | 16 x 34 | Far-side upper arm, slightly darker. |
| `forearm-far` | top-center (elbow) | 14 x 30 | Far-side forearm. |
| `hand-far` | top-center (wrist) | 16 x 16 | Far-side hand. |
| `thigh-near` | top-center (hip) | 19 x 35 | Camera-side thigh. |
| `shin-near` | top-center (knee) | 17 x 33 | Camera-side shin. |
| `foot-near` | top-back (ankle) | 23 x 13 | Camera-side boot, toe pointing right. |
| `thigh-far` | top-center (hip) | 18 x 34 | Far-side thigh, slightly darker. |
| `shin-far` | top-center (knee) | 16 x 32 | Far-side shin. |
| `foot-far` | top-back (ankle) | 22 x 12 | Far-side boot. |

Joint guidance: a "top-center" pivot means the joint where this part attaches
to its parent sits at the top-middle of the image. Leave a few transparent
pixels of overlap margin around each joint so parts meet cleanly when rotated.
Far-side limbs read about 15 percent darker than near-side to suggest depth.
```

- [ ] **Step 2: Write the per-part prompt template**

Create `prompts/parts/_template.md`:

```markdown
# Part Prompt Template

Use this with `../../style-guide.md` and the character's full-body prompt
(`../../<character>.md`) for color and costume detail. Fill the bracketed
fields per slot using the manifest. Generate one part per request.

Prompt:

Create a single isolated wrestling-character body part in the shared LoCo Pro
2D cartoon style: confident black linework, flat colors, restrained cel
shading. Part: [SLOT DESCRIPTION]. Character: [CHARACTER NAME]. Match this
character's colors and costume exactly: [KEY COLORS AND COSTUME NOTES FROM THE
CHARACTER PROMPT]. Orientation: authored as if the wrestler faces to the right,
[ORIENTATION NOTE FOR SLOT]. Transparent background, no other body parts, no
scenery, no border, no caption. Leave a few transparent pixels of margin around
the [JOINT NAME] joint. Output a vertical PNG sized about [TARGET SIZE] pixels.
```

- [ ] **Step 3: Generate the 16 Zeak slot files**

For each slot in the manifest, create `prompts/parts/zeak-gallent/<slot>.md` from the template, filling the brackets from `prompts/zeak-gallent.md` (black hood, sleeveless black vest, bare chest/abdomen, black fingerless gloves with red wrist wraps, black-and-red tights with red panels, black knee pads, tall black boots). Example, `prompts/parts/zeak-gallent/torso.md`:

```markdown
# Zeak Gallent: torso

Create a single isolated wrestling-character body part in the shared LoCo Pro
2D cartoon style: confident black linework, flat colors, restrained cel
shading. Part: bare muscular chest and abdomen with an open sleeveless black
vest, neck stub at the top. Character: Zeak Gallent. Match this character's
colors and costume exactly: open sleeveless black vest over a bare fair-skinned
torso. Orientation: authored as if the wrestler faces to the right, chest
oriented forward-right. Transparent background, no other body parts, no
scenery, no border, no caption. Leave a few transparent pixels of margin around
the waist and neck joints. Output a vertical PNG sized about 50 x 55 pixels.
```

Create the remaining 15 Zeak files the same way, one per manifest slot, each
describing that slot for Zeak with the orientation note from the manifest
(`head`: bearded face under no hood, neutral; `headpiece`: black hood;
`hand-near`/`hand-far`: black fingerless glove with red wrist wrap; `thigh-*`/
`shin-*`: black-and-red tights with red panels; `foot-*`: tall black boot;
far-side limbs about 15 percent darker).

- [ ] **Step 4: Generate the 16 JT slot files**

Repeat step 3 for `prompts/parts/jt-staten/<slot>.md`, filling brackets from
`prompts/jt-staten.md`. Create all 16 files, one per manifest slot.

- [ ] **Step 5: Commit**

```bash
git add prompts/part-manifest.md prompts/parts
git commit -m "docs: add part manifest and per-part prompts for Zeak and JT"
```

---

## Task 16: 2D QA checklist and manual verification pass

**Files:**
- Modify: `Documentation/TestingChecklist.md` (append a 2D section)

**Interfaces:**
- Consumes: the full build from Tasks 1 to 14.
- Produces: a recorded manual QA pass confirming the slice.

This is the in-editor verification the earlier tasks deferred. It requires pressing Play in Unity 6.

- [ ] **Step 1: Append the 2D checklist**

Add to the end of `Documentation/TestingChecklist.md`:

```markdown
## 2D redesign (vertical slice)

Run the default match (Zeak vs JT) by pressing Play.

- [ ] Camera renders side-on and orthographic; both wrestlers stay framed.
- [ ] Camera zooms out as they separate, in as they close.
- [ ] A/D move horizontally; W/S step between the three depth lanes.
- [ ] When not pressing W/S, the wrestler settles onto a lane.
- [ ] A wrestler in the back lane draws higher and smaller; front lane lower and larger.
- [ ] The front wrestler overlaps the back wrestler correctly.
- [ ] Each wrestler casts a ground shadow.
- [ ] Strikes and grapples only connect when both are on the same lane.
- [ ] The CPU steps onto the player's lane before attacking.
- [ ] Rig limbs swing while walking and on strikes/grapples; facing flips with direction.
- [ ] State poses read: stunned, rope stagger, cornered, downed, pin, submission, victory, defeat.
- [ ] Front and back ropes frame the wrestlers (back behind, front in front).
- [ ] A full match completes: strike, grapple, reversal, rope interaction, pin or submission, win.
- [ ] If real parts exist under Assets/Resources/Parts/<id>/, they appear in place of placeholders.
```

- [ ] **Step 2: Run the manual pass**

Open `Assets/Scenes/PrototypeMatch.unity` (or run the bootstrap scene), press Play, and work through every checkbox above. Fix any item that fails (tuning fields: `DepthProjector.yFactor/scalePerUnit`, `TwoTargetMatchCamera.baseSize/viewSide`, `LaneSystem` constants, `Sprite2DAnimationDriver` swing amounts). If `viewSide` makes the scene mirrored, flip it once on the camera component.

- [ ] **Step 3: Commit**

```bash
git add Documentation/TestingChecklist.md
git commit -m "docs: add 2D vertical-slice QA checklist"
```

---

## Self-Review (completed by plan author)

- **Spec coverage:** Camera (Tasks 5, 6); depth faking (Tasks 3, 7); lanes and movement (Tasks 2, 8); lane-alignment gate (Tasks 2, 9); AI lane nudge (Task 10); paper-doll rig and slots (Task 11); WrestlerView rework (Tasks 11, 14); part pipeline and prompts (Task 15); animation driver and full IAnimationDriver coverage (Task 12); arena visuals (Task 13); bootstrap wiring (Task 14); engine target and packages (Task 1); verification approach and QA (Tasks 1 through 16, Task 16); out-of-scope respected (only Zeak and JT). All spec sections map to a task.
- **Placeholder scan:** no TBD/TODO/"implement later"; the one cross-task dependency (DepthProjector needs PlaceholderSprites) is called out with an explicit ordering note, not left vague.
- **Type consistency:** `LaneSystem`, `DepthProjection`, `FacingUtil`, `CameraFraming` signatures used in Tasks 6 to 10 match their Task 2 to 5 definitions; `RigSlot`, `WrestlerRig.Build/Joint`, `PlaceholderSprites.Box/Ellipse`, and `Sprite2DAnimationDriver.Bind` are consistent across Tasks 11, 12, 14; slot file names in Task 15 match `WrestlerRig.SlotFile`.
- **Known editor-dependent seams to confirm during execution:** the exact `WrestlerCore` animation-driver field name and the exact wrestler spawn/bind site (Task 14 step 2) are located by grep at execution time, since they live in files not fully reproduced here.
