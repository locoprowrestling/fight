# Classic Grappling Game Modding Research Notes: Moves, Modularity, and Custom Wrestlers

> **Repository note:** This document is informational research, not an
> implementation guide, specification, roadmap, backlog, or source of truth.
> It includes speculative systems that are outside the current prototype.
> Appropriate findings may be adopted only after validation against this
> game's code, scope, and goals, then promotion into authoritative project
> documentation.

## Purpose

This document records possible lessons from classic console grappling-game
modding communities for consideration in a modern pro wrestling game. The
research focuses on:

- battle-system modularity
- move creation workflows
- animation splicing concepts
- CAW / custom wrestler feature design
- moveset editing
- AI personality editing
- costume / attire / texture modularity
- data-driven implementation patterns
- complexity management

This document intentionally avoids naming the specific legacy games, branded titles, or development label discussed during source research. The usable design lessons are abstracted into engine-agnostic principles.

---

# 1. Executive Summary

The most useful lesson from classic grappling-game modding is that the best wrestling battle systems are not primarily built around combos. They are built around **state, position, timing, move slots, animation branches, and wrestler identity data**.

A strong modern implementation should use:

```text
WrestlerData
MoveData
MoveSlotData
AnimationClipData
AnimationBranchData
CAWTemplateData
AttirePartData
AIProfileData
RulesetData
MatchStateData
```

The system should avoid hardcoded wrestler logic. A wrestler should be a composition of:

```text
body type
appearance parts
attires
moveset slots
logic profile
stat profile
special traits
entrance metadata
victory metadata
voice / sound metadata
```

The battle system should avoid hardcoded move behavior. A move should be a composition of:

```text
required position
input slot
startup animation
attacker animation
defender animation
impact timing
reversal windows
sell animation
landing state
damage profile
stamina cost
momentum reward
crowd reaction
pin/submission follow-up options
```

The main design rule:

> Treat every wrestler, move, attire, rule, animation, and AI behavior as editable data attached to a reusable runtime system.

---

# 2. Major Research Findings

## 2.1 Move lists are slot-based systems, not merely animation lists

Classic grappling games organized moves around context-sensitive slots. A wrestler does not simply have one giant universal move list. Instead, moves are bound to slots such as:

```text
front weak grapple
front weak grapple + direction
front strong grapple
front strong grapple + direction
rear weak grapple
rear strong grapple
grounded upper body
grounded lower body
running strike
running grapple
corner front
corner rear
top rope
apron
rope rebound
special state
```

This matters because it creates a clean interface between input, position, and move selection.

Modern implementation:

```json
{
  "slotId": "front_grapple_weak_up",
  "requiredAttackerState": ["standing", "front_grapple_advantage"],
  "requiredDefenderState": ["standing", "front_grappled"],
  "validMoveTags": ["front_grapple", "weak", "standing_target"],
  "defaultMoveId": "snap_suplex_basic",
  "canBeEmpty": false
}
```

Best practice:

- Moves belong to slots.
- Slots belong to contexts.
- Contexts are produced by position and state.
- Inputs select slots, not directly hardcoded moves.

---

## 2.2 The control scheme should be simple, but the resolver should be deep

The classic model works because player inputs are readable:

```text
weak grapple
strong grapple
strike
run
block / reverse
special action
direction modifier
```

The complexity happens after the input:

```text
current state
opponent state
distance
facing
momentum
stamina
weight class
move slot
move tier
reversal window
animation compatibility
ruleset
```

Modern design should preserve that philosophy.

Bad design:

```text
Every major move requires a unique command.
```

Good design:

```text
The same few inputs mean different things depending on state, position, direction, and wrestler moveset data.
```

This keeps the game playable while still allowing a deep battle system.

---

## 2.3 Move creation is animation composition plus state resolution

Classic modding communities often created new moves by combining or modifying existing animation sequences. The important lesson is not the exact toolchain. The lesson is that a move can be represented as a structured animation recipe:

```text
attacker setup animation
defender setup animation
sync frame
impact frame
throw / lift / drop phase
sell animation
landing animation
recovery animation
resulting states
```

A modern engine should formalize that as data.

