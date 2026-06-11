# Pro Wrestling Battle System Research Notes: Modularity and Complexity

> **Repository note:** This document is informational background only. It is not
> an implementation guide, specification, roadmap, source of truth, or instruction
> to change the current game. Some recommendations are speculative, may conflict
> with the existing prototype, and have not been accepted as project decisions.
> Current behavior is defined by the code and the tracked documentation under
> `Documentation/`. Any idea from this file requires a separate design decision
> before implementation. Appropriate findings may be adopted after they are
> evaluated against this game's actual architecture and goals.

**Purpose:** This preserved research artifact collects design opinions,
architecture examples, data schemas, source notes, implementation-oriented
ideas, and a generic AI build prompt. Their presence records what was
researched; it does not adopt them for this repository.

**Research topic:** A possible pro wrestling game model built around **state,
position, momentum, reversals, rules, and match drama**, rather than a generic
fighting-game attack list.

---

## 0. Source Index

The following sources informed the design recommendations in this document.

### Wrestling-game specific sources

1. **Fire Pro Wrestling World Grappling Guide, GameFAQs**  
   URL: https://gamefaqs.gamespot.com/ps4/206703-fire-pro-wrestling-world/faqs/76355/grappling-with-your-opponent  
   Relevant point: Fire Pro uses a timing-based grapple system. Wrestlers automatically grapple on contact; both have an opportunity to win; button mashing is punished.

2. **Fire Pro Wrestling World CPU Logic Guide, Steam Community**  
   URL: https://steamcommunity.com/sharedfiles/filedetails/?id=2838953982  
   Relevant point: Fire Pro-style simulated matches rely heavily on CPU logic percentages and situational behavior tuning.

3. **Fire Pro CPU Logic Guide, SCFL Fire Pro**  
   URL: https://www.scflfirepro.com/guide-cpu-logic-in-fire-pro-wrestling-wold/  
   Relevant point: CPU logic is a deep behavior-tuning system that can make simulated wrestlers behave differently without unique code for every wrestler.

4. **WWE 2K26 Patch Notes, 2K Support**  
   URL: https://support.wwe2k.com/hc/en-us/sections/48015560292115-Patch-Notes  
   Relevant point: Modern wrestling games frequently require tuning to stamina, reversal, and pacing systems. This supports the recommendation to keep stamina and reversal logic modular instead of tightly coupled.

### General combat-design sources

5. **The Logic Behind Violence: A Primer on Combat System Design, Game Developer**  
   URL: https://www.gamedeveloper.com/design/the-logic-behind-violence-a-primer-on-combat-system-design  
   Relevant point: Combat systems should begin with the intended power fantasy, then use mechanics to reinforce that fantasy.

6. **Game Combat Design, Mechanics and Systems Guide, GameDesignSkills**  
   URL: https://gamedesignskills.com/game-design/combat-design/  
   Relevant point: Combat design should be structured around clear combat verbs, player feedback, enemy response, risk, reward, and satisfaction.

### Architecture / modularity sources

7. **State, Game Programming Patterns**  
   URL: https://gameprogrammingpatterns.com/state.html  
   Relevant point: State machines organize complex behavior by enforcing constrained states and transitions, but they can become rigid if overused or flattened.

8. **Three Ways to Architect Your Game with ScriptableObjects, Unity**  
   URL: https://unity.com/how-to/architect-game-code-scriptable-objects  
   Relevant point: Data objects can help keep gameplay code easier to change, debug, and reuse.

9. **Separate Game Data and Logic with ScriptableObjects, Unity**  
   URL: https://unity.com/how-to/separate-game-data-logic-scriptable-objects  
   Relevant point: Separating data from logic is useful for configurable gameplay systems.

10. **Create Modular Game Architecture with ScriptableObjects, Unity**  
    URL: https://unity.com/resources/create-modular-game-architecture-scriptableobjects-unity-6  
    Relevant point: Project-level data assets support modular architecture and reusable gameplay definitions.

### AI behavior sources

11. **A Survey of Behavior Trees in Robotics and AI, arXiv**  
    URL: https://arxiv.org/abs/2005.05842  
    Relevant point: Behavior trees originated as a modular AI tool in computer games and became useful because finite state machines scale poorly for complex, reusable behavior.

