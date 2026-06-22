# Fight Game 2D Redesign (Design Spec)

- Date: 2026-06-21
- Status: Approved (design), pending implementation plan
- Scope: First spec of a multi-spec effort. Covers the foundational 3D-to-2D
  conversion plus a playable vertical slice. Follow-on specs cover the rest of
  the roster, special/trait visuals, menus, and audio.

## 1. Goal

Convert the existing `fightgame/` Unity 3D wrestling prototype into a 2D
sprite-based wrestling/fighting game with a "false 2D" playing field, reusing
the same characters, art direction, and gameplay design. The entire data-driven
gameplay model (16-wrestler roster, moves, specials, passive traits, AI, ropes,
pins, submissions) is preserved. The work concentrates on presentation,
movement framing, and a new skeletal sprite-rig pipeline.

This spec delivers a vertical slice: a complete match (Zeak Gallent vs JT
Staten) rendered in 2D, proving the rendering port, the side-on lane movement,
and the part-sprite plus skeletal-rig pipeline end to end.

## 2. Current state (context)

`fightgame/` is a Unity 2022.3 LTS prototype written in C#. Key facts that shape
this design:

- Gameplay is cleanly separated from presentation. `IAnimationDriver` is the
  only animation seam combat/state/AI code calls, and `WrestlerView` is a fully
  swappable body that exposes named part transforms (head, torso, arms, legs).
  Gameplay never touches meshes.
- Movement and ring math run in flat world space: X for ring length, Z for
  depth, Y for height, via `CharacterController` with Y gravity. Combat ranges,
  rope sides, AI positioning, and specials are all geometric and operate in
  world space (see `WrestlerMotor`, `RingInteractionSystem`).
- The only thing that makes the prototype "3D" to the viewer is the camera: a
  perspective broadcast camera (`TwoTargetMatchCamera`) with a diagonal view
  angle.
- `GameBootstrap` builds the whole scene procedurally (arena, systems,
  wrestlers, camera, light, HUD). `ArenaRig.BuildPrimitiveArena` builds an 8x8
  mat with ropes, posts, and the invisible gameplay zones/anchors. Spawns are at
  X = -2 (player) and X = +2 (CPU) on the mid lane (Z = 0).
- The reusable art assets are single static full-body character portraits (one
  neutral pose each, bold-outlined flat cartoon style, transparent background),
  plus logos and per-character generation prompts under `prompts/` with a shared
  `style-guide.md`. There are no animation sprite sheets.

## 3. Decisions (locked during brainstorming)

| Topic | Decision |
|---|---|
| Platform | Stay in Unity, switch rendering to its 2D system. Reuse the C# data model. |
| Playing field | Side-on view with 3 discrete depth lanes (front rope, mid, back rope). |
| Animation | Skeletal rig assembled from per-part sprites generated through the prompt pipeline, then animated procedurally in code. |
| Art handoff | I author the per-part prompts and manifest; the user generates the part art; I then wire the rig against the real sprites. |
| Scope | Vertical slice first: Zeak vs JT, full match loop in 2D. Rest of roster and all special/trait visuals are follow-on specs. |
| Architecture | Approach A, "view-flattening": keep gameplay in 3D world space, change only presentation. |

## 4. Architecture: view-flattening (Approach A)

All gameplay stays in the existing 3D world space. The `CharacterController`,
ring math, combat ranges, AI positioning, pins, submissions, and specials run
unchanged. The conversion is a presentation swap plus a depth-faking projection.
This preserves every working, tested system and isolates new engineering to the
camera, the depth projection, the lane input, the sprite rig, and the animation
driver.

### 4.1 Coordinate model and camera

- World axes keep their current meaning: X is ring length (screen horizontal),
  Z is depth (into the screen, the lanes), Y is height (jumps and aerials).
- `TwoTargetMatchCamera` is reworked from perspective to orthographic with a
  straight-on front view. Screen-X maps to world-X and screen-Y maps to world-Y.
  It tracks the horizontal midpoint of the two wrestlers and zooms by adjusting
  orthographic size as their horizontal separation changes (replacing the
  perspective `baseDistance` and `viewDirection` logic). Aerials nudge the
  framing up and zoom out slightly, reusing the existing `IsAerial` check.

### 4.2 Depth faked with a DepthProjector

Because the camera looks straight along Z, real depth is invisible, so depth is
stylized (the classic brawler technique). A new `DepthProjector` component reads
each wrestler's world-Z and applies, to the visual root only (not the gameplay
transform):

- A vertical screen offset: deeper (back lane) draws higher on screen.
- A scale factor: deeper draws slightly smaller.
- A sprite sorting order derived from Z, so the front wrestler overlaps the back
  one correctly.
- A simple ground shadow at the wrestler's true ground point for readability.

Combat still happens in honest 3D world space; only the look is flattened.

### 4.3 Depth lanes and movement

- Three lanes at fixed Z values: front rope, mid, back rope.
- Horizontal movement stays free and continuous.
- Player depth input snaps to the nearest lane and steps one lane at a time.
  World-Z stays continuous under the hood so knockback, rope rebound, and
  scripted special repositioning still slide smoothly; only player-driven depth
  quantizes.
