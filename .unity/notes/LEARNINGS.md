# Learning Log

## 2026-06-10 - UnityMCP package state

Context:

UnityMCP documentation was ingested for this repository.

Learning:

The project already has `com.coplaydev.unity-mcp` installed and connected. The
lock file, not the moving manifest URL, identifies the installed source.

Evidence:

- Unity version: `6000.4.10f1`
- Package version: `9.7.1`
- Package commit: `78ee5418415953b79c358bfe6355fcc3fde7912b`
- Connected instance at ingestion: `fightgame@fc0efb860bfd225d`

Reuse:

Compare the lock hash before relying on the offline snapshot.

## 2026-06-10 - Live schemas outrank copied examples

Context:

The upstream agent guide contains useful templates, but tool availability and
payload schemas depend on version, group activation, and project extensions.

Learning:

Use the connected server's live resources and callable tool schema as the final
authority. Treat upstream examples as adaptable templates.

Evidence:

The project reported 30 custom tools and nine groups, with only `core` enabled
by default.

Reuse:

Read `mcpforunity://custom-tools` and `mcpforunity://tool-groups` before choosing
an operation.

## 2026-06-10 - uGUI and UI Toolkit require different workflows

Context:

The current match HUD is constructed with `UnityEngine.UI`, while UnityMCP's
`manage_ui` group is for UI Toolkit.

Learning:

Use GameObject and component operations for the existing uGUI Canvas. Reserve
`manage_ui` for UXML, USS, and UIDocument work.

Evidence:

`Assets/Scripts/UI/MatchHUD.cs` creates a Canvas, CanvasScaler, and
GraphicRaycaster at runtime.

Reuse:

Identify the UI system before selecting UnityMCP tools.

## 2026-06-10 - Capability gates can hide role-specific actions

Context:

The match soft-locked whenever the CPU initiated a grapple lockup; the console
showed `[Grapple] ... locks up with ...` repeating every ~2 seconds forever.

Learning:

`CPUWrestlerAI.Decide()` early-returned on `!Profile.canAttack` before checking
`InGrappleLockAsAttacker`. `GrappleLock` correctly sets `canAttack=false` for
both wrestlers, but the attacker still has a legal follow-up (quick/power
grapple). Capability flags describe the state for everyone; role-specific
branches must be evaluated before generic capability gates. The log cadence
(1.8 s lock timeout + AI reaction delay ≈ 2 s) identified the loop before any
code was read.

Evidence:

`Assets/Scripts/AI/CPUWrestlerAI.cs` (`Decide()` ordering,
`ChooseGrappleMove` fallback), `Assets/Scripts/Wrestlers/WrestlerStateMachine.cs`
(`GrappleLock` profile), regression line in
`Documentation/TestingChecklist.md`.

Reuse:

When an AI or input controller "does nothing" in a state, diff the state's
profile flags against the actions that state is supposed to produce, and check
gate ordering. Repeating-log cadence that matches a state timeout is a loop
signature.

## 2026-06-10 - Non-uniform root scale shears rotated child joints

Context:

The placeholder body was rebuilt as an articulated humanoid (pelvis/spine/neck,
shoulders/elbows, hips/knees as empty pivots with primitive meshes offset under
them).

Learning:

Weight-class sizing must be applied as per-mesh x/z bulk plus a *uniform* root
scale. A non-uniform `visualRoot.localScale` visibly shears any rotated child
joint (bent elbows/knees smear). Joint sign conventions and the lying-pose
`shift`/`lift` compensation are documented in
`Documentation/KnowledgeBase/Examples.md`.

Evidence:

`Assets/Scripts/Wrestlers/WrestlerView.cs` (`BulkFor`, `HeightFor`, `Bulked`),
`Assets/Scripts/Animation/PlaceholderAnimationDriver.cs` (`LyingPose`).

Reuse:

If a bent limb ever renders distorted, look for a non-uniform scale upstream of
the joint before suspecting the pose math.

