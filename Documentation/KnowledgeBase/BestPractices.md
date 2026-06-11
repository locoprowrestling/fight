# Best Practices

The rules this codebase depends on. Each one exists because breaking it produces a specific, known failure — noted as the "why".

## Data

**Code is the source of truth; assets are a cache.** All game data (moves, stats, specials, traits, roster, rules, difficulty) is defined in [DefaultGameData.cs](../../Assets/Scripts/Roster/DefaultGameData.cs). The `.asset` files under `Assets/Resources/LoCoData/` are serialized copies made by **Tools > LoCo Fight Game > Create Default Prototype Assets**. After editing `DefaultGameData.cs`, regenerate the assets.
*Why:* `RosterLoader`/`GameBootstrap` prefer saved assets over the in-code factory, so stale assets silently shadow your code change — the game runs, your numbers don't.

**Don't hand-edit generated assets for values that exist in code.** Tuning in the Inspector is fine for a quick experiment, but port the final value back into `DefaultGameData.cs` before committing, then regenerate.

## States

**Behavior rules live in `StateProfile`, not scattered if-checks.** Every `WrestlerState` declares what's allowed in it (move/attack/grapple/reverse, pinnable/grabbable/strikable, timeout, exit state) in `BuildProfiles()` in [WrestlerStateMachine.cs](../../Assets/Scripts/Wrestlers/WrestlerStateMachine.cs). Consumers check `core.States.Profile`, never the state enum directly (except for genuinely state-specific logic).

