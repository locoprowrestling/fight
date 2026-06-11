# AKI Wrestling Games Research Ingestion Pack

**Scope:** Development, design, technical analysis, modding, move creation, CAW/edit systems, fan research, and technical lessons from the AKI wrestling lineage: Virtual Pro-Wrestling, WCW, and WWF N64/PS1-era wrestling games.

**Purpose:** Extract usable lessons for building a modern modular pro wrestling battle system, with emphasis on move creation, CAW systems, animation/data architecture, battle-system complexity, modding research, and long-term extensibility.

**Important limitation:** Publicly available primary-source development documentation is limited. The best available material comes from translated Hideyuki “Geta” Iwashita blog posts, current tool/decompilation projects, old GameShark/modding documentation, GameFAQs move/CAW guides, and fan reverse-engineering communities.

---

## High-value source index

### Developer / design perspective

#### Hideyuki “Geta” Iwashita translated blog: “Turning Pro Wrestling into a Game”
URL: https://melonbread.co.uk/turning-pro-wrestling-into-a-game-what-are-the-differences-between-other-sports-games-that-may-appear-similar-but-are-actually-different/

Key findings:
- Pro wrestling/fighting games are not ordinary sports games because they are not just rule recreations.
- In conventional sports games, rules and ball behavior define the system. In pro wrestling, the game must recreate a performed reality.
- Pro wrestling has a real-world referent made of bodies, timing, spectacle, audience expectation, and recognizable move language.
- The design problem is not only “who wins?” but “does this feel like pro wrestling?”

Applicable lessons:
- Build around context, spectacle, state, and momentum, not just damage.
- Do not copy a generic fighting-game combat loop.
- Rulesets should be modular because pro wrestling is full of exceptions, stipulations, rope behavior, referee discretion, dirty tactics, crowd rhythm, and match psychology.
- The game must support performance logic alongside competition logic.

#### Hideyuki “Geta” Iwashita translated blog: “The Ecstasy and Anxiety of the Chosen One”
URL: https://melonbread.co.uk/first-night-the-ecstasy-and-anxiety-of-the-chosen-one/

Key findings:
- Geta-san identifies himself as director of the Virtual Pro-Wrestling series and discusses the lineage into American licensed games.
- He describes the American titles as adaptations of the Virtual Pro-Wrestling foundation.
- The source confirms creative continuity between Japanese puroresu design and later U.S. wrestling games.

Applicable lessons:
- Treat the system as a wrestling-language engine, not a roster-specific engine.
- Licenses, names, promotions, and presentation changed, but the core play model survived because it was modular enough to be adapted.
- The engine’s strength was portability across wrestling cultures: Japanese puroresu, WCW spectacle, WWF sports-entertainment, and later broader fighting/wrestling hybrids.

---

### Current tooling and reverse-engineering

#### AKI-Club GitHub organization
URLs:
- https://github.com/AKI-Club
- https://aki-club.github.io/

Key findings:
- AKI-Club hosts preservation and tooling projects.
- Listed projects include VPW Studio, Virtual Pro-Wrestling 2 decompilation, archive handlers, text handlers, emulator scripts, menu background conversion tools, ROM identification tools, and palette tools.
- The AKI-Club page points to AKI Live, GenHex, Old Skool Reunion, Virtual Pro Wrestling 2 Dojo, and WldFb Archive Forum.

Applicable lessons:
- The original games are understood by modders as structured data containers: archives, file tables, text archives, palettes, move data, wrestler data, assets, animations.
- A modern spiritual successor should expose this structure intentionally instead of forcing fans to reverse-engineer it.
- Build first-party tooling early.

#### VPW Studio GitHub
URL: https://github.com/AKI-Club/VPWStudio

Key findings:
- VPW Studio is a C# tool for creating projects around AKI Corporation’s Nintendo 64 wrestling games.
- Supported titles include WCW vs. nWo World Tour, Virtual Pro-Wrestling 64, WCW/nWo Revenge, WWF WrestleMania 2000, Virtual Pro-Wrestling 2, and WWF No Mercy.
- Requires .NET Framework 4.7.2 and OpenGL 3 for 3D rendering-related tools.
- The project is incomplete but meaningful to the modding ecosystem.

