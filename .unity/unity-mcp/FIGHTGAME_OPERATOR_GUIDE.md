# Fightgame UnityMCP Operator Guide

## Required Preflight

For every UnityMCP task:

1. Read `mcpforunity://custom-tools` first.
2. Read `mcpforunity://instances`.
3. If multiple editors are connected, set
   `fightgame@fc0efb860bfd225d` as the active instance.
4. Read `mcpforunity://editor/state`.
5. Stop and poll if Unity is compiling, reloading the domain, entering play
   mode, or otherwise not ready for tools.
6. Read the project or scene resource relevant to the requested change.

The instance hash can change after reconnection. Match the project name and
project root rather than permanently trusting the hash above.

## Resource-First Rule

Resources are for state and documentation; tools mutate or execute.

Read before acting:

- Project identity: `mcpforunity://project/info`
- Editor readiness: `mcpforunity://editor/state`
- Instances: `mcpforunity://instances`
- Available groups: `mcpforunity://tool-groups`
- Project extensions: `mcpforunity://custom-tools`
- Tags and layers: `mcpforunity://project/tags`,
  `mcpforunity://project/layers`
- Tests: `mcpforunity://tests`
- Menus: `mcpforunity://menu-items`
- GameObject resource API: `mcpforunity://scene/gameobject-api`
- Prefab resource API: `mcpforunity://prefab-api`
- Cameras: `mcpforunity://scene/cameras`
- Selection and prefab stage:
  `mcpforunity://editor/selection`,
  `mcpforunity://editor/prefab-stage`

## Tool Groups

Only `core` is enabled by default. Activate optional groups for the current
session and deactivate them when no longer needed.

| Group | Purpose |
|---|---|
| `core` | Scenes, GameObjects, components, scripts, assets, prefabs, editor, packages, physics, cameras, graphics, materials, builds |
| `docs` | Live reflection and official Unity documentation lookup |
| `testing` | Unity Test Framework execution and async jobs |
| `animation` | Animator and AnimationClip operations |
| `profiling` | Profiler sessions, counters, snapshots, Frame Debugger |
| `scripting_ext` | Arbitrary C# execution and ScriptableObject management |
| `ui` | UI Toolkit UXML, USS, UIDocument workflows |
| `vfx` | Shaders, textures, particles, trails, VFX Graph |
| `probuilder` | ProBuilder shape and mesh editing |

For Unity API work, activate `docs` and use this trust order:

1. `unity_reflect` against the connected editor;
2. actual project assets and package state;
3. `unity_docs` for official manuals and ScriptReference;
4. upstream examples and templates.

## Project-Specific Boundaries

- Keep combat rules in the existing runtime systems, not scene objects or
  Animator transitions.
- Preserve `RingInteractionSystem` as rope-math authority.
- Preserve `IAnimationDriver` as the presentation boundary.
- Keep generated data aligned between `DefaultGameData` and
  `PrototypeAssetBuilder`.
- The HUD is uGUI. `manage_ui` is specifically for UI Toolkit; use GameObject
  and component tools for the existing Canvas hierarchy.
- AI Navigation is installed, but the ring AI uses custom combat positioning.
- Do not add ProBuilder solely because its tool group exists.
- Do not run `execute_code` when a normal, reviewable script edit is adequate.

## Script Workflow

1. Inspect the target file and surrounding architecture.
2. Prefer targeted text/script edits over replacing whole files.
3. Validate script syntax when supported.
4. Let script creation/editing trigger import and compilation.
5. Do not call `refresh_unity` redundantly after tools that already request
   compilation.
6. Poll `mcpforunity://editor/state` until compilation and domain reload finish.
7. Call `read_console` for errors.
8. Only after successful compilation attach new component types or use them in
   scenes.

Use hashes or other optimistic concurrency controls where the live tool schema
supports them. Never overwrite user edits based on stale file content.

## Scene and Asset Workflow

1. Query scene hierarchy with pagination, starting around 50 items.
2. Find objects first, then address them by instance ID when names are
   ambiguous.
3. Request component metadata without properties first; request full properties
   only for the components being changed.
4. Search assets with modest pages and previews disabled unless images are
   required.
5. Use `batch_execute` for independent repetitive operations.
6. Use `fail_fast` when later batch operations depend on earlier ones.
7. Save the scene explicitly after successful changes.
8. Verify through resources, console output, and screenshots when visual state
   matters.

For prefabs, inspect `mcpforunity://editor/prefab-stage` before modifying. Use
`manage_gameobject` with a prefab path to instantiate; use prefab tools for
prefab asset creation and editing.

## Test Workflow

1. Activate the `testing` group.
2. Read `mcpforunity://tests` for a quick inventory.
3. Use `run_tests` with a narrow mode/filter first.
4. For asynchronous jobs, poll with `get_test_job`.
5. Include failed-test detail and report exact failures.
6. Run broader suites after focused tests pass.

This project currently has the Test Framework package but no project test
assemblies. Test discovery returning no project tests is expected until those
are added.

## Visual Verification

Use camera screenshots when scene appearance is part of the request:

- inline images only when analysis needs image pixels;
- resolution around 256 to 512 for routine verification;
- Scene View capture for gizmos and editor-level inspection;
- multiview or surround captures for geometry and arena review;
- a named camera for gameplay framing checks.

Screenshots prove presentation, not gameplay correctness. Pair them with state,
console, and test checks.

## Multi-Instance Routing

HTTP mode can expose several Unity editors. With more than one instance:

- pin the session using `set_active_instance` with exact `Name@hash`; or
- pass `unity_instance` per call for one-off routing.

Never mutate when multiple instances exist and routing is unresolved.

## Security

- Local HTTP should remain bound to loopback.
- Do not enable LAN binding without a specific need and network review.
- Remote transport should use HTTPS and API-key authentication.
- Do not put credentials into project files, prompts, logs, or screenshots.
- Treat arbitrary C# execution, package installation, builds, and external
  network access as high-impact operations.
- Report security vulnerabilities privately to `security@coplay.dev`.

## Troubleshooting Order

1. Confirm the package resolved in `Packages/packages-lock.json`.
2. Confirm the Unity editor is running and the MCP window/server is active.
3. Read `mcpforunity://instances`.
4. Read `mcpforunity://editor/state`.
5. Inspect `read_console`.
6. Confirm the required tool group is active.
7. Check transport, host, and port configuration.
8. Use the upstream troubleshooting guide for client-specific, macOS, Windows,
   WSL2, Python, `uv`, and DLL-conflict cases.

Relevant local paths:

- Setup problems:
  `upstream/website/docs/guides/troubleshooting.md`
- Transport:
  `upstream/website/docs/architecture/transports.md`
- Multi-instance:
  `upstream/website/docs/guides/multi-instance.md`
- Tool groups:
  `upstream/website/docs/guides/tool-groups.md`
- Roslyn:
  `upstream/website/docs/guides/roslyn.md`
- Remote auth:
  `upstream/website/docs/guides/remote-server-auth.md`