## 2026-06-10 - Offline Roslyn compile check without Unity

Context:

The repo has no CLI build, and Unity batch-mode compiles are unsafe while the
editor is open, so script errors normally surface only after tabbing back to
Unity.

Learning:

All non-Editor scripts compile in ~2 s with the dotnet SDK Roslyn against
Unity's reference DLLs (`.../Unity.app/Contents/Resources/Scripting/Managed/UnityEngine/*.dll`,
`NetStandard/ref/2.1.0/netstandard.dll`, plus
`Library/ScriptAssemblies/UnityEngine.UI.dll`), `-langversion:9.0`. Flags,
references, and the source list must go through a single `@file.rsp` response
file — multi-line shell variables collapse into one bogus filename (CS1504).

Evidence:

Full command recorded in `Documentation/KnowledgeBase/Examples.md`
("Offline compile check"); used to verify the rig/AI changes on 2026-06-10
(exit 0, one pre-existing CS0649 warning).

Reuse:

Run it after any multi-file script change before returning to the editor.

## 2026-06-10 - Offline compile covers Editor scripts and tests too

Context:

The contextual combat slice added edit-mode tests and editor validators while
the Unity editor was open (no batch mode, no UnityMCP session), so all
verification ran through the offline Roslyn check.

Learning:

The documented offline compile extends to `Assets/Scripts/Editor/` by adding
the netfx mscorlib shim and the package-cache nunit DLL and compiling all of
`Assets/Scripts`. Do not reference `Managed/UnityEditor.dll` alongside the
`Managed/UnityEngine/*.dll` glob — `UnityEditor.CoreModule` is already in the
glob and the duplicate types collide (CS0433). The net40 nunit assembly needs
`NetStandard/compat/2.1.0/shims/netfx/mscorlib.dll` or `[Test]` attributes fail
with CS0012.

Evidence:

`/tmp/loco_editor_check.sh` used for every task commit of the contextual
combat slice (branch unity-wrestling-prototype, commits e51aac0..0e276e4);
command promoted to `Documentation/KnowledgeBase/Examples.md`
("Editor + test variant").

Reuse:

Run the editor variant after touching anything under `Assets/Scripts/Editor/`;
the runtime-only check misses those files entirely.

## 2026-06-10 - Pre-generating .meta files while the editor is open

Context:

New `.cs` files needed `.meta` sidecars for per-task commits, but the open
editor only imports on focus and no UnityMCP session was available to force a
refresh.

Learning:

Writing a minimal MonoImporter `.meta` (fileFormatVersion 2 + lowercase
32-hex guid from `uuidgen`) before Unity imports the file is safe: Unity keeps
pre-existing GUIDs on import instead of generating new ones, so references
remain stable and the commit can include the meta immediately.

Evidence:

All new scripts in the contextual combat slice (e.g.
`Assets/Scripts/Combat/CombatContextResolver.cs.meta`) were generated by
`/tmp/loco_meta.sh` and accepted unchanged by the editor.

Reuse:

Only create metas for files Unity has not yet imported; never rewrite an
existing meta's guid.

## 2026-06-11 - Second lockup loop: Rethink-spam suppresses Decide

Context:

The lockup soft-lock returned despite the 2026-06-10 Decide-ordering fix:
"[Grapple] JT Staten locks up with Zeak Gallent" repeated every ~2 s with zero
"[Move]" logs in between while the player stood passive.

Learning:

`CPUWrestlerAI.Act` runs every frame. After `TryGrappleAttempt` succeeded,
`CurrentState` stayed `AttemptGrapple`, whose case re-attempted (silently
failing on the `Role != None` guard) and called `Rethink()` every frame —
permanently pushing `_nextDecisionAt` into the future, so `Decide()` (and with
it the 06-10 lock-follow-up fix) never ran. The lock timed out and instantly
re-formed. The loop only reproduced with a passive player because any player
attack trips `ReactDefensively`, which flips `CurrentState` and unfreezes the
FSM — which is why earlier active-play QA passed. Two accomplices in the same
path: `ExecuteGrappleMove` released locks silently on stamina failure
(breaking `TryPower || TryQuick` chains invisibly), and the AI's affordability
pre-gate sampled a different random move than the one later executed.