- Strikes and grapples require lane alignment (the attacker and defender within
  a small Z tolerance) so hits read correctly in side view. Stepping to another
  lane is a positioning and whiff-avoidance tool.
- The CPU AI already closes XZ distance toward the opponent, so it naturally
  lines up. A light "match the opponent's lane" nudge is added to its approach
  so the human is not whiffing against a perpetually off-lane CPU.

## 5. Skeletal rig (paper-doll)

Each wrestler is a transform hierarchy of `SpriteRenderer` joints. No mesh
skinning or bone-weight painting is used, which suits flat cartoon parts and is
fully buildable without the Unity skinning GUI.

### 5.1 Slot manifest

The rig is authored facing screen-right. Facing flips via the visual root's
X-scale. Near-side limbs sort in front of the torso; far-side limbs sort behind.

| Slot | Count | Pivot (joint) | Parent |
|---|---|---|---|
| `pelvis` | 1 | center (rig root anchor) | visual root |
| `torso` | 1 | bottom-center (waist) | pelvis |
| `head` | 1 | bottom-center (neck) | torso |
| `headpiece` | 1 | matches head | head |
| `upper-arm` (near, far) | 2 | top-center (shoulder) | torso |
| `forearm` (near, far) | 2 | top-center (elbow) | matching upper-arm |
| `hand` (near, far) | 2 | top-center (wrist) | matching forearm |
| `thigh` (near, far) | 2 | top-center (hip) | pelvis |
| `shin` (near, far) | 2 | top-center (knee) | matching thigh |
| `foot` (near, far) | 2 | top-back (ankle) | matching shin |

Hierarchy summary:

```
visualRoot
  pelvis (root)
    torso
      head
        headpiece
      upperArmFar  -> forearmFar  -> handFar   (sorted behind torso)
      upperArmNear -> forearmNear -> handNear  (sorted in front of torso)
    thighFar  -> shinFar  -> footFar           (sorted behind)
    thighNear -> shinNear -> footNear          (sorted in front)
```

Per-part sorting offsets plus the `DepthProjector` base order produce correct
near/far overlap within a wrestler and correct overlap between wrestlers.

### 5.2 WrestlerView rework

`BuildPlaceholder` (primitive meshes) is replaced by `Build2DRig`, which
instantiates a per-character rig prefab and exposes the joint transforms used by
the animation driver. Gameplay still never touches the rig.

## 6. Part-sprite pipeline and prompts (handoff boundary)

Extend the existing `prompts/` system:

- `prompts/part-manifest.md`: defines every slot's name, joint/pivot location,
  target pixel size, and a small overlap margin at joints. This is the contract
  the generated art conforms to and the rig assembles against.
- Per-part generation prompts for Zeak Gallent and JT Staten, consistent with
  `style-guide.md`: each prompt requests an isolated single body part,
  transparent background, side-facing/neutral orientation, and colors and line
  weight matching that character's existing portrait.
- Output naming convention, for example `parts/zeak-gallent/upper-arm-near.png`.
- Placeholder parts (simple, correctly pivoted shapes) so the rig and all
  animation work end to end before real art arrives.

Handoff flow: deliver the manifest, prompts, and a working placeholder rig; the
user generates the Zeak and JT parts from the prompts; then wire the real
sprites into the named slots with no gameplay or animation-logic changes.

## 7. Animation driver

`Sprite2DAnimationDriver` is a new class implementing the existing
`IAnimationDriver` interface, so combat, state, and AI code stay untouched. It
animates the rig procedurally in code by rotating joint transforms over time.
This is the natural evolution of the existing `PlaceholderAnimationDriver`
(which already poses transforms in code), is fully art-agnostic, and is tunable
without the Unity animation GUI. Baking to hand-keyed clips is a possible future
step, not part of this slice.

Call coverage (the full existing interface):

| Interface call | Behavior |
|---|---|
| `SetMovementSpeed(speed)` | Blend idle, walk, and run leg/arm cycles by speed. |
| `PlayMove(state, "strike")` | Near-arm strike swing with torso follow-through. |
| `PlayMove(state, "grapple")` | Both arms forward into a tie-up reach. |
| `PlayMove(state, "special")` | Special wind-up plus accent flash. |
| `PlayState(name)` | Pose for Idle, Moving, Running, Stunned, RopeStaggered, Cornered, Downed, Pinned, RollingAway, GettingUp, TurnbuckleClimb, AerialSetup, Pinning, SubmissionApplying, SubmissionDefending, RopeTrapLocked, Victory, Defeat. |
| `TriggerHitReact` | Recoil and flinch. |
| `TriggerReversal` | Counter-motion accent. |
| `TriggerDodge` | Quick sidestep lean. |
| `TriggerDowned` / `TriggerGetUp` | Fall to mat / rise. |
| `TriggerRopeStagger` / `TriggerCornered` | Lean into ropes / corner. |
| `TriggerAerialLaunch` / `TriggerAerialLanding(hit)` | Launch tuck / land or whiff. |
| `TriggerSpecial(id)` | Special accent. |