**Check role-specific branches before generic capability gates.** A controller that early-outs on `!Profile.canAttack` will never reach logic for states where acting is the *point* but the profile says "can't attack" (e.g. the grapple-lock attacker choosing a follow-up — see the [lockup-loop postmortem](Examples.md#postmortem-the-endless-lockup-loop)).
*Why:* capability flags describe the state, not the role; the attacker and defender share `GrappleLock` but only one of them may act.

**Every mutual/locking state needs three things:** a timeout in its profile, an owner responsible for resolving it, and a cleanup path if it dissolves externally. `GrappleLock` has all three: the 1.8 s profile timeout, the attacker's follow-up (AI falls back quick↔power and force-releases on failure), and the stale-role safety net in `WrestlerCombat.Update()`.
*Why:* any one of these missing turns a dropped edge case into a soft-lock.

## Control symmetry

**New combat actions go through [WrestlerCombat.cs](../../Assets/Scripts/Combat/WrestlerCombat.cs).** `PlayerInputController` (human) and `CPUWrestlerAI` (CPU) drive the exact same API. Never implement an action inside the input controller or the AI directly.
*Why:* otherwise one side can do things the other can't, and balance/QA assumptions break.

## Combat architecture

These practices are consistent with the external research, but they are listed
here because they were separately validated against this game's current code.
This section, not the research document, is authoritative.

**Controllers choose actions; gameplay systems resolve them.** Human input and
CPU logic may decide what to attempt, but legality, contests, damage, stamina,
momentum, and state changes remain in `WrestlerCombat` and the relevant combat
system. Animation and UI present results rather than deciding them.
*Why:* one resolution path prevents player/CPU drift and keeps presentation
changes from altering gameplay.

**Use the existing authority for each rule.** State permissions belong in
`StateProfile`; rope and corner queries belong in `RingInteractionSystem`;
match flow and rules belong in `MatchManager`/`MatchRulesData`; move properties
belong in `MoveData`; character variation belongs in stats, traits, specials,
and AI data. Add a new authority only when no current owner fits.
*Why:* scattering the same decision across controllers, executors, and UI
creates contradictory behavior that is difficult to tune or test.

**Prefer reusable data and capabilities over wrestler-id branches.** Shared
combat logic should ask what a wrestler, move, trait, special, or ruleset can do
instead of checking a specific roster id. Character-specific behavior should
enter through the existing data or executor extension points.
*Why:* the roster already shares one combat engine; capability-driven variation
keeps new wrestlers from multiplying special cases.

**Treat position and result state as part of a move outcome.** When adding or
changing a move, verify not only damage but the attacker/defender states,
recovery ownership, downed or stunned result, and rope/corner interaction.
*Why:* those outcomes determine the next legal action and are therefore
gameplay, not presentation detail.

**Keep moves in context-specific database categories.** `MoveDatabase` separates
light strikes, heavy strikes, quick grapples, power grapples, running attacks,
and ground submissions. Add a new context as an explicit category and resolver
path; do not flatten the moveset into one universal list and reconstruct
context through scattered filtering.
*Why:* the category is the contract between input/AI intent and legal move
selection, and prevents standing, running, grapple, and grounded actions from
being mixed accidentally.

**Every constrained move needs a compatibility contract and a failure path.**
Requirements such as running state, rope/corner context, lift class, target
weight, and grapple role belong in `MoveData` or the owning resolver. A failed
attempt must return cleanly, release any lock it owns, or execute an explicit
fallback such as `fallbackMoveIfLiftFails`.
*Why:* silent no-ops and partially entered states create stuck wrestlers,
unclear controls, and animation/gameplay disagreement.

**New contextual families resolve context through `CombatContextResolver`.**
Context (grapple lock, ground upper/lower, corner, rope stagger, rebound,
standing) is computed transiently when an action is attempted — never cached as
a second persistent state and never re-derived ad hoc in controllers. Geometry
questions still go to `RingInteractionSystem`, state questions to
`WrestlerStateMachine`.
*Why:* one resolution priority keeps player and CPU agreeing about which family
applies and avoids stale-context execution.

**Validate structurally before spending resources.** Every contextual request
goes through a `ContextualMoveValidator` method that returns a
`MoveValidationResult` (validity, `MoveRejectionReason`, debug message) before
any stamina is spent or temporary state taken. Stamina gates use
`MovePacingRules.RequiredStamina` (max of cost and minimum); only `staminaCost`
is spent. Intentional resolved failures (failed lifts) may still consume
resources.
*Why:* invalid attempts must be free and explainable, and player/CPU must hit
the identical rejection path.

**One physical press resolves at most one action.** Tap/hold buttons go
through `PressTracker` (tap on release before
`PlayerInputLogic.HoldThreshold`, hold committed at the crossing, nothing on
release after a hold), and pause / match end / reset call `Reset()` on every
tracker plus `InputBuffer.Clear()` so nothing fires on resume.
*Why:* double-fired or resumed presses read as phantom inputs and are nearly
impossible to QA after the fact.

**One direction frame.** Directional input is camera-mapped to a world vector
first (exactly like locomotion) and only then classified against a wrestler's
facing (`PlayerInputLogic.ResolveMoveDirection`). Never interpret raw stick
axes against a wrestler's local axes directly.
*Why:* with a moving camera, "up" on the stick is only meaningful on screen;
mixing frames makes identical inputs do different things in different systems.

**HUD prompts are presentation-only.** `ControlPromptLogic` maps the resolved
combat context to labels; nothing may read prompt strings back into gameplay,
and a wrong prompt is a bug in the mapping, never in combat.
*Why:* the prompt exists to make context-sensitive controls legible; letting
it gate behavior would create a second, contradictory authority.

**Record every contextual request for F1.** Use the `RecordContext` funnel in
`WrestlerCombat` so the overlay shows context, zone, direction, family,
candidate count, selection, tier, validation result, and fallback use; log
rejections.
*Why:* "nothing happened" is the costliest bug class in contextual input — the
overlay must always be able to say why.

**Gameplay data owns effects and timing; animation only presents them.**
`MoveData` and combat routines define startup, active, recovery, damage,
reversal windows, stamina, momentum, and result states.
`IAnimationDriver.PlayMove()` receives presentation identifiers and speed but
does not decide whether or when a move hits.
*Why:* placeholder poses and future animation clips can be replaced without
changing combat behavior.

**Keep roster presentation separate from combat configuration.** `RosterEntry`
owns selection-facing data such as portrait and view prefab, while
`WrestlerDefinition` owns stats, moveset, specials, and traits. Do not clone or
alter combat data solely to create a different portrait or future attire.
*Why:* visual variants should not fork wrestler balance or behavior.

## Presentation isolation

**Only [WrestlerView.cs](../../Assets/Scripts/Wrestlers/WrestlerView.cs) and `IAnimationDriver` implementations touch meshes or animation.** Gameplay reads/writes the wrestler root transform and calls `core.Anim.*`; it never reaches into body parts.
*Why:* real art replaces the placeholder rig without touching combat/AI/rules ([FutureAssetIntegration.md](../FutureAssetIntegration.md)).

**Visual parts carry no colliders.** The `CharacterController` on the wrestler root is the only physics volume, sized from `WrestlerView.RigHeight` × `HeightFor(weight)` with radius scaled by `BulkFor(weight)` in `WrestlerCore.Create`.
*Why:* limb colliders would fight the CharacterController and make hit detection frame-dependent; ranges are data (`MoveData.range`), distance checks are flat transform-to-transform (`HitboxProbe`).

**Scale the visual root uniformly only.** Weight-class bulk is applied per-mesh (x/z), never as a non-uniform `visualRoot.localScale`.
*Why:* non-uniform scale on a parent shears rotated child joints — bent elbows/knees visibly distort.

## Single authorities

- Rope math: [RingInteractionSystem.cs](../../Assets/Scripts/Ring/RingInteractionSystem.cs) — nothing else computes rope distances/directions.
- Match flow: [MatchManager.cs](../../Assets/Scripts/Match/MatchManager.cs) (`Instance` singleton) — gate all combat on `MatchManager.Instance.IsCombatAllowed`, null-tolerant (`MatchActive` pattern in `WrestlerCombat`).
- Wrestler wiring: subsystems are created and bound in `WrestlerCore.Create()` — add new per-wrestler components there, nowhere else.

## Unity specifics

- Unity 6.4 (6000.4.x): use `FindAnyObjectByType`, not `FindFirstObjectByType` (deprecated in 6.4, warns).
- Input is the **legacy Input Manager** (`Input.GetKeyDown`); don't introduce the new Input System.
- One assembly, one namespace (`LoCoFight`), no asmdefs — keep it that way until compile times hurt.
- Editor-only code goes under `Assets/Scripts/Editor/`; menu items under **Tools > LoCo Fight Game/**.

## Logging

Use the established bracketed tags so the console reads as a match transcript: `[Move]`, `[Grapple]`, `[Reversal]`, `[Rope]`, `[AI]`, `[Match]`, `[Bootstrap]`. One line, present tense, names included (`[Grapple] JT Staten locks up with Zeak Gallent`).
*Why:* the lockup-loop bug was diagnosed from log cadence alone — uniform logs make timing bugs visible.

**Expose hidden state in [DebugOverlay.cs](../../Assets/Scripts/UI/DebugOverlay.cs).**
When adding a stateful mechanic, show the minimum information needed to explain
its current decision: active state, timer/progress, selected action or move,
eligibility, and rejection reason where available.
*Why:* combat timing, rope eligibility, AI intent, reversal windows, pins, and
submissions cannot be diagnosed reliably from animation alone.

## Verification

There is no CLI build or test suite. Three layers, cheapest first:

1. **Offline compile check** (catches errors without switching to Unity): compile all non-Editor scripts with the dotnet SDK's Roslyn against Unity's reference DLLs — full command in [Examples.md](Examples.md#offline-compile-check). Don't run Unity in batch mode while the editor is open.
2. **In-editor**: tab back to Unity, let it compile, press Play. F1 debug overlay, F2 roster selector, R reset.
3. **Manual QA**: run the relevant section of [TestingChecklist.md](../TestingChecklist.md); add a checklist line for every behavior bug you fix.
