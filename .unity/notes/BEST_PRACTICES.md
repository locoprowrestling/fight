# Fightgame Best Practices

## Scope and Architecture

- Read the existing system before adding a new manager or abstraction.
- Keep gameplay rules separate from presentation.
- Keep `RingInteractionSystem` authoritative for rope geometry and behavior.
- Keep `IAnimationDriver` as the boundary between combat state and visuals.
- Human and CPU control should use the same combat APIs.
- Store authored configuration in ScriptableObjects and mutable match state in
  runtime objects.
- Keep `DefaultGameData` behavior aligned with generated assets from
  `PrototypeAssetBuilder`.
- Preserve narrow ownership boundaries when implementing wrestler-specific
  specials and traits.

## UnityMCP

- Start by reading `mcpforunity://custom-tools`, instances, and editor state.
- Pin the correct Unity instance before mutation when multiple editors exist.
- Read resources before calling mutation tools.
- Activate optional tool groups only for the task that needs them.
- For Unity API changes, use live reflection before examples or memory.
- Use pagination and summary-first queries to bound payload size.
- Use instance IDs when names are ambiguous.
- Use `batch_execute` for independent repeated operations and `fail_fast` for
  dependent operations.
- Save scenes explicitly and verify the result after editor mutations.

## Scripts

- Inspect surrounding code and call sites before editing.
- Prefer focused edits over whole-file replacement.
- Preserve user changes in a dirty worktree.
- After script edits, wait for Unity compilation and domain reload.
- Check the Unity console before attaching or using newly compiled types.
- Do not call `refresh_unity` when the script tool already imported the asset
  and requested compilation.
- Use current Unity reflection and official docs for uncertain APIs.
- Avoid arbitrary editor C# execution when a reviewable repository change works.

## Scenes and Assets

- Read hierarchy and component metadata before requesting full property payloads.
- Search assets with previews disabled unless visual inspection is needed.
- Check prefab stage before editing prefab contents.
- Use prefabs for reusable scene structures.
- Include a camera and primary light in newly created scenes.
- Keep paths relative to `Assets/` in UnityMCP payloads unless a tool explicitly
  requires another form.
- Do not silently overwrite hand-authored scenes or assets.

## Combat and Physics

- Preserve buffered input semantics for reversals, grapples, and combos.
- In controllers, evaluate role-specific follow-ups (e.g. grapple-lock
  attacker) before generic capability gates like `!Profile.canAttack`; the
  flags describe the state, not the role.
- Give every mutual locking state three things: a profile timeout, an owner
  that resolves it, and cleanup if it dissolves externally.
- Keep frame-rate-independent timers on `Time.deltaTime`.
- Use `FixedUpdate` only for Rigidbody physics operations.
- Do not mix Transform-driven movement with a dynamic Rigidbody.
- Test rope rebound, rope stagger, cornering, rope breaks, aerial anchors, and
  referee behavior after collider or trigger changes.
- Keep gameplay outcomes deterministic rather than relying on animation or
  ragdoll simulation.

## Animation and UI

- Decide whether `WrestlerMotor` or root motion owns displacement per state.
- New placeholder poses are `BodyPose` cases in
  `PlaceholderAnimationDriver.ComputePose`; one-shot gestures are `ActionKind`
  overlays. Joint sign conventions:
  `Documentation/KnowledgeBase/Examples.md`.
- Apply weight-class bulk per mesh and scale the visual root uniformly only;
  non-uniform parent scale shears rotated child joints.
- Keep visual parts collider-free; the root `CharacterController` (sized from
  `WrestlerView.RigHeight`/`HeightFor`/`BulkFor`) is the only physics volume.
- Keep Animator parameters downstream of authoritative gameplay state.
- Use animation events sparingly and keep critical rules testable without clips.
- The current match HUD is uGUI; UnityMCP `manage_ui` targets UI Toolkit.
- Use anchors and `CanvasScaler` for resolution-independent HUD layout.
- Prefer event-driven HUD updates over rebuilding or allocating every frame.

## Verification

- Run the narrowest relevant check first, then broaden.
- After multi-file script edits, run the offline Roslyn compile check before
  returning to the editor (command in
  `Documentation/KnowledgeBase/Examples.md`); never batch-compile while the
  editor is open.
- Check console errors after significant Unity operations.
- Use screenshots for visual claims, not as proof of gameplay correctness.
- Pair visual verification with state inspection, tests, or console evidence.
- Use EditMode tests for pure formulas and data validation.
- Use PlayMode tests for bootstrap and lifecycle behavior.
- Keep manual testing for game feel, timing, camera, and full special sequences.
- Profile representative builds before optimizing.

## Documentation

- Keep tracked `Documentation/` files accurate when behavior or setup changes.
- Record reusable development findings in this notes folder; promote
  contributor-relevant practices, templates, and postmortems into the tracked
  `Documentation/KnowledgeBase/` in the same change.
- Include exact paths, versions, commands, and evidence where they matter.
- Distinguish confirmed current behavior from plans and older examples.
- Append decisions and learnings; do not erase their history.
