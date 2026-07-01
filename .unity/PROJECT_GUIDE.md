# Project-Specific Unity Guide

## Architecture

Keep gameplay state and presentation separate. The repository already follows
this well:

- ScriptableObjects hold authored configuration.
- `WrestlerCore` and system components own runtime state.
- `WrestlerView` and `IAnimationDriver` own presentation.
- Human and CPU control both drive the same combat API.
- `RingInteractionSystem` is the authority for rope geometry and interactions.

Prefer extending these existing boundaries over adding unrelated MonoBehaviour
managers or putting game rules into animation callbacks.

## Unity Lifecycle

Use `Awake` for local component initialization, `OnEnable`/`OnDisable` for
event subscriptions, and `Start` for dependencies that require other objects to
have completed `Awake`. Keep per-frame allocation out of `Update`.

Use `Time.deltaTime` for frame-rate-independent timers and movement. Use
`FixedUpdate` only for Rigidbody-driven physics operations. This project uses
substantial custom movement and interaction math, so do not migrate behavior to
physics impulses without checking combat timing, rope behavior, and AI.

## ScriptableObject Data

The project uses ScriptableObjects for wrestler definitions, stats, moves,
specials, traits, rules, roster data, and AI difficulty.

- Treat asset ScriptableObjects as immutable configuration during a match.
- Put mutable health, stamina, momentum, cooldowns, and status effects in
  runtime objects.
- Keep the in-memory defaults in `DefaultGameData` aligned with editor-generated
  assets from `PrototypeAssetBuilder`.
- Remember that `Resources.Load` paths omit both `Assets/Resources/` and the
  file extension.
- Avoid adding more `Resources` usage casually. It is acceptable for this small
  prototype, but direct references or Addressables scale better later.

## Input

The current project uses a custom `PlayerInputController` and `InputBuffer`.
Preserve buffered action semantics for reversals, combos, and grapples.

The modern Input System is worth adopting only as a deliberate migration. Do
not mix a partial Input System conversion into unrelated gameplay work.

## Physics, Collision, and Ropes

Unity Learn physics material is relevant for collider setup, trigger callbacks,
layers, collision matrices, and Rigidbody behavior. The ring is not a generic
physics arena, however:

- `RingInteractionSystem` owns rope math.
- `RingBoundary` owns legal movement bounds.
- Typed rope/corner/aerial zones feed combat and special requirements.
- Trigger/collider changes must be tested for running rebounds, rope stagger,
  cornering, rope breaks, aerial anchors, and referee counts.

Avoid using `Transform` movement on a non-kinematic Rigidbody. If Rigidbody
movement is introduced, choose interpolation, collision detection, and
FixedUpdate behavior explicitly.

## Animation

`IAnimationDriver` is the replacement boundary for future authored animation.
Keep gameplay outcomes deterministic and independent of visual clips.

When real animation is added:

- Use Animator parameters to express gameplay state.
- Use animation events only for presentation or tightly controlled hit-frame
  signals; combat remains authoritative.
- Use root motion only after deciding whether animation or `WrestlerMotor` owns
  displacement for each state.
- Test interruption, reversal, dodge, rope, pin, and submission transitions.
- Ragdolls should be a presentation state with an explicit recovery path, not
  the source of match rules.

## AI and Navigation

The game currently uses custom combat-aware AI, which is appropriate inside a
small ring. NavMesh concepts may help with future backstage or arena traversal,
but a NavMeshAgent should not replace ring positioning logic without preserving:

- rope and corner herding;
- special setup positions;
- facing and strike ranges;
- action reaction delays;
- grapple and reversal windows.

## Camera

The two-target camera should keep both wrestlers framed, preserve readable ring
orientation, and avoid rapid zoom changes. Cinemachine concepts can guide
damping and target groups, but adding the package is an architectural choice,
not a prerequisite for tuning the existing camera.

## UI

The HUD is built at runtime with uGUI. Continue using `CanvasScaler` with
`Scale With Screen Size`, anchors, and layout-aware sizing. Avoid rebuilding
the hierarchy or allocating strings every frame where event-driven updates work.

UI Toolkit is appropriate for future editor tools and possibly menus, but there
is no need to rewrite the in-match HUD during gameplay work.

## Editor Tooling

Editor scripts belong under an `Editor` folder and may use `UnityEditor`.
The setup commands should remain repeatable:

- prompt before replacing or discarding modified scenes;
- generate deterministic paths and asset names;
- use `Undo` where tools modify user-authored scene content;
- call `AssetDatabase` APIs only from editor code;
- avoid silently overwriting hand-edited assets.

## Testing

The Unity Test Framework package is installed, but the project has no test
assemblies. Highest-value first tests:

1. EditMode tests for pure formulas: grapple resolution inputs, stamina costs,
   kickout/submission calculations, lift validation, timers, and math helpers.
2. EditMode tests for ScriptableObject validation and default data completeness.
3. PlayMode tests for bootstrap, state transitions, pin/submission cleanup, and
   match reset.
4. Manual tests for timing feel, camera, visual feedback, and full specials.

Create asmdefs before adding Unity tests so editor-only and runtime dependencies
are explicit.

## Performance

Profile before optimizing. Likely hotspots are per-frame AI decisions, repeated
component lookups, physics overlap queries, runtime UI updates, and allocations
from debug text.

Use the CPU Profiler, Timeline view, GC allocation column, and Physics Profiler.
Validate in a development build as well as the Editor because Editor overhead
can distort results.

## Version Caveat

Unity Learn contains lessons authored for older long-term-support releases.
Their concepts are often valid, but menus, package versions, render pipelines,
and APIs may differ in Unity `6000.4.10f1`. Verify implementation details
against current Unity 6 manuals and package documentation.