12. **Comparison Between Behavior Trees and Finite State Machines, arXiv**  
    URL: https://arxiv.org/html/2405.16137v1  
    Relevant point: Behavior trees and finite state machines can be compared on modularity, readability, reactivity, and design complexity.

13. **Behavior Trees in Robot Control Systems, arXiv**  
    URL: https://arxiv.org/abs/2203.13083  
    Relevant point: Behavior-tree modularity allows components to be developed, debugged, tested, and extended separately.

---

## 1. Core Thesis

A pro wrestling battle system should not be built like a normal fighting game with wrestling flavor layered on top.

Wrestling has different priorities:

1. **Position matters more than raw attack selection.**
2. **Momentum matters more than health.**
3. **The battle system must support both winning and putting on a good match.**
4. **Reversals, fatigue, rope breaks, crowd reactions, pins, submissions, and character style should all be modular systems, not hardcoded exceptions.**
5. **Complexity should live in data and state rules, not in giant move-specific scripts.**

The strongest design direction is a **modular, state-driven, data-authored battle engine** where each wrestler, move, match type, arena object, and special mechanic is composed from reusable rules.

---

## 2. Wrestling Game Lessons

### 2.1 Fire Pro Wrestling: Timing, Pacing, and CPU Logic

Fire Pro’s key lesson is that a wrestling game does not need an oversized combo system to create depth. Its grappling model is built around:

- timing
- pacing
- wrestler logic
- stamina
- move sequencing
- situational decision-making

In Fire Pro, wrestlers collide to initiate a grapple, then both have an opportunity to win the exchange. Button mashing is punished. This implies that complexity can come from **when to engage** and **what state the match is in**, not simply from button count.

**Design takeaway:** Grappling should be a state contest, not just an attack button.

### 2.2 WWE 2K: Stamina, Reversals, and Tuning Pressure

Modern WWE 2K games show how sensitive stamina and reversal tuning can be. Patch notes for WWE 2K26 include stamina and reversal adjustments, which supports the larger design point: stamina, reversals, fatigue, and pacing should not be one tangled system.

**Design takeaway:** Reversal logic should be a modular rule stack:

```text
Can attempt reversal?
↓
What reversal type?
↓
What timing/input challenge?
↓
What stamina or momentum cost?
↓
What animation branch?
↓
What resulting advantage state?
```

### 2.3 General Combat Design: Define the Fantasy First

Combat design should begin with the intended fantasy. For a wrestling game, the fantasy is not merely “beat the opponent.” It is:

- survive punishment
- control the ring
- build crowd energy
- hit signature moments
- escape disaster at the last second
- use character style to create drama
- win through pin, submission, knockout, count-out, DQ, escape, stipulation, or story condition

**Design takeaway:** The battle system needs multiple success axes, not just HP depletion.

---

## 3. Recommended Architecture

Use this high-level architecture:

```text
Match Engine
├── Rules Engine
├── Wrestler State Machine
├── Position / Ring System
├── Grapple System
├── Strike System
├── Move Resolver
├── Reversal / Counter System
├── Damage / Fatigue / Momentum System
├── Crowd / Drama System
├── Pin / Submission / Escape System
├── AI Logic System
├── Animation Event System
└── Data Registry
```

The **Match Engine** coordinates systems. It should not know the exact behavior of every move.

---

## 4. State Machine Foundation

State machines are useful because they constrain complex behavior into explicit states and transitions. However, a flat state machine will become unmanageable for wrestling.

Use a **hierarchical state machine**.

### 4.1 Top-Level Wrestler States

```text
Neutral
Striking
Grappling
Grappled
Grounded
Stunned
Running
Cornered
OnRopes
OnApron
Climbing
Diving
Pinning
Pinned
Submitting
Submitted
Recovering
OutsideRing
WeaponInteraction
SpecialAbility
```

### 4.2 Nested Grappling States

```text
Grappling
├── LockupStart
├── GrappleContest
├── AdvantageChosen
├── MoveStartup
├── MoveExecution
├── Impact
├── Sell / Bump
└── Recovery
```

### 4.3 Nested Corner States

