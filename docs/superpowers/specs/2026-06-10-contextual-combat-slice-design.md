# Contextual Combat Slice Milestone Design

**Status:** Approved design

**Audience:** Internal team

**Scope:** Current one-player-versus-CPU prototype

**Priority:** Match pacing and move variety

## Objective

Increase match variety by making wrestler state and ring position produce
distinct offensive choices, then regulate stronger choices through the existing
stamina, momentum, recovery, and reversal systems.

The milestone remains inside the current prototype scope:

- one player and one CPU opponent
- existing roster, ring, rulesets, pins, submissions, reversals, and specials
- no creation suite, weapons, multiplayer, campaign, entrances, or online play

## Selected Approach

Implement a **Contextual Combat Slice** as six vertical phases:

1. Context and validation foundation
2. Grounded positional offense
3. Directional grapple selection
4. Corner offense
5. Rope and rebound offense
6. Move-tier pacing regulation

Each phase must include player control, CPU use, move data, validation,
execution, cleanup, presentation, debug visibility, and manual QA.

Do not replace the current category-based move architecture with a full editable
move-slot system during this milestone.

## Architecture

The runtime path is:

```text
Player input or CPU intent
→ resolve transient combat context
→ request contextual move family
→ select compatible move
→ validate state, position, rules, and resources
→ execute through WrestlerCombat
→ apply state, position, damage, stamina, and momentum results
→ present through animation, UI, and logs
```

Ownership remains:

```text
WrestlerStateMachine  physical state and permissions
RingInteractionSystem rope and corner geometry
MoveData              move requirements, costs, timing, and effects
MoveDatabase          context-specific move collections
WrestlerCombat        shared player/CPU action resolution
IAnimationDriver      presentation only
DebugOverlay          runtime decision visibility
```

Context is evaluated when an action is attempted. It is not stored as a second
persistent state.

## Data Model

### Combat Context

```csharp
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
```

Resolution priority:

```text
1. Active grapple lock
2. Downed target within ground-action range
3. Cornered target within corner-action range
4. Rope-staggered target within rope-action range
5. Attacker in rebound state
6. Normal standing context
```

### Grapple Direction

```csharp
public enum MoveDirection
{
    Neutral,
    Forward,
    Backward,
    Left,
    Right
}
```

Left and right may share a lateral data bucket for this milestone. Input is
converted relative to attacker facing and uses a dead zone.

Shared combat API:

```csharp
TryQuickGrappleFromLock(MoveDirection direction)
TryPowerGrappleFromLock(MoveDirection direction)
```

### Move Tier

```csharp
public enum MoveTier
{
    Light,
    Medium,
    Heavy,
    Special
}
```

Tier coordinates pacing validation and AI selection. Existing authored fields
remain authoritative for concrete behavior:

```text
staminaCost
startupTime
activeTime
recoveryTime
reversalWindowStart
reversalWindowEnd
damage
momentumGainOnHit
```

### Move Collections

Extend `MoveDatabase` with:

```text
groundUpperAttacks
groundLowerAttacks
directional quick grapples
directional power grapples
cornerStrikes
cornerGrapples
ropeStaggerAttacks
ropeReboundAttacks
```

Directional selection:

```text
requested bucket
→ compatible candidates
→ selected candidate
→ neutral fallback
→ clean rejection when neutral is empty
```

### Compatibility and Validation

Represent only requirements needed by this milestone. Reuse existing `MoveData`
fields before adding overlapping ones.

Potential requirements:

```text
target downed
ground target zone
target cornered
target rope-staggered
attacker rebounding
minimum stamina
running state
grapple role
lift strength and target weight
```

Validation returns a structured result with:

```text
validity
rejection reason
debug message
```

Validation occurs before stamina spending or temporary state ownership.
Intentional resolved failures, such as a failed lift, may still consume
resources and apply consequences.

## Phase 1: Context and Validation Foundation

Deliver:

- `CombatContext`, `MoveDirection`, and `MoveTier`
- transient context resolution
- shared compatibility validation
- directional grapple migration with existing moves in neutral buckets
- context, tier, and rejection information in F1

Acceptance criteria:

- Existing matches remain playable through pin or submission.
- Existing strikes, grapples, running attacks, reversals, specials, pins, and
  submissions retain their behavior.
- Invalid actions spend no stamina unless failure is an intentional outcome.
- Context priority is deterministic.
- Player and CPU requests use the same validation path.
- No stale grapple role or indefinite locking state remains.

## Phase 2: Grounded Positional Offense

Add:

```text
upper-body ground attack
lower-body ground attack
existing ground submission
existing pin attempt
```

Initial input:

```text
J near a downed target → positional ground attack
O near a downed target → submission
I near a downed target → pin
```

Ground zone is determined from attacker position relative to the defender's
facing axis. This milestone does not add limb damage.

Use two upper-body and two lower-body attacks initially.

Acceptance criteria:

- Upper and lower positions select only their matching move family.
- Out-of-range attempts spend no stamina.
- Standing strikes continue to whiff against downed defenders.
- Pin and submission behavior remains unchanged.
- Ground moves apply authored effects and recovery.
- Recovery or roll-away ends the offensive opportunity.
- CPU ground offense does not suppress credible pin or submission attempts.
- Repeated attacks cannot permanently prevent defender recovery.

## Phase 3: Directional Grapples

Input:

```text
L                         quick neutral
L + movement direction    quick directional
K                         power neutral
K + movement direction    power directional
```

Initial buckets:

```text
Neutral
Forward
Backward
Lateral
```

Acceptance criteria:

- Direction is evaluated relative to wrestler facing.
- Neutral and directional buckets select only assigned moves.
- Missing directional content falls back to neutral.
- Missing neutral content rejects cleanly and releases the grapple.
- Quick and power families remain distinct.
- Lift, stamina, reversal, and fallback rules remain active.
- CPU accesses every populated bucket through the shared API.

## Phase 4: Corner Offense

Add:

```text
corner strike
corner grapple
corner escape or reversal opportunity
```

A valid corner action requires both compatible defender state and valid corner
geometry.

Each move must end in one documented result:

```text
remain cornered
become stunned
become downed
exit toward ring center
```

Seated-corner, top-turnbuckle, and avalanche substates are deferred.

Acceptance criteria:

- Corner actions require both state and geometry.
- Corner strike and grapple use distinct move families.
- Every move has a documented result and cleanup path.
- Invalid or interrupted actions leave both wrestlers valid.
- Defender has a defined escape or reversal opportunity.
- CPU exploits corner position without repeatedly herding an already cornered
  opponent.

## Phase 5: Rope and Rebound Offense

Add:

```text
rope-stagger strike
rope-stagger grapple or continuation
dedicated rebound attack family
```

`RingInteractionSystem` remains the only rope-geometry authority. Apron combat
and springboards are deferred.

Acceptance criteria:

- Rope-stagger moves require a rope-staggered target.
- Rebound moves require an active rebound state.
- Ordinary running attacks remain available outside rebound context.
- Leaving the required context prevents stale execution.
- Resulting states and positions are authored and applied consistently.
- CPU can use rope-stagger and rebound offense.
- Rulesets that ignore rope breaks continue detecting rope contact.

## Phase 6: Pacing Regulation

Pacing is applied after all contextual families are playable.

Guidelines:

| Tier    | Cost                   | Recovery          | Intended use          |
| ------- | ---------------------- | ----------------- | --------------------- |
| Light   | Low                    | Short             | Frequent setup        |
| Medium  | Moderate               | Moderate          | Sustained control     |
| Heavy   | High                   | Long              | Escalation and payoff |
| Special | Existing special rules | Category-specific | Peak offense          |

Runtime rules:

- Light offense remains available at low stamina.
- Heavy moves require meaningful stamina commitment.
- Heavy misses and failures retain recovery disadvantage.
- Specials retain existing momentum requirements.
- Player and CPU use the same restrictions.
- No match-phase system or hidden time-based damage scaling is added.
- Runtime does not silently rewrite authored move values.

Editor warnings should identify:

```text
reversal window outside move duration
lift-tagged move without lift validation
contextual move in incompatible family
missing result or cleanup path
directional set without neutral fallback
suspicious heavy-versus-light cost or recovery values
```

Warnings remain advisory unless data is structurally invalid.

## CPU Behavior

Extend the current FSM rather than replacing it.

Priority examples:

```text
Downed target:
credible pin or submission
→ ground attack
→ reposition or wait

Cornered target:
corner grapple or strike
→ normal fallback

Rope-staggered target:
rope-context attack
→ normal fallback

Grapple-lock attacker:
directional quick or power move
→ release lock if no move resolves
```

Reuse reaction delay, stamina checks, `AIMemory`, and existing situational
positioning.

## Debugging

Extend F1 and structured logs with:

```text
resolved combat context
requested direction
requested move family
candidate count
selected move
move tier
validation result
rejection reason
fallback use
CPU decision reason
```

Do not create a separate debugging framework.

## Failure Invariants

- No stamina is spent before final validation.
- No temporary role remains without an owner.
- No wrestler remains indefinitely in a locking state.
- Missing content cannot produce a null-reference failure.
- Incompatible move families cannot execute silently.
- Animation does not determine gameplay success.
- Interrupted actions leave both wrestlers in valid states.

## Verification

Every phase must pass:

```text
offline compile check
focused editor tests for pure logic
phase-specific manual matrix
full existing checklist smoke test
F1 inspection for unexplained state or ownership
documentation update
```

Manual matrices must cover:

- player and CPU attackers
- center, rope, and corner positions
- sufficient and insufficient stamina
- compatible and incompatible move requirements
- success, miss, reversal, interruption, and recovery
- standard, no-rope-break, and hardcore rules where relevant

## Milestone Exit Criteria

The milestone is complete when:

1. Ground, directional grapple, corner, and rope/rebound contexts are playable.
2. Human and CPU use the same contextual combat APIs.
3. Every contextual move has compatibility requirements and cleanup behavior.
4. Move tiers regulate stamina and recovery without match phases.
5. Existing pins, submissions, reversals, specials, ropes, and reset flow pass
   regression testing.
6. Debug output explains context selection and failed actions.
7. Manual QA covers contextual and existing match systems.
8. No creation-suite, move-editor, animation-editor, or broader match scope has
   entered the milestone.

## Deferred Scope

- full editable move-slot assets
- moveset or move-creation editors
- animation recipes or timeline editing
- custom wrestlers and attire systems
- match-phase state machine
- crowd simulation
- behavior-tree replacement
- apron and ringside combat
- springboards, weapons, tag teams, and multi-wrestler systems

## Sources

- `Documentation/DesignDoc.md`: current prototype scope and architecture
- `Documentation/KnowledgeBase/BestPractices.md`: adopted combat boundaries
- `Assets/Scripts/Moves/MoveData.cs`: current move timing and requirements
- `Assets/Scripts/Moves/MoveDatabase.cs`: current contextual move categories
- `Assets/Scripts/Combat/WrestlerCombat.cs`: shared player/CPU resolution
- `research/pro_wrestling_battle_system_monolithic_research.md`: positional
  combat, pacing, move-tier, stamina, and momentum research
- `research/classic_grappling_game_modding_research_monolithic.md`:
  context-sensitive move selection, compatibility, validation, and tooling
  research