```json
{
  "moveId": "custom_spinning_bomb_01",
  "name": "Spinning Bomb",
  "category": "grapple",
  "tier": "signature",
  "positionRequired": "front_strong_grapple",
  "animationRecipe": {
    "attackerClips": [
      { "clip": "lift_to_shoulder", "startFrame": 0, "endFrame": 34 },
      { "clip": "spin_rotation", "startFrame": 0, "endFrame": 28 },
      { "clip": "sitout_drop", "startFrame": 0, "endFrame": 42 }
    ],
    "defenderClips": [
      { "clip": "being_lifted_to_shoulder", "startFrame": 0, "endFrame": 34 },
      { "clip": "carried_spin", "startFrame": 0, "endFrame": 28 },
      { "clip": "back_impact_sell", "startFrame": 0, "endFrame": 42 }
    ],
    "syncEvents": [
      { "event": "lockBodies", "frame": 4 },
      { "event": "impact", "frame": 72 },
      { "event": "releaseBodies", "frame": 83 }
    ]
  },
  "resultState": {
    "attacker": "seated_recovery",
    "defender": "grounded_faceup_stunned"
  }
}
```

Best practice:

- Treat animation as segments.
- Treat impact as an event.
- Treat landing state as data.
- Treat recovery advantage as data.
- Treat reversals as branch points.

---

# 3. Battle System Modularity Blueprint

## 3.1 Top-level modules

```text
BattleEngine
├── InputInterpreter
├── IntentResolver
├── WrestlerStateMachine
├── PositionSystem
├── GrappleSystem
├── StrikeSystem
├── MoveSlotResolver
├── MoveDatabase
├── MoveExecutionSystem
├── AnimationRecipeSystem
├── ReversalSystem
├── DamageSystem
├── StaminaSystem
├── MomentumSystem
├── PinSystem
├── SubmissionSystem
├── RopeSystem
├── RuleSystem
├── RefereeSystem
├── CrowdSystem
├── CAWSystem
├── AppearanceSystem
├── MovesetEditorSystem
├── AIProfileSystem
└── DebugInspector
```

Each module should own one responsibility. The move resolver should not know how to draw masks. The appearance editor should not know how to calculate pin pressure. The AI profile should not hardcode animation names.

---

## 3.2 Runtime flow

```text
Player input
↓
InputInterpreter converts input to intent
↓
IntentResolver checks current state and target context
↓
MoveSlotResolver finds the selected slot
↓
MoveDatabase returns valid move
↓
RuleSystem checks legality
↓
ReversalSystem opens or closes branch windows
↓
MoveExecutionSystem plays animation recipe
↓
Animation events trigger impact and state changes
↓
Damage, stamina, momentum, crowd, and AI memory update
↓
WrestlerStateMachine resolves final states
```

This protects the engine from becoming a pile of one-off special cases.

---

# 4. State Machine Design

## 4.1 Required wrestler states

```text
Idle
Walking
Running
Striking
StrikeRecovery
FrontGrappleAttempt
FrontGrappleAdvantage
RearGrappleAdvantage
Grappled
MoveStartup
MoveExecution
MoveSell
MoveRecovery
GroundedFaceUp
GroundedFaceDown
Kneeling
Seated
StunnedStanding
StunnedGrounded
CornerFront
CornerRear
CornerSeated
RopeLean
RopeRebound
ApronStanding
OutsideRing
ClimbingTurnbuckle
Diving
Pinning
Pinned
Submitting
InSubmission
RopeBreak
RefereeBreak
Victory
Defeat
```

## 4.2 Use hierarchical states

Flat state machines become unmanageable. Use hierarchy:

```text
Grapple
├── Attempt
├── Lockup
├── Advantage
├── MoveSelection
├── MoveStartup
├── MoveExecution
├── Sell
└── Recovery
```

```text
Grounded
├── FaceUp
├── FaceDown
├── Sitting
├── Kneeling
├── Rolling
├── CrawlingToRopes
├── Pinned
└── InSubmission
```

## 4.3 State transition data

```json
{
  "from": "FrontGrappleAdvantage",
  "intent": "execute_move_slot",
  "conditions": [
    "slot.valid",
    "defender.state:grappled",
    "attacker.stamina>=required"
  ],
  "to": "MoveStartup"
}
```

Best practice:

- States should be inspectable.
- Transitions should be logged.
- Invalid transitions should fail safely.
- Animation events should request transitions, not directly mutate everything.

---

# 5. Move Creation System

## 5.1 Move object schema