```text
Cornered
├── LeaningBack
├── SeatedCorner
├── TreeOfWoe
├── TopTurnbuckleSetup
├── AvalancheMoveSetup
└── EscapeAttempt
```

### 4.4 Why This Matters

If corner moves, rope moves, apron moves, and grounded moves are hardcoded as attack exceptions, the combat system will collapse under its own complexity.

Instead, each move should ask:

```text
What state is the attacker in?
What state is the defender in?
What ring zone are they in?
What ruleset is active?
What tags apply?
What transitions are legal?
```

---

## 5. Data-Driven Move Design

Every move should be data first.

Do not write hundreds of one-off move scripts. Define moves as data objects consumed by resolvers.

### 5.1 Move Definition Schema

```json
{
  "id": "front_grapple_suplex_basic",
  "displayName": "Snap Suplex",
  "category": "grapple",
  "positionRequired": "front_grapple",
  "attackerStateRequired": ["standing"],
  "defenderStateRequired": ["standing"],
  "ringContextAllowed": ["center_ring", "near_ropes"],
  "moveTier": "medium",
  "startupFrames": 18,
  "activeFrames": 12,
  "recoveryFrames": 24,
  "damage": {
    "head": 4,
    "body": 8,
    "back": 10
  },
  "staminaCost": 8,
  "momentumGain": 6,
  "crowdImpact": 3,
  "reversalWindows": [
    {
      "phase": "startup",
      "type": "grapple_counter",
      "difficulty": 0.45
    },
    {
      "phase": "lift",
      "type": "mid_move_escape",
      "difficulty": 0.25
    }
  ],
  "resultState": {
    "attacker": "standing",
    "defender": "grounded_faceup"
  },
  "tags": ["suplex", "technical", "back_damage"]
}
```

### 5.2 Why Data-Driven Moves Matter

Data-driven moves allow:

- fast move tuning
- easier testing
- reusable move tags
- shared reversal logic
- rule overrides by match type
- AI selection based on tags
- easier modding
- easier future create-a-wrestler tools

---

## 6. Complexity Model: Layered, Not Tangled

Wrestling games contain many interacting systems:

- stamina
- health
- limb damage
- momentum
- crowd heat
- reversals
- match phase
- rope breaks
- illegal actions
- managers/interference
- weapon legality
- pin strength
- submission pressure
- recovery speed
- character style
- AI personality

Do not let every system directly modify every other system.

Use this layered model:

```text
Input Layer
↓
Intent Layer
↓
Legality Layer
↓
Contest Layer
↓
Resolution Layer
↓
Consequences Layer
↓
Presentation Layer
```

### 6.1 Example: Player Presses Grapple Near Ropes

```text
Input Layer:
Player presses grapple.

Intent Layer:
System creates Intent: "front grapple attempt."

Legality Layer:
Checks if both wrestlers are in valid states.

Contest Layer:
Determines lockup winner based on timing, stamina, size, skill, surprise, fatigue.

Resolution Layer:
Winner chooses move from valid move list.

Consequences Layer:
Applies damage, stamina cost, position change, crowd response.

Presentation Layer:
Plays animation, camera shake, commentary cue, sound effect.
```

This is cleaner than putting every rule into the move button.

---

## 7. Grappling System Best Practice

Grappling should be the heart of the wrestling battle system.

### 7.1 Recommended Grapple Flow

```text
Approach
↓
Contact / Lockup
↓
Timing or Advantage Contest
↓
Winner gets control state
↓
Move tier selected
↓
Reversal opportunity
↓
Move resolves
↓
Position changes
```

### 7.2 Move Tiers

| Tier | Example | Match Phase |
|---|---|---|
| Light | arm drag, body slam, snapmare | early match |
| Medium | suplex, DDT, backbreaker | mid match |
| Heavy | powerbomb, piledriver, superplex | late match |
| Signature | named character move | high momentum |
| Finisher | match-ending threat | peak drama |

### 7.3 Pacing Rule

Early-match heavy moves should either be:

- unavailable
- easy to reverse
- extremely stamina-expensive
- low-impact because the opponent is too fresh

This preserves wrestling pacing.

---

## 8. Momentum System

Momentum should replace traditional “mana.”

Momentum governs:

- signature availability
- finisher availability
- comeback ability
- crowd boost
- reversal strength
- desperation kickout
- taunt payoff
- comeback sequences

### 8.1 Momentum Gain Sources

```text
+ successful grapple
+ clean strike combo
+ reversal
+ rope escape
+ taunt after advantage
+ risky dive connects
+ crowd-favorite behavior
+ surviving a pin
+ story-specific rivalry moment
```

### 8.2 Momentum Loss Sources

```text
- missed high-risk move
- reversed signature
- failed pin attempt
- weapon use in clean match
- being stunned
- illegal action caught by referee
```

### 8.3 Design Rule

Momentum should represent **control of the match narrative**, not just a super meter.

---

## 9. Stamina System

Stamina should prevent spam without preventing play.

### 9.1 Good Stamina Effects

Stamina should affect:

- running speed
- lift ability
- recovery time
- reversal window size
- pin escape difficulty
- submission resistance
- move tier availability

### 9.2 Bad Stamina Design

```text
No stamina = no playing.
```

### 9.3 Better Stamina Design

```text
Low stamina = weaker options, slower recovery, higher risk.
```

### 9.4 Recommended Stamina States

```text
Fresh
Breathing
Tired
Winded
Exhausted
Adrenaline Surge
```

### 9.5 Adrenaline Surge

A tired wrestler should sometimes get a short dramatic window. Wrestling needs comeback bursts. Exhaustion should not only be a punishment state.

---

## 10. Reversal System: Modular Rule Stack

Reversals are one of the hardest systems to get right.

Bad reversal design causes:

- constant interruption
- no agency while getting beaten
- random-feeling outcomes
- animation desync
- unbeatable AI
- finisher spam
- player frustration

### 10.1 Reversal Object Structure

```json
{
  "id": "mid_lift_counter",
  "triggerPhase": "lift",
  "validAgainstTags": ["power", "suplex", "slam"],
  "invalidAgainstTags": ["finisher_locked"],
  "inputType": "timed_button",
  "baseDifficulty": 0.35,
  "cost": {
    "stamina": 12,
    "momentum": 4
  },
  "successResult": "attacker_staggered_defender_behind",
  "failurePenalty": "increased_damage_10_percent"
}
```

### 10.2 Reversal Types

| Type | Use |
|---|---|
| Strike counter | punch catch, duck, parry |
| Grapple counter | arm wringer, go-behind, shove-off |
| Mid-move counter | escape lift, sunset flip, roll-through |
| Ground counter | trip, leg sweep, small package |
| Rope counter | hold ropes, rebound reversal |
| Corner counter | boot up, shove from turnbuckle |
| Finisher counter | rare, high drama |
| Desperation counter | low stamina, high risk |

### 10.3 Rule

Do not use one generic “reverse” button internally for everything.

Even if the player presses the same button, the engine should resolve different reversal modules based on:

```text
move phase + position + stamina + style + move tags + match phase
```

---

## 11. Rope Mechanics

Ropes are not scenery. Ropes are a combat system.

### 11.1 Rope States

```text
NearRopes
TouchingRopes
AgainstRopes
Rebounding
HungOnRopes
Entangled
OnApron
ThroughRopes
RopeBreakEligible
```

### 11.2 Rope Mechanics

| Mechanic | Function |
|---|---|
| Rope break | breaks pins/submissions |
| Irish whip | transitions to running/rebound state |
| Rebound attack | clothesline, back body drop, leapfrog |
| Rope escape | defensive shove or hold |
| Illegal choke | referee count |
| Springboard | high-risk offense |
| Apron transition | outside/inside boundary |
| Ring-out | count-out and weapon access |

### 11.3 Rope Break Rule Example

```text
If defender is pinned or submitted:
    if defender limb reaches rope collider:
        if match allows rope breaks:
            trigger rope break
        else:
            apply "no rope break frustration" crowd/story modifier
```

### 11.4 No Rope Breaks Rule

For a “No Rope Breaks” match, do not disable rope detection.

Instead:

```text
Detect rope break.
Do not honor it.
Trigger commentary, crowd reaction, frustration, or story consequence.
```

That keeps the system expressive.

---

## 12. Pin System