Applicable lessons:
- Tooling should treat each game as a project with multiple data domains, not a single ROM blob.
- Version/region variants matter. Modern games should anticipate data compatibility, migrations, and versioned schemas.
- If building a game intended for mods, make the editor project-based with asset import/export, validation, and structured build output.

#### VPW Studio website
URL: https://vpw.ajworld.net/vpwstudio/

Key findings:
- VPW Studio is described as a ROM-hacking tool focused on AKI’s Virtual Pro-Wrestling series.
- Work began in 2018.
- Older modding relied heavily on GameShark codes and emulator-only plugin texture replacements.
- VPW Studio works directly on game data, allowing hacks to run on original hardware when internal data rules are followed.
- It supports multiple N64 titles and some prototype/pre-release builds.
- It requires Z64 ROM format.
- Not every game supports every portion of VPW Studio.
- PS1 support would require non-trivial UI/project/tooling differences.
- Def Jam support was declared out of scope because those games would need a purpose-built tool.

Applicable lessons:
- Direct data editing is superior to runtime patching for stable mods.
- Tooling must validate internal rules so creators do not build broken assets.
- Each platform/game format needs adapters, not assumptions.
- For a new game, avoid opaque data formats. Provide stable documented schemas.

#### Virtual Pro-Wrestling 2 decompilation project
URL: https://github.com/aki-club/vpw2

Key findings:
- The repository is a work-in-progress decompilation project.
- Requires a legally obtained Z64 ROM to extract assets because assets are not included.
- Its stated purposes are documenting game information for hackers and serving as a base for future hacks requiring non-trivial changes.
- Requires Linux, GNU make, MIPS binutils/compiler, gcc-multilib, and Python.

Applicable lessons:
- Serious modding requires code-level understanding, not just asset replacement.
- For a new wrestling game, separate engine code from content data so major changes do not require binary hacking.
- Provide a scripting layer for non-trivial behavior changes.
- Provide an official headless build/validate pipeline for tools and CI.

---

## Move hacking / move creation research

### No Mercy Library: “Master Move Mods”
URL: https://www.tapatalk.com/groups/no_mercy_library/master-move-mods-t47.html

Key findings:
- “Master move modifiers” are addresses the game reads to identify moves.
- These values use the same values as move animation modifiers, but include additional move stats such as damage, secondary animations, pins/submissions, reversal information, and save behavior for CAWs.
- The post warns not to edit CAWs with certain codes active because the game may read altered moves incorrectly, causing missing or wrong move data.
- The list shows address/value pairs where existing moves are replaced by other moves.

Applicable lessons:
- A move is not only an animation. It is a composite data object.
- At minimum, a move definition needs animation IDs, attacker animation track, defender animation track, damage values, target body regions, pin/submission linkage, reversal metadata, CAW editor eligibility, move category/slot compatibility, result states, and save/load safety.
- If move data and CAW save data are tightly coupled, hacks can corrupt created characters. Modern systems should isolate user-created content from engine-level move definitions.
- Move editor validation must check whether a move can be safely assigned to a CAW slot.

### Geocities No Mercy Hacking: “How to Hack Normal Moves”
URL: https://www.geocities.ws/no_mercy_hacking/learn_normal.html

Key findings:
- Old-school move hacking used GameShark and memory inspection.
- The guide describes move hacking as changing animations and splicing moves together.
- It gives an example of ending one animation at a specific point and switching into another animation.
- It distinguishes Player 1 and Player 2 animation tracks.
- It notes that high-flying moves, strikes, and taunts may only need Player 1 lines, while normal grapples need both attacker and receiver data.
- It warns that glitched splices may require freezing/stopping the animation before the glitch.
- Turnbuckle grapples use a different formula.

Applicable lessons:
- Move creation needs multi-actor timeline editing.
- A proper move editor should expose attacker track, defender track, optional partner track, optional referee/camera/crowd track, frame markers, contact frames, transition frames, cancel/reversal windows, physics/impact events, and result states.
- Different move classes need different templates: strike, taunt, front grapple, rear grapple, ground grapple, diving move, corner grapple, rope move, double-team move, weapon move.
- Do not pretend one move-creation workflow fits all categories.

### WldFb Archive Forum
URL: https://www.tapatalk.com/groups/wldfbarchiveforum/

Key findings:
- The archive includes categories for move hacks, moveset editor, new moves made by splicing animations, master move mods, tutorials/resources, submissions, and other specialized hacks.