```json
{
  "moveId": "front_grapple_snap_suplex_01",
  "displayName": "Snap Suplex",
  "category": "grapple",
  "tier": "medium",
  "tags": ["front_grapple", "standing_target", "suplex", "back_damage"],
  "requiredContext": {
    "attackerState": ["FrontGrappleAdvantage"],
    "defenderState": ["Grappled"],
    "position": ["front"],
    "ringZones": ["center", "near_ropes"]
  },
  "cost": {
    "stamina": 8,
    "momentum": 0
  },
  "reward": {
    "momentum": 6,
    "crowd": 2
  },
  "damage": {
    "head": 2,
    "body": 3,
    "back": 8,
    "legs": 0
  },
  "animationRecipeId": "recipe_snap_suplex_01",
  "reversalWindows": [
    {
      "phase": "startup",
      "startFrame": 8,
      "endFrame": 18,
      "type": "grapple_counter",
      "difficulty": 0.45
    },
    {
      "phase": "lift",
      "startFrame": 30,
      "endFrame": 38,
      "type": "mid_move_shift",
      "difficulty": 0.25
    }
  ],
  "resultState": {
    "attacker": "StandingRecovery",
    "defender": "GroundedFaceUp"
  },
  "followUps": ["pin", "ground_attack", "taunt"]
}
```

---

## 5.2 Animation recipe schema

```json
{
  "recipeId": "recipe_snap_suplex_01",
  "participants": 2,
  "rootMotionMode": "attacker_driven",
  "alignment": {
    "attackerAnchor": "hips",
    "defenderAnchor": "hips",
    "snapToContact": true
  },
  "tracks": {
    "attacker": [
      { "clip": "front_grapple_hold", "start": 0, "end": 10 },
      { "clip": "suplex_lift", "start": 0, "end": 34 },
      { "clip": "suplex_bridge_release", "start": 0, "end": 28 }
    ],
    "defender": [
      { "clip": "front_grapple_caught", "start": 0, "end": 10 },
      { "clip": "suplex_lifted", "start": 0, "end": 34 },
      { "clip": "back_bump_medium", "start": 0, "end": 28 }
    ]
  },
  "events": [
    { "frame": 3, "event": "syncParticipants" },
    { "frame": 15, "event": "lockGrapple" },
    { "frame": 44, "event": "impact", "payload": { "impactType": "back_bump" } },
    { "frame": 58, "event": "releaseParticipants" },
    { "frame": 69, "event": "moveComplete" }
  ]
}
```

---

## 5.3 Move creation editor requirements

A proper move creator should expose:

```text
move name
move category
move tier
required position
attacker animation segments
defender animation segments
sync point
impact point
release point
damage profile
stamina cost
momentum gain
crowd value
reversal windows
landing states
follow-up options
AI usage weight
illegal / dirty flag
pin combo flag
submission flag
rope interaction flag
corner interaction flag
```

Minimum viable editor:

```text
1. Select base move template.
2. Replace attacker animation segment.
3. Replace defender sell segment.
4. Set impact frame.
5. Set result state.
6. Test in sandbox.
7. Save as new move data.
```

Advanced editor:

```text
1. Multi-segment timeline.
2. Attacker and defender preview.
3. Frame event editor.
4. Reversal branch editor.
5. Damage/stamina preview.
6. Ring-position preview.
7. Rope/corner/apron validity checker.
8. AI usage simulator.
9. Export/import JSON.
10. Version history.
```

---

# 6. Move Creation Best Practices

## 6.1 Build moves from templates

Do not start every move from zero. Use move templates:

```text
standing strike
grapple slam
grapple suplex
grapple driver
running attack
corner attack
ground attack
diving attack
submission hold
pin combo
rope rebound move
apron move
weapon attack
```

Template example:

```json
{
  "templateId": "grapple_lift_drop_template",
  "requiredEvents": [
    "syncParticipants",
    "liftStart",
    "impact",
    "releaseParticipants",
    "moveComplete"
  ],
  "requiredResultStates": ["attacker", "defender"],
  "defaultReversalWindows": ["startup", "lift"]
}
```

## 6.2 Every move needs a compatibility contract

A move must declare what bodies and states it supports.

```json
{
  "compatibility": {
    "attackerWeightClasses": ["light", "medium", "heavy"],
    "defenderWeightClasses": ["light", "medium"],
    "requiresLiftStrength": 65,
    "requiresDefenderStanding": true,
    "supportsLargeDefenderFallback": true,
    "fallbackMoveId": "failed_lift_club_to_back"
  }
}
```

This prevents animation and logic breaks when a small wrestler tries to lift a monster.

## 6.3 Moves should fail gracefully

Bad:

```text
Move cannot play, so nothing happens.
```

Good:

```text
Move attempt fails into a shove, clubbing blow, stagger, or reversal opportunity.
```

Fallbacks should be part of move data.

## 6.4 Do not let animation drive all gameplay

Animation events should trigger gameplay, but they should not own gameplay rules.

Correct split:

```text
MoveData says what the move does.
AnimationRecipe says when events occur.
Resolvers decide what those events mean.
```

