# LoCo Fight Game — Prototype Match

A playable 3D arcade-sim wrestling prototype built from Unity primitives. One
human player vs one CPU opponent; win by pinfall or submission. All 16 original
LoCo Pro Wrestling characters are data-driven, each with a unique special and
passive traits. No external 3D art, animations, or audio required.

## Quick start

1. Open this folder (`fightgame/`) as a project in **Unity 6 (6000.x)** (3D
   template; Built-in render pipeline is assumed, URP also works for the
   placeholder materials).
2. Wait for the scripts to compile.
3. Run **Tools > LoCo Fight Game > Setup Everything (Assets + Scene)**.
   - This creates all ScriptableObject data under `Assets/Resources/LoCoData/`,
     imports the `tas-*` roster portraits into `Assets/Art/RosterPortraits/`,
     and creates `Assets/Scenes/PrototypeMatch.unity`.
4. Open `Assets/Scenes/PrototypeMatch.unity` and press **Play**.

Zero-setup alternative: create any empty scene, add an empty GameObject, attach
the `GameBootstrap` component, and press Play. The arena, wrestlers, HUD, and
camera are all built procedurally; roster data falls back to the in-code
defaults (portraits will show placeholder colors until the importer has run).

Default match: **Zeak Gallent (player) vs JT Staten (CPU)**. Press **F2** in
Play mode for the debug roster selector.

## Controls

Two context-sensitive core buttons carry the fight, AKI-style: one press = one
move, direction is the only modifier. Everything fires on press; the HUD's
bottom-center prompt always shows what each button will do, confirms what fired,
and explains dead presses.

| Action                                                                                                                                                                                                                 | Key                                                                       |
| ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------- |
| Move                                                                                                                                                                                                                   | W / A / S / D                                                             |
| Run                                                                                                                                                                                                                    | Left Shift                                                                |
| **Strike** — neutral: light · +held direction: heavy · contextual attack (ground, corner, rope, rebound, running) when one applies; always fires on press                                                              | J                                                                         |
| **Grapple / Control** — press with an optional direction: tap for a quick grapple, hold for a power grapple; the same press acquires and resolves the tie-up · beside a downed opponent: tap pin / hold submission     | K                                                                         |
| Directional grapple from lock                                                                                                                                                                                          | hold a movement direction (toward opponent on screen = forward) + press K |
| Special                                                                                                                                                                                                                | L                                                                         |
| Dodge / escape / kickout mash                                                                                                                                                                                          | ; (Left Alt also works)                                                   |
| Reversal / block / kickout mash                                                                                                                                                                                        | Space                                                                     |
| Taunt / handshake accept                                                                                                                                                                                               | T                                                                         |
| Roll away while downed                                                                                                                                                                                                 | A or D + Space                                                            |
| Reset match (after finish)                                                                                                                                                                                             | R                                                                         |
| Full controls panel (hold)                                                                                                                                                                                             | Tab (View on controller)                                                  |
| Debug overlay                                                                                                                                                                                                          | F1                                                                        |
| Debug roster select                                                                                                                                                                                                    | F2                                                                        |
| Debug CPU behavior (Full → NoOffense → Dummy)                                                                                                                                                                          | F3                                                                        |
| Pause                                                                                                                                                                                                                  | Escape                                                                    |

### Controller

Verified on 8BitDo SN30 Pro (Nintendo/SNES button layout). Button names
printed on the controller, not Xbox equivalents.

| Action                                               | Default legacy gamepad binding |
| ---------------------------------------------------- | ------------------------------ |
| Move                                                 | Left stick / D-pad             |
| Run                                                  | Y                              |
| Strike (neutral light / +direction heavy)            | B                              |
| Grapple / Control (tap quick / hold power)           | A                              |
| Special                                              | X                              |
| Dodge / escape / kickout mash                        | L (left bumper)                |
| Reversal / block                                     | R (right bumper)               |
| Pause / reset after finish                           | Start (+)                      |
| Full controls panel (hold)                           | Tab (keyboard only)            |

Controller bindings verified on 8BitDo SN30 Pro (Nintendo/SNES button layout).
The code uses Xbox/PlayStation slot numbering internally — the printed labels on
Nintendo-layout controllers (A=right, B=bottom, X=top, Y=left) differ from Xbox
(A=bottom, B=right, X=left, Y=top).

## Project layout

- `Assets/Scripts/` — gameplay code organized by system (Core, Input, Camera,
  Arena, Ring, Ropes, Wrestlers, Roster, Combat, Moves, Specials, Traits, AI,
  Match, Rules, UI, Animation, Utilities, Editor).
- `Assets/Resources/LoCoData/` — generated ScriptableObject data (moves,
  specials, traits, stats, roster, rules, AI difficulty). Regenerate any time
  with the builder menu.
- `Assets/Art/RosterPortraits/` — imported `tas-*.png` portraits.
- `Documentation/` — design docs ([DesignDoc.md](DesignDoc.md),
  [RopeMechanics.md](RopeMechanics.md),
  [SpecialAbilities.md](SpecialAbilities.md), [Roster.md](Roster.md),
  [FutureAssetIntegration.md](FutureAssetIntegration.md)) and the engineering
  [KnowledgeBase/](KnowledgeBase/README.md) (best practices, templates, worked
  examples, and clearly labeled informational research notes).
- `research/` — background source material used to identify possible best
  practices. It is not itself an implementation guide, specification, roadmap,
  or source of truth; accepted findings are promoted into authoritative project
  docs.

## What's intentionally ugly

Wrestlers are jointed primitive humanoids — pelvis/spine/neck plus shoulders,
elbows, hips, and knees as bendable pivots, with a yellow chest marker for
facing and torso/limb bulk scaled by weight class. Animation is procedural
posing driven through those joints (walk/run cycles, punches and kicks, a
collar-and-elbow lockup, downed/stunned/victory poses, color flashes) with no
animation clips. The architecture isolates all of this behind `WrestlerView` and
`IAnimationDriver` so real models and animations can replace it without touching
combat, AI, rules, or match logic — see
[FutureAssetIntegration.md](FutureAssetIntegration.md).