Applicable lessons:
- Modding communities naturally organize around data domains: moves, textures, arenas, roster, logic, parameters, saves.
- Official game architecture should mirror that reality.
- Tooling should have discrete modules instead of one giant editor screen.

### GameFAQs move-list guides
URLs:
- WWF No Mercy Move List and Guide: https://gamefaqs.gamespot.com/n64/914112-wwf-no-mercy/faqs/9549
- WWF WrestleMania 2000 Move List and Guide: https://gamefaqs.gamespot.com/n64/199352-wwf-wrestlemania-2000/faqs/3678
- Virtual Pro-Wrestling 2 Move List and Guide: https://gamefaqs.gamespot.com/n64/576850-virtual-pro-wrestling-2-oudou-keishou/faqs/6901

Key findings:
- Move lists are organized by practical combat situation: specials, front grapple, back grapple, top rope moves, turnbuckle moves, running opponent moves, player running, ground moves, apron moves, and double-team moves.
- Specials are tied to spirit/special state and strong grapple context.
- Double-team moves are activated from specific positioning contexts such as front grapple, back grapple, sandwich grapple, and Irish whip grapple.
- VPW2 includes shoot fighting coverage and edit mode notes.

Applicable lessons:
- Move selection should be position/context-driven.
- CAW move editing should use the same taxonomy as runtime move selection.
- Special move should not be a disconnected super button. It should be a conditional upgrade inside a valid combat context.
- Double-team moves are not just two-player attacks; they are position-state-dependent move templates.
- The AKI lineage used a consistent move taxonomy across games, helping players transfer mastery.

---

## CAW / edit mode research

### WWF No Mercy Wiki: CAW
URL: https://wwfnomercy.fandom.com/wiki/CAW

Key findings:
- CAW options include name, stats, profile picture, theme music, entrance video, attire, moveset, fighting style, parameters, and allies/enemies.
- The game has 18 CAW slots, with additional storage via memory pak.

Applicable lessons:
- CAW is not only visual customization.
- A serious CAW system must include appearance, moves, entrance/presentation, stats, fighting style, AI profile, relationships, faction/alignment, match behavior, and save/export/share.
- Allies/enemies turns CAWs into part of a living roster, not isolated avatars.

### WWF No Mercy CAW guides, GameFAQs
URLs:
- https://gamefaqs.gamespot.com/n64/914112-wwf-no-mercy/faqs/9824
- https://gamefaqs.gamespot.com/n64/914112-wwf-no-mercy/faqs/9799
- https://gamefaqs.gamespot.com/n64/914112-wwf-no-mercy/faqs/14257

Key findings:
- Community CAW formulas list exact appearance parts, music, titantron/video, height, weight, required moves, accompanied-by settings, and fighting style parameters.
- Example fighting style fields include stance, ring entry, counter/reversal setting, speed, submission skills, Irish whip behavior, recovery rate, bleeding frequency, reaction to blood, endurance, jump distance, and weapon preference.

Applicable lessons:
- CAW should be a shareable recipe, not only a local save blob.
- CAW export should support compact formula/share code and full package export with assets.
- Fighting style should be broken into tunable modules.
- CAW UI should let the creator define how the wrestler behaves, not just how the wrestler looks.

### Virtual Pro-Wrestling 2 CAW FAQ, GameFAQs
URL: https://gamefaqs.gamespot.com/n64/576850-virtual-pro-wrestling-2-oudou-keishou/faqs/9925

Key findings:
- The guide recommends editing logic before moves because some characters have combos and shoot styles.
- This implies style/logic can constrain or affect move assignment.
- If logic is changed later, move editing may need to be redone.

Applicable lessons:
- In a modern CAW editor, style selection should come before move assignment.
- Move slots should be filtered based on fighting style, stance, size, body rig compatibility, and ruleset.
- The editor should warn when changing a base style would invalidate assigned moves.
- Better: changing style should offer automatic migration, conflict reports, and unresolved move slot warnings.

### VPW2 moveset location notes
URL: https://vpw.ajworld.net/vpw2/moveset_locations.txt

Key findings:
- Notes indicate that parameters appear before moveset data because the game needs to know whether a wrestler uses wrestling/combo/shoot style.
- The note compares VPW2 and No Mercy data storage and points out format differences.