Pins should not be a simple health check.

### 12.1 Pin Difficulty Inputs

Pin difficulty should depend on:

```text
damage
limb condition
stamina
momentum
finisher impact
surprise factor
ring position
rope proximity
referee position
match phase
crowd energy
character resilience
```

### 12.2 Pin Formula Concept

```text
pinPressure =
    moveImpact
  + attackerMomentum
  + defenderDamage
  + defenderFatigue
  + surpriseBonus
  - defenderResilience
  - ropeProximityEscapeChance
  - earlyMatchResistance
```

### 12.3 Pin Types

| Pin Type | Use |
|---|---|
| Lateral press | normal pin |
| Hooked leg | stronger pin |
| Roll-up | surprise pin |
| Bridge pin | technical pin |
| Dirty pin | rope leverage |
| Post-finisher pin | high pressure |
| Desperation collapse pin | low stamina both |

### 12.4 Pin State Flow

A pin is a battle state, not a single calculation.

```text
PinStart
RefSlide
CountOne
CountTwo
KickoutWindow
RopeBreakCheck
CountThree / Escape
```

---

## 13. Submission System

Submission systems should be tactical, not just minigame-heavy.

Use three central concepts:

```text
Pressure
Escape Progress
Rope / Position Context
```

### 13.1 Submission Variables

```text
attackerTechnique
attackerStamina
targetLimbDamage
defenderStamina
defenderSubmissionDefense
ropeDistance
crowdEnergy
holdDuration
```

### 13.2 Submission Outcomes

```text
Tap out
Escape
Rope break
Referee break
Counter into pin
Counter into reversal
Pass out
Illegal hold count
```

### 13.3 Submission Tactical Layer

The tactical layer should be:

- apply limb damage first
- choose correct submission
- position away from ropes
- manage stamina
- respond to escape direction
- decide whether to release and reapply

---

## 14. Character Archetypes as Modular Modifiers

Every wrestler should be a bundle of traits, not a unique hardcoded class.

### 14.1 Archetype Table

| Archetype | Strengths | Weaknesses |
|---|---|---|
| Technician | reversals, submissions, chain wrestling | lower raw impact |
| Powerhouse | slams, lift moves, stun | slow recovery |
| High flyer | dives, springboards, crowd gain | high miss penalty |
| Brawler | strikes, weapons, street fight | weaker technical defense |
| Showman | taunts, crowd control, comeback | risky pacing |
| Monster | intimidation, no-sell, high damage | slower, vulnerable to speed |
| Cheater | dirty pins, distractions | referee risk |
| Underdog | kickouts, desperation counters | low base control |

### 14.2 Trait Example

```json
{
  "id": "technical_master",
  "effects": [
    { "stat": "grappleContest", "modifier": 0.12 },
    { "stat": "submissionPressure", "modifier": 0.15 },
    { "stat": "finisherDamage", "modifier": -0.05 }
  ]
}
```

---

## 15. AI: Behavior Trees for Match Psychology

Use:

- **state machines** for physical wrestler state
- **behavior trees** for decision-making

Behavior trees are well-suited to modular AI because they organize decision logic hierarchically and are easier to extend than large flat finite state machines.

### 15.1 AI Behavior Tree Concept

```text
Root
├── If can win now → attempt pin/submission
├── If opponent near ropes → drag away / rope attack
├── If momentum high → signature / finisher setup
├── If stamina low → stall / taunt / retreat / rest hold
├── If opponent grounded → ground offense
├── If opponent stunned in corner → corner sequence
├── If match stipulation allows weapons → consider weapon
├── If crowd cold → taunt / high-risk move
└── Default → grapple / strike / reposition
```

### 15.2 Fire Pro-Style CPU Logic Weights

Each wrestler should have AI weights by match phase.

```json
{
  "earlyMatch": {
    "lightGrapple": 40,
    "strike": 20,
    "taunt": 10,
    "heavyMove": 0,
    "restHold": 15,
    "ropeWhip": 15
  },
  "midMatch": {
    "mediumGrapple": 35,
    "strikeCombo": 20,
    "cornerAttack": 15,
    "submission": 10,
    "dive": 10,
    "pinAttempt": 10
  },
  "lateMatch": {
    "signature": 25,
    "finisher": 20,
    "pinAttempt": 20,
    "submission": 15,
    "desperationCounter": 10,
    "taunt": 10
  }
}
```