---

# 7. CAW / Custom Wrestler System

## 7.1 CAW should be a layered data object

```json
{
  "cawId": "custom_001",
  "identity": {
    "ringName": "Example Wrestler",
    "shortName": "Example",
    "hometown": "Longmont, CO",
    "alignment": "face",
    "weightClass": "middleweight",
    "styleArchetypes": ["technical", "showman"]
  },
  "body": {
    "height": 72,
    "weight": 220,
    "bodyType": "athletic",
    "scale": {
      "head": 1.0,
      "torso": 1.0,
      "arms": 1.0,
      "legs": 1.0
    }
  },
  "appearance": {
    "headPart": "head_12",
    "hairPart": "hair_04",
    "faceTexture": "face_custom_01",
    "maskLayer": "mask_none",
    "upperBody": "shirt_02",
    "lowerBody": "tights_07",
    "boots": "boots_03"
  },
  "attires": [
    "attire_default",
    "attire_alt_01",
    "attire_street_fight"
  ],
  "movesetId": "moveset_custom_001",
  "aiProfileId": "ai_custom_technical_showman",
  "entranceId": "entrance_custom_001",
  "victoryId": "victory_custom_001"
}
```

---

## 7.2 CAW feature categories

A serious CAW system should include:

```text
Identity
- ring name
- short name
- nickname
- hometown
- alignment
- faction
- entrance call name
- menu portrait

Body
- height
- weight
- weight class
- body type
- body scale
- skin tone
- muscle definition

Head
- face model
- face texture
- hair
- facial hair
- mask
- paint
- scars
- accessories

Attire
- entrance attire
- ring attire
- alternate attires
- street attire
- manager attire
- color palettes
- texture slots
- logo slots

Moveset
- standing strikes
- grapple slots
- rear grapple slots
- running moves
- corner moves
- ground moves
- diving moves
- submissions
- taunts
- signatures
- finishers

Behavior
- AI personality
- aggression
- reversal tendency
- taunt tendency
- risk tolerance
- pin tendency
- submission tendency
- illegal move tendency
- weapon tendency
- comeback behavior

Presentation
- entrance animation
- theme music
- video package
- lighting
- pyro
- victory animation
- crowd reaction baseline
```

---

## 7.3 CAW best practices

### Separate appearance from moveset

A created wrestler should not lose move identity when changing clothes.

Correct structure:

```text
CAW identity
├── appearance profiles
├── moveset profile
├── AI profile
├── entrance profile
└── stat profile
```

### Support attire variants without cloning the whole wrestler

Bad:

```text
Each attire is a separate wrestler record.
```

Good:

```text
One wrestler record has multiple attire records.
```

```json
{
  "wrestlerId": "custom_001",
  "attireIds": [
    "custom_001_default",
    "custom_001_black_red",
    "custom_001_halloween",
    "custom_001_street_fight"
  ]
}
```

### Give AI profiles their own editor

Classic CAW systems often created strong appearance and moveset customization but weaker behavior editing. A modern design should fix that.

AI profile fields:

```json
{
  "aiProfileId": "ai_showman_heel_01",
  "phaseWeights": {
    "opening": {
      "taunt": 20,
      "lightGrapple": 30,
      "strike": 20,
      "stall": 20,
      "cheapShot": 10
    },
    "middle": {
      "mediumGrapple": 30,
      "cornerAttack": 15,
      "submission": 10,
      "taunt": 15,
      "dirtyMove": 15,
      "pinAttempt": 15
    },
    "finish": {
      "signature": 25,
      "finisher": 25,
      "pinAttempt": 20,
      "desperationCounter": 10,
      "dirtyPin": 10,
      "stall": 10
    }
  },
  "riskTolerance": 0.72,
  "reversalDiscipline": 0.58,
  "ropeAwareness": 0.81,
  "submissionPreference": 0.35,
  "weaponPreference": 0.44,
  "crowdPlaying": 0.86
}
```

---

# 8. Moveset Editor Design

## 8.1 Slot categories

A complete moveset editor should support these categories:

```text
Standing strike neutral
Standing strike directional
Weak front grapple
Strong front grapple
Weak rear grapple
Strong rear grapple
Grounded upper-body attack
Grounded lower-body attack
Grounded submission upper
Grounded submission lower
Running strike
Running grapple
Rope rebound attack
Corner front attack
Corner rear attack
Corner seated attack
Top rope attack
Diving attack to standing
Diving attack to grounded
Apron attack
Ringside grapple
Weapon strike
Taunt neutral
Taunt directional
Signature front
Signature rear
Signature running
Signature corner
Finisher front
Finisher rear
Finisher grounded
Finisher diving
```