Applicable lessons:
- Runtime needs high-level behavior/style metadata before it can interpret move data.
- A modern schema should put character archetype/style metadata before move slots.
- Move data should be validated against those fields.

---

## Technical interpretation of the AKI-style battle system

The AKI-style system is best understood as a contextual state machine with modular move tables.

The player does not open a giant move list. The game determines what actions are possible from the current state:

- standing neutral
- weak grapple
- strong grapple
- front grapple
- rear grapple
- grounded opponent
- corner
- ropes
- apron
- top rope
- running
- rebound
- weapon
- special state
- double-team context

Each context maps input directions/buttons to move slots.

Example modern data shape:

```json
{
  "context": "front_strong_grapple",
  "slots": {
    "neutral": "ddt_01",
    "up": "vertical_suplex_02",
    "down": "powerbomb_01",
    "left": "belly_to_belly_01",
    "right": "neckbreaker_02",
    "special": "character_finisher_front"
  }
}
```

Why this works:
- The player learns a small control vocabulary.
- The wrestler feels deep because context changes the move.
- CAW move editing is structured by slot.
- AI can reason by context and priority.
- Moves are modular assets, not custom input scripts.
- Special moves fit into wrestling position logic.
- The same system scales from jobbers to main-eventers.

Complexity is hidden in context, not input:
- timing
- positioning
- spirit/momentum
- move strength
- rope/corner/ring context
- opponent state
- reversal state
- stamina/damage
- style/logic settings
- match type
- multiplayer chaos

Modern lesson:
- Do not build complexity through command-input inflation.
- Build complexity through state-rich contextual resolution.

---

## Move creation best practices

### Treat moves as composite assets

```json
{
  "id": "snap_suplex_01",
  "name": "Snap Suplex",
  "category": "grapple",
  "subtype": "front_grapple",
  "strength": "medium",
  "styleTags": ["technical", "suplex"],
  "requiredContext": ["front_grapple", "standing_attacker", "standing_defender"],
  "attackerAnimation": "anim_attacker_snap_suplex_01",
  "defenderAnimation": "anim_defender_snap_suplex_01",
  "timeline": {
    "startupFrame": 0,
    "lockFrame": 12,
    "liftFrame": 25,
    "impactFrame": 48,
    "recoveryStart": 60,
    "endFrame": 82
  },
  "events": [
    { "frame": 12, "type": "lock_grapple" },
    { "frame": 25, "type": "lift" },
    { "frame": 48, "type": "impact", "damageProfile": "back_medium" },
    { "frame": 50, "type": "camera_shake", "intensity": 0.25 },
    { "frame": 52, "type": "crowd_reaction", "value": 3 }
  ],
  "reversalWindows": [
    { "startFrame": 6, "endFrame": 15, "type": "grapple_counter", "difficulty": 0.45 },
    { "startFrame": 25, "endFrame": 31, "type": "mid_lift_escape", "difficulty": 0.25 }
  ],
  "resultStates": {
    "attacker": "standing",
    "defender": "grounded_face_up"
  },
  "pinLink": null,
  "submissionLink": null,
  "cawEligible": true
}
```

### Separate animation from move logic

Old move hacking often changed animation IDs directly. That worked, but caused glitches because animations, move stats, receiver tracks, pins/submissions, reversals, and CAW saves were linked.

Modern rule:
- Animation clips are media.
- Move definitions are gameplay objects.
- Move assignments are character data.
- CAW saves reference move IDs, not raw animation tracks.
- Reversal definitions are separate but linked.
- Pins/submissions are outcome modules, not animation-only hacks.

### Use move templates

```text
MoveTemplate
├── Strike
├── Running Strike
├── Front Grapple
├── Rear Grapple
├── Ground Head
├── Ground Legs
├── Corner Front
├── Corner Seated
├── Top Rope Dive
├── Springboard
├── Apron Attack
├── Weapon Strike
├── Submission
├── Pin Combo
├── Double Team
└── Taunt
```

Each template defines required participants, required states, animation tracks, event requirements, valid reversal windows, valid result states, and CAW slot compatibility.

### Timeline events are mandatory

A move editor should expose frame/timeline events:

```text
startup
contact
grab lock
lift
turn
drop
impact
pin transition
submission transition
rope collision check
reversal open
reversal close
attacker release
defender bump
recovery
```

Without event markers, move creation becomes guesswork.