This makes wrestlers feel distinct without writing custom AI for each wrestler.

---

## 16. Match Phase System

Wrestling matches have phases. The system should know which phase the match is in.

```text
Opening
FeelingOut
HeatSegment
Comeback
Escalation
FalseFinish
Finish
PostMatch
```

### 16.1 Why Match Phase Matters

A body slam in the first 20 seconds should not mean the same thing as a body slam after two finishers.

The match phase should influence:

- move availability
- reversal chance
- crowd response
- pin chance
- AI risk tolerance
- stamina drain
- commentary
- camera intensity
- referee behavior

### 16.2 Example Phase Rules

```text
Opening:
Heavy moves are harder to land.
Pins are low threat.
Crowd reaction is moderate.

FalseFinish:
Pins are high drama.
Reversal windows shrink.
Crowd reaction spikes.
Desperation counters unlock.
```

---

## 17. Tags Everywhere

Tags keep the system modular.

### 17.1 Move Tags

```text
strike
grapple
suplex
slam
driver
submission
pin_combo
rope
corner
springboard
dirty
weapon
high_risk
finisher
signature
technical
power
comedy
desperation
```

### 17.2 State Tags

```text
standing
grounded
running
stunned
near_ropes
cornered
outside
legal_man
illegal_man
ref_distracted
```

### 17.3 Rule Example

```json
{
  "rule": "Powerhouses resist lift moves while fresh",
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

### 17.4 Avoid Hardcoding

Bad:

```text
if wrestler == BigGuy and move == Suplex and stamina > 80...
```

Good:

```text
if defender.hasTag(powerhouse) and move.hasTag(lift) and defender.staminaState == Fresh...
```

---

## 18. Recommended Battle-System Modules

### 18.1 Minimum Viable Version

Build these first:

```text
1. Wrestler state machine
2. Position/ring zones
3. Basic strike
4. Basic grapple contest
5. Light/medium/heavy move tiers
6. Stamina
7. Momentum
8. Reversal window
9. Pin attempt
10. Rope break
```

Do not start with:

- weapons
- managers
- tag teams
- ladders
- blood
- custom finishers
- table physics

Those will bury the core system.

### 18.2 Expanded Version

Add after the core feels good:

```text
11. Limb damage
12. Submissions
13. Match phases
14. Crowd heat
15. Signature/finisher system
16. Character traits
17. AI behavior weights
18. Referee logic
19. Illegal moves
20. Ringside/outside combat
```

### 18.3 Advanced Version

Only after the expanded system is stable:

```text
21. Tag team logic
22. Multi-man targeting
23. Managers/interference
24. Weapons
25. Tables/ladders/chairs
26. Blood/injury
27. Story objectives
28. Create-a-wrestler move editor
29. Custom match rules
30. Full simulation mode
```

---

## 19. Complexity Philosophy

Use this rule:

> Every mechanic must either create a new tactical decision, a new dramatic moment, or a new character distinction.

If it does not, cut it.

### 19.1 Bad Complexity

```text
Five different stamina bars.
Hidden reversal math.
Ten pin minigames.
Every move has unique bespoke logic.
```

### 19.2 Good Complexity

```text
Same grapple system supports suplexes, throws, submissions, roll-ups, rope moves, and finishers.
Same tag system supports character traits, move legality, AI preferences, and reversal rules.
Same match phase system affects pins, crowd, AI, and comeback logic.
```

---

## 20. Practical Implementation Model

### 20.1 Core Classes / Objects

```text
MatchController
WrestlerController
WrestlerStateMachine
MoveDatabase
MoveResolver
RuleEngine
GrappleResolver
ReversalResolver
DamageResolver
MomentumResolver
StaminaResolver
PinResolver
SubmissionResolver
RingPositionSystem
CrowdSystem
RefereeSystem
AIController
AnimationEventBridge
```

### 20.2 Move Resolution Pseudocode

```pseudo
function attemptMove(attacker, defender, moveIntent):
    context = buildContext(attacker, defender, match, ring)

    validMoves = MoveDatabase.findValid(moveIntent, context)

    selectedMove = chooseMove(attacker, validMoves, context)

    legalityResult = RuleEngine.checkLegality(selectedMove, context)
    if legalityResult.blocked:
        return failIntent(legalityResult.reason)

    contestResult = ContestResolver.resolve(attacker, defender, selectedMove, context)

    if contestResult.reversed:
        return ReversalResolver.apply(contestResult.reversal, context)

    result = MoveResolver.execute(selectedMove, context)

    DamageResolver.apply(result)
    StaminaResolver.apply(result)
    MomentumResolver.apply(result)
    CrowdSystem.react(result)
    StateMachine.applyResultStates(result)
    AnimationBridge.play(result.animation)