## 8.2 Slot schema

```json
{
  "movesetId": "moveset_custom_001",
  "slots": {
    "frontWeakNeutral": "move_headlock_punch_01",
    "frontWeakUp": "move_snapmare_01",
    "frontWeakDown": "move_scoop_slam_01",
    "frontWeakLeftRight": "move_arm_wrench_01",
    "frontStrongNeutral": "move_headlock_01",
    "frontStrongUp": "move_vertical_suplex_01",
    "frontStrongDown": "move_small_package_01",
    "frontStrongLeftRight": "move_sidewalk_slam_01"
  }
}
```

## 8.3 Editor validation

The editor should prevent impossible assignments.

Validation examples:

```text
Do not allow a grounded-only move in a standing slot.
Do not allow a two-person move in a four-person-only sequence.
Do not allow a top-rope move unless wrestler can climb.
Do not allow a super-heavy lift move unless strength threshold is met, unless fallback exists.
Do not allow a submission finisher without a submission resolver profile.
Do not allow a pin combo if the result state cannot enter Pinning.
```

Validation object:

```json
{
  "slotId": "frontStrongUp",
  "moveId": "move_super_heavy_lift_01",
  "valid": false,
  "errors": [
    "Wrestler lift strength is below requirement.",
    "No fallback move assigned."
  ],
  "suggestedFallbacks": [
    "move_failed_lift_club_01",
    "move_body_shot_break_01"
  ]
}
```

---

# 9. Appearance, Attire, Texture, and Mask Systems

## 9.1 Appearance should be modular and layered

Classic modding practices around attire, textures, masks, and visual variants show that appearance must be component-based.

Recommended layer stack:

```text
base body
skin texture
face texture
hair mesh
facial hair mesh
mask base
mask details
face paint
torso clothing
arm accessories
wrist accessories
hand accessories
lower-body clothing
knee pads
boots
entrance props
```

## 9.2 Color palette model

```json
{
  "partId": "tights_07",
  "colorChannels": {
    "primary": "#111111",
    "secondary": "#cc0000",
    "trim": "#ffffff",
    "logo": "#ffcc00"
  }
}
```

## 9.3 Texture import rules

```text
Use fixed texture dimensions.
Warn on unsupported transparency.
Support mirrored and non-mirrored face textures.
Support preview under arena lighting.
Support automatic mipmap generation.
Keep original source texture separate from optimized runtime texture.
```

## 9.4 Best practice: never bake identity into attire

Bad:

```text
A mask texture changes the wrestler's stats.
```

Good:

```text
A mask texture belongs only to appearance.
Stats belong to stat profile.
AI belongs to AI profile.
Moves belong to moveset profile.
```

---

# 10. AI Personality and Simulation Logic

## 10.1 AI should use editable behavior weights

Classic simulation-heavy grappling games are remembered partly because characters can feel different without complex controls. A modern implementation should make that explicit.

AI profile categories:

```text
opening behavior
mid-match behavior
late-match behavior
comeback behavior
finisher behavior
pin behavior
submission behavior
rope behavior
corner behavior
weapon behavior
crowd behavior
stamina conservation
risk tolerance
reversal discipline
```

## 10.2 Behavior tree + weighted action model

Use a behavior tree for high-level decision-making and weighted tables for style.

```text
Root
├── If can win now → attempt finish
├── If trapped → escape
├── If opponent vulnerable → exploit position
├── If stamina low → recover / stall
├── If momentum high → use signature setup
├── If crowd cold → taunt or risky move
├── If rules allow weapon and personality allows → weapon path
└── Default → select weighted offense
```

Weighted table example:

```json
{
  "opening": {
    "lightGrapple": 35,
    "strike": 20,
    "taunt": 10,
    "stall": 10,
    "ropeWhip": 15,
    "heavyMove": 0,
    "submission": 10
  },
  "middle": {
    "mediumGrapple": 30,
    "strikeCombo": 20,
    "cornerAttack": 15,
    "groundAttack": 10,
    "submission": 10,
    "pinAttempt": 5,
    "taunt": 10
  },
  "finish": {
    "signature": 25,
    "finisher": 25,
    "pinAttempt": 20,
    "desperationCounter": 10,
    "submission": 10,
    "riskyMove": 10
  }
}
```

## 10.3 Best practice: AI should understand match phase

Do not let AI choose randomly from all moves. The AI should know whether the match is in:

```text
opening
control segment
comeback
escalation
false finish
finish
post-match
```