### Multi-actor synchronization is non-negotiable

Normal grapples require at least attacker animation, defender animation, relative positioning, root motion lock, contact frame, impact frame, and separation result.

Double-team moves may require attacker 1, attacker 2, defender, optional partner/receiver, legal-man metadata, and referee/camera handling.

### Do not allow unsafe move assignments

```json
{
  "compatibility": {
    "minWeightClass": "light",
    "maxWeightClass": "superheavy",
    "allowedStances": ["normal", "technical", "power"],
    "disallowedBodyTypes": [],
    "requiresRopes": false,
    "requiresCorner": false,
    "requiresOpponentStanding": true,
    "requiresAttackerStanding": true,
    "cawEligible": true,
    "aiUsable": true
  }
}
```

The editor should block or warn about wrong context, missing defender track, bad result state, incompatible slot, missing reversal data, missing impact frame, invalid pin/submission transition, and invalid weight/body pairing.

---

## CAW/edit mode best practices

### CAW is a full gameplay entity

```json
{
  "identity": {
    "name": "Example Wrestler",
    "shortName": "Example",
    "nickname": "The Example",
    "hometown": "Longmont, Colorado",
    "height": 72,
    "weight": 220,
    "alignment": "face"
  },
  "appearance": {},
  "entrance": {},
  "moveset": {},
  "fightingStyle": {},
  "parameters": {},
  "aiLogic": {},
  "relationships": {},
  "factions": [],
  "presentation": {},
  "metadata": {}
}
```

### Order of operations matters

Suggested CAW editor flow:

1. Body/weight class
2. Base style
3. Stance/movement
4. Parameters
5. AI/personality
6. Moveset
7. Appearance
8. Entrance
9. Relationships/faction
10. Validation/export

Reason:
- Style affects valid moves.
- Weight/body affects lift moves.
- AI affects move priorities.
- Moves should be filtered by the wrestler’s base logic.

### Fighting style should be modular

```json
{
  "fightingStyle": {
    "stance": "technical",
    "movement": "normal",
    "grappleStyle": "chain",
    "strikeStyle": "balanced",
    "submissionStyle": "technical",
    "reversalStyle": "light_heavy",
    "pinStyle": "standard",
    "ropeStyle": "springboard_capable",
    "irishWhipEvasion": "jump",
    "recoveryRate": "fast",
    "endurance": "normal",
    "bleeding": "often",
    "bloodReaction": "normal",
    "weaponPreference": "random",
    "jumpDistance": "long"
  }
}
```

### Move assignment should be slot-based

```json
{
  "moveset": {
    "standing": {},
    "frontWeakGrapple": {},
    "frontStrongGrapple": {},
    "rearWeakGrapple": {},
    "rearStrongGrapple": {},
    "groundHead": {},
    "groundLegs": {},
    "running": {},
    "rebound": {},
    "corner": {},
    "topRope": {},
    "apron": {},
    "doubleTeam": {},
    "specials": {},
    "taunts": {}
  }
}
```

### CAW sharing should be first-class

Support share codes, JSON export, full package export, thumbnails, missing asset warnings, dependency lists, and version/migration metadata.

```json
{
  "packageType": "caw",
  "schemaVersion": "1.0.0",
  "dependencies": {
    "movePacks": ["base_moves_v1"],
    "texturePacks": ["retro_attire_v1"],
    "entrancePacks": []
  },
  "compatibility": {
    "minGameVersion": "0.4.0"
  }
}
```

---

## Battle-system architecture lessons

### Use separate modules

```text
BattleEngine
├── MatchController
├── RulesetEngine
├── WrestlerStateMachine
├── PositionSystem
├── GrappleResolver
├── StrikeResolver
├── MoveResolver
├── ReversalResolver
├── DamageResolver
├── MomentumResolver
├── StaminaResolver
├── PinResolver
├── SubmissionResolver
├── RopeSystem
├── RefereeSystem
├── CrowdSystem
├── AIController
├── AnimationEventSystem
├── CAWSystem
├── MoveEditor
└── ModLoader
```

### Runtime chain

```text
Input
→ Intent
→ Context detection
→ Legal move-slot lookup
→ Move selected
→ Rule validation
→ Reversal/contest check
→ Move execution
→ Timeline events
→ Damage/state/momentum consequences
→ Pin/submission/rope/referee checks
→ Crowd/camera/audio response
→ Result state
```