Evidence:

`Assets/Scripts/AI/CPUWrestlerAI.cs` (`Act` attempt cases),
`Assets/Scripts/Combat/WrestlerCombat.cs` (`ExecuteDirectionalGrapple`,
`ExecuteGrappleMove`); fix commit "Fix CPU lockup decision-suppression loop".
No-logs-between-lockups was the discriminating evidence versus the 06-10 bug
(which also produced no follow-ups but for a different reason).

Reuse:

In Act, `Rethink()` is for successful or state-changing actions only; a failed
`Try*` must transition out (IdleThink or a fallback state) so Decide runs.
Promoted to Documentation/KnowledgeBase/BestPractices.md ("A failed AI attempt
must yield control back to Decide"). When a lockup loop appears, first check
whether Decide is even running before re-auditing its ordering.

## 2026-06-11 - Mid-play recompile nulls all Bind()-wired references

Context:

Unity console showed repeated NullReferenceExceptions at
WrestlerMotor.Update():93 (`_core.Anim.SetMovementSpeed`), logged at the same
moment as "Reloading assemblies for play mode ... forced synchronous
recompile".

Learning:

This project wires every subsystem at runtime via Bind() with non-serialized
references (including interface-typed `Anim`). Editing/committing scripts
while the editor is in Play mode triggers a domain reload that nulls all of
that wiring, so the first hot Update throws. With an agent committing code
during playtests this happens constantly. Old compile errors also linger in
the cumulative ~1 GB Editor.log — always check the tail, not the first grep
hits.

Evidence:

`~/Library/Logs/Unity/Editor.log` (NREs at 08:39 followed immediately by the
forced recompile lines); guard added in WrestlerMotor.Update.

Reuse:

Set Preferences > General > "Script Changes While Playing" to
"Recompile After Finished Playing" on this machine. In code, treat
presentation calls in hot Updates as reload-tolerant (`?.`) but do not
null-guard gameplay state — a broken reload should fail loudly there.

## 2026-06-11 - State machine and coroutines are parallel truths

Context:

Hits "didn't register": damage applied and Stunned was set, but the struck
wrestler's attack landed anyway.

Learning:

`WrestlerStateMachine.Current` and the combat coroutines are independent —
a routine mid-flight keeps Set()-ing its phase states right over whatever an
external system wrote. Any externally-imposed state change that should stop an
action must also stop the coroutine (`InterruptMove()`), gated by
`StateProfile.canBeInterrupted` — which had zero consumers until today.

Evidence:

`WrestlerCombat.ApplyHit` + contextual appliers (commit "Implement the react
rule"); promoted to Documentation/KnowledgeBase (BestPractices "Getting hit
interrupts", Examples postmortem "the punch that didn't stop anything").

Reuse:

When behavior contradicts the visible state, look for a running coroutine; when
trusting a profile flag, grep for its readers first.

## 2026-06-11 - GlobalScale broke every body-relative constant silently

Context:

Wrestlers scaled 1.25x for ring proportions; grapples then "failed 1000 times."

Learning:

Distance constants (grapple/strike/pin ranges, tie-up snap, contextual move
ranges, camera look-at) encode body size implicitly. Capsule radii scaled →
minimum separation grew → usable range margin shrank ~40% with no error
anywhere. Scaling actors requires a same-change audit of every distance
constant; the working margin is range minus the sum of capsule radii.

Evidence:

Commits "Scale wrestlers 1.25x" (the miss) and "Make grapples land" (the fix);
promoted to KnowledgeBase BestPractices + Examples postmortem.

Reuse:

Grep for float literals near HitboxProbe/InRange/Teleport call sites whenever
GlobalScale or capsule sizing changes.