A powerbomb in the opening minute should be rare unless the wrestler style or match type supports it.

---

# 11. Reversal and Counter Design

## 11.1 Reversals should be branch points

A reversal should not simply cancel a move. It should branch into a new state.

Examples:

```text
front grapple reversed into rear control
lift reversed into escape behind attacker
slam reversed into small package
running attack reversed into leapfrog / counter strike
corner attack reversed into boot up
submission reversed into roll-up
finisher reversed into stunned attacker state
```

## 11.2 Reversal schema

```json
{
  "reversalId": "rev_lift_escape_behind_01",
  "validAgainstTags": ["lift", "suplex", "slam"],
  "invalidAgainstTags": ["unreverseable_story_event"],
  "window": {
    "phase": "lift",
    "startFrame": 22,
    "endFrame": 34
  },
  "requirements": {
    "defenderStaminaMin": 12,
    "defenderAwarenessMin": 40
  },
  "cost": {
    "stamina": 10,
    "momentum": 3
  },
  "successResult": {
    "attackerState": "StunnedStanding",
    "defenderState": "RearGrappleAdvantage"
  },
  "failureResult": {
    "damageMultiplier": 1.1
  }
}
```

## 11.3 Reversal best practices

```text
Give each move at least one logical reversal window.
High-tier moves may have fewer or harder windows.
Fresh defenders should reverse more easily than exhausted defenders.
Repeated reversal attempts should cost stamina.
Failed reversals should matter.
Successful reversals should create advantage, not reset to neutral every time.
```

---

# 12. Rope and Ring Position Systems

## 12.1 Ropes are a battle mechanic

The ring is not just a rectangle. It is a combat grid with tactical zones:

```text
center ring
near ropes
touching ropes
against ropes
corner
turnbuckle
apron
ringside
entrance side
hard camera side
```

## 12.2 Rope state model

```json
{
  "ropeState": {
    "nearRopes": true,
    "touchingRopes": false,
    "ropeBreakEligible": false,
    "reboundVelocity": 0,
    "entangled": false
  }
}
```

## 12.3 Rope interaction rules

```text
Pins check rope proximity.
Submissions check rope reach.
Irish whips transition into rebound state.
Rope leaning enables rope strikes.
Illegal rope chokes trigger referee count.
Springboards require apron or rope state.
No-break rules should disable the break, not the detection.
```

Best practice:

> Keep rope detection active even when the ruleset ignores rope breaks. This allows commentary, crowd, AI, and story logic to react.

---

# 13. Pin and Submission Systems

## 13.1 Pins should be stateful

Pin flow:

```text
PinStart
↓
RefereePositionCheck
↓
CountOne
↓
KickoutWindowOne
↓
CountTwo
↓
KickoutWindowTwo
↓
CountThree or Escape
```

Pin pressure calculation:

```text
pinPressure =
    moveImpact
  + attackerMomentum
  + defenderDamage
  + defenderFatigue
  + surpriseBonus
  + finisherBonus
  - defenderResilience
  - ropeEscapeChance
  - earlyMatchResistance
```

## 13.2 Submissions should combine pressure, escape, and position

Submission flow:

```text
HoldApplied
↓
PressureBuild
↓
DefenderEscapeInput
↓
RopeCrawlCheck
↓
Tap / Escape / RopeBreak / Counter
```

Submission data:

```json
{
  "submissionId": "leg_lock_01",
  "targetLimb": "legs",
  "basePressure": 12,
  "escapeDifficulty": 0.45,
  "ropeCrawlAllowed": true,
  "counterAllowed": true,
  "attackerStaminaDrainPerSecond": 2,
  "defenderStaminaDrainPerSecond": 4
}
```

---

# 14. Complexity Management Rules

## 14.1 Complexity should live in data, not branching code

Bad:

```text
if wrestler is X and move is Y and match is Z...
```

Good:

```text
traits + tags + ruleset modifiers + resolver output
```

## 14.2 Use tags everywhere

Move tags:

```text
strike
grapple
weak
strong
special
finisher
signature
lift
slam
suplex
driver
submission
pin_combo
rope
corner
running
diving
high_risk
dirty
weapon
comedy
technical
power
```

Wrestler tags:

```text
technical
powerhouse
high_flyer
brawler
showman
monster
underdog
cheater
submission_specialist
hardcore
```

Ruleset tags:

```text
standard
no_rope_breaks
falls_count_anywhere
submission_only
no_disqualification
weapons_allowed
cage
battle_royal
elimination
```

## 14.3 Resolvers should read tags

Example:

```json
{
  "ruleId": "powerhouse_resists_lift_when_fresh",
  "conditions": [
    "defender.hasTag:powerhouse",
    "defender.staminaState:fresh",
    "move.hasTag:lift"
  ],
  "effects": [
    "increaseReversalChance:0.15",
    "increaseAttackerStaminaCost:5"
  ]
}
```

---

# 15. Custom Match Rules

## 15.1 Match types should be rule overlays

Do not duplicate the whole match engine for every stipulation.

Ruleset object:

```json
{
  "rulesetId": "falls_count_anywhere",
  "baseRuleset": "standard_singles",
  "overrides": {
    "pinLegalZones": ["ring", "ringside", "stage", "crowd"],
    "countOutEnabled": false,
    "ropeBreakEnabled": false,
    "disqualificationEnabled": false
  }
}
```

Another example:

```json
{
  "rulesetId": "submission_only",
  "baseRuleset": "standard_singles",
  "overrides": {
    "pinfallEnabled": false,
    "submissionEnabled": true,
    "koEnabled": false,
    "validWinConditions": ["submission"]
  }
}
```

Best practice:

> Match type = base rule set + overrides.

---

# 16. Debugging and Tooling

A modular wrestling engine needs aggressive debugging tools.

## 16.1 Required debug panels

```text
Current wrestler state
Current opponent state
Current move slot
Resolved move ID
Current animation recipe
Current frame event
Valid reversal windows
Stamina values
Momentum values
Pin pressure
Submission pressure
Rope break eligibility
Rule overrides active
AI selected intent
AI decision reason
Invalid move assignment warnings
```

## 16.2 Move sandbox

The move sandbox should allow:

```text
spawn two wrestlers
choose position
choose move
choose defender size
choose ring zone
force stamina value
force momentum value
enable / disable reversal
preview frame events
export debug log
```

Without this, move creation will become guesswork.

---

# 17. Recommended File Structure

```text
/src
  /battle
    BattleEngine.ts
    InputInterpreter.ts
    IntentResolver.ts
    WrestlerStateMachine.ts
    PositionSystem.ts
    MoveSlotResolver.ts
    MoveExecutionSystem.ts
    ReversalSystem.ts
    DamageSystem.ts
    StaminaSystem.ts
    MomentumSystem.ts
    PinSystem.ts
    SubmissionSystem.ts
    RopeSystem.ts
    RuleSystem.ts
    RefereeSystem.ts
    CrowdSystem.ts
  /caw
    CAWSystem.ts
    AppearanceSystem.ts
    AttireSystem.ts
    MovesetEditorSystem.ts
    AIProfileEditorSystem.ts
  /animation
    AnimationRecipeSystem.ts
    AnimationEventBridge.ts
    AnimationValidator.ts
  /ai
    AIController.ts
    BehaviorTree.ts
    WeightedDecisionTable.ts
  /data
    MoveDatabase.ts
    WrestlerDatabase.ts
    RulesetDatabase.ts
    AnimationRecipeDatabase.ts
  /debug
    BattleDebugPanel.ts
    MoveSandbox.ts
    CAWValidationPanel.ts
/data
  /moves
  /moveSlots
  /animationRecipes
  /wrestlers
  /caws
  /attires
  /aiProfiles
  /rulesets
  /traits
```

---

# 18. Minimum Viable Implementation Plan

## Phase 1: Core battle loop

```text
1. Wrestler state machine
2. Position detection
3. Standing strike
4. Front grapple attempt
5. Weak / strong grapple slots
6. Move execution through data
7. Damage, stamina, and momentum
8. Basic reversal window
9. Grounded result state
10. Pin attempt
```

## Phase 2: Move editor foundation

```text
1. Move JSON schema
2. Animation recipe JSON schema
3. Slot assignment editor
4. Move validation
5. Move sandbox
6. Export/import custom moves
```

## Phase 3: CAW foundation

```text
1. CAW identity data
2. Appearance part selection
3. Attire variants
4. Moveset assignment
5. Stat profile
6. AI profile
7. Save/load custom wrestler
```

## Phase 4: Advanced systems

```text
1. Submissions
2. Rope breaks
3. Match rule overrides
4. Corner moves
5. Running rebound moves
6. Diving moves
7. Finishers and signatures
8. AI simulation tuning
```

---

# 19. Design Principles Extracted from Modding Practice

## 19.1 Modders need stable data boundaries

The easier something is to isolate, the easier it is to mod.

Good boundaries:

```text
Move data separate from animation data.
Animation data separate from wrestler data.
Wrestler data separate from attire data.
Attire data separate from AI data.
Ruleset data separate from move data.
```