### Data hierarchy

```text
GameDatabase
├── Moves
├── MoveTemplates
├── Reversals
├── DamageProfiles
├── Stances
├── FightingStyles
├── Wrestlers
├── CAWs
├── Entrances
├── Arenas
├── Rulesets
├── AIProfiles
├── Factions
├── Relationships
└── ModPackages
```

### Tags are the safest abstraction

Move tags:

```text
strike
grapple
front_grapple
rear_grapple
suplex
slam
driver
submission
pin_combo
dirty
weapon
rope
corner
apron
high_risk
springboard
technical
power
lucha
shoot
special
finisher
double_team
```

Wrestler tags:

```text
technician
powerhouse
high_flyer
brawler
showman
cheater
shoot_style
superheavy
lightweight
hardcore
resilient
glass_cannon
```

Ruleset tags:

```text
rope_breaks
no_rope_breaks
dq_enabled
dq_disabled
countout_enabled
falls_count_anywhere
submission_only
pin_only
weapons_legal
tag_rules
battle_royal
```

### Match rules should override systems, not duplicate them

```json
{
  "id": "no_rope_breaks",
  "overrides": {
    "ropeBreak": "detect_but_do_not_break",
    "commentaryTrigger": "rope_break_denied",
    "crowdReaction": "controversy"
  }
}
```

```json
{
  "id": "falls_count_anywhere",
  "overrides": {
    "pinLegalZones": ["ring", "ringside", "stage", "crowd"],
    "countout": false
  }
}
```

---

## AI / logic lessons

### Logic before moves

The VPW2 CAW guide’s warning to edit logic before moves is a major architectural clue.

Interpretation:
- A wrestler’s logic/style affects how the moveset is interpreted.
- Some styles unlock or require special move structures.
- Combo/shoot systems are not just visual differences.

Modern design:
- AI profile and fighting style should be authored before move selection.
- Move slots can be enabled/disabled based on style.
- The editor should surface invalid combinations.

### AI should use weighted context tables

```json
{
  "aiProfile": {
    "earlyMatch": {
      "weakGrapple": 35,
      "strike": 20,
      "taunt": 10,
      "irishWhip": 15,
      "restHold": 10,
      "highRisk": 0
    },
    "midMatch": {
      "mediumGrapple": 30,
      "cornerAttack": 15,
      "submission": 10,
      "runningAttack": 15,
      "pinAttempt": 5,
      "taunt": 5
    },
    "lateMatch": {
      "signature": 20,
      "finisher": 20,
      "pinAttempt": 20,
      "submission": 15,
      "desperationCounter": 10,
      "highRisk": 10
    }
  }
}
```

### AI behavior should include match psychology

Wrestling AI needs to decide when to slow down, taunt, pin, drag opponent away from ropes, use a finisher, attempt a risky dive, go outside, cheat, tag, conserve stamina, sell, or pursue story objectives.

---

## Direct lessons for a new game

### Build the move editor early

The move editor is not a bonus feature. It is the tool that proves whether your move architecture is actually modular.

Minimum move editor features:
- choose template
- assign attacker/defender animations
- define context requirements
- set contact/impact/recovery frames
- add damage profile
- add result states
- add reversal windows
- add pin/submission transitions
- validate CAW compatibility
- preview in sandbox ring
- export as move package

### Build the CAW editor around behavior

Appearance is not enough.

CAW must cover:
- look
- moves
- fighting style
- AI
- entrance
- relationships
- factions
- parameters
- share/export

### Use simple controls with deep context

Recommended controls:

```text
Strike
Grapple
Run
Block/Reversal
Taunt
Pick Up / Interact
Pin / Submission Context
Irish Whip / Rope Action
Special Modifier
```

Depth comes from weak vs strong grapple, front/rear/ground/corner/rope/apron contexts, momentum/special state, direction modifiers, and style-specific move slots.

### Keep the feel rules

The lasting appeal of the AKI-style games appears to come from:
- low input friction
- fast readability
- deep move variety
- strong context logic
- consistent control grammar
- satisfying grapples
- reversible momentum
- expressive CAWs
- multiplayer chaos
- wrestlers feeling different without needing complex combos

### Design for modders from day one