## 8. Arena visuals

`ArenaRig` keeps building all invisible gameplay geometry (boundary, rope,
corner, trap, rebound, and aerial zones and anchors) exactly as-is, since that
is what ring math reads. Only the visible primitives (mat, ropes, posts) are
replaced by a layered sprite ring keyed to the same world coordinates:

- Back ropes and posts on a sorting layer behind all wrestlers (back-rope lane).
- Mat and apron floor.
- Wrestlers sorted by depth (section 4.2).
- Front ropes on a layer in front of wrestlers (front-rope lane), so a wrestler
  in the front lane is framed by the near ropes.
- Side posts and ropes at the X extents.

Sprites use an unlit material, so the directional light is no longer required.

## 9. Bootstrap wiring

`GameBootstrap` changes are small: set the camera to orthographic, build the
sprite ring instead of the primitive surfaces, and attach `DepthProjector` and
`Sprite2DAnimationDriver` instead of `PlaceholderAnimationDriver`. Match setup,
data loading, input, HUD, and match flow are unchanged.

## 10. File-level change map

Exact insertion points are finalized in the implementation plan. New and
modified files are expected to be:

New:
- `Assets/Scripts/Animation/Sprite2DAnimationDriver.cs`
- `Assets/Scripts/View/DepthProjector.cs`
- `Assets/Scripts/Arena/LaneSystem.cs` (lane definitions and snapping math)
- `Assets/Scripts/Arena/Arena2DBackdrop.cs` (layered sprite ring builder)
- Rig prefab assets for Zeak and JT plus placeholder part sprites
- `prompts/part-manifest.md`
- `prompts/parts/zeak-gallent/*.md`, `prompts/parts/jt-staten/*.md`
- EditMode test assembly: `Assets/Tests/EditMode/*` (lane snapping, depth
  projection, facing flip, lane-alignment gate) plus an asmdef

Modified:
- `Assets/Scripts/Wrestlers/WrestlerView.cs` (Build2DRig)
- `Assets/Scripts/Camera/TwoTargetMatchCamera.cs` (orthographic framing)
- `Assets/Scripts/Input/PlayerInputController.cs` (lane-snapped depth input)
- `Assets/Scripts/AI/CPUWrestlerAI.cs` (lane-alignment nudge)
- The strike/grapple hit-validation path (`HitboxProbe` and/or
  `CombatResolver`) for the lane-alignment gate
- `Assets/Scripts/Core/GameBootstrap.cs` (orthographic camera, sprite backdrop,
  driver swap)
- `Assets/Scripts/Arena/ArenaRig.cs` (skip visible primitive surfaces when 2D)
- `Documentation/TestingChecklist.md` (2D QA items)

## 11. Verification and testing

I cannot open the Unity editor in this environment to visually confirm
rendering. "Verified" therefore means three concrete things:

- Headless EditMode unit tests (Unity Test Framework) for the new pure logic
  that does not need the GUI: lane snapping math, depth-to-screen projection,
  facing flip, and the lane-alignment gate.
- Compile-correctness of all new and changed C# against the existing project
  structure.
- A manual QA checklist (extending `Documentation/TestingChecklist.md`) the user
  runs in Unity by pressing Play: the match loop still completes (strike to
  grapple to reversal to pin or submission to win), lanes snap, strikes only
  land when aligned, depth overlap and scale read correctly, the camera frames
  and zooms, the rig poses through all states, facing flips, and ropes and
  aerials read in 2D.

The implementation plan will mark, per step, what I can verify directly versus
what requires the user's in-editor pass.

## 12. Success criteria (vertical slice)

- A full Zeak vs JT match plays start to finish rendered in 2D: neutral, strike,
  grapple, reversal, rope interaction, pin and submission, and a win.
- Movement is side-on with three working depth lanes; depth reads through
  offset, scale, and overlap.
- Both wrestlers use the paper-doll skeletal rig driven by
  `Sprite2DAnimationDriver`, with placeholder parts first and real generated
  parts wired in after the art handoff.
- The arena renders as a layered 2D ring with correct front/back rope framing.
- New pure-logic unit tests pass headlessly; the manual QA checklist passes in
  the editor.

## 13. Risks and mitigations

- Pivot mismatch between generated art and the manifest. Mitigation: the
  manifest is the contract, placeholder parts share the same pivots, and an
  in-editor calibration pass is included in the handoff step.
- Procedural animation reading as stiff. Acceptable for the slice; it is tunable
  and can be baked to keyed clips later.
- Lane-alignment making combat feel finicky. Mitigation: tune the Z tolerance
  and add a light auto-step assist toward the opponent's lane.
- Inability to visually verify in this environment. Mitigation: headless tests
  plus the manual in-editor checklist, with responsibilities marked per step.
- Sorting or overlap glitches when wrestlers cross lanes. Mitigation:
  deterministic sort by Z, then by role.

## 14. Out of scope (follow-on specs)

- Rigs and part art for the other 14 wrestlers.
- Re-presenting every special and passive trait's bespoke visuals in 2D.
- Menus and character-select screen art.
- Audio and final-pass polish.