## 19.2 Modding reveals where the original engine is too rigid

Common pain points in legacy modding:

```text
hardcoded roster limits
hardcoded attire limits
hardcoded profile data
manual hex editing
animation replacement without visual validation
AI values hidden from the editor
move compatibility problems
texture mirroring limitations
```

Modern response:

```text
No hardcoded roster limit in logic.
Attires as data arrays.
Profiles as editable JSON.
Move editor with preview.
AI editor exposed in UI.
Texture tools with validation.
Animation recipes with compatibility checks.
```

## 19.3 The editor is part of the game design

For a wrestling game, editors are not bonus features. They are core systems.

Required editors:

```text
CAW editor
moveset editor
move creator
attire editor
AI profile editor
ruleset editor
arena zone editor
entrance editor
```

If those editors are weak, the game will feel smaller no matter how good the base roster is.

---

# 20. Archived Generic Codex / Claude Code Prompt

> This prompt is retained as part of the research record. It is not approved
> as a task specification for this repository.

```text
Build a modular pro wrestling battle system inspired by classic console grappling-game design principles, but do not reference any real licensed game, engine, company, or branded title in comments, filenames, UI text, or documentation.

The system must be data-driven.

Create these modules:

1. BattleEngine
2. WrestlerStateMachine
3. PositionSystem
4. InputInterpreter
5. IntentResolver
6. MoveSlotResolver
7. MoveDatabase
8. MoveExecutionSystem
9. AnimationRecipeSystem
10. ReversalSystem
11. DamageSystem
12. StaminaSystem
13. MomentumSystem
14. PinSystem
15. SubmissionSystem
16. RopeSystem
17. RuleSystem
18. CAWSystem
19. AppearanceSystem
20. MovesetEditorSystem
21. AIProfileSystem
22. MoveSandbox

Use JSON data for:

- wrestlers
- CAWs
- attires
- moves
- move slots
- animation recipes
- traits
- AI profiles
- rulesets

Requirements:

- Moves are assigned to context-sensitive slots.
- Slots are selected by input + wrestler state + opponent state + position.
- Moves use animation recipes with attacker and defender tracks.
- Animation recipes expose frame events such as sync, impact, release, and complete.
- Reversals are branch points with timing windows.
- CAWs separate identity, appearance, attire, moveset, stats, AI, entrance, and victory data.
- AI profiles are editable weighted behavior tables.
- Match types are rule overlays, not duplicated engines.
- Rope detection remains active even when rope breaks are disabled.
- Include a debug inspector that shows current state, selected slot, selected move, active rule overrides, and valid reversal windows.

First deliver:

- folder structure
- TypeScript interfaces
- sample JSON files
- a working front-grapple example
- a working CAW example
- a working custom move example
- a test sandbox that executes one move and prints the resulting states
```

---

# 21. Source Notes

The research behind this document used public materials from these categories:

1. Public move-list guides for classic console grappling games.
   - Useful for studying context-sensitive move slot structure, weak/strong grapple tiers, directional move binding, rear grapple slots, ground slots, running moves, and special move assignment.

2. Public CAW guides for classic console grappling games.
   - Useful for studying identity fields, body scaling, attire components, move assignment categories, entrance presentation, and appearance-part organization.

3. Public modding tutorials about moveset editing.
   - Useful for understanding how move assignments were edited externally, why visual tools matter, and how slot-based move edits can be separated from wrestler identity.

4. Public modding tutorials about animation splicing and move hacking.
   - Useful for abstracting modern move creation into animation segments, sync frames, impact events, defender sell tracks, and branch points.

5. Public community discussions about attire expansion, roster expansion, texture editing, and custom behavior values.
   - Useful for identifying common pain points: hardcoded limits, hidden AI values, tedious profile editing, mirrored texture constraints, and fragile manual editing workflows.

6. General game architecture sources on state machines.
   - Used to support hierarchical state-machine recommendations and clean state transition design.

7. General modular game architecture sources on data-driven assets.
   - Used to support separating reusable gameplay data from runtime controller logic.

8. General AI architecture sources on behavior trees.
   - Used to support behavior trees plus weighted decision tables for wrestler AI.

---

# 22. Implementation Doctrine

Use this as the final design filter:

```text
Can this feature be represented as data?
Can this feature be edited without touching battle code?
Can this feature be previewed in a sandbox?
Can this feature be validated before runtime?
Can this feature be combined with other features without special-case code?
```

If the answer is no, the system is not modular enough.

The battle system should be simple at the input layer, deep at the resolver layer, expressive at the data layer, and inspectable at the tooling layer.