Provide:
- documented JSON schemas
- asset folder conventions
- package manifests
- mod dependency support
- official import/export tools
- validation CLI
- sandbox preview mode
- deterministic IDs
- migration scripts
- changelogged schema versions

---

## Recommended modern schemas

### Move schema

```json
{
  "id": "move.snap_suplex.01",
  "name": "Snap Suplex",
  "version": "1.0.0",
  "template": "front_grapple",
  "tags": ["grapple", "front_grapple", "suplex", "technical"],
  "context": {
    "attackerState": ["standing"],
    "defenderState": ["standing"],
    "position": ["front"],
    "ringZones": ["ring_center", "near_ropes"],
    "momentumRequired": 0
  },
  "animations": {
    "attacker": "anim.snap_suplex.attacker",
    "defender": "anim.snap_suplex.defender"
  },
  "timeline": {
    "frames": 82,
    "events": [
      { "frame": 12, "event": "grab_lock" },
      { "frame": 48, "event": "impact", "damageProfile": "back_medium" }
    ]
  },
  "reversals": [
    {
      "window": [6, 15],
      "type": "grapple_counter",
      "difficulty": 0.45
    }
  ],
  "results": {
    "attackerState": "standing",
    "defenderState": "grounded_face_up",
    "positionDelta": "small_forward"
  },
  "editor": {
    "cawEligible": true,
    "aiEligible": true,
    "slotCompatibility": ["frontWeakGrapple", "frontStrongGrapple"]
  }
}
```

### Wrestler / CAW schema

```json
{
  "id": "wrestler.example",
  "identity": {
    "name": "Example Wrestler",
    "shortName": "Example",
    "nickname": "The Example"
  },
  "body": {
    "height": 72,
    "weight": 220,
    "weightClass": "heavyweight",
    "rig": "standard_male"
  },
  "fightingStyle": {
    "stance": "technical",
    "grappleStyle": "chain",
    "strikeStyle": "balanced",
    "submissionStyle": "technical",
    "reversalStyle": "light_heavy",
    "pinStyle": "standard",
    "recoveryRate": "fast",
    "jumpDistance": "normal",
    "weaponPreference": "rare"
  },
  "parameters": {
    "strength": 70,
    "speed": 65,
    "technical": 85,
    "submission": 80,
    "stamina": 75,
    "durability": 70,
    "charisma": 60
  },
  "moveset": {
    "frontWeakGrapple": {},
    "frontStrongGrapple": {},
    "rearGrapple": {},
    "ground": {},
    "corner": {},
    "running": {},
    "topRope": {},
    "specials": {}
  },
  "aiProfile": "ai.technical_balanced",
  "relationships": {
    "allies": [],
    "enemies": []
  }
}
```

### Mod package schema

```json
{
  "id": "mod.example_pack",
  "name": "Example Wrestling Pack",
  "version": "1.0.0",
  "author": "Creator",
  "type": "total_conversion",
  "gameVersion": ">=0.5.0",
  "content": {
    "wrestlers": [],
    "moves": [],
    "arenas": [],
    "textures": [],
    "music": [],
    "rulesets": [],
    "story": []
  },
  "dependencies": [],
  "loadOrder": 100
}
```

---

## Practical build recommendations

### Minimum viable engine

Build in this order:

1. Wrestler state machine
2. Position/context detector
3. Move database
4. Slot-based moveset resolver
5. Weak/strong grapple
6. Move timeline events
7. Basic reversal windows
8. Damage and stamina
9. Momentum/special state
10. Pin and rope break
11. CAW data object
12. Basic CAW move assignment
13. Move preview sandbox

### First move classes to support

Start with:
- standing strike
- running strike
- front weak grapple
- front strong grapple
- rear grapple
- grounded head move
- grounded leg move
- turnbuckle front move
- top rope dive
- pin attempt
- submission hold
- taunt

Delay:
- double-team moves
- ladder/table moves
- complex weapons
- referee bumps
- multi-person targeting
- backstage brawls

### Editor validation checklist

Every move must pass:
- has valid template
- has required animation tracks
- has impact/contact event
- has result states
- has slot compatibility
- has reversal metadata or explicit no-reversal flag
- has damage profile or explicit no-damage flag
- does not reference missing assets
- does not require unavailable style/body type
- can be simulated in preview without dead states