```

---

## 21. Pro Wrestling Battle Design Rules

### Rule 1: Position Before Move

The move list should come from position.

```text
front grapple
rear grapple
side grapple
ground head
ground legs
corner front
corner seated
near ropes
running rebound
apron
top rope
outside ring
```

Avoid giving the player a giant universal move list at all times.

### Rule 2: Moves Should Change Position

Every move should answer:

```text
Where does the attacker end?
Where does the defender end?
What state are they in?
How close are they to ropes/corner?
Who recovers first?
```

Position is more important than raw damage.

### Rule 3: Reversals Should Create New States

Bad:

```text
Opponent reverses. Move stops. Back to neutral.
```

Good:

```text
Opponent reverses powerbomb into sunset flip pin.
Opponent ducks lariat and ends behind attacker.
Opponent blocks suplex and transitions to front grapple advantage.
Opponent catches dive and both crash to ringside.
```

### Rule 4: Pins and Submissions Are Battle States

A pin is not just a result. It is a temporary combat state.

```text
PinStart
RefSlide
CountOne
CountTwo
KickoutWindow
RopeBreakCheck
CountThree / Escape
```

A submission is also a temporary combat state.

```text
HoldApplied
PressureBuild
EscapeDirection
RopeCrawl
CounterWindow
Tap / Break / Reverse
```

### Rule 5: Match Types Should Override Rules, Not Duplicate Systems

A “No Rope Breaks” match should not use a separate pin system.

It should apply a ruleset modifier:

```json
{
  "matchType": "no_rope_breaks",
  "overrides": [
    {
      "system": "ropeBreak",
      "behavior": "detected_but_not_honored"
    }
  ]
}
```

A Falls Count Anywhere match should not duplicate pin logic.

It should change pin location legality:

```json
{
  "pinLegalZones": ["ring", "ringside", "stage", "crowd_area"]
}
```

---

## 22. Data Objects

### 22.1 WrestlerData

```json
{
  "id": "wrestler_example_technician",
  "displayName": "Example Technician",
  "archetypes": ["technician", "underdog"],
  "baseStats": {
    "strength": 55,
    "speed": 70,
    "technique": 90,
    "stamina": 75,
    "resilience": 80,
    "charisma": 60
  },
  "traits": ["technical_master", "late_match_resilience"],
  "moveSet": {
    "standingStrike": ["chop", "forearm", "dropkick"],
    "frontGrappleLight": ["arm_drag", "snapmare"],
    "frontGrappleMedium": ["snap_suplex", "ddt"],
    "frontGrappleHeavy": ["brainbuster"],
    "signature": ["falling_star"],
    "finisher": ["final_word"]
  },
  "aiProfile": "technical_babyface"
}
```

### 22.2 TraitData

```json
{
  "id": "late_match_resilience",
  "displayName": "Late Match Resilience",
  "conditions": [
    "match.phase:FalseFinish",
    "self.staminaState:TiredOrWorse"
  ],
  "effects": [
    { "stat": "kickoutChance", "modifier": 0.10 },
    { "stat": "desperationReversalChance", "modifier": 0.08 }
  ]
}
```

### 22.3 RulesetData

```json
{
  "id": "standard_singles",
  "displayName": "Standard Singles Match",
  "pinLegalZones": ["ring"],
  "submissionLegalZones": ["ring"],
  "ropeBreaksEnabled": true,
  "countOutEnabled": true,
  "disqualificationEnabled": true,
  "weaponsLegal": false,
  "refereeRequiredForFall": true
}
```

### 22.4 No Rope Breaks RulesetData

```json
{
  "id": "no_rope_breaks",
  "displayName": "No Rope Breaks Match",
  "inherits": "standard_singles",
  "overrides": {
    "ropeBreaksEnabled": false,
    "ropeBreakDetectionEnabled": true,
    "onRopeBreakDetected": "crowd_reacts_but_ref_ignores"
  }
}
```

### 22.5 Falls Count Anywhere RulesetData

```json
{
  "id": "falls_count_anywhere",
  "displayName": "Falls Count Anywhere",
  "inherits": "standard_singles",
  "overrides": {
    "pinLegalZones": ["ring", "ringside", "stage", "crowd_area"],
    "submissionLegalZones": ["ring", "ringside", "stage", "crowd_area"],
    "countOutEnabled": false,
    "disqualificationEnabled": false
  }
}
```

---

## 23. Event System

Use events heavily. Wrestling is full of reactive systems.

### 23.1 Core Events

```text
OnMoveStarted
OnReversalWindowOpened
OnMoveConnected
OnBumpLanded
OnRopeTouched
OnPinStarted
OnCountOne
OnCountTwo
OnKickout
OnSubmissionApplied
OnTapOut
OnCrowdPop
OnFinisherReady
OnIllegalAction
OnRefereeDistracted
OnMatchPhaseChanged
```

### 23.2 Example Event Flow: Snap Suplex Into Pin

```text
OnGrappleContact
OnGrappleContestResolved
OnMoveStarted:snap_suplex
OnReversalWindowOpened:startup
OnMoveConnected:snap_suplex
OnBumpLanded:defender_grounded_faceup
OnMomentumChanged:attacker+6
OnCrowdReact:small_pop
OnPinStarted:lateral_press
OnRopeBreakCheck
OnKickout or OnCountThree
```

---

## 24. Archived Generic Build Prompt

> This prompt is retained as part of the research record. It is not approved
> for use against this repository and does not describe required work.

The original research artifact included the following generic prompt:

```text
We are designing a modular pro wrestling battle system.