Every CAW must pass:
- has valid body/rig
- has valid fighting style
- has no empty required move slots
- has no invalid move-slot assignments
- has valid AI profile
- has valid entrance references
- has valid relationship references
- package dependencies are satisfied

---

## Research conclusions

The major technical lesson from the AKI wrestling lineage is not “copy the exact controls.” The deeper lesson is:

**A great wrestling engine is a contextual move/state resolver backed by modular wrestler data, move data, style logic, and editable content.**

The lasting strength of these games comes from:
- simple controls
- deep contextual move tables
- strong position logic
- modular CAW/edit systems
- data-driven moves
- reusable animations
- style-aware movesets
- momentum/special states
- readable timing
- extensibility across licenses and promotions
- community-editable structure

The biggest lesson from modding is:

**Anything players love enough to hack should probably be a first-class editor feature in a modern game.**

That means:
- move creation
- moveset editing
- CAW formulas
- appearance parts
- entrance customization
- roster replacement
- arena packs
- faction/relationship editing
- logic/AI editing
- texture/style replacement
- ruleset customization

A modern implementation should not bury these in binary formats. It should make them official, documented, validated, versioned, and shareable.

---

## Source bibliography

1. Hideyuki “Geta” Iwashita translated blog, “Turning Pro Wrestling into a Game”
   https://melonbread.co.uk/turning-pro-wrestling-into-a-game-what-are-the-differences-between-other-sports-games-that-may-appear-similar-but-are-actually-different/

2. Hideyuki “Geta” Iwashita translated blog, “The Ecstasy and Anxiety of the Chosen One”
   https://melonbread.co.uk/first-night-the-ecstasy-and-anxiety-of-the-chosen-one/

3. VPW Studio GitHub
   https://github.com/AKI-Club/VPWStudio

4. VPW Studio website
   https://vpw.ajworld.net/vpwstudio/

5. AKI-Club GitHub organization
   https://github.com/AKI-Club

6. AKI-Club project index
   https://aki-club.github.io/

7. Virtual Pro-Wrestling 2 decompilation project
   https://github.com/aki-club/vpw2

8. No Mercy Library, “Master Move Mods”
   https://www.tapatalk.com/groups/no_mercy_library/master-move-mods-t47.html

9. Geocities No Mercy Hacking, “How to Hack Normal Moves”
   https://www.geocities.ws/no_mercy_hacking/learn_normal.html

10. WldFb Archive Forum
   https://www.tapatalk.com/groups/wldfbarchiveforum/

11. WWF No Mercy Move List and Guide, GameFAQs
   https://gamefaqs.gamespot.com/n64/914112-wwf-no-mercy/faqs/9549

12. WWF No Mercy Create-A-Wrestler Guide, GameFAQs
   https://gamefaqs.gamespot.com/n64/914112-wwf-no-mercy/faqs/9824

13. WWF No Mercy Create-A-Wrestler FAQ, GameFAQs
   https://gamefaqs.gamespot.com/n64/914112-wwf-no-mercy/faqs/9799

14. WWF No Mercy Create-A-Wrestler Guide, GameFAQs
   https://gamefaqs.gamespot.com/n64/914112-wwf-no-mercy/faqs/14257

15. WWF WrestleMania 2000 Move List and Guide, GameFAQs
   https://gamefaqs.gamespot.com/n64/199352-wwf-wrestlemania-2000/faqs/3678

16. WWF WrestleMania 2000 Create-A-Wrestler Guide, GameFAQs
   https://gamefaqs.gamespot.com/n64/199352-wwf-wrestlemania-2000/faqs/8279

17. Virtual Pro-Wrestling 2 Move List and Guide, GameFAQs
   https://gamefaqs.gamespot.com/n64/576850-virtual-pro-wrestling-2-oudou-keishou/faqs/6901

18. Virtual Pro-Wrestling 2 Create-A-Wrestler FAQ, GameFAQs
   https://gamefaqs.gamespot.com/n64/576850-virtual-pro-wrestling-2-oudou-keishou/faqs/9925

19. Virtual Pro-Wrestling 2 moveset location notes
   https://vpw.ajworld.net/vpw2/moveset_locations.txt

20. WWF No Mercy Wiki: CAW
   https://wwfnomercy.fandom.com/wiki/CAW

21. WWF No Mercy Wiki: Community
   https://wwfnomercy.fandom.com/wiki/Community