Build the architecture first, not the full game.

Create a data-driven battle engine with these modules:

1. MatchController
2. WrestlerController
3. WrestlerStateMachine
4. MoveDatabase
5. MoveResolver
6. GrappleResolver
7. ReversalResolver
8. StaminaResolver
9. MomentumResolver
10. RingPositionSystem
11. PinResolver
12. RulesetEngine
13. CrowdSystem
14. AIController
15. AnimationEventBridge

The design must support:
- standing, grounded, corner, ropes, apron, outside-ring states
- light / medium / heavy grapple move tiers
- stamina cost
- momentum gain
- reversible move phases
- rope break detection
- pin attempts
- submission attempts
- match phase logic
- character traits
- AI behavior weights
- match rule overrides such as No Rope Breaks and Falls Count Anywhere

Do not hardcode individual wrestlers into combat logic.
Do not hardcode match types into move logic.
Use tags, reusable data objects, and rule checks.

First deliver:
- folder/file structure
- core TypeScript or JavaScript classes
- JSON schemas for moves, wrestlers, traits, and match rules
- one working example: front grapple → snap suplex → pin attempt → rope break or kickout

Architectural rules:
- Rules define what is legal.
- Position defines what is possible.
- State defines what is happening.
- Move data defines what can occur.
- Resolvers decide outcomes.
- Events trigger reactions.
- Traits modify behavior.
- AI chooses intent.
- Animation presents the result.
```

---

## 25. Bottom Line

Build the wrestling engine around:

```text
state
position
momentum
rules
reversals
match phase
crowd drama
```

Do not build it around generic attack buttons.

The correct modular hierarchy is:

```text
Rules define what is legal.
Position defines what is possible.
State defines what is happening.
Move data defines what can occur.
Resolvers decide outcomes.
Events trigger reactions.
Traits modify behavior.
AI chooses intent.
Animation presents the result.
```

That gives the system room to grow into finishers, submissions, rope mechanics, stipulations, managers, weapons, tag matches, and unique character gimmicks without collapsing into hardcoded nonsense.
